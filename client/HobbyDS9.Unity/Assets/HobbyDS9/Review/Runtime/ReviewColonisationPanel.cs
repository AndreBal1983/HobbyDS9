#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.ArtDemo;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    // Only actor-filtered observations enter this view. It has no package/root lookup.
    public sealed class ReviewColonisationPanel:VisualElement,IDisposable
    {
        private readonly ColonisationCampaignSnapshot observation;
        private readonly IReadOnlyList<ColonisationPlanetSnapshot> planets;
        private readonly CampaignActivityObservationDto activities;
        private readonly ILookup<string,string>? systemNodes;
        private readonly Action<ColonisationCommandDto,string> reviewOrder;
        private readonly Action<ColonisationCommandDto> submit;
        private readonly Action<CampaignActivityCommandDto> submitActivity;
        private readonly Action<CampaignActivityCommandDto,string> reviewActivity;
        private readonly ScrollView body;
        private readonly VisualElement tabs;
        private readonly VisualElement review;
        private readonly Label status;
        private ReviewActivitiesPanel? science;
        private Dictionary<string,string>? knownSiteLocations;
        private Action<string>? selectSiteOnMap;
        private ShipPreviewStage? hullPreview;
        private ReviewSolOverview? solOverview;
        private ReviewPlanetPortrait? planetPortrait;
        private Action? fitConstruction;
        private float solYaw,solZoom=1; private bool solReducedMotion;
        private readonly Dictionary<string,bool> planetSections=new Dictionary<string,bool>(StringComparer.Ordinal);
        private string inspectedBlueprint="";private int inspectedRevision;
        private bool disposed,submitted;private int controlEpoch,reviewEpoch;private bool inspectedOpen;
        private Foldout? currentInspector;
        private ReviewState? displayedReview;
        internal sealed class ReviewState
        {
            internal readonly ColonisationCommandDto? Command;internal readonly ColonisationEligibilitySnapshot? Eligibility;
            internal readonly ReviewActivitiesPanel.ReviewState? Activity;internal readonly string Caption;
            internal ReviewState(ColonisationCommandDto command,string caption,ColonisationEligibilitySnapshot eligibility){Command=Copy(command);Caption=caption;Eligibility=eligibility;}
            internal ReviewState(ReviewActivitiesPanel.ReviewState activity){Activity=activity;Caption=activity.Caption;}
        }
        internal ReviewState? CaptureReview()=>!Live?null:science!=null?(science.CaptureReview() is ReviewActivitiesPanel.ReviewState r?new ReviewState(r):null):displayedReview;
        internal void RestoreReview(ReviewState? saved)
        {if(saved?.Command!=null)PresentReviewedOrder(Copy(saved.Command),saved.Caption,saved.Eligibility!);else if(saved?.Activity!=null){if(science!=null)science.RestoreReview(saved.Activity);else PresentReviewedActivity(saved.Activity.Command,saved.Caption,saved.Activity.Eligibility);}}
        private static List<ResourceAmountDto>? CopyResources(List<ResourceAmountDto>? rows)=>rows?.Select(r=>new ResourceAmountDto{ResourceDefinitionId=r.ResourceDefinitionId,Amount=r.Amount}).ToList();
        private static ColonisationCommandDto Copy(ColonisationCommandDto c)=>new ColonisationCommandDto{
            CommandId=c.CommandId,ActorId=c.ActorId,Turn=c.Turn,Revision=c.Revision,Action=c.Action,
            StartSurvey=c.StartSurvey==null?null:new PlanetSurveyOrderDto{ShipId=c.StartSurvey.ShipId,PlanetId=c.StartSurvey.PlanetId},
            CancelSurvey=c.CancelSurvey==null?null:new CancelPlanetSurveyDto{SurveyId=c.CancelSurvey.SurveyId},
            Embark=c.Embark==null?null:new EmbarkSettlersDto{ShipId=c.Embark.ShipId,ColonyId=c.Embark.ColonyId,CohortId=c.Embark.CohortId,Population=c.Embark.Population,Food=c.Embark.Food,Materials=CopyResources(c.Embark.Materials)},
            Unload=c.Unload==null?null:new UnloadSettlersDto{ManifestId=c.Unload.ManifestId,ColonyId=c.Unload.ColonyId},
            Deploy=c.Deploy==null?null:new DeploySettlersDto{ManifestId=c.Deploy.ManifestId,PlanetId=c.Deploy.PlanetId},
            Withdraw=c.Withdraw==null?null:new WithdrawDeploymentDto{IntentId=c.Withdraw.IntentId},
            Evacuate=c.Evacuate==null?null:new EvacuateFoundingColonyDto{CampId=c.Evacuate.CampId,ShipId=c.Evacuate.ShipId},
            Deliver=c.Deliver==null?null:new DeliverCarriedSuppliesDto{ManifestId=c.Deliver.ManifestId,ColonyId=c.Deliver.ColonyId,Food=c.Deliver.Food,Materials=CopyResources(c.Deliver.Materials)},
            EnqueueKit=c.EnqueueKit==null?null:new EnqueueSettlementKitDto{ColonyId=c.EnqueueKit.ColonyId},
            LoadSupplies=c.LoadSupplies==null?null:new LoadCarriedSuppliesDto{ShipId=c.LoadSupplies.ShipId,SourceColonyId=c.LoadSupplies.SourceColonyId,Food=c.LoadSupplies.Food,Materials=CopyResources(c.LoadSupplies.Materials)}};
        private string page="Worlds",world="",ship="",planet="",surveyShip="";
        private TransportDraft? transportDraft;
        internal sealed class TransportDraft
        {
            internal readonly string World,Ship,Node;internal readonly string? Manifest;internal readonly ColonyEstablishmentStage Stage;
            internal int Food;internal string Cohort;internal readonly Dictionary<string,int> Materials;
            internal TransportDraft(ColonisationWorldSnapshot w,ColonisationTransportSnapshot t)
            {World=w.Id;Ship=t.ShipId;Node=t.NodeId;Manifest=t.ManifestId;Stage=w.Stage;Food=t.ManifestId==null?20:checked((int)t.Food);Cohort=w.Cohorts.FirstOrDefault()?.Id??"";Materials=new Dictionary<string,int>(StringComparer.Ordinal);}
            private TransportDraft(TransportDraft d){World=d.World;Ship=d.Ship;Node=d.Node;Manifest=d.Manifest;Stage=d.Stage;Food=d.Food;Cohort=d.Cohort;Materials=new Dictionary<string,int>(d.Materials,StringComparer.Ordinal);}
            internal TransportDraft Copy()=>new TransportDraft(this);
            internal bool Matches(ColonisationWorldSnapshot w,ColonisationTransportSnapshot t)=>World==w.Id&&Ship==t.ShipId&&Node==t.NodeId&&Manifest==t.ManifestId&&Stage==w.Stage;
        }
        internal sealed class Selection
        {internal readonly string Page,World,Ship,Planet,SurveyShip,Blueprint;internal readonly string? SystemCaption;internal readonly int Revision;internal readonly bool InspectorOpen;internal readonly float SolYaw,SolZoom;internal readonly bool ReducedMotion;internal readonly ReviewActivitiesPanel.Selection? Science;internal readonly TransportDraft? Draft;internal Selection(string page,string world,string ship,string planet,string surveyShip,string blueprint,int revision,bool open,ReviewActivitiesPanel.Selection? science,TransportDraft? draft,string? systemCaption,float solYaw=0,float solZoom=1,bool reducedMotion=false){SolYaw=solYaw;SolZoom=solZoom;ReducedMotion=reducedMotion;SystemCaption=systemCaption;Page=page;World=world;Ship=ship;Planet=planet;SurveyShip=surveyShip;Blueprint=blueprint;Revision=revision;InspectorOpen=open;Science=science;Draft=draft?.Copy();}}
        internal Selection? CaptureSelection()=>disposed?null:new Selection(page,world,ship,planet,surveyShip,inspectedBlueprint,inspectedRevision,this.Q<Foldout>("colonisation-hull-inspector")?.value??inspectedOpen,science?.CaptureSelection(),transportDraft,inspectedSystemCaption,solYaw,solZoom,solReducedMotion);
        internal void OpenMissionPage(bool expeditions)
        {
            if(!Live)return;
            Show(expeditions?"Science":"Worlds");
            if(expeditions)science?.RestoreSelection(new ReviewActivitiesPanel.Selection(observation.Actor,true,null,null,null));
        }
        internal void RestoreSelection(Selection? saved)
        {
            if(disposed||saved==null)return;
            if(observation.Worlds.Any(x=>x.Id==saved.World))world=saved.World;
            if(observation.Transports.Any(x=>x.ShipId==saved.Ship))ship=saved.Ship;
            if(planets.Any(x=>x.PlanetId==saved.Planet)||saved.Planet.Length==0)
            {planet=saved.Planet;inspectedSystemCaption=saved.SystemCaption;}
            if(activities.Ships.Any(x=>x.ShipId==saved.SurveyShip))surveyShip=saved.SurveyShip;
            solYaw=saved.SolYaw;solZoom=saved.SolZoom;solReducedMotion=saved.ReducedMotion;transportDraft=saved.Draft?.Copy();inspectedBlueprint=saved.Blueprint;inspectedRevision=saved.Revision;inspectedOpen=saved.InspectorOpen;Show(saved.Page);science?.RestoreSelection(saved.Science);
        }
        private readonly bool supportsKitFreePassengers;
        private readonly int? environmentScale;
        public ReviewColonisationPanel(ColonisationCampaignSnapshot observation,IReadOnlyList<ColonisationPlanetSnapshot> planets,CampaignActivityObservationDto activities,
            Action<ColonisationCommandDto,string> reviewOrder,Action<ColonisationCommandDto> submit,Action<CampaignActivityCommandDto,string> reviewActivity,Action<CampaignActivityCommandDto> submitActivity,bool supportsKitFreePassengers=false, ILookup<string,string>? systemNodes=null,int? environmentScale=null)
        {
            if(environmentScale.HasValue&&environmentScale.Value<=0)throw new ArgumentOutOfRangeException(nameof(environmentScale));
            this.environmentScale=environmentScale;
            this.observation=observation;this.planets=planets;this.activities=activities;this.reviewOrder=reviewOrder;this.submit=submit;this.reviewActivity=reviewActivity;this.submitActivity=submitActivity;this.supportsKitFreePassengers=supportsKitFreePassengers;this.systemNodes=systemNodes;
            name="colonisation-panel";style.flexGrow=1;style.minHeight=0;style.minWidth=0;
            world=observation.Worlds.FirstOrDefault()?.Id??"";ship=observation.Transports.FirstOrDefault()?.ShipId??"";planet=planets.FirstOrDefault()?.PlanetId??"";
            tabs=Row(this);foreach(var name in new[]{"Worlds","Planets","Transports","Science"}){var captured=name;Button(tabs,name=="Planets"?"System":name=="Worlds"?"Construction":name,"colonisation-tab-"+name,()=>Show(captured),true);}
            body=new ScrollView(ScrollViewMode.Vertical){name="colonisation-scroll"};body.style.flexGrow=1;body.style.minHeight=0;Add(body);
            body.contentViewport.RegisterCallback<GeometryChangedEvent>(_=>fitConstruction?.Invoke());
            review=new VisualElement{name="colonisation-order-review"};review.style.flexShrink=0;review.style.maxHeight=220;review.style.display=DisplayStyle.None;Add(review);
            status=Text(this,"","colonisation-command-status");status.style.flexShrink=0;status.style.display=DisplayStyle.None;
            RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,this))Dispose();});Show("Worlds");
        }
        private bool Live=>!disposed&&!submitted;
        internal void BindKnownSiteNavigation(IReadOnlyDictionary<string,string> locations,Action<string> select)
        {
            if(!Live)return;
            knownSiteLocations=locations.ToDictionary(pair=>pair.Key,pair=>pair.Value,StringComparer.Ordinal);
            selectSiteOnMap=select;science?.BindKnownSiteNavigation(knownSiteLocations,select);
        }
        private static Label Text(VisualElement p,string text,string id="")
        {var l=new Label(text){name=id};l.style.whiteSpace=WhiteSpace.Normal;l.style.fontSize=18;l.style.flexShrink=0;l.style.marginBottom=6;p.Add(l);return l;}
        private static VisualElement Row(VisualElement p)
        {var r=new VisualElement();r.style.flexDirection=FlexDirection.Row;r.style.flexWrap=Wrap.Wrap;r.style.minWidth=0;r.style.flexShrink=0;r.style.marginBottom=6;p.Add(r);return r;}
        private Button Button(VisualElement p,string text,string id,Action action,bool persistent=false)
        {var epoch=controlEpoch;var b=new Button(()=>{if(Live&&(persistent||epoch==controlEpoch))action();}){text=text,name=id};b.style.minHeight=44;b.style.fontSize=16;b.style.whiteSpace=WhiteSpace.Normal;b.style.marginRight=6;b.style.marginBottom=6;p.Add(b);return b;}
        private void Picker<T>(VisualElement p,string caption,IReadOnlyList<T> values,Func<T,string> id,Func<T,string> label,string current,Action<string> changed,string control)
        {
            var epoch=controlEpoch;var labels=values.Select((x,i)=>label(x)+" \u00b7 "+(i+1)).ToList();var index=values.ToList().FindIndex(x=>id(x)==current);
            var f=new DropdownField(caption,labels,index){name=control};f.style.minHeight=44;f.style.fontSize=16;f.style.flexShrink=0;p.Add(f);f.SetEnabled(values.Count>0);
            f.RegisterValueChangedCallback(_=>{if(Live&&epoch==controlEpoch&&f.index>=0&&f.index<values.Count)changed(id(values[f.index]));});
        }
        internal void BindWorldsOverview(Action back)=>Button(tabs,"Owned worlds overview","worlds-back-to-overview",back,true);
        private static string MaterialName(string id)=>id=="resource.dilithium"?"Dilithium":id=="resource.settlement-kit"?"Settlement kits":"Material";
        private static string Materials(IEnumerable<ResourceAmountSnapshot> rows)=>string.Join(", ",rows.Where(x=>x.Amount!=0).Select(x=>MaterialName(x.ResourceDefinitionId)+" "+x.Amount));
        private ColonisationCommandDto Command(ColonisationAction action)=>new ColonisationCommandDto{ActorId=observation.Actor,Turn=observation.Turn,Revision=observation.Revision,Action=action,CommandId="command.colony.ui."+Guid.NewGuid().ToString("N")};
        private CampaignActivityCommandDto Activity(CampaignActivityCommandKind kind,string target)=>new CampaignActivityCommandDto{ActorId=observation.Actor,ExpectedTurn=observation.Turn,ExpectedRevision=observation.Revision,Kind=kind,TargetId=target,CommandId="command.colony.activity."+Guid.NewGuid().ToString("N")};
        private void Check(ColonisationCommandDto command,string caption){if(Live)reviewOrder(command,caption);}
        private void Send(ColonisationCommandDto command){if(!Live)return;displayedReview=null;submitted=true;submit(command);}
        private void SendActivity(CampaignActivityCommandDto command){if(!Live)return;displayedReview=null;submitted=true;submitActivity(command);}
        private void Show(string selected)
        {
            if(page=="Transports"&&selected!="Transports")transportDraft=null;
            if(disposed)return;controlEpoch++;reviewEpoch++;displayedReview=null;currentInspector=null;page=new[]{"Worlds","Planets","Planet","Transports","Science"}.Contains(selected)?selected:"Worlds";
            fitConstruction=null;science?.Dispose();science=null;ReleaseHull();ReleaseSolOverview();body.Clear();review.Clear();review.style.display=DisplayStyle.None;
            if(page=="Worlds")Worlds();else if(page=="Planets")Planets();else if(page=="Planet")PlanetInterface();else if(page=="Transports")Transports();else
            {
                science=new ReviewActivitiesPanel(activities,reviewActivity,submitActivity,true);
                if(knownSiteLocations!=null&&selectSiteOnMap!=null)science.BindKnownSiteNavigation(knownSiteLocations,selectSiteOnMap);
                body.Add(science);
            }
        }
        private void Worlds()
        {
            var command=Text(body,"Construction Command","colonisation-construction-heading");
            command.style.fontSize=24;
            command.style.color= (Color)new Color32(240,205,165,255);
            command.style.unityFontStyleAndWeight=FontStyle.Bold;
            Picker(body,"Owned world",observation.Worlds,x=>x.Id,x=>x.Name,world,id=>{world=id;Show(page);},"colonisation-world-selector");
            var w=observation.Worlds.FirstOrDefault(x=>x.Id==world);if(w==null){Text(body,"No owned worlds observed.");return;}
            Text(body,w.Name+" \u00b7 "+w.Stage,"colonisation-world-facts");
            var readouts=Row(body);readouts.AddToClassList("construction-world-readouts");
            void Readout(string caption,long value)
            {
                var metric=new VisualElement();metric.AddToClassList("construction-world-readout");readouts.Add(metric);
                Text(metric,caption).AddToClassList("construction-readout-label");
                Text(metric,value.ToString()).AddToClassList("construction-readout-value");
            }
            Readout("Population",w.Population);Readout("Industry / week",w.Industry);
            Readout("Research / week",w.Research);Readout("Food output / week",w.FoodOutput);
            Text(body,"Local stocks · Food "+w.Food+" · "+(Materials(w.Stocks)==""?"no other materials":Materials(w.Stocks)),"construction-local-stocks");
            var balance=w.LocalFoodBalance.HasValue?(w.LocalFoodBalance.Value>=0?"+":"")+w.LocalFoodBalance.Value:"unknown";
            Text(body,w.FoodDemand.HasValue?"Residents need "+w.FoodDemand.Value+" food/week; local balance "+balance+" before imports/exports.":"Resident food demand unavailable.","colonisation-food-balance");
            var preparation=new Foldout{text="Prepare a settlement expedition",value=false,name="colonisation-preparation-guide"};preparation.style.flexShrink=0;body.Add(preparation);
            if(w.Stage==ColonyEstablishmentStage.Established)Text(preparation,"Consecutive food-shortage weeks: "+w.ShortageWeeks+". Deliveries depend on actual surplus, routes and capacity.","colonisation-food-shortage");
            Text(preparation,"Choose a populated source world separately from your industrial yard. Check food output against resident demand before loading voyage food. Planetary buildings show their base effects below.");
            Text(preparation,"At a food-deficit yard, consider a food-producing building before a long ship order. Check its material cost and base effects below. Materials and work are consumed gradually; extra food output begins after actual completion.");
            Text(preparation,"Build a transport at a shipyard and a settlement kit at the source world, then bring the empty transport there. In Transports, review the actual source cohort, remaining residents, local kit and voyage food. Survey a suitable known planet, travel there and review deployment; retain food for establishment and later supply trips. Travel and deliveries are not guaranteed safe.");
            if(w.Stage==ColonyEstablishmentStage.Founding)Text(body,"Establishment: "+w.EstablishmentWork+" work, "+w.FedWeeks+" fed weeks. Shortage "+w.ShortageWeeks+" weeks; deaths "+w.Deaths+". "+w.Report);
            if(w.Stage==ColonyEstablishmentStage.EmptyOutpost)Text(body,"Empty outpost: no production or freight. A surveyed, supplied settlement expedition must recommission it.");
            Text(body,"Builds use Industry and local materials as work advances.");
            var row=Row(body);
            row.style.flexWrap=Wrap.NoWrap;
            var catalogue=new ScrollView{name="colonisation-catalogue"};
            var dock=new VisualElement{name="construction-context-dock"};
            var inspector=new VisualElement{name="construction-selected-hull"};dock.Add(inspector);
            var queue=new ScrollView{name="colonisation-queue"};
            queue.style.flexGrow=1;queue.style.flexBasis=0;queue.style.minHeight=120;queue.style.minWidth=0;dock.Add(queue);
            foreach(var column in new VisualElement[]{catalogue,dock})
            {
                column.style.flexBasis=0;
                column.style.minWidth=0;
                column.style.height=420;
                row.Add(column);
            }
            catalogue.style.flexGrow=1;
            catalogue.style.marginRight=16;
            dock.style.flexBasis=380;dock.style.flexShrink=0;
            fitConstruction=()=>
            {
                if(!Live||page!="Worlds"||row.panel==null)return;
                var sideBySide=row.layout.width>=760;
                var direction=sideBySide?FlexDirection.Row:FlexDirection.Column;
                row.style.flexDirection=direction;
                catalogue.style.marginRight=sideBySide?16:0;
                catalogue.style.flexBasis=sideBySide?new StyleLength(0f):new StyleLength(StyleKeyword.Auto);
                dock.style.flexBasis=sideBySide?new StyleLength(380):new StyleLength(StyleKeyword.Auto);
                var available=Mathf.Clamp(body.contentViewport.layout.height-row.layout.y-12,320,680);
                if(Mathf.Abs(catalogue.style.height.value.value-(sideBySide?available:420))>.5f)catalogue.style.height=sideBySide?available:420;
                if(Mathf.Abs(dock.style.height.value.value-(sideBySide?available:520))>.5f)dock.style.height=sideBySide?available:520;
                catalogue.style.flexShrink=sideBySide?1:0;
                dock.style.flexShrink=0;
            };
            row.RegisterCallback<GeometryChangedEvent>(_=>fitConstruction?.Invoke());

            var orders=observation.Production.Where(x=>x.ColonyId==w.Id).ToArray();
            ConstructionHeading(queue,"Build queue · "+orders.Length+(orders.Length==1?" order":" orders"),"colonisation-queue-heading");
            int index=0;
            foreach(var kind in new[]{ActivityProductionKind.PublishedShip,ActivityProductionKind.Infrastructure})
            {
                var ships=kind==ActivityProductionKind.PublishedShip;
                ConstructionHeading(catalogue,ships?"Ships":"Planetary buildings","colonisation-section-"+kind);
                var cards=Row(catalogue);
                cards.RegisterCallback<GeometryChangedEvent>(e=>
                {
                    var useGrid=e.newRect.width>=620;
                    var width=Length.Percent(useGrid?48:100);
                    foreach(var card in cards.Children())
                    {card.style.width=width;card.style.marginRight=useGrid?6:0;}
                });
                foreach(var option in activities.BuildOptions.Where(x=>x.Kind==kind))
                {
                    var selected=option;
                    // Ship study thumbnails can resemble exploded parts. Exact live hull inspection
                    // belongs in the shared dock; never enlarge those old pixels into a hero.
                    var art=ships?null:ConstructionCardArt.Building(selected.DefinitionId);
                    var title=selected.DisplayName+(selected.BlueprintRevision>0?" R"+selected.BlueprintRevision:"");
                    var card=ConstructionCard(cards,title,art);
                    card.AddToClassList("construction-option-card");
                    card.style.marginRight=6;
                    card.style.width=Length.Percent(100);
                    Text(card,selected.Cost.Industry+" Industry");
                    var materials=selected.Cost.Resources;
                    Text(card,"Materials: "+(materials==null||materials.Count==0?"none":string.Join(", ",
                        materials.Select(r=>MaterialName(r.ResourceDefinitionId)+" "+r.Amount))));

                    var queued=orders.Count(x=>x.DefinitionId==selected.DefinitionId&&x.BlueprintRevision==selected.BlueprintRevision);
                    var built=ships
                        ?observation.OwnShipSources.Count(x=>x.BlueprintId==selected.DefinitionId&&x.Revision==selected.BlueprintRevision)
                        :activities.Structures.Count(x=>x.ColonyId==w.Id&&x.DefinitionId==selected.DefinitionId);
                    Text(card,ships?"Owned ships: "+built+" · queued here: "+queued:"Built here: "+built+" · queued here: "+queued);

                    var unavailable=!selected.Supported?selected.Reason:w.ConstructionUnavailableReason(ships);
                    if(unavailable==null&&!ships&&(built>0||queued>0))unavailable="Unique building: already built or queued.";
                    var actions=Row(card);
                    var add=Button(actions,"+ Add","colonisation-build-"+index++,()=>
                    {
                        var c=Activity(ships?CampaignActivityCommandKind.EnqueueShip:CampaignActivityCommandKind.EnqueueStructure,w.Id);
                        c.DefinitionId=selected.DefinitionId;
                        c.BlueprintRevision=selected.BlueprintRevision;
                        SendActivity(c);
                    });
                    add.tooltip="Add "+title+" at "+w.Name;
                    ConstructionPrimary(add);
                    add.SetEnabled(selected.Supported&&unavailable==null);
                    if(unavailable!=null)Text(card,unavailable);
                    if(ships)
                    {
                        var inspect=Button(actions,"Inspect hull","colonisation-inspect-"+index,()=>
                        {
                            inspectedBlueprint=selected.DefinitionId;
                            inspectedRevision=selected.BlueprintRevision;
                            inspectedOpen=true;
                            InspectHull(inspector,selected);
                        });
                        inspect.tooltip="Inspect "+title+" · exact published hull";
                        if(inspectedBlueprint==selected.DefinitionId&&inspectedRevision==selected.BlueprintRevision)
                            InspectHull(inspector,selected);
                    }
                    else
                    {
                        var effects=new Foldout{text="Effects & conditions",value=false};card.Add(effects);
                        ConstructionEffects(effects,selected.DefinitionId);
                    }
                }
            }

            ConstructionHeading(catalogue,"Special projects","colonisation-section-projects");
            var kit=ConstructionCard(catalogue,"Settlement kit",ConstructionCardArt.Kit());
            kit.AddToClassList("construction-option-card");
            Text(kit,observation.KitIndustry+" Industry · "+ConstructionMaterials(observation.KitResources));
            var addKit=Button(kit,"+ Settlement kit","colonisation-build-kit",()=>
            {
                var c=Command(ColonisationAction.EnqueueKit);
                c.EnqueueKit=new EnqueueSettlementKitDto{ColonyId=w.Id};
                Send(c);
            });
            ConstructionPrimary(addKit);
            var kitUnavailable=w.ConstructionUnavailableReason(false);
            addKit.SetEnabled(kitUnavailable==null);
            if(kitUnavailable!=null)Text(kit,kitUnavailable);

            ConstructionHeading(catalogue,"System improvements","colonisation-section-system");
            Text(catalogue,"No system improvements are available in this playtest.");

            if(orders.Length==0)Text(queue,"No queued orders.");
            for(var i=0;i<orders.Length;i++)
            {
                var order=orders[i];
                var artwork=order.Kind switch
                {
                    ColonisationProductionKind.PublishedShip => null,
                    ColonisationProductionKind.Infrastructure => ConstructionCardArt.Building(order.DefinitionId),
                    ColonisationProductionKind.SettlementKit => ConstructionCardArt.Kit(),
                    _ => null,
                };
                var card=ConstructionCard(queue,order.Name,artwork);
                card.AddToClassList("construction-queue-row");
                if(i==0)card.style.borderLeftColor= (Color)new Color32(239,179,107,255);
                Text(card,(i+1)+" · "+order.Status);
                Text(card,"Industry progress: "+order.CompletedIndustry+" / "+order.RequiredIndustry);
                if(order.RequiredIndustry>0)
                    card.Add(new ProgressBar{lowValue=0,highValue=order.RequiredIndustry,value=order.CompletedIndustry});
                var materialsDetail=new Foldout{text="Materials & cancellation",value=false};card.Add(materialsDetail);
                Text(materialsDetail,"Materials required: "+ConstructionMaterials(order.Required));
                Text(materialsDetail,"Materials consumed: "+ConstructionMaterials(order.Consumed));
                Text(materialsDetail,"Cancel discards work and consumed materials.");
                var cancel=Button(card,"Cancel order","colonisation-queue-cancel-"+order.Id,
                    ()=>SendActivity(Activity(CampaignActivityCommandKind.CancelProduction,order.Id)));
                cancel.tooltip="Cancel construction now. Work and consumed materials are not refunded.";
            }
        }

        private static void ConstructionPrimary(Button button)
        {
            button.AddToClassList("primary-action");
            button.style.backgroundColor= (Color)new Color32(239,179,107,255);
            button.style.color= (Color)new Color32(17,24,32,255);
        }

        private static void ConstructionHeading(VisualElement parent,string title,string name)
        {
            var heading=Text(parent,title,name);
            heading.style.color= (Color)new Color32(175,162,228,255);
            heading.style.unityFontStyleAndWeight=FontStyle.Bold;
            heading.style.marginTop=8;
        }

        private static VisualElement ConstructionCard(VisualElement parent,string title,Texture2D? artwork)
        {
            var card=new VisualElement();
            card.AddToClassList("console-panel");
            card.style.flexShrink=0;
            card.style.minWidth=0;
            card.style.marginBottom=8;
            card.style.backgroundColor= (Color)new Color32(17,32,47,255);
            parent.Add(card);
            var header=Row(card);
            if(artwork!=null)
            {
                var image=new Image{image=artwork,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
                image.style.width=78;
                image.style.height=54;
                image.style.marginRight=8;
                header.Add(image);
            }
            var label=Text(header,title);
            label.style.maxWidth=Length.Percent(100);
            label.style.unityFontStyleAndWeight=FontStyle.Bold;
            return card;
        }

        private static string ConstructionMaterials(IEnumerable<ResourceAmountSnapshot> materials)
        {
            var text=Materials(materials);
            return text.Length==0?"none":text;
        }

        private void ConstructionEffects(VisualElement card,string definitionId)
        {
            var benefit=observation.BuildingBenefits.FirstOrDefault(b=>b.DefinitionId==definitionId);
            if(benefit==null){Text(card,"Building effects unavailable.");return;}
            Text(card,"Base building effects: Food +"+benefit.FoodOutput+", Industry +"+benefit.Industry+
                ", Research +"+benefit.Research+", power supply +"+benefit.PowerSupply+", power demand +"+benefit.PowerDemand+".",
                "colonisation-building-effects-"+definitionId);
            if(benefit.Extraction.Count>0)Text(card,"Base extraction/week: "+Materials(benefit.Extraction)+"; requires finite deposits.");
            Text(card,"Actual output depends on power, politics and deposits.");
        }

        private void ReleaseHull(){hullPreview?.Dispose();hullPreview=null;}
        private void InspectHull(VisualElement parent,ActivityBuildOptionDto option)
        {
            var oldInspector=currentInspector;
            currentInspector=null;ReleaseHull();oldInspector?.RemoveFromHierarchy();
            var fold=new Foldout{text="Inspect "+option.DisplayName+" R"+option.BlueprintRevision,value=inspectedOpen,name="colonisation-hull-inspector"};fold.style.flexShrink=0;fold.style.fontSize=16;parent.Add(fold);
            currentInspector=fold;var inspectorEpoch=controlEpoch;
            bool OwnInspector()=>Live&&inspectorEpoch==controlEpoch&&ReferenceEquals(currentInspector,fold);
            var image=new Image{name="colonisation-hull-image",scaleMode=ScaleMode.ScaleToFit};image.style.height=180;image.style.minHeight=180;image.style.flexShrink=0;fold.Add(image);
            image.tooltip="Exact published hull presentation. No statistics or unlocks are supplied by this image.";
            var label=Text(fold,"Expand to inspect exact hull art.");
            void Refresh()
            {
                if(!OwnInspector())return;
                if(!fold.value||fold.panel==null){image.image=null;ReleaseHull();return;}
                if(ActivityHullCatalog.Find(option.HullVersionId)==null){label.text="No exact hull art binding is available.";return;}
                ReleaseHull();hullPreview=new ShipPreviewStage(512,320);hullPreview.ShowActivityHull(option.HullVersionId);
                image.image=hullPreview.Availability==ShipPreviewAvailability.Model?hullPreview.Texture:null;
                label.text=hullPreview.Availability==ShipPreviewAvailability.Model&&!hullPreview.Label.Contains("missing art")
                    ?ActivityHullCatalog.Find(option.HullVersionId).Label:hullPreview.Label;
                label.tooltip=hullPreview.Label;
            }
            fold.RegisterValueChangedCallback(_=>{if(!OwnInspector())return;inspectedOpen=fold.value;Refresh();});fold.RegisterCallback<AttachToPanelEvent>(_=>Refresh());
            Button(fold,"Rotate hull","colonisation-hull-orbit",()=>{if(OwnInspector())hullPreview?.OrbitReference(80,26);});
            // An inspector added to an attached page has already received its attach event.
            if(fold.panel!=null)Refresh();
        }
        private string? inspectedSystemCaption;
        internal bool InspectOwnedWorld(string worldId)
        {
            if(!Live||!observation.Worlds.Any(x=>x.Id==worldId))return false;
            world=worldId;transportDraft=null;
            Show("Worlds");
            return true;
        }
        internal void InspectSystem(string? systemId,string caption)
        {
            if(!Live)return;
            inspectedSystemCaption=caption;
            if(!planets.Any(x=>x.PlanetId==planet&&x.SystemId==systemId)){planet=planets.FirstOrDefault(x=>x.SystemId==systemId)?.PlanetId??"";solYaw=0;}
            Show("Planets");
        }
        private void Planets()
        {
            Text(body,inspectedSystemCaption??"System","colonisation-system-heading").style.unityFontStyleAndWeight=FontStyle.Bold;
            var p=planets.FirstOrDefault(x=>x.PlanetId==planet);
            if(p==null){Text(body,"No planetary bodies have been disclosed here.");return;}
            var systems=planets.GroupBy(x=>x.SystemId,StringComparer.Ordinal).ToArray();
            Picker(body,"Disclosed system",systems,x=>x.Key,x=>"System containing "+x.First().DisplayName,p.SystemId,id=>{
                inspectedSystemCaption=null;planet=systems.First(x=>x.Key==id).First().PlanetId;solYaw=0;Show("Planets");
            },"colonisation-system-selector");
            var bodies=systems.First(x=>x.Key==p.SystemId).ToArray();
            var surface=new VisualElement{name="colonisation-system-content"};body.Add(surface);
            var activation=new PlanetActivationScope(surface);
            Text(body,"Click / Space: focus · Double-click / Enter: open planet","colonisation-planet-interaction-hint");
            var layout=Row(surface);layout.name="colonisation-system-layout";
            var visual=new VisualElement{name="colonisation-system-visual"};visual.style.flexBasis=520;visual.style.flexGrow=1;visual.style.minWidth=0;
            var detail=new ScrollView(ScrollViewMode.Vertical){name="colonisation-planet-focus"};detail.AddToClassList("system-selected-inspector");
            detail.style.height=420;detail.style.minHeight=300;
            detail.style.width=360;detail.style.flexShrink=0;detail.style.minWidth=0;
            layout.Add(visual);layout.Add(detail);
            bool? compact=null;
            surface.RegisterCallback<GeometryChangedEvent>(e=>{
                if(!ReferenceEquals(e.target,surface))return;
                var narrow=e.newRect.width<940;if(compact==narrow)return;compact=narrow;
                layout.style.flexDirection=narrow?FlexDirection.Column:FlexDirection.Row;
                visual.style.flexBasis=narrow?StyleKeyword.Auto:new StyleLength(520);
                detail.style.width=narrow?new StyleLength(Length.Percent(100)):new StyleLength(360);
                detail.style.marginLeft=narrow?0:12;
            });
            var cards=new Dictionary<string,PlanetBodyCard>(StringComparer.Ordinal);var epoch=controlEpoch;
            void FocusPlanet(ColonisationPlanetSnapshot target)
            {
                if(!Live||epoch!=controlEpoch||!bodies.Any(x=>ReferenceEquals(x,target)))return;
                planet=target.PlanetId;
                foreach(var pair in cards)pair.Value.SetSelected(pair.Key==planet);
                // Do not rebuild cards/markers between the first and second click.
                solOverview?.Select(planet);detail.Clear();PlanetDetails(detail,target);
            }
            void OpenPlanet(ColonisationPlanetSnapshot target)
            {
                if(!Live||epoch!=controlEpoch||!bodies.Any(x=>ReferenceEquals(x,target)))return;
                planet=target.PlanetId;Show("Planet");
            }
            solOverview=new ReviewSolOverview(bodies,p.PlanetId,solYaw,solZoom,FocusPlanet,
                (yaw,zoom)=>{if(Live&&epoch==controlEpoch){solYaw=yaw;solZoom=zoom;}},OpenPlanet,solReducedMotion,
                reduced=>{if(Live&&epoch==controlEpoch)solReducedMotion=reduced;},activation);
            visual.Add(solOverview);PlanetDetails(detail,p);
            Text(body,"⌂ Own colony / outpost · ? Colony unknown · Gold edge: selected. Survey and settlement are separate observations.");
            var strip=Row(visual);strip.name="colonisation-system-bodies";
            foreach(var disclosed in bodies)
            {
                var own=observation.Worlds.Where(x=>x.PlanetId==disclosed.PlanetId).ToArray();
                var status=own.Length==0?null:string.Join(" / ",own.Select(x=>StageLabel(x.Stage)));
                var card=new PlanetBodyCard(disclosed,status,disclosed.PlanetId==planet,()=>FocusPlanet(disclosed),()=>OpenPlanet(disclosed),activation);
                cards.Add(disclosed.PlanetId,card);strip.Add(card);
            }
        }
        private static string StageLabel(ColonyEstablishmentStage stage)=>stage==ColonyEstablishmentStage.EmptyOutpost?"Empty outpost":stage==ColonyEstablishmentStage.Founding?"Founding camp":stage.ToString();
        private Foldout PlanetSection(VisualElement parent,string id,string label,bool initiallyOpen)
        {
            var key=planet+"/"+id;var epoch=controlEpoch;
            var fold=new Foldout{name="colonisation-planet-"+id,text=label,value=planetSections.TryGetValue(key,out var open)?open:initiallyOpen};
            fold.style.flexShrink=0;parent.Add(fold);
            fold.RegisterValueChangedCallback(e=>{if(Live&&epoch==controlEpoch&&ReferenceEquals(e.target,fold))planetSections[key]=e.newValue;});return fold;
        }
        private static string ReadableKey(string? key)
        {
            if(string.IsNullOrWhiteSpace(key))return "Unknown";
            var value=key!.Substring(key.LastIndexOf('.')+1).Replace('-',' ').Replace('_',' ');
            return value.Length==0?"Unknown":char.ToUpperInvariant(value[0])+value.Substring(1);
        }
        private void PlanetDetails(VisualElement parent,ColonisationPlanetSnapshot p)
        {
            Text(parent,p.DisplayName,"colonisation-focused-planet-name").style.fontSize=24;
            Text(parent,PlanetVisualProfile.From(p).Caption);
            if(page=="Planets"&&solOverview!=null)
            {
                var portrait=new Image{name="colonisation-focused-planet-portrait",scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
                portrait.style.height=220;portrait.style.flexShrink=0;parent.Add(portrait);
                var portraitStatus=Text(parent,"Selected-body preview unavailable.","colonisation-portrait-unavailable");
                portraitStatus.style.display=DisplayStyle.None;solOverview.BindPortrait(portrait,portraitStatus);
            }
            var ownWorlds=observation.Worlds.Where(x=>x.PlanetId==p.PlanetId).ToArray();
            if(ownWorlds.Length==0)Text(parent,"Colony presence, population, production and build queue: unknown. No own-world record is not proof of an uncolonised body.");
            foreach(var own in ownWorlds)
            {
                Text(parent,own.Name+" · "+StageLabel(own.Stage),"colonisation-colony-"+own.Id);
                Text(parent,"Population: "+own.Population+" people");
                Text(parent,"Industry "+own.Industry+" / week · Local Research "+own.Research+" / week");
                Text(parent,"Food produced "+own.FoodOutput+" / week · Consumed "+(own.FoodDemand.HasValue?own.FoodDemand.Value.ToString():"unknown")+" / week");
                Text(parent,"Net food "+(own.LocalFoodBalance.HasValue?(own.LocalFoodBalance.Value>=0?"+":"")+own.LocalFoodBalance.Value:"unknown")+" / week · Stored "+own.Food);
                var queue=PlanetSection(parent,"queue-"+own.Id,"Build queue · "+own.Name,true);
                var orders=observation.Production.Where(x=>x.ColonyId==own.Id).ToArray();
                if(orders.Length==0)Text(queue,"No queued orders in this observation.");
                for(int i=0;i<orders.Length;i++)
                {
                    var order=orders[i];
                    var icon=order.Kind==ColonisationProductionKind.PublishedShip?"◇":order.Kind==ColonisationProductionKind.Infrastructure?"▣":"□";
                    Text(queue,(i+1)+". "+icon+" "+order.Name+" · "+order.Status);
                    Text(queue,order.CompletedIndustry+" / "+order.RequiredIndustry+" Industry");
                    if(order.RequiredIndustry>0)queue.Add(new ProgressBar{lowValue=0,highValue=100,value=Mathf.Clamp01((float)((double)order.CompletedIndustry/order.RequiredIndustry))*100});
                }
            }
            var environment=PlanetSection(parent,"environment",p.Surveyed?"Environment · "+PlanetVisualProfile.From(p).Caption:"Environment · unknown",false);
            if(p.Surveyed&&p.PlanetClass!=null)
                Text(environment,ReadableKey(p.PlanetClass)+" · "+ReadableKey(p.Atmosphere)+"\n"+
                    (environmentScale.HasValue?"Gravity "+((double)p.Gravity/environmentScale.Value).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" g · Pressure "+((double)p.Pressure/environmentScale.Value).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" (reference scale)":
                        "Gravity "+p.Gravity+" · Pressure "+p.Pressure+" (measurement units unavailable)")+" · Radiation index "+p.Radiation);
            else Text(environment,"Environment not disclosed. Missing values are unknown, not zero.");
            var resources=PlanetSection(parent,"resources","Resources · initial environment record",false);
            if(p.Surveyed&&p.PlanetClass!=null)
                Text(resources,"Initial deposits: "+(p.InitialDeposits.Count==0?"none recorded":string.Join(", ",p.InitialDeposits.Select(r=>MaterialName(r.ResourceDefinitionId)+" "+r.Amount)))+". Observation date unavailable; current remaining stock and access require campaign validation.");
            else Text(resources,"Deposits unknown.");
            Text(parent,p.KnownControl.HasValue?"Last observed control: "+p.KnownControl+(p.KnownActor==null?"":" · "+p.KnownActor)+" · week "+(p.ControlObservedTurn?.ToString()??"unknown"):"Last observed control: unknown");
            if(p.ActiveSurveyId!=null)
            {
                var survey=observation.Surveys.FirstOrDefault(x=>x.Id==p.ActiveSurveyId&&x.PlanetId==p.PlanetId);
                var assigned=survey==null?null:activities.Ships.FirstOrDefault(x=>x.ShipId==survey.ShipId);
                Text(parent,"Survey in progress · "+p.CompletedSurveyWeeks+" completed weeks · "+(assigned?.DisplayName??"Ship assignment unavailable"));
                var indeterminate=new VisualElement{name="colonisation-survey-indeterminate",tooltip="Required duration unavailable; no percentage or ETA"};
                indeterminate.style.height=5;indeterminate.style.backgroundColor=new Color(.16f,.24f,.32f);
                var segment=new VisualElement{pickingMode=PickingMode.Ignore};segment.style.width=18;segment.style.height=5;segment.style.backgroundColor=new Color(.86f,.65f,.35f);indeterminate.Add(segment);parent.Add(indeterminate);
                Text(parent,"Survey in progress · duration unavailable");
            }
            else
            {
                var completed=observation.Surveys.Any(s=>s.PlanetId==p.PlanetId&&s.Stage==PlanetSurveyStage.Completed);
                Text(parent,completed?"Survey complete":p.Surveyed?"Environment known · no completed survey recorded":"Not surveyed");
                foreach(var report in observation.Surveys.Where(x=>x.PlanetId==p.PlanetId))Text(parent,"Survey: "+report.Stage+" · "+report.Weeks+" completed weeks");
            }
        }
        private void PlanetInterface()
        {
            Button(body,"Back to system","colonisation-planet-back",()=>Show("Planets"));
            var p=planets.FirstOrDefault(x=>x.PlanetId==planet);
            if(p==null){Text(body,"This planet is no longer disclosed.");return;}
            var context=Row(body);context.name="planet-detail-context";context.style.flexWrap=Wrap.NoWrap;
            var portrait=new ReviewPlanetPortrait(p);planetPortrait=portrait;context.Add(portrait);
            var detail=new VisualElement{name="planet-detail-facts"};detail.style.minWidth=0;detail.style.flexGrow=1;context.Add(detail);
            context.RegisterCallback<GeometryChangedEvent>(e=>{
                if(!ReferenceEquals(e.target,context))return;
                var narrow=e.newRect.width<860;
                context.style.flexDirection=narrow?FlexDirection.Column:FlexDirection.Row;
                portrait.style.width=narrow?new StyleLength(Length.Percent(100)):new StyleLength(380);
            });
            PlanetDetails(detail,p);
            var own=observation.Worlds.Where(x=>x.PlanetId==p.PlanetId).ToArray();
            foreach(var colony in own)Button(body,"Manage "+colony.Name,"colonisation-planet-colony-"+colony.Id,()=>{world=colony.Id;Show("Worlds");});
            var localShips=systemNodes==null?Array.Empty<ActivityOwnShipDto>():activities.Ships
                .Where(x=>x.Stationary&&systemNodes[p.SystemId].Contains(x.NodeId,StringComparer.Ordinal)).ToArray();
            var assignedShips=observation.Surveys.Where(s=>s.Stage==PlanetSurveyStage.Active).Select(s=>s.ShipId)
                .Concat(activities.Findings.Where(f=>f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim).Select(f=>f.ShipId)).ToHashSet(StringComparer.Ordinal);
            var completed=observation.Surveys.Any(s=>s.PlanetId==p.PlanetId&&s.Stage==PlanetSurveyStage.Completed);
            var needsSurvey=p.ActiveSurveyId==null&&!completed;
            if(p.ActiveSurveyId is string activeSurvey)
                Button(body,"Review cancel survey","colonisation-survey-cancel",()=>{
                    var command=Command(ColonisationAction.CancelPlanetSurvey);command.CancelSurvey=new CancelPlanetSurveyDto{SurveyId=activeSurvey};
                    Check(command,"Cancel this survey; completed work is discarded.");
                });
            if(localShips.Length>0)
            {
                Text(body,"Your stationary ships here");var ships=Row(body);
                foreach(var local in localShips)
                {
                    var card=ConstructionCard(ships,local.DisplayName,ConstructionCardArt.Ship(local.HullVersionId));card.style.marginRight=6;
                    var select=Button(card,"Select ship","colonisation-survey-select-"+local.ShipId,()=>{surveyShip=local.ShipId;Show("Planet");});
                    if(local.ShipId==surveyShip)select.text="Selected ship";
                    if(!needsSurvey)continue;
                    if(local.StudyWork<1)Text(card,"Survey capability unavailable.");
                    else if(assignedShips.Contains(local.ShipId))Text(card,"Already assigned to survey or study.");
                    else Button(card,"Survey "+p.DisplayName,"colonisation-survey-start-"+local.ShipId,()=>StartSurvey(p,local));
                }
            }
            else if(needsSurvey)Text(body,"Bring a survey-capable ship to this system.");
            var presence=observation.Presence.FirstOrDefault(x=>x.SystemId==p.SystemId);
            if(presence!=null)Text(body,presence.Caption+" · observed week "+presence.ObservationTurn+". Unobserved bodies may have other governments.");
        }
        private void StartSurvey(ColonisationPlanetSnapshot target,ActivityOwnShipDto local)
        {
            surveyShip=local.ShipId;
            var command=Command(ColonisationAction.StartPlanetSurvey);
            command.StartSurvey=new PlanetSurveyOrderDto{ShipId=local.ShipId,PlanetId=target.PlanetId};
            Send(command);
        }

        private void Transports()
        {
            foreach(var landing in observation.LatestDeployments)
            {
                var outcome=landing.Outcome==DeploymentBoundaryOutcome.Interrupted?"Landing did not complete":landing.Outcome==DeploymentBoundaryOutcome.Disputed?"Landing disputed":"Landing completed";
                var message=outcome+" in week "+landing.Turn+"."+(landing.StillAboard?" Settlers remain aboard; check food and transport status before another order.":" Check the current transport and world status.");
                var label=Text(body,message);label.name="colonisation-deployment-result-"+landing.IntentId;
            }
            var history=new Foldout{text="Own expedition history",value=observation.Transports.Count==0,name="colonisation-expedition-history"};history.style.flexShrink=0;history.style.fontSize=16;body.Add(history);
            foreach(var report in observation.Reports)Text(history,report.Stage+" \u00b7 deaths "+report.Deaths+", people transferred "+report.PopulationTransferred+", food consumed "+report.FoodConsumed+", lost "+report.FoodLost+". "+report.Report);
            if(observation.Reports.Count==0)Text(history,"No expedition manifests recorded yet.");
            Picker(body,"Owned transport",observation.Transports,x=>x.ShipId,x=>x.Name,ship,id=>{ship=id;Show(page);},"colonisation-transport-selector");
            var t=observation.Transports.FirstOrDefault(x=>x.ShipId==ship);if(t==null){Text(body,"Build the ready colony transport at an established owned world.");Button(body,"Open construction","transport-open-construction",()=>Show("Worlds"));return;}
            var manifest=new VisualElement();manifest.AddToClassList("transport-manifest-summary");body.Add(manifest);
            Text(manifest,t.Name+" · R"+t.BlueprintRevision+" · "+(t.Stationary?"Stationary":"Travelling")).AddToClassList("console-section");
            Text(manifest,"People aboard "+t.Population+" / "+t.PassengerCapacity+" · Food aboard "+t.Food+" / "+t.FoodCapacity);
            Text(manifest,"Materials: "+Materials(t.Cargo)+" · Capacity "+t.MaterialCapacity+". Use the Map to issue an ordinary known route.");
            var manifestReport=new Foldout{text="Current transport report",value=false};manifest.Add(manifestReport);Text(manifestReport,t.Report);
            Picker(body,"Owned source / receiving world",observation.Worlds,x=>x.Id,x=>x.Name,world,id=>{world=id;Show(page);},"colonisation-transport-world");
            var w=observation.Worlds.FirstOrDefault(x=>x.Id==world);if(w==null)return;
            if(transportDraft==null||!transportDraft.Matches(w,t))transportDraft=new TransportDraft(w,t);
            var draft=transportDraft;var draftEpoch=controlEpoch;
            bool OwnDraft()=>Live&&draftEpoch==controlEpoch&&ReferenceEquals(transportDraft,draft);
            if(!w.Cohorts.Any(c=>c.Id==draft.Cohort))draft.Cohort=w.Cohorts.FirstOrDefault()?.Id??"";
            var resourceIds=(t.ManifestId==null?w.Stocks:t.Cargo).Select(x=>x.ResourceDefinitionId).Distinct(StringComparer.Ordinal).ToList();
            foreach(var id in draft.Materials.Keys.Where(id=>!resourceIds.Contains(id,StringComparer.Ordinal)).ToList())draft.Materials.Remove(id);
            foreach(var id in resourceIds)if(!draft.Materials.ContainsKey(id))draft.Materials.Add(id,0);
            Text(body,"Embark uses 100 actual settlers and one local kit; at least 500 people must remain, with no more than a quarter of the selected source cohort embarking. Choose 8–20 food for settlers. Carried food is finite; blocked journeys can cause shortages and deaths.");
            var quantities=Row(body);quantities.AddToClassList("transport-quantities");
            var food=new IntegerField("Food to transfer"){value=draft.Food,name="colonisation-food"};food.style.minHeight=44;food.style.fontSize=18;quantities.Add(food);food.RegisterValueChangedCallback(_=>{if(OwnDraft())draft.Food=food.value;});
            var materialFields=new List<(string id,IntegerField field)>();
            foreach(var resource in resourceIds)
            {var f=new IntegerField(MaterialName(resource)){value=draft.Materials[resource],name="colonisation-material-"+resource};f.style.minHeight=44;f.style.fontSize=18;quantities.Add(f);materialFields.Add((resource,f));f.RegisterValueChangedCallback(_=>{if(OwnDraft())draft.Materials[resource]=f.value;});}
            List<ResourceAmountDto> MaterialsInput()=>materialFields.Where(x=>x.field.value!=0).Select(x=>new ResourceAmountDto{ResourceDefinitionId=x.id,Amount=x.field.value}).ToList();
            if(t.ManifestId==null)
            {
                Picker(body,"Source cohort",w.Cohorts,x=>x.Id,x=>x.SpeciesName+" "+x.Population,draft.Cohort,id=>{if(OwnDraft())draft.Cohort=id;},"colonisation-cohort");
                var cohorts=body.Q<DropdownField>("colonisation-cohort");
                bool ValidCohort()=>w.Stage==ColonyEstablishmentStage.Established&&cohorts.index>=0&&cohorts.index<w.Cohorts.Count;
                var embark=Button(body,"Review embark 100 settlers + kit","colonisation-embark",()=>{
                    if(!ValidCohort()){PresentStatus("Choose an active established source world and a resident cohort before embarking settlers.");return;}
                    var cohort=w.Cohorts[cohorts.index];var c=Command(ColonisationAction.EmbarkSettlers);c.Embark=new EmbarkSettlersDto{ShipId=t.ShipId,ColonyId=w.Id,CohortId=cohort.Id,Population=100,Food=food.value,Materials=MaterialsInput()};
                    Check(c,"Move 100 actual people from the selected cohort (remaining "+(cohort.Population-100)+"), one locally manufactured kit, "+c.Embark.Food+" food and "+string.Join(", ",c.Embark.Materials!.Select(x=>MaterialName(x.ResourceDefinitionId)+" "+x.Amount))+" onto this transport. Source safeguards apply.");});
                embark.SetEnabled(ValidCohort());if(!ValidCohort())Text(body,"No eligible source cohort. Embarking settlers requires an active established owned world with residents; supply delivery and founding evacuation remain separate actions.");
                if(supportsKitFreePassengers)
                {
                    var passengers=Button(body,"Review load 100 passengers (no kit)","colonisation-load-passengers",()=>{
                        if(!ValidCohort()){PresentStatus("Choose an active established source world and a resident cohort before loading passengers.");return;}
                        var cohort=w.Cohorts[cohorts.index];var c=Command(ColonisationAction.LoadPassengers);c.Embark=new EmbarkSettlersDto{ShipId=t.ShipId,ColonyId=w.Id,CohortId=cohort.Id,Population=100,Food=food.value,Materials=MaterialsInput()};
                        Check(c,"Carry 100 actual people from this cohort (remaining "+(cohort.Population-100)+"), "+c.Embark.Food+" finite food and the selected local materials. No settlement kit is loaded. Passengers can unload only at an active established owned world; they cannot deploy or recommission.");});
                    passengers.SetEnabled(ValidCohort());
                }
                Button(body,"Review load supplies only","colonisation-load-supplies",()=>{var c=Command(ColonisationAction.LoadCarriedSupplies);c.LoadSupplies=new LoadCarriedSuppliesDto{ShipId=t.ShipId,SourceColonyId=w.Id,Food=food.value,Materials=MaterialsInput()};Check(c,"Load "+c.LoadSupplies.Food+" local food and "+string.Join(", ",c.LoadSupplies.Materials!.Select(x=>MaterialName(x.ResourceDefinitionId)+" "+x.Amount))+" without passengers or a kit.");});
                if(w.Stage==ColonyEstablishmentStage.Founding&&w.CampId!=null)Button(body,"Review founding evacuation","colonisation-evacuate",()=>{var c=Command(ColonisationAction.EvacuateColony);c.Evacuate=new EvacuateFoundingColonyDto{CampId=w.CampId,ShipId=t.ShipId};Check(c,"Evacuate the remaining founding cohort to this present transport; return passengers may unload only at an active established owned world.");});
            }
            else
            {
                Button(body,"Review unload at established world","colonisation-unload",()=>{var c=Command(ColonisationAction.UnloadTransport);c.Unload=new UnloadSettlersDto{ManifestId=t.ManifestId!,ColonyId=w.Id};Check(c,"Unload at this actual receiving world. Passengers require an active established owned colony.");});
                Button(body,"Review deliver carried supplies","colonisation-deliver",()=>{var c=Command(ColonisationAction.DeliverCarriedSupplies);c.Deliver=new DeliverCarriedSuppliesDto{ManifestId=t.ManifestId!,ColonyId=w.Id,Food=food.value,Materials=MaterialsInput()};Check(c,"Transfer "+c.Deliver.Food+" carried food and "+string.Join(", ",c.Deliver.Materials!.Select(x=>MaterialName(x.ResourceDefinitionId)+" "+x.Amount))+" to this present owned world; no remote delivery.");});
                Picker(body,"Surveyed destination",planets.Where(x=>x.Surveyed).ToList(),x=>x.PlanetId,x=>x.DisplayName,planet,id=>planet=id,"colonisation-deploy-planet");
                Button(body,"Review deployment / recommission","colonisation-deploy",()=>{var c=Command(ColonisationAction.RequestDeployment);c.Deploy=new DeploySettlersDto{ManifestId=t.ManifestId!,PlanetId=planet};Check(c,"Request deployment. All actors are considered together after combat this week. Contested landings pause; no sibling planet is claimed.");});
                foreach(var intent in observation.Intents.Where(x=>x.ManifestId==t.ManifestId)){var i=intent;Text(body,i.Disputed?"Deployment disputed; remains aboard.":"Deployment requested for the next settlement boundary.");Button(body,"Review withdraw deployment","colonisation-withdraw",()=>{var c=Command(ColonisationAction.WithdrawDeployment);c.Withdraw=new WithdrawDeploymentDto{IntentId=i.Id};Check(c,"Withdraw this landing request; people and supplies remain on the ship.");});}
            }
            foreach(var report in observation.Reports.Where(x=>x.ShipId==t.ShipId))Text(body,report.Stage+" \u00b7 deaths "+report.Deaths+", food consumed "+report.FoodConsumed+", lost "+report.FoodLost+". "+report.Report);
        }
        internal void PresentAdmissionRejected(string text){if(disposed)return;submitted=false;PresentStatus(text);}
        internal void PresentStatus(string? text){if(!disposed&&text!=null){status.text=text;status.style.display=string.IsNullOrWhiteSpace(text)?DisplayStyle.None:DisplayStyle.Flex;}}
        internal void PresentReviewedOrder(ColonisationCommandDto command,string caption,ColonisationEligibilitySnapshot eligibility)
        {
            if(!Live||eligibility.Actor!=observation.Actor||eligibility.Turn!=observation.Turn||eligibility.Revision!=observation.Revision||command.ActorId!=observation.Actor||command.Turn!=observation.Turn||command.Revision!=observation.Revision||eligibility.Action!=command.Action)return;
            displayedReview=new ReviewState(command,caption,eligibility);command=Copy(displayedReview.Command!);
            var token=++reviewEpoch;review.Clear();review.style.display=DisplayStyle.Flex;Text(review,caption);Text(review,eligibility.Available?"Available at the observed campaign state. Confirm revalidates the order.":eligibility.Reason);
            if(eligibility.Available)Button(review,"Confirm order","colonisation-order-confirm",()=>{if(token==reviewEpoch)Send(command);});Button(review,"Back","colonisation-order-back",()=>DismissReview(token));
        }
        internal void PresentReviewedActivity(CampaignActivityCommandDto command,string caption,CampaignActivityEligibilityDto eligibility)
        {if(science!=null){science.PresentReviewedOrder(command,caption,eligibility);return;}if(!Live||eligibility.ActorId!=observation.Actor||eligibility.Turn!=observation.Turn||eligibility.Revision!=observation.Revision||command.ActorId!=observation.Actor||command.ExpectedTurn!=observation.Turn||command.ExpectedRevision!=observation.Revision)return;
         var activity=new ReviewActivitiesPanel.ReviewState(command,caption,eligibility);displayedReview=new ReviewState(activity);command=activity.Command;
         var token=++reviewEpoch;review.Clear();review.style.display=DisplayStyle.Flex;Text(review,caption);Text(review,eligibility.Accepted?"Available at this observed state.":eligibility.Reason);if(eligibility.Accepted)Button(review,"Confirm order","colonisation-order-confirm",()=>{if(token==reviewEpoch)SendActivity(command);});Button(review,"Back","colonisation-order-back",()=>DismissReview(token));}
        private void DismissReview(int token){if(token!=reviewEpoch)return;reviewEpoch++;displayedReview=null;review.Clear();review.style.display=DisplayStyle.None;}
        private void ReleaseSolOverview(){solOverview?.Dispose();solOverview=null;planetPortrait?.Dispose();planetPortrait=null;}
        public void Dispose(){if(disposed)return;disposed=true;fitConstruction=null;reviewEpoch++;displayedReview=null;currentInspector=null;science?.Dispose();science=null;knownSiteLocations=null;selectSiteOnMap=null;ReleaseHull();ReleaseSolOverview();review.Clear();}
    }
}
