#nullable enable
using System;
using System.IO;
using HobbyDS9.TacticalReviewClient;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public static class ReviewLaunch
    {
        public static void Launch(string[] args)
        {
            if(!ReviewLaunchOptions.TryParse(args,out var options))throw new InvalidOperationException("Explicit review launch options required.");
            if(UnityEngine.Object.FindAnyObjectByType<ReviewEntry>(FindObjectsInactive.Include)!=null)return;
            var go=new GameObject("HobbyDS9 Physical Review");go.SetActive(false);UnityEngine.Object.DontDestroyOnLoad(go);
            var entry=go.AddComponent<ReviewEntry>();entry.Configure(options!);go.SetActive(true);
        }
    }
    public static class IntegratedReviewLaunch
    {
        public static void Launch(string[] args)
        {
            if(!IntegratedLaunchOptions.TryParse(args,out var options))throw new InvalidOperationException("Explicit integrated launch options required.");
            if(UnityEngine.Object.FindAnyObjectByType<ReviewEntry>(FindObjectsInactive.Include)!=null)return;
            var go=new GameObject("HobbyDS9 Integrated Playtest");go.SetActive(false);UnityEngine.Object.DontDestroyOnLoad(go);
            var entry=go.AddComponent<ReviewEntry>();entry.ConfigureIntegrated(options!);go.SetActive(true);
        }
    }
    public sealed partial class ReviewEntry : MonoBehaviour
    {
        private ReviewLaunchOptions? options;
        private IntegratedLaunchOptions? integratedOptions;
        private SingleplayerDemoLaunchOptions? demoOptions;
        private ReviewUiModel? model;
        private ReviewCombatFlow? flow;
        public ReviewCombatFlow? Presentation=>flow;
        private ReviewWorkspace? workspace;
        public ReviewWorkspace? Workspace=>workspace;
        private PanelSettings? panel;
        private VisualElement? root,confirmation,content;
        private Label? message;
        private ReviewCentralView? central;
        private ReviewStrategyView? strategy;
        private ReviewController? centralContext;
        private string slotName="review-1",actorInput="";
        private long viewEpoch;
        private IDisposable? audioInput;
        private Action? buttonSound;
        private readonly ReviewModalPermission modalPermission=new ReviewModalPermission();
        public void Configure(ReviewLaunchOptions value){if(options==null)options=value;}
        public void ConfigureIntegrated(IntegratedLaunchOptions value){if(options==null&&integratedOptions==null)integratedOptions=value;}
        public void ConfigureDemo(SingleplayerDemoLaunchOptions value){if(options==null&&integratedOptions==null&&demoOptions==null)demoOptions=value;}
        private void OnEnable()
        {
            viewEpoch++;modalPermission.Invalidate();
            try
            {
                if(options==null&&integratedOptions==null&&demoOptions==null)throw new InvalidOperationException();
                if(model==null)
                {
                    byte[] Bytes(string name)
                    {
                        string path;
                        if(demoOptions!=null)path="DemoPackages/v1/"+name;
                        else if(integratedOptions!=null)path="IntegratedPackages/v1/"+name;
                        else{if(options==null)throw new InvalidOperationException();path="ReviewPackages/"+options.Profile+"/"+name;}
                        var asset=Resources.Load<TextAsset>(path);
                        if(asset==null)throw new InvalidOperationException();return asset.bytes;
                    }
                    var codec=new ReviewNewtonsoftCodec();
                    if(demoOptions!=null)
                    {
                        demoSource=ReviewExactPackageLoader.LoadDemo(Bytes("demo"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
                        model=new ReviewUiModel(demoSource,codec,new ReviewDemoSlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9SingleplayerDemoOnly-v4"),codec));
                        slotName="demo-1";demoLanding=true;
                    }
                    else if(integratedOptions!=null)
                    {
                        var package=ReviewExactPackageLoader.LoadIntegrated(Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
                        model=new ReviewUiModel(package,codec,new ReviewIntegratedSlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9IntegratedPlaytestOnly-v3"),codec),integratedOptions.Actor);
                        slotName="campaign-1";
                    }
                    else
                    {
                        var package=ReviewExactPackageLoader.Load(Bytes("package"),Bytes("core"),Bytes("scenario"),codec);
                        model=new ReviewUiModel(package,codec,new ReviewUnitySlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9PhysicalReviewOnly-v2"),codec),options!.Actor);
                    }
                    flow=new ReviewCombatFlow(model);workspace=new ReviewWorkspace(flow);actorInput=model.Current.Frame.Actor;
                }
                Build();
            }
            catch(Exception)
            {
                modalPermission.Invalidate();root?.UnregisterCallback<GeometryChangedEvent>(Geometry);ReleasePanel();
                var doc=GetComponent<UIDocument>()??gameObject.AddComponent<UIDocument>();panel=ScriptableObject.CreateInstance<PanelSettings>();doc.panelSettings=panel;
                root=doc.rootVisualElement;root.Clear();root.Add(new Label("OPT-IN PHYSICAL TACTICAL V2 — unavailable. Check profile/resources/preparation; default campaign was not started."));
            }
        }
        private void OnDisable(){viewEpoch++;modalPermission.Invalidate();root?.UnregisterCallback<GeometryChangedEvent>(Geometry);UnbindDemoKeyboard();ResetDemoKeyboard();confirmation=null;ReleaseAudio();}
        private void OnDestroy(){ResetDemoKeyboard();ReleasePanel();}
        private void ReleasePanel()
        {
            ReleaseAudio();
            if(panel==null)return;
            if(UnityEngine.Application.isPlaying)Destroy(panel);else DestroyImmediate(panel);
            panel=null;
        }
        private void ReleaseAudio(){audioInput?.Dispose();audioInput=null;buttonSound=null;}
        private void BindAudio()
        {
            try
            {
                var bridge=Type.GetType("HobbyDS9.UnityClient.AudioPresentationBridge, HobbyDS9.UnityClient",false);
                if(bridge==null)throw new InvalidOperationException();
                audioInput=bridge.GetMethod("Bind")!.Invoke(null,new object[]{root!}) as IDisposable;
                buttonSound=(Action)Delegate.CreateDelegate(typeof(Action),bridge.GetMethod("ButtonPress")!);
            }
            catch(Exception){ReleaseAudio();Debug.LogWarning("Review audio unavailable; playtest continues.");}
        }
        private void Build()
        {
            modalPermission.Invalidate();root?.UnregisterCallback<GeometryChangedEvent>(Geometry);UnbindDemoKeyboard();ReleaseAudio();
            var doc=GetComponent<UIDocument>()??gameObject.AddComponent<UIDocument>();
            if(!PlayerProfile||panel==null||doc.panelSettings!=panel)
            {
                ReleasePanel();panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=model!.Integrated?PanelScaleMode.ConstantPixelSize:PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1920,1080);
                panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=1;panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("HobbyTheme");doc.panelSettings=panel;
            }
            root=doc.rootVisualElement;root.Clear();root.styleSheets.Clear();confirmation=null;root.AddToClassList("physical-review-root");
            PreserveDemoKeyOrigins();
            BindDemoKeyboard();
            BindAudio();
            root.style.flexGrow=1;root.style.backgroundColor=new Color(0.03f,0.05f,0.08f);root.style.color=new Color(0.92f,0.95f,1);root.style.fontSize=16;
            foreach(var resource in new[]{"HobbyClient","LcarsTokens","Lcars","ShipDesigner","ReviewControls","ReviewCombat","PlayerConsole","GeneratedGalaxy"})
            {var sheet=Resources.Load<StyleSheet>(resource);if(sheet!=null)root.styleSheets.Add(sheet);}
            if(generatedGalaxyOpen){BuildGeneratedGalaxy();root.RegisterCallback<GeometryChangedEvent>(Geometry);return;}
            if(model!.Integrated){workspace!.Compact=Screen.width<1600||Screen.height<850;root.EnableInClassList("player-narrow",workspace.Compact);BuildPlayerShell();root.RegisterCallback<GeometryChangedEvent>(Geometry);Draw();return;}
            var identity=Readout((model!.Integrated?"OPT-IN INTEGRATED PLAYTEST | SCENARIO: integrated-playtest-v1":"OPT-IN PHYSICAL TACTICAL V2 | SCENARIO: "+options!.Profile)+" | ACTOR VIEW: "+model.Current.Frame.Actor+" | Proposed policies; default campaign activation disabled");
            identity.name="review-scenario-identity";identity.AddToClassList("review-heading");root.Add(identity);
            root.Add(Readout(workspace!.BoundaryIdentity));
            message=Readout(model.Message);message.name="review-command-status";root.Add(message);
            var tools=new VisualElement();tools.AddToClassList("review-toolbar");root.Add(tools);
            AddButton(tools,"Refresh authority",()=>Do(()=>model.Refresh()));
            AddButton(tools,"Reset / discard local draft",()=>Confirm("Discard unsent edits? This sends no command.",()=>Do(()=>model.Reset())));
            var slot=Field("Separate review slot",slotName);slot.RegisterValueChangedCallback(e=>slotName=e.newValue);tools.Add(slot);
            AddButton(tools,"Save committed / keep draft",()=>Do(()=>{
                if(!model.Save(slotName)&&model.Message=="Checkpoint result: SlotOccupied")Confirm("Overwrite explicitly named review slot? Unsent edits stay local and are not saved.",()=>Do(()=>model.Save(slotName,true)));
            }));
            AddButton(tools,"Load / resume",()=>Confirm("Load a validated complete checkpoint? Successful load discards local edits; failed load preserves them.",()=>Do(()=>model.Load(slotName,true))));
            var testHost=new Foldout{text="Trusted human test-host controls — not opponent AI or account security",value=false};root.Add(testHost);
            testHost.Add(Readout("This authored review uses human actors civ.federation and civ.cardassian. Deliberately switch view to supply that human's choices. Do not infer opponent readiness from the host controls."));
            var actor=Field("Explicit actor ID",actorInput);actor.RegisterValueChangedCallback(e=>actorInput=e.newValue);testHost.Add(actor);
            AddButton(testHost,"Switch human actor view",()=>{
                if(model.HasLocalEdits)Confirm("Switch actor and discard unsent edits?",()=>Do(()=>model.SwitchActor(actorInput,true)));
                else Do(()=>model.SwitchActor(actorInput));
            });
            AddButton(testHost,"Exit review",()=>Confirm("Exit without automatic save? Unsent edits will be lost.",()=>Do(()=>model.Close(true))));
            var navigation=new VisualElement{name="review-workspace-navigation"};navigation.AddToClassList("review-toolbar");root.Add(navigation);
            foreach(var page in new[]{ReviewWorkspacePage.Map,ReviewWorkspacePage.Research,ReviewWorkspacePage.Production,ReviewWorkspacePage.FleetOrders,ReviewWorkspacePage.Combat})
            {
                var nav=AddButton(navigation,PageLabel(page),()=>Navigate(page));nav.name="review-nav-"+page;
                nav.SetEnabled(workspace.CanNavigate);nav.EnableInClassList("review-selected-page",workspace.Page==page);
            }
            content=new VisualElement{name="review-central-content"};content.style.flexGrow=1;content.style.minHeight=0;root.Add(content);
            if(central==null||!ReferenceEquals(centralContext,model.Current))
            {
                central=new ReviewCentralView(content,flow!,(parent,title,action)=>AddButton(parent,title,action),Do,Confirm);centralContext=model.Current;
            }
            else central.Bind(content);
            strategy=new ReviewStrategyView(model,workspace,(parent,title,action)=>AddButton(parent,title,action),Do,Confirm,Navigate,(fleet,node,ship)=>{central.SelectStrategicFleet(fleet,node,ship);Navigate(ReviewWorkspacePage.FleetOrders);});
            root.RegisterCallback<GeometryChangedEvent>(Geometry);Draw();
        }
        private void Navigate(ReviewWorkspacePage page)
        {
            if(workspace==null||confirmation!=null||!workspace.Navigate(page))return;
            viewEpoch++;Build(); // Old detached controls/modal permissions cannot act on another panel.
        }
        private static string PageLabel(ReviewWorkspacePage page)=>page==ReviewWorkspacePage.Research?"R&D":page==ReviewWorkspacePage.FleetOrders?"Fleet orders":page.ToString();
        private void Geometry(GeometryChangedEvent e)
        {
            if(root==null||panel==null)return;
            if(generatedGalaxyOpen){GalaxyResponsive(e.newRect.width);return;}
            if(model?.Integrated==true){var playerCompact=e.newRect.width<1600||e.newRect.height<850;root.EnableInClassList("player-narrow",playerCompact);if(workspace!.Compact!=playerCompact){workspace.Compact=playerCompact;Draw();}return;}
            var compact=e.newRect.width<1700||e.newRect.height<850;
            root.EnableInClassList("review-compact",compact);
            var scale=Mathf.Max(1,1080f*0.9f/Mathf.Max(1,Screen.height));if(!Mathf.Approximately(panel.scale,scale))panel.scale=scale;
        }
        private void Draw()
        {
            if(model==null||root==null)return;
            if(generatedGalaxyOpen){DrawGeneratedGalaxyStatus();PreserveDemoKeyOrigins();return;}
            if(model.Integrated){try{DrawPlayer();}finally{PreserveDemoKeyOrigins();}return;}
            message!.text=model.Message+" "+flow!.Notice;
            if(workspace!.Page==ReviewWorkspacePage.Combat||workspace.Page==ReviewWorkspacePage.FleetOrders)central?.Render();
            else DrawStrategicPanel();
        }
        private void DrawStrategicPanel()
        {
            if(model!.Integrated){strategy!.Render(content!,workspace!.Page);return;}
            content!.Clear();var page=workspace!.Page;var frame=model!.Current.Frame;
            var heading=Readout(PageLabel(page));heading.name="review-workspace-heading";heading.AddToClassList("review-heading");content.Add(heading);
            var details=new ScrollView(ScrollViewMode.Vertical){name="review-strategic-details"};details.style.flexGrow=1;details.style.minHeight=0;content.Add(details);
            if(page==ReviewWorkspacePage.Map)
            {
                details.Add(Readout("Owned fleets and lawful last-seen contacts — known-map topology binding pending."));
                foreach(var fleet in frame.OwnStrategicFleets)details.Add(Readout("Your fleet "+fleet.FleetId+" | sector "+fleet.LocationNodeId+" | owned ships "+fleet.ShipIds.Count));
                foreach(var contact in frame.Travel?.KnownContacts??Array.Empty<HobbyDS9.Contracts.CampaignKnownContact>())
                    details.Add(Readout("Last seen "+contact.FleetId+" | "+contact.LastSeenNodeId+" | turn "+contact.LastSeenTurn+" | fresh "+contact.Fresh+". Current enemy route and destination unavailable."));
                if(frame.Status.Phase!=HobbyDS9.Contracts.SessionPhase.Planning)
                    AddButton(details,"Resume pending combat",()=>Navigate(ReviewWorkspacePage.Combat));
            }
            else details.Add(Readout(page==ReviewWorkspacePage.Research?"Research display unavailable for this scenario until its player-scoped campaign projection is connected.":"Ship construction unavailable for this scenario: supported authored-v2 production and equipment provisioning are required. No resources or progress are simulated by this panel."));
            details.Add(Readout("Panel navigation preserves this campaign and unsent orders. Use Fleet orders for current travel choices. Integration is in progress; R&D/production acceptance is not complete."));
        }
        private static Label Readout(string value){var l=new Label(value);l.style.whiteSpace=WhiteSpace.Normal;return l;}
        private static TextField Field(string title,string value){var f=new TextField(title){value=value};f.AddToClassList("review-input");return f;}
        private Button AddButton(VisualElement parent,string title,Action action,bool dialog=false)
        {
            var button=new Button{text=title};
            BindButton(button,action,dialog);
            button.AddToClassList("review-action");button.style.minHeight=36;parent.Add(button);return button;
        }
        private void BindButton(Button button,Action action,bool dialog=false)
        {
            var epoch=viewEpoch;Action guarded=()=>{if(epoch==viewEpoch&&isActiveAndEnabled&&!model!.Busy&&!model.Closed&&(dialog?modalPermission.IsOpen:confirmation==null)){buttonSound?.Invoke();try{action();}finally{PreserveDemoKeyOrigins();}}};
            button.clicked+=guarded;
            if(PlayerProfile)BindDemoButtonKeyboard(button,guarded);
        }
        private void Do(Action action)
        {
            if(model==null||model.Busy||model.Closed)return;
            var before=model.Current.Frame;action();
            if(!ReferenceEquals(before,model.Current.Frame)){viewEpoch++;Build();}else Draw();
        }
        private void Confirm(string title,Action action)
        {
            if(confirmation!=null)return;
            var callbacks=modalPermission.TryOpen(()=>{ClearConfirmation();action();},()=>ClearConfirmation());if(callbacks==null)return;
            confirmation=new VisualElement{name="review-one-use-confirmation"};confirmation.AddToClassList("review-confirmation");
            foreach(var child in root!.Children())child.SetEnabled(false);
            if(model!.Integrated)
            {
                confirmation.style.position=Position.Absolute;confirmation.style.left=0;confirmation.style.top=0;confirmation.style.right=0;confirmation.style.bottom=0;
                confirmation.style.marginLeft=0;confirmation.style.marginRight=0;confirmation.style.marginTop=0;confirmation.style.marginBottom=0;
                confirmation.style.alignItems=Align.Center;confirmation.style.justifyContent=Justify.Center;confirmation.style.backgroundColor=new Color(.02f,.03f,.06f,.96f);
                var box=new VisualElement{name="player-confirmation-summary"};box.AddToClassList("console-panel");box.style.width=620;box.style.maxWidth=new Length(90,LengthUnit.Percent);box.style.maxHeight=new Length(80,LengthUnit.Percent);
                var text=Readout(title);text.style.fontSize=22;
                if(PlayerProfile)
                {
                    box.style.height=new Length(70,LengthUnit.Percent);
                    var body=new ScrollView(ScrollViewMode.Vertical){name="player-confirmation-details"};body.style.flexGrow=1;body.style.minHeight=0;body.Add(text);box.Add(body);
                }
                else box.Add(text);
                var actions=new VisualElement();actions.AddToClassList("review-field-row");actions.style.flexShrink=0;box.Add(actions);
                AddButton(actions,"Cancel",callbacks.Cancel,true);var yes=AddButton(actions,"Confirm",callbacks.Confirm,true);yes.AddToClassList("primary-action");
                confirmation.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape){callbacks.Cancel();e.StopPropagation();}});
                confirmation.Add(box);root.Add(confirmation);yes.Focus();return;
            }
            var scroll=new ScrollView();scroll.style.flexGrow=1;scroll.style.minHeight=0;
            scroll.Add(Readout(title));confirmation.Add(scroll);
            AddButton(confirmation,"Cancel",callbacks.Cancel,true);var button=AddButton(confirmation,"Confirm",callbacks.Confirm,true);
            root.Add(confirmation);button.Focus();
        }
        private void ClearConfirmation()
        {modalPermission.Invalidate();confirmation?.RemoveFromHierarchy();confirmation=null;foreach(var child in root!.Children())child.SetEnabled(true);Draw();if(model!.Integrated)root.Q<Button>("player-primary-action")?.Focus();}
    }
}
