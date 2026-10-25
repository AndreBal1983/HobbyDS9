#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.ArtDemo;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // A native view of one detached own-world observation. Inspection is local;
    // + submits one captured command to the normal async authoritative path.
    internal sealed class ReviewConstructionCatalogue : VisualElement, IDisposable
    {
        internal sealed class Selection
        {
            internal string Colony="",Definition="",FocusDefinition="";
            internal int Revision,FocusRevision;
            internal ActivityProductionKind Kind,FocusKind;
            internal bool Expanded,FocusAdd;
            internal Vector2 CatalogueScroll,QueueScroll;
        }
        private readonly CampaignStreamedActivityObservationDto snapshot;
        private readonly string colony;
        private readonly Action<CampaignActivityCommandDto> submit;
        private readonly Action<string> cancel;
        private readonly ScrollView catalogue,queue;
        private readonly Foldout inspector;
        private readonly VisualElement detail;
        private readonly Label feedback;
        private readonly List<Button> addButtons=new List<Button>();
        private ActivityBuildOptionDto? selected,focused;
        private bool focusAdd,disposed,submitted,active=true;
        private ShipPreviewStage? stage;
        private Image? preview;
        private Label? artCaption;
        private float yaw=38;

        internal ReviewConstructionCatalogue(CampaignStreamedActivityObservationDto observation,string colonyId,
            Action<CampaignActivityCommandDto> submit,Action<string> cancel)
        {
            snapshot=observation;colony=colonyId;this.submit=submit;this.cancel=cancel;
            name="construction-catalogue";AddToClassList("construction-catalogue");
            var columns=new VisualElement();columns.AddToClassList("construction-columns");Add(columns);
            catalogue=new ScrollView(ScrollViewMode.Vertical){name="construction-catalogue-scroll"};catalogue.AddToClassList("construction-scroll");columns.Add(catalogue);
            queue=new ScrollView(ScrollViewMode.Vertical){name="construction-queue"};queue.AddToClassList("construction-queue");columns.Add(queue);
            Section("Ships","construction-section-ships",snapshot.Activities.BuildOptions.Where(o=>o.Kind==ActivityProductionKind.PublishedShip));
            Section("Planetary buildings","construction-section-buildings",snapshot.Activities.BuildOptions.Where(o=>o.Kind==ActivityProductionKind.Infrastructure));
            EmptySection("Special projects","construction-section-projects","No buildable projects in this campaign.");
            EmptySection("System improvements","construction-section-system","No buildable system improvements in this campaign.");
            inspector=new Foldout{text="Inspect construction",value=false,name="construction-inspector-toggle"};inspector.AddToClassList("construction-inspector");catalogue.Add(inspector);
            detail=new VisualElement{name="construction-inspector"};inspector.Add(detail);
            inspector.RegisterValueChangedCallback(_=>{if(inspector.value)RefreshInspector();else ReleaseStage();});
            Copy(detail,"Select a tile to inspect it. Inspecting does not add an order.");
            feedback=Copy(this,"Normal construction charges no credits. Work consumes delivered materials; interruptions pause progress.","construction-status");
            feedback.name="construction-command-status";feedback.focusable=true;
            DrawQueue();
            RegisterCallback<GeometryChangedEvent>(_=>EnableInClassList("construction-wide",contentRect.width>=1150));
            RegisterCallback<AttachToPanelEvent>(_=>{if(active&&inspector.value)RefreshInspector();});
            RegisterCallback<DetachFromPanelEvent>(_=>ReleaseStage());
        }
        private StreamedColonyConstructionDto? Facts=>snapshot.Construction.SingleOrDefault(c=>c.ColonyId==colony);
        private StreamedBuildingConstructionDto? Building(ActivityBuildOptionDto option)=>Facts?.Buildings.SingleOrDefault(b=>b.DefinitionId==option.DefinitionId);
        private static string Number(long value)=>value.ToString("N0",CultureInfo.InvariantCulture);
        private static string Output(StreamedConstructionOutputDto value)=>"Industry "+Number(value.Industry)+" · Research "+Number(value.Research)+" · Food "+Number(value.Food);
        private static string Projection(StreamedConstructionProjectionDto? value)=>value?.Status==StreamedConstructionProjectionStatus.Available&&value.Output!=null?Output(value.Output):value?.Reason??"Projection unavailable.";
        private static string Change(long value)=>value>0?"+"+Number(value):Number(value);
        private static Label Copy(VisualElement parent,string text,string cls="construction-copy")
        {var label=new Label(text);label.AddToClassList(cls);parent.Add(label);return label;}
        private static Button Action(VisualElement parent,string text,string id,Action action,string cls="construction-action")
        {var button=new Button(action){text=text,name=id};button.AddToClassList(cls);parent.Add(button);return button;}
        private static bool Same(ActivityBuildOptionDto option,string definition,int revision,ActivityProductionKind kind)=>option.DefinitionId==definition&&option.BlueprintRevision==revision&&option.Kind==kind;
        private string Unavailable(ActivityBuildOptionDto option)
        {
            if(!option.Supported)return string.IsNullOrWhiteSpace(option.Reason)?"This class is unsupported in this campaign.":option.Reason;
            if(option.Kind==ActivityProductionKind.Infrastructure)
            {
                var fact=Building(option);if(fact==null)return "Construction details are unavailable. Refresh the campaign.";
                if(fact.Unavailable==StreamedBuildingUnavailable.AlreadyBuilt)return "Already built at this world.";
                if(fact.Unavailable==StreamedBuildingUnavailable.AlreadyQueued)return "Already queued at this world.";
            }
            return "";
        }
        private void Section(string title,string id,IEnumerable<ActivityBuildOptionDto> values)
        {
            var section=new VisualElement{name=id};section.AddToClassList("construction-section");catalogue.Add(section);Copy(section,title,"construction-heading");
            var grid=new VisualElement();grid.AddToClassList("construction-grid");section.Add(grid);
            foreach(var option in values)
            {
                int ordinal=snapshot.Activities.BuildOptions.IndexOf(option);
                var tile=new VisualElement{name="construction-tile-"+ordinal};tile.AddToClassList("construction-tile");grid.Add(tile);
                var inspect=Action(tile,option.DisplayName+(option.Kind==ActivityProductionKind.PublishedShip?" · revision "+option.BlueprintRevision:""),"construction-inspect-"+ordinal,()=>Inspect(option),"construction-inspect");
                inspect.tooltip="Inspect "+option.DisplayName+". This does not add an order.";
                inspect.RegisterCallback<FocusInEvent>(_=>{focused=option;focusAdd=false;});
                if(option.Kind==ActivityProductionKind.PublishedShip)
                {var art=ActivityHullCatalog.Find(option.HullVersionId);Copy(tile,art!=null?art.Label+" · inspect hull":"Published class · exact hull art unavailable.");}
                else DrawBuilding(tile,option);
                Copy(tile,Number(option.Cost.Industry)+" Industry work · no credit charge");
                Copy(tile,"Materials: "+Materials(option.Cost.Resources));
                var reason=Unavailable(option);if(reason.Length>0)Copy(tile,reason,"construction-reason");
                var add=Action(tile,"+","construction-add-"+ordinal,()=>AddOne(option),"construction-add");
                add.tooltip="Add one "+option.DisplayName+" to this world's queue.";add.SetEnabled(reason.Length==0);
                add.RegisterCallback<FocusInEvent>(_=>{focused=option;focusAdd=true;});addButtons.Add(add);
            }
            if(grid.childCount==0)Copy(section,"No construction options in this observation.");
        }
        private void EmptySection(string title,string id,string text)
        {var section=new VisualElement{name=id};section.AddToClassList("construction-section");catalogue.Add(section);Copy(section,title,"construction-heading");Copy(section,text);}
        private void DrawBuilding(VisualElement parent,ActivityBuildOptionDto option)
        {
            var fact=Building(option);if(fact==null){Copy(parent,"Building details unavailable.");return;}
            Copy(parent,Number(fact.BuiltCount)+" built · "+Number(fact.QueuedCount)+" queued");
            if(Facts!=null)Copy(parent,"Current weekly output: "+Output(Facts.Current));
            if(fact.AfterOne?.Output!=null&&Facts!=null)
            {
                var after=fact.AfterOne.Output;var before=Facts.Current;
                Copy(parent,"One additional: Industry "+Change(after.Industry-before.Industry)+" · Research "+Change(after.Research-before.Research)+" · Food "+Change(after.Food-before.Food));
            }
            else if(fact.AfterOne!=null)Copy(parent,"One additional: "+Projection(fact.AfterOne));
            if(Facts!=null)Copy(parent,"After queued buildings: "+Projection(Facts.AfterPendingStructures));
        }
        private static string Materials(IEnumerable<ResourceAmountDto>? resources,string empty="None required")
        {
            var values=resources?.Where(r=>r.Amount>0).Select(r=>(r.ResourceDefinitionId=="resource.dilithium"?"Dilithium":r.ResourceDefinitionId)+" "+Number(r.Amount)).ToList();
            return values==null||values.Count==0?empty:string.Join(" · ",values);
        }
        private void AddOne(ActivityBuildOptionDto option)
        {
            if(disposed||submitted||!active||Unavailable(option).Length>0)return;
            submitted=true;focused=option;focusAdd=true;
            foreach(var button in addButtons)button.SetEnabled(false);
            feedback.text="Adding one "+option.DisplayName+"…";
            var observed=snapshot.Activities;
            submit(new CampaignActivityCommandDto{ActorId=observed.ActorId,ExpectedTurn=observed.Turn,ExpectedRevision=observed.Revision,
                CommandId="command.activity."+Guid.NewGuid().ToString("N"),Kind=option.Kind==ActivityProductionKind.Infrastructure?CampaignActivityCommandKind.EnqueueStructure:CampaignActivityCommandKind.EnqueueShip,
                TargetId=colony,DefinitionId=option.DefinitionId,BlueprintRevision=option.BlueprintRevision});
        }
        private void Inspect(ActivityBuildOptionDto option)
        {
            if(disposed||submitted||!active)return;
            selected=option;focused=option;focusAdd=false;
            if(inspector.value)RefreshInspector();else inspector.value=true;
            catalogue.ScrollTo(inspector);
        }
        private void RefreshInspector()
        {
            if(disposed)return;ReleaseStage();detail.Clear();
            if(selected==null){Copy(detail,"Select a tile to inspect it. Inspecting does not add an order.");return;}
            Copy(detail,selected.DisplayName,"construction-heading");
            Copy(detail,selected.Kind==ActivityProductionKind.PublishedShip?"Published revision "+selected.BlueprintRevision:"Planetary building");
            Copy(detail,Number(selected.Cost.Industry)+" Industry work · no normal credit charge");Copy(detail,"Materials: "+Materials(selected.Cost.Resources));
            if(selected.Cost.Maintenance>0)Copy(detail,"Recurring maintenance: "+Number(selected.Cost.Maintenance));
            var reason=Unavailable(selected);if(reason.Length>0)Copy(detail,reason,"construction-reason");
            if(selected.Kind==ActivityProductionKind.Infrastructure)
            {
                DrawBuilding(detail,selected);var fact=Building(selected);
                if(fact?.AfterPendingAndOne!=null)Copy(detail,"With this added after queued buildings: "+Projection(fact.AfterPendingAndOne));
                Copy(detail,"Projections use current population, politics and power. Queued buildings are not completed output; deliveries can pause work.");
            }
            else
            {
                preview=new Image{name="construction-hull-model",scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};preview.style.height=180;detail.Add(preview);
                artCaption=Copy(detail,"Exact hull reference unavailable.");artCaption.name="construction-hull-caption";
                Action(detail,"Rotate hull","construction-hull-orbit",()=>{if(!disposed&&active&&stage!=null){yaw+=35;stage.OrbitReference(yaw,26);}});
                if(active&&panel!=null&&ActivityHullCatalog.Find(selected.HullVersionId)!=null)
                {
                    stage=new ShipPreviewStage(512,320);stage.ShowActivityHull(selected.HullVersionId);preview.image=stage.Texture;
                    preview.style.display=stage.Availability==ShipPreviewAvailability.Model?DisplayStyle.Flex:DisplayStyle.None;artCaption.text=stage.Label;
                }
                else preview.style.display=DisplayStyle.None;
            }
        }
        private void DrawQueue()
        {
            Copy(queue,"Construction queue","construction-heading");
            var own=snapshot.Activities.Colonies.Single(c=>c.ColonyId==colony);Copy(queue,own.DisplayName+" · one working order");
            Copy(queue,"Current weekly output: "+(Facts==null?"Unavailable":Output(Facts.Current)));
            var supply=snapshot.Supply.SingleOrDefault(s=>s.ColonyId==colony);
            if(supply!=null)
            {
                Copy(queue,"Local stock: "+Materials(supply.Stocks,"None stocked"));
                foreach(var delivery in supply.Deliveries.Where(d=>d.ResourceDefinitionId!="resource.food"))Copy(queue,"Last delivery: "+Number(delivery.Delivered)+" · "+(delivery.RouteIntegrity==0?"Route blocked or unavailable":delivery.Delivered==0?"No shipment available":"Physical stock delivered"));
            }
            int index=0;
            foreach(var order in snapshot.Production.Where(q=>q.ColonyId==colony))
            {
                var option=snapshot.Activities.BuildOptions.FirstOrDefault(o=>o.DefinitionId==order.DefinitionId&&o.BlueprintRevision==(order.Blueprint?.Revision??0));
                var availability=snapshot.Availability.SingleOrDefault(a=>a.QueueItemId==order.QueueItemId);
                var item=new VisualElement{name="construction-queue-item-"+index};item.AddToClassList("construction-queue-item");queue.Add(item);
                Copy(item,(index==0?"Working: ":"Queued: ")+(option?.DisplayName??"Construction"),"construction-item-title");
                if(order.Blueprint!=null)Copy(item,"Revision "+order.Blueprint.Revision);
                Copy(item,index==0?availability?.Status??"Awaiting materials":"Waiting for earlier orders");
                Copy(item,Number(order.CompletedIndustry)+" / "+Number(order.RequiredIndustry)+" Industry work");
                var bar=new ProgressBar{lowValue=0,highValue=100,value=(float)(100d*order.CompletedIndustry/order.RequiredIndustry)};bar.style.height=14;item.Add(bar);
                Copy(item,"Consumed: "+(order.RequiredResources!.Count==0?"No material inputs required":string.Join(" · ",order.RequiredResources.Select(r=>(r.ResourceDefinitionId=="resource.dilithium"?"Dilithium":r.ResourceDefinitionId)+" "+Number(order.ConsumedResources!.Single(c=>c.ResourceDefinitionId==r.ResourceDefinitionId).Amount)+" / "+Number(r.Amount)))));
                if(availability!=null)Copy(item,"Current stocks allow "+Number(availability.WorkWithCurrentStocks)+" Industry; deliveries may change this.");
                var id=order.QueueItemId;Action(item,"Cancel order…","activity-queue-cancel-"+index,()=>{if(!disposed&&!submitted&&active)cancel(id);});index++;
            }
            if(index==0)Copy(queue,"No construction orders at this world.");
            Copy(queue,"No guaranteed completion date: supply interruptions retain progress and pause work.");
        }
        internal Selection CaptureSelection()=>new Selection{Colony=colony,Definition=selected?.DefinitionId??"",Revision=selected?.BlueprintRevision??0,Kind=selected?.Kind??0,
            FocusDefinition=focused?.DefinitionId??"",FocusRevision=focused?.BlueprintRevision??0,FocusKind=focused?.Kind??0,FocusAdd=focusAdd,
            Expanded=inspector.value,CatalogueScroll=catalogue.scrollOffset,QueueScroll=queue.scrollOffset};
        internal void RestoreSelection(Selection? saved)
        {
            if(disposed||saved==null||saved.Colony!=colony)return;
            selected=snapshot.Activities.BuildOptions.FirstOrDefault(o=>Same(o,saved.Definition,saved.Revision,saved.Kind));
            focused=snapshot.Activities.BuildOptions.FirstOrDefault(o=>Same(o,saved.FocusDefinition,saved.FocusRevision,saved.FocusKind));focusAdd=saved.FocusAdd;
            inspector.SetValueWithoutNotify(saved.Expanded);if(saved.Expanded)RefreshInspector();
            schedule.Execute(()=>
            {
                if(disposed||!active)return;
                if(focused!=null)
                {
                    int ordinal=snapshot.Activities.BuildOptions.IndexOf(focused);var button=this.Q<Button>((focusAdd?"construction-add-":"construction-inspect-")+ordinal);
                    if(button?.enabledInHierarchy==true)button.Focus();else feedback.Focus();
                }
                catalogue.scrollOffset=saved.CatalogueScroll;queue.scrollOffset=saved.QueueScroll;
            }).StartingIn(1);
        }
        internal void SetActive(bool value){active=value;if(!value)ReleaseStage();else if(panel!=null&&inspector.value)RefreshInspector();}
        private void ReleaseStage(){if(preview!=null)preview.image=null;stage?.Dispose();stage=null;}
        public void Dispose(){if(disposed)return;disposed=true;ReleaseStage();}
    }
}
