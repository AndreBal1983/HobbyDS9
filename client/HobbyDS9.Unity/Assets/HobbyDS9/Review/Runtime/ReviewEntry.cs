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
    public sealed class ReviewEntry : MonoBehaviour
    {
        private ReviewLaunchOptions? options;
        private ReviewUiModel? model;
        private ReviewCombatFlow? flow;
        public ReviewCombatFlow? Presentation=>flow;
        private PanelSettings? panel;
        private VisualElement? root,confirmation,content;
        private Label? message;
        private ReviewCentralView? central;
        private string slotName="review-1",actorInput="";
        private long viewEpoch;
        private readonly ReviewModalPermission modalPermission=new ReviewModalPermission();
        public void Configure(ReviewLaunchOptions value){if(options==null)options=value;}
        private void OnEnable()
        {
            viewEpoch++;modalPermission.Invalidate();
            try
            {
                if(options==null)throw new InvalidOperationException();
                if(model==null)
                {
                    byte[] Bytes(string name)
                    {
                        var asset=Resources.Load<TextAsset>("ReviewPackages/"+options.Profile+"/"+name);
                        if(asset==null)throw new InvalidOperationException();return asset.bytes;
                    }
                    var codec=new ReviewNewtonsoftCodec();
                    var package=ReviewExactPackageLoader.Load(Bytes("package"),Bytes("core"),Bytes("scenario"),codec);
                    model=new ReviewUiModel(package,codec,new ReviewUnitySlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9PhysicalReviewOnly-v2"),codec),options.Actor);
                    flow=new ReviewCombatFlow(model);actorInput=options.Actor;
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
        private void OnDisable(){viewEpoch++;modalPermission.Invalidate();root?.UnregisterCallback<GeometryChangedEvent>(Geometry);confirmation=null;}
        private void OnDestroy(){ReleasePanel();}
        private void ReleasePanel()
        {
            if(panel==null)return;
            if(UnityEngine.Application.isPlaying)Destroy(panel);else DestroyImmediate(panel);
            panel=null;
        }
        private void Build()
        {
            modalPermission.Invalidate();root?.UnregisterCallback<GeometryChangedEvent>(Geometry);ReleasePanel();
            panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1920,1080);
            panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=1;panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("HobbyTheme");
            var doc=GetComponent<UIDocument>()??gameObject.AddComponent<UIDocument>();doc.panelSettings=panel;
            root=doc.rootVisualElement;root.Clear();confirmation=null;root.AddToClassList("physical-review-root");
            root.style.flexGrow=1;root.style.backgroundColor=new Color(0.03f,0.05f,0.08f);root.style.color=new Color(0.92f,0.95f,1);root.style.fontSize=16;
            foreach(var resource in new[]{"HobbyClient","LcarsTokens","Lcars","ReviewControls","ReviewCombat"})
            {var sheet=Resources.Load<StyleSheet>(resource);if(sheet!=null)root.styleSheets.Add(sheet);}
            var identity=Readout("OPT-IN PHYSICAL TACTICAL V2 | SCENARIO: "+options!.Profile+" | ACTOR VIEW: "+model!.Current.Frame.Actor+" | Proposed policies; production activation disabled");
            identity.name="review-scenario-identity";identity.AddToClassList("review-heading");root.Add(identity);
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
                if(model.Current.Dirty)Confirm("Switch actor and discard unsent edits?",()=>Do(()=>model.SwitchActor(actorInput,true)));
                else Do(()=>model.SwitchActor(actorInput));
            });
            AddButton(testHost,"Exit review",()=>Confirm("Exit without automatic save? Unsent edits will be lost.",()=>Do(()=>model.Close(true))));
            content=new VisualElement{name="review-central-content"};content.style.flexGrow=1;content.style.minHeight=0;root.Add(content);
            central=new ReviewCentralView(content,flow!,(parent,title,action)=>AddButton(parent,title,action),Do,Confirm);
            root.RegisterCallback<GeometryChangedEvent>(Geometry);Draw();
        }
        private void Geometry(GeometryChangedEvent e)
        {
            if(root==null||panel==null)return;
            var compact=e.newRect.width<1700||e.newRect.height<850;
            root.EnableInClassList("review-compact",compact);
            var scale=Mathf.Max(1,1080f*0.9f/Mathf.Max(1,Screen.height));if(!Mathf.Approximately(panel.scale,scale))panel.scale=scale;
        }
        private void Draw()
        {
            if(model==null||root==null)return;
            message!.text=model.Message+" "+flow!.Notice;central?.Render();
        }
        private static Label Readout(string value){var l=new Label(value);l.style.whiteSpace=WhiteSpace.Normal;return l;}
        private static TextField Field(string title,string value){var f=new TextField(title){value=value};f.AddToClassList("review-input");return f;}
        private Button AddButton(VisualElement parent,string title,Action action,bool dialog=false)
        {
            var epoch=viewEpoch;var button=new Button(()=>{if(epoch==viewEpoch&&isActiveAndEnabled&&!model!.Busy&&!model.Closed&&(dialog?modalPermission.IsOpen:confirmation==null))action();}){text=title};
            button.AddToClassList("review-action");button.style.minHeight=36;parent.Add(button);return button;
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
            confirmation.Add(Readout(title));var scroll=new ScrollView();scroll.style.flexGrow=1;scroll.style.minHeight=0;
            scroll.Add(Readout(flow!.OrderReview()));confirmation.Add(scroll);
            AddButton(confirmation,"Cancel",callbacks.Cancel,true);var button=AddButton(confirmation,"Confirm",callbacks.Confirm,true);
            root.Add(confirmation);button.Focus();
        }
        private void ClearConfirmation()
        {modalPermission.Invalidate();confirmation?.RemoveFromHierarchy();confirmation=null;foreach(var child in root!.Children())child.SetEnabled(true);Draw();}
    }
}
