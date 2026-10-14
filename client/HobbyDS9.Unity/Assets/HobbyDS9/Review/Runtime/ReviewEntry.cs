#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Invoked ONLY by the existing ClientEntry's explicit selector. No second runtime bootstrap.
    public static class ReviewLaunch
    {
        public static void Launch(string[] args)
        {
            if(!ReviewLaunchOptions.TryParse(args,out var options)) throw new InvalidOperationException("Explicit review launch options required.");
            if(UnityEngine.Object.FindAnyObjectByType<ReviewEntry>(FindObjectsInactive.Include)!=null)return;
            var go=new GameObject("HobbyDS9 Physical Review");go.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(go);
            var entry=go.AddComponent<ReviewEntry>();entry.Configure(options!);go.SetActive(true);
        }
    }
    public sealed class ReviewEntry : MonoBehaviour
    {
        private ReviewLaunchOptions? options;
        private ReviewUiModel? model;
        private PanelSettings? panel;
        private VisualElement? root,body,controls,confirmation;
        private Label? banner,message,facts,report,inspection;
        private TextField? actor,destination,slot,command;
        private DropdownField? fleet,contact,maneuver,focus,ship,travelIntent,encounter;
        private Button? advance,round,finalize,sendTravel;
        private ScrollView? controlScroll;
        private long viewEpoch;
        private readonly ReviewModalPermission modalPermission=new ReviewModalPermission();
        public void Configure(ReviewLaunchOptions value){if(options==null)options=value;}
        private void OnEnable()
        {
            viewEpoch++;
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
                    var store=new ReviewUnitySlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9PhysicalReviewOnly-v2"),codec);
                    model=new ReviewUiModel(package,codec,store,options.Actor);
                }
                Build();
            }
            catch(Exception)
            {
                root?.UnregisterCallback<GeometryChangedEvent>(Geometry);
                if(panel!=null)Destroy(panel);
                var doc=GetComponent<UIDocument>()??gameObject.AddComponent<UIDocument>();
                panel=ScriptableObject.CreateInstance<PanelSettings>();doc.panelSettings=panel;
                root=doc.rootVisualElement;root.Clear();
                root.Add(new Label("PHYSICAL REVIEW / PROPOSED / DEFAULT ACTIVATION DISABLED\nReview content or binding unavailable. Preparation and Editor/native verification are still required."));
            }
        }
        private void OnDisable(){viewEpoch++;modalPermission.Invalidate();root?.UnregisterCallback<GeometryChangedEvent>(Geometry);confirmation=null;}
        private void OnDestroy(){if(panel!=null)Destroy(panel);}
        private void Build()
        {
            modalPermission.Invalidate();
            root?.UnregisterCallback<GeometryChangedEvent>(Geometry);
            if(panel!=null)Destroy(panel);
            panel=ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1920,1080);
            panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=1;
            panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("HobbyTheme");
            var document=GetComponent<UIDocument>()??gameObject.AddComponent<UIDocument>();document.panelSettings=panel;
            root=document.rootVisualElement;root.Clear();confirmation=null;
            root.style.flexGrow=1;root.style.backgroundColor=new Color(0.025f,0.025f,0.035f);root.style.color=new Color(1f,0.72f,0.4f);
            root.style.fontSize=ReviewLayoutPolicy.MinimumReadableFont;
            foreach(var resource in new[]{"HobbyClient","LcarsTokens","Lcars"})
            {var sheet=Resources.Load<StyleSheet>(resource);if(sheet!=null)root.styleSheets.Add(sheet);}
            banner=new Label();banner.style.whiteSpace=WhiteSpace.Normal;banner.style.unityFontStyleAndWeight=FontStyle.Bold;root.Add(banner);
            message=new Label();message.style.whiteSpace=WhiteSpace.Normal;root.Add(message);
            var toolbar=Row();root.Add(toolbar);
            actor=Field("Actor ID",model!.Current.Frame.Actor);toolbar.Add(actor);
            AddButton(toolbar,"Switch actor view",()=>Replace("Switch actor and discard local edits?",()=>model!.SwitchActor(actor.value,true),model!.Current.Dirty));
            AddButton(toolbar,"Refresh",()=>Do(()=>model!.Refresh()));AddButton(toolbar,"Reset local edits",()=>Replace("Discard all unsent local edits?",()=>model!.Reset(),model!.Current.Dirty));
            AddButton(toolbar,"Exit review",()=>Replace("Exit review? Unsent edits will be discarded; no automatic save.",()=>model!.Close(true),true));
            slot=Field("Review slot","review-1");toolbar.Add(slot);
            AddButton(toolbar,"Save committed",()=>{
                var selected=slot.value;
                Do(()=>{
                    if(!model!.Save(selected,false)&&model.Message=="Checkpoint result: SlotOccupied")
                        Confirm("Overwrite this explicitly named review slot?",()=>Do(()=>model.Save(selected,true)));
                });
            });
            AddButton(toolbar,"Load checkpoint",()=>Replace("Load replaces the review host and discards unsent edits. Continue?",()=>model!.Load(slot.value,true),true));
            body=new VisualElement();body.style.flexGrow=1;body.style.minHeight=0;root.Add(body);
            controlScroll=new ScrollView(ScrollViewMode.Vertical);controlScroll.style.flexGrow=1;controlScroll.style.flexShrink=1;controlScroll.style.minWidth=0;controlScroll.style.minHeight=0;controlScroll.style.maxWidth=640;
            controls=new VisualElement();controls.style.flexWrap=Wrap.Wrap;controlScroll.Add(controls);body.Add(controlScroll);
            fleet=Choice("Owned fleet",model.Current.Frame.OwnStrategicFleets.Select(f=>f.FleetId.Value).ToList());controls.Add(fleet);
            destination=Field("Explicit known destination","nav.bajor");controls.Add(destination);
            contact=Choice("Known contact / no hidden destination",new List<string>{"none"});controls.Add(contact);
            travelIntent=Choice("Travel intent",new List<string>{"Stay","StayEngage","Leave","Follow"});controls.Add(travelIntent);
            AddButton(controls,"Draft travel",()=>Do(()=>{
                var intent=(CampaignTravelIntent)Enum.Parse(typeof(CampaignTravelIntent),travelIntent.value);
                model!.DraftTravel(fleet.value,intent,intent==CampaignTravelIntent.Leave||intent==CampaignTravelIntent.Follow?destination.value:null,contact.value=="none"?null:contact.value);
            }));
            command=Field("Explicit command ID",ReviewUiModel.NewCommandId());controls.Add(command);
            sendTravel=AddButton(controls,"Commit travel draft",()=>Submit(()=>model!.SendTravel(command.value)));
            AddButton(controls,"Set approach cloak",()=>Submit(()=>model!.SetCloak(fleet.value,true,command.value)));
            AddButton(controls,"Clear approach cloak",()=>Submit(()=>model!.SetCloak(fleet.value,false,command.value)));
            advance=AddButton(controls,"Advance strategic turn",()=>Replace("Advance once with authoritative committed travel orders?",()=>model!.Advance(),true));
            encounter=Choice("Owned encounter",new List<string>{"none"});controls.Add(encounter);
            AddButton(controls,"Select encounter",()=>Replace("Discard local edits and select encounter?",()=>model!.SelectEncounter(encounter.value,true),model!.Current.Dirty));
            maneuver=Choice("Legal fleet maneuver",new List<string>{"Hold"});controls.Add(maneuver);
            focus=Choice("Enemy-fleet preference",new List<string>{"retain standing focus","clear focus"});controls.Add(focus);
            AddButton(controls,"Draft fleet order",()=>Do(()=>model!.DraftFleet(fleet.value,(CampaignFleetManeuver)Enum.Parse(typeof(CampaignFleetManeuver),maneuver.value),focus.value=="retain standing focus"||focus.value=="clear focus"?null:focus.value,focus.value=="retain standing focus")));
            round=AddButton(controls,"Commit one human round",()=>Replace("Commit the shown human orders for exactly one round?",()=>model!.CommitRound(command.value),true));
            finalize=AddButton(controls,"Finalize ready batch",()=>Replace("Apply ready batch aftermath once?",()=>model!.FinalizeBatch(),true));
            ship=Choice("Owned or visible ship inspection",new List<string>{"none"});controls.Add(ship);
            AddButton(controls,"Inspect selected ship",()=>Do(()=>model!.Inspect(ship.value)));
            var dataScroll=new ScrollView(ScrollViewMode.Vertical);dataScroll.style.flexGrow=1;dataScroll.style.minWidth=0;dataScroll.style.minHeight=0;body.Add(dataScroll);
            facts=Readout();inspection=Readout();report=Readout();dataScroll.Add(facts);dataScroll.Add(inspection);dataScroll.Add(report);
            root.RegisterCallback<GeometryChangedEvent>(Geometry);
            Draw();
        }
        private void Geometry(GeometryChangedEvent e)
        {
            if(body==null)return;
            var stacked=ReviewLayoutPolicy.StackPanels(e.newRect.width);
            body.style.flexDirection=stacked?FlexDirection.Column:FlexDirection.Row;
            controlScroll!.style.maxWidth=stacked?new StyleLength(StyleKeyword.None):new StyleLength(640);
            var scale=Mathf.Max(1,1080f*0.9f/Mathf.Max(1,Screen.height));
            if(!Mathf.Approximately(panel!.scale,scale))panel.scale=scale;
        }
        private void Draw()
        {
            if(model==null||banner==null)return;
            banner.text="PHYSICAL REVIEW / PROPOSED POLICIES / ACTIVATION DISABLED | ACTOR VIEW: "+model.Current.Frame.Actor+" | Trusted hotseat host can deliberately select each human view.";
            message!.text=model.Message;
            facts!.text=model.Facts();inspection!.text=model.Inspection();report!.text=model.Report();
            Set(fleet!,model.Current.Frame.OwnStrategicFleets.Select(f=>f.FleetId.Value).ToList());
            Set(contact!,new[]{"none"}.Concat(model.Current.Frame.Travel?.KnownContacts.Select(k=>k.FleetId)??Array.Empty<string>()).ToList());
            Set(encounter!,model.Current.Frame.Encounters.Count==0?new List<string>{"none"}:model.Current.Frame.Encounters.ToList());
            Set(maneuver!,model.Current.Frame.Battle?.LegalManeuvers.Select(m=>m.ToString()).ToList()??new List<string>{"Hold"});
            Set(focus!,new[]{"retain standing focus","clear focus"}.Concat(model.Current.Frame.Battle?.LegalFocusFleetIds??Array.Empty<string>()).ToList());
            Set(ship!,model.Current.Frame.OwnShips.Select(s=>s.Condition.ShipId).Concat(model.Current.Frame.Battle?.VisibleEnemies.Select(s=>s.ShipId)??Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToList());
            advance!.SetEnabled(!model.Busy&&!model.Closed&&model.Current.Mode==ReviewMode.Planning&&!model.Current.Dirty);
            sendTravel!.SetEnabled(!model.Busy&&!model.Closed&&model.Current.TravelDraft!=null);
            round!.SetEnabled(!model.Busy&&!model.Closed&&model.Current.Mode==ReviewMode.Orders&&!model.Current.RefreshRequired);
            finalize!.SetEnabled(!model.Busy&&!model.Closed&&model.Current.Mode==ReviewMode.Complete&&model.Current.Frame.Status.CanFinalizeBatch);
            if(model.Closed){controls!.SetEnabled(false);root!.Add(new Label("Review closed; restart without review flag for default v1."));}
        }
        private static Label Readout(){var l=new Label();l.style.whiteSpace=WhiteSpace.Normal;l.style.flexShrink=0;l.style.marginTop=12;return l;}
        private static VisualElement Row(){var r=new VisualElement();r.style.flexDirection=FlexDirection.Row;r.style.flexWrap=Wrap.Wrap;r.style.flexShrink=0;return r;}
        private static TextField Field(string label,string value){var f=new TextField(label){value=value};f.style.minWidth=ReviewLayoutPolicy.MinimumControlWidth;return f;}
        private static DropdownField Choice(string label,List<string> values){var f=new DropdownField(label,values,0);f.style.minWidth=ReviewLayoutPolicy.MinimumControlWidth;return f;}
        private static void Set(DropdownField field,List<string> values){if(values.Count==0)values.Add("none");var current=field.value;field.choices=values;field.SetValueWithoutNotify(values.Contains(current)?current:values[0]);}
        private Button AddButton(VisualElement parent,string title,Action action,bool dialog=false)
        {
            var epoch=viewEpoch;var button=new Button(()=>{if(epoch==viewEpoch&&isActiveAndEnabled&&!model!.Busy&&!model.Closed&&(dialog||confirmation==null))action();}){text=title};
            button.style.minHeight=32;button.style.flexShrink=0;parent.Add(button);return button;
        }
        private void Do(Action action)
        {
            if(model==null||model.Busy||model.Closed)return;
            var before=model.Current.Frame;action();
            // Rebuild bindings on a new observation/actor/host. Already queued old-view callbacks
            // retain their old epoch and cannot operate on the replacement boundary.
            if(!ReferenceEquals(before,model.Current.Frame)){viewEpoch++;Build();}else Draw();
        }
        private void Submit(Func<bool> action){Do(()=>{if(action())command!.SetValueWithoutNotify(ReviewUiModel.NewCommandId());});}
        private void Replace(string title,Func<bool> action,bool confirm)
        {if(confirm)Confirm(title,()=>Do(()=>{if(action())command!.SetValueWithoutNotify(ReviewUiModel.NewCommandId());}));else Do(()=>action());}
        private void Confirm(string title,Action action)
        {
            if(confirmation!=null)return;
            var callbacks=modalPermission.TryOpen(()=>{ClearConfirmation();action();},()=>ClearConfirmation());
            if(callbacks==null)return;
            confirmation=new VisualElement();confirmation.style.position=Position.Absolute;confirmation.style.left=0;confirmation.style.right=0;confirmation.style.top=0;confirmation.style.bottom=0;
            confirmation.style.backgroundColor=new Color(0.04f,0.04f,0.06f,0.98f);confirmation.style.paddingTop=60;confirmation.style.paddingLeft=30;
            var summary=new Label(title);summary.style.whiteSpace=WhiteSpace.Normal;confirmation.Add(summary);
            var review=new ScrollView();review.style.flexGrow=1;review.style.minHeight=0;
            var boundary=new Label(model!.Facts());boundary.style.whiteSpace=WhiteSpace.Normal;review.Add(boundary);confirmation.Add(review);
            foreach(var child in root!.Children())child.SetEnabled(false);
            AddButton(confirmation,"Cancel",callbacks.Cancel,true);
            var confirmButton=AddButton(confirmation,"Confirm",callbacks.Confirm,true);
            root!.Add(confirmation);
            confirmButton.Focus();
        }
        private void ClearConfirmation()
        {modalPermission.Invalidate();confirmation?.RemoveFromHierarchy();confirmation=null;foreach(var child in root!.Children())child.SetEnabled(true);Draw();}
    }
}
