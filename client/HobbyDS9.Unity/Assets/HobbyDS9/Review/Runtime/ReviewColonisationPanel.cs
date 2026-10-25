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
        private readonly Action<ColonisationCommandDto,string> reviewOrder;
        private readonly Action<ColonisationCommandDto> submit;
        private readonly Action<CampaignActivityCommandDto> submitActivity;
        private readonly Action<CampaignActivityCommandDto,string> reviewActivity;
        private readonly ScrollView body;
        private readonly VisualElement review;
        private readonly Label status;
        private ReviewActivitiesPanel? science;
        private ShipPreviewStage? hullPreview;
        private string inspectedBlueprint="";private int inspectedRevision;
        private bool disposed,submitted;private int controlEpoch,reviewEpoch;private bool inspectedOpen;
        private Foldout? currentInspector;
        private string page="Worlds",world="",ship="",planet="",surveyShip="";
        internal sealed class Selection
        {internal readonly string Page,World,Ship,Planet,SurveyShip,Blueprint;internal readonly int Revision;internal readonly bool InspectorOpen;internal readonly ReviewActivitiesPanel.Selection? Science;internal Selection(string page,string world,string ship,string planet,string surveyShip,string blueprint,int revision,bool open,ReviewActivitiesPanel.Selection? science){Page=page;World=world;Ship=ship;Planet=planet;SurveyShip=surveyShip;Blueprint=blueprint;Revision=revision;InspectorOpen=open;Science=science;}}
        internal Selection? CaptureSelection()=>disposed?null:new Selection(page,world,ship,planet,surveyShip,inspectedBlueprint,inspectedRevision,this.Q<Foldout>("colonisation-hull-inspector")?.value??inspectedOpen,science?.CaptureSelection());
        internal void RestoreSelection(Selection? saved)
        {
            if(disposed||saved==null)return;
            if(observation.Worlds.Any(x=>x.Id==saved.World))world=saved.World;
            if(observation.Transports.Any(x=>x.ShipId==saved.Ship))ship=saved.Ship;
            if(planets.Any(x=>x.PlanetId==saved.Planet))planet=saved.Planet;
            if(activities.Ships.Any(x=>x.ShipId==saved.SurveyShip))surveyShip=saved.SurveyShip;
            inspectedBlueprint=saved.Blueprint;inspectedRevision=saved.Revision;inspectedOpen=saved.InspectorOpen;Show(saved.Page);science?.RestoreSelection(saved.Science);
        }
        public ReviewColonisationPanel(ColonisationCampaignSnapshot observation,IReadOnlyList<ColonisationPlanetSnapshot> planets,CampaignActivityObservationDto activities,
            Action<ColonisationCommandDto,string> reviewOrder,Action<ColonisationCommandDto> submit,Action<CampaignActivityCommandDto,string> reviewActivity,Action<CampaignActivityCommandDto> submitActivity)
        {
            this.observation=observation;this.planets=planets;this.activities=activities;this.reviewOrder=reviewOrder;this.submit=submit;this.reviewActivity=reviewActivity;this.submitActivity=submitActivity;
            name="colonisation-panel";style.flexGrow=1;style.minHeight=0;style.minWidth=0;
            world=observation.Worlds.FirstOrDefault()?.Id??"";ship=observation.Transports.FirstOrDefault()?.ShipId??"";planet=planets.FirstOrDefault()?.PlanetId??"";surveyShip=activities.Ships.FirstOrDefault(s=>s.StudyWork>0)?.ShipId??"";
            var tabs=Row(this);foreach(var name in new[]{"Worlds","Planets","Transports","Science"}){var captured=name;Button(tabs,name,"colonisation-tab-"+name,()=>Show(captured),true);}
            body=new ScrollView(ScrollViewMode.Vertical){name="colonisation-scroll"};body.style.flexGrow=1;body.style.minHeight=0;Add(body);
            review=new VisualElement{name="colonisation-order-review"};review.style.flexShrink=0;review.style.maxHeight=220;review.style.display=DisplayStyle.None;Add(review);
            status=Text(this,"Provisional colony playtest. Construction uses Industry and delivered materials; credits fund expedition choices.","colonisation-command-status");status.style.flexShrink=0;
            RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,this))Dispose();});Show("Worlds");
        }
        private bool Live=>!disposed&&!submitted;
        private static Label Text(VisualElement p,string text,string id="")
        {var l=new Label(text){name=id};l.style.whiteSpace=WhiteSpace.Normal;l.style.fontSize=16;l.style.flexShrink=0;l.style.marginBottom=6;p.Add(l);return l;}
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
        private static string MaterialName(string id)=>id=="resource.dilithium"?"Dilithium":id=="resource.settlement-kit"?"Settlement kits":"Material";
        private static string Materials(IEnumerable<ResourceAmountSnapshot> rows)=>string.Join(", ",rows.Where(x=>x.Amount!=0).Select(x=>MaterialName(x.ResourceDefinitionId)+" "+x.Amount));
        private ColonisationCommandDto Command(ColonisationAction action)=>new ColonisationCommandDto{ActorId=observation.Actor,Turn=observation.Turn,Revision=observation.Revision,Action=action,CommandId="command.colony.ui."+Guid.NewGuid().ToString("N")};
        private CampaignActivityCommandDto Activity(CampaignActivityCommandKind kind,string target)=>new CampaignActivityCommandDto{ActorId=observation.Actor,ExpectedTurn=observation.Turn,ExpectedRevision=observation.Revision,Kind=kind,TargetId=target,CommandId="command.colony.activity."+Guid.NewGuid().ToString("N")};
        private void Check(ColonisationCommandDto command,string caption){if(Live)reviewOrder(command,caption);}
        private void Send(ColonisationCommandDto command){if(!Live)return;submitted=true;submit(command);}
        private void SendActivity(CampaignActivityCommandDto command){if(!Live)return;submitted=true;submitActivity(command);}
        private void Show(string selected)
        {
            if(disposed)return;controlEpoch++;reviewEpoch++;currentInspector=null;page=new[]{"Worlds","Planets","Transports","Science"}.Contains(selected)?selected:"Worlds";
            science?.Dispose();science=null;ReleaseHull();body.Clear();review.Clear();review.style.display=DisplayStyle.None;
            if(page=="Worlds")Worlds();else if(page=="Planets")Planets();else if(page=="Transports")Transports();else
            {science=new ReviewActivitiesPanel(activities,reviewActivity,submitActivity,true);body.Add(science);}
        }
        private void Worlds()
        {
            Picker(body,"Owned world",observation.Worlds,x=>x.Id,x=>x.Name,world,id=>{world=id;Show(page);},"colonisation-world-selector");
            var w=observation.Worlds.FirstOrDefault(x=>x.Id==world);if(w==null){Text(body,"No owned worlds observed.");return;}
            Text(body,w.Name+" \u00b7 "+w.Stage+" \u00b7 "+w.Population+" people \u00b7 "+w.Food+" stored food","colonisation-world-facts");
            Text(body,"Current weekly output: Industry "+w.Industry+", Research "+w.Research+", Food "+w.FoodOutput+". Local stock: "+(Materials(w.Stocks)==""?"none":Materials(w.Stocks)));
            if(w.Stage==ColonyEstablishmentStage.Founding)Text(body,"Establishment: "+w.EstablishmentWork+" work, "+w.FedWeeks+" fed weeks. Shortage "+w.ShortageWeeks+" weeks; deaths "+w.Deaths+". "+w.Report);
            if(w.Stage==ColonyEstablishmentStage.EmptyOutpost)Text(body,"Empty outpost: no production or freight. A surveyed, supplied settlement expedition must recommission it.");
            var row=Row(body);row.style.flexWrap=Wrap.NoWrap;
            var catalogue=new ScrollView(){name="colonisation-catalogue"};catalogue.style.flexGrow=2;catalogue.style.flexBasis=0;catalogue.style.minWidth=0;catalogue.style.height=300;row.Add(catalogue);
            var queue=new ScrollView(){name="colonisation-queue"};queue.style.flexGrow=1;queue.style.flexBasis=0;queue.style.minWidth=0;queue.style.height=300;row.Add(queue);Text(queue,"Own world queue");
            int index=0;
            foreach(var kind in new[]{ActivityProductionKind.PublishedShip,ActivityProductionKind.Infrastructure})
            {
                Text(catalogue,kind==ActivityProductionKind.PublishedShip?"Ships":"Planetary buildings","colonisation-section-"+kind);
                foreach(var option in activities.BuildOptions.Where(x=>x.Kind==kind))
                {
                var selected=option;var card=new VisualElement();card.AddToClassList("console-panel");card.style.flexShrink=0;catalogue.Add(card);Text(card,selected.DisplayName+" "+(selected.BlueprintRevision>0?"R"+selected.BlueprintRevision:"")+" \u00b7 "+selected.Cost.Industry+" Industry");
                Text(card,"Materials: "+string.Join(", ",selected.Cost.Resources!.Select(r=>MaterialName(r.ResourceDefinitionId)+" "+r.Amount)));
                var button=Button(card,"+ Add "+selected.DisplayName,"colonisation-build-"+index++,()=>{var c=Activity(selected.Kind==ActivityProductionKind.PublishedShip?CampaignActivityCommandKind.EnqueueShip:CampaignActivityCommandKind.EnqueueStructure,w.Id);c.DefinitionId=selected.DefinitionId;c.BlueprintRevision=selected.BlueprintRevision;SendActivity(c);});button.SetEnabled(selected.Supported&&w.Stage==ColonyEstablishmentStage.Established);if(!selected.Supported)Text(card,selected.Reason);
                var queued=observation.Production.Count(x=>x.ColonyId==w.Id&&x.DefinitionId==selected.DefinitionId&&x.BlueprintRevision==selected.BlueprintRevision);
                var built=selected.Kind==ActivityProductionKind.PublishedShip?observation.OwnShipSources.Count(x=>x.BlueprintId==selected.DefinitionId&&x.Revision==selected.BlueprintRevision):activities.Structures.Count(x=>x.ColonyId==w.Id&&x.DefinitionId==selected.DefinitionId);
                Text(card,selected.Kind==ActivityProductionKind.PublishedShip?"Own living ships of this published revision: "+built+"; queued at this world: "+queued:"Built at this world: "+built+"; queued: "+queued);
                if(selected.Kind==ActivityProductionKind.Infrastructure&&(built>0||queued>0)){button.SetEnabled(false);Text(card,"Unique building: already built or queued.");}
                if(selected.Kind==ActivityProductionKind.PublishedShip)Button(card,"Inspect "+selected.DisplayName,"colonisation-inspect-"+index,()=>{inspectedBlueprint=selected.DefinitionId;inspectedRevision=selected.BlueprintRevision;inspectedOpen=true;InspectHull(body,selected);});
                }
            }
            Text(catalogue,"Special projects","colonisation-section-projects");
            Text(catalogue,"Settlement equipment: "+observation.KitIndustry+" Industry; "+Materials(observation.KitResources));
            Button(catalogue,"+ Settlement kit","colonisation-build-kit",()=>{var c=Command(ColonisationAction.EnqueueKit);c.EnqueueKit=new EnqueueSettlementKitDto{ColonyId=w.Id};Send(c);}).SetEnabled(w.Stage==ColonyEstablishmentStage.Established);
            Text(catalogue,"System improvements","colonisation-section-system");Text(catalogue,"No system improvement recipes are implemented in this playtest.");
            foreach(var order in observation.Production.Where(x=>x.ColonyId==w.Id))
            {Text(queue,order.Name+" \u00b7 "+order.Status+" "+order.CompletedIndustry+"/"+order.RequiredIndustry);Text(queue,"Consumed: "+Materials(order.Consumed));var captured=order;Button(queue,"Cancel order","colonisation-queue-cancel-"+captured.Id,()=>reviewActivity(Activity(CampaignActivityCommandKind.CancelProduction,captured.Id),"Cancel construction; consumed materials are not refunded."));}
            if(!observation.Production.Any(x=>x.ColonyId==w.Id))Text(queue,"No queued orders.");
            var inspected=activities.BuildOptions.FirstOrDefault(x=>x.DefinitionId==inspectedBlueprint&&x.BlueprintRevision==inspectedRevision);if(inspected!=null)InspectHull(body,inspected);
        }
        private void ReleaseHull(){hullPreview?.Dispose();hullPreview=null;}
        private void InspectHull(VisualElement parent,ActivityBuildOptionDto option)
        {
            currentInspector=null;ReleaseHull();parent.Q("colonisation-hull-inspector")?.RemoveFromHierarchy();
            var fold=new Foldout{text="Inspect "+option.DisplayName+" R"+option.BlueprintRevision,value=inspectedOpen,name="colonisation-hull-inspector"};fold.style.flexShrink=0;fold.style.fontSize=16;parent.Add(fold);
            currentInspector=fold;var inspectorEpoch=controlEpoch;
            bool OwnInspector()=>Live&&inspectorEpoch==controlEpoch&&ReferenceEquals(currentInspector,fold);
            Text(fold,"Exact published hull art. This image supplies no statistics or unlocks.");
            var image=new Image{name="colonisation-hull-image",scaleMode=ScaleMode.ScaleToFit};image.style.height=180;image.style.minHeight=180;image.style.flexShrink=0;fold.Add(image);
            var label=Text(fold,"Expand to inspect exact hull art.");
            void Refresh()
            {
                if(!OwnInspector())return;
                if(!fold.value||fold.panel==null){image.image=null;ReleaseHull();return;}
                if(ActivityHullCatalog.Find(option.HullVersionId)==null){label.text="No exact hull art binding is available.";return;}
                ReleaseHull();hullPreview=new ShipPreviewStage(512,320);hullPreview.ShowActivityHull(option.HullVersionId);image.image=hullPreview.Texture;label.text=hullPreview.Label;
            }
            fold.RegisterValueChangedCallback(_=>{if(!OwnInspector())return;inspectedOpen=fold.value;Refresh();});fold.RegisterCallback<AttachToPanelEvent>(_=>Refresh());
            Button(fold,"Rotate hull","colonisation-hull-orbit",()=>hullPreview?.OrbitReference(80,26));
        }
        private void Planets()
        {
            Text(body,"Only discovered bodies are listed. Survey reveals environment and the last observed control; remote control can change.");
            Picker(body,"Discovered planet",planets,x=>x.PlanetId,x=>x.DisplayName,planet,id=>{planet=id;Show(page);},"colonisation-planet-selector");
            var p=planets.FirstOrDefault(x=>x.PlanetId==planet);if(p==null){Text(body,"Travel to a charted system to discover its bodies.");return;}
            Text(body,p.DisplayName+" \u00b7 "+(p.Surveyed?"Survey complete":"Not surveyed"));
            if(p.PlanetClass!=null){var e=p;Text(body,e.PlanetClass+"; "+e.Atmosphere+"; gravity "+e.Gravity+", pressure "+e.Pressure+", radiation "+e.Radiation);Text(body,"Frozen initial deposits: "+string.Join(", ",e.InitialDeposits.Select(r=>MaterialName(r.ResourceDefinitionId)+" "+r.Amount))+". Remaining deposits and access are rechecked by the campaign.");}
            if(p.KnownControl!=null)Text(body,"Last observed control: "+p.KnownControl+" \u00b7 week "+p.ControlObservedTurn+". This is not a claim of system sovereignty.");
            Picker(body,"Survey ship",activities.Ships,x=>x.ShipId,x=>x.DisplayName,surveyShip,id=>surveyShip=id,"colonisation-survey-ship");
            Button(body,"Review planetary survey","colonisation-survey-review",()=>{var c=Command(ColonisationAction.StartPlanetSurvey);c.StartSurvey=new PlanetSurveyOrderDto{ShipId=surveyShip,PlanetId=p.PlanetId};Check(c,"Begin planetary survey with this ship.");});
            foreach(var survey in observation.Surveys.Where(s=>s.PlanetId==p.PlanetId))
            {Text(body,survey.Stage+" \u00b7 "+survey.Weeks+" completed weeks");if(survey.Stage==PlanetSurveyStage.Active){var s=survey;Button(body,"Review cancel survey","colonisation-survey-cancel",()=>{var c=Command(ColonisationAction.CancelPlanetSurvey);c.CancelSurvey=new CancelPlanetSurveyDto{SurveyId=s.Id};Check(c,"Cancel survey; completed work is discarded and no survey result is awarded.");});}}
            var presence=observation.Presence.FirstOrDefault(x=>x.SystemId==p.SystemId);if(presence!=null)Text(body,presence.Caption+" \u00b7 observed week "+presence.ObservationTurn+". Unobserved bodies may have other governments.");
        }
        private void Transports()
        {
            var history=new Foldout{text="Own expedition history",value=observation.Transports.Count==0,name="colonisation-expedition-history"};history.style.flexShrink=0;history.style.fontSize=16;body.Add(history);
            foreach(var report in observation.Reports)Text(history,report.Stage+" \u00b7 deaths "+report.Deaths+", people transferred "+report.PopulationTransferred+", food consumed "+report.FoodConsumed+", lost "+report.FoodLost+". "+report.Report);
            if(observation.Reports.Count==0)Text(history,"No expedition manifests recorded yet.");
            Picker(body,"Owned transport",observation.Transports,x=>x.ShipId,x=>x.Name,ship,id=>{ship=id;Show(page);},"colonisation-transport-selector");
            var t=observation.Transports.FirstOrDefault(x=>x.ShipId==ship);if(t==null){Text(body,"Build the ready colony transport at an established owned world.");foreach(var report in observation.Reports)Text(body,report.Stage+" \u00b7 deaths "+report.Deaths+", food consumed "+report.FoodConsumed+". "+report.Report);return;}
            Text(body,t.Name+" \u00b7 frozen R"+t.BlueprintRevision+" \u00b7 "+(t.Stationary?"stationary":"travelling"));Text(body,"Capacity: "+t.PassengerCapacity+" people, "+t.FoodCapacity+" food, "+t.MaterialCapacity+" materials. Use the Map to issue an ordinary known route.");
            Text(body,"Aboard: "+t.Population+" people, "+t.Food+" food; "+Materials(t.Cargo)+". "+t.Report);
            Picker(body,"Owned source / receiving world",observation.Worlds,x=>x.Id,x=>x.Name,world,id=>{world=id;Show(page);},"colonisation-transport-world");
            var w=observation.Worlds.FirstOrDefault(x=>x.Id==world);if(w==null)return;
            Text(body,"Embark uses 100 actual settlers and one local kit; at least 500 people must remain, with no more than a quarter of the selected source cohort embarking. Choose 8–20 food for settlers. Carried food is finite; blocked journeys can cause shortages and deaths.");
            var food=new IntegerField("Food to transfer"){value=t.ManifestId==null?20:checked((int)t.Food),name="colonisation-food"};food.style.minHeight=44;food.style.fontSize=16;body.Add(food);
            var materialFields=new List<(string id,IntegerField field)>();
            foreach(var resource in (t.ManifestId==null?w.Stocks:t.Cargo).Select(x=>x.ResourceDefinitionId).Distinct(StringComparer.Ordinal))
            {var f=new IntegerField(MaterialName(resource)){value=0};f.style.minHeight=44;f.style.fontSize=16;body.Add(f);materialFields.Add((resource,f));}
            List<ResourceAmountDto> MaterialsInput()=>materialFields.Where(x=>x.field.value!=0).Select(x=>new ResourceAmountDto{ResourceDefinitionId=x.id,Amount=x.field.value}).ToList();
            if(t.ManifestId==null)
            {
                Picker(body,"Source cohort",w.Cohorts,x=>x.Id,x=>x.SpeciesName+" "+x.Population,w.Cohorts.FirstOrDefault()?.Id??"",_=>{},"colonisation-cohort");
                var cohorts=body.Q<DropdownField>("colonisation-cohort");
                bool ValidCohort()=>w.Stage==ColonyEstablishmentStage.Established&&cohorts.index>=0&&cohorts.index<w.Cohorts.Count;
                var embark=Button(body,"Review embark 100 settlers + kit","colonisation-embark",()=>{
                    if(!ValidCohort()){PresentStatus("Choose an active established source world and a resident cohort before embarking settlers.");return;}
                    var cohort=w.Cohorts[cohorts.index];var c=Command(ColonisationAction.EmbarkSettlers);c.Embark=new EmbarkSettlersDto{ShipId=t.ShipId,ColonyId=w.Id,CohortId=cohort.Id,Population=100,Food=food.value,Materials=MaterialsInput()};
                    Check(c,"Move 100 actual people from the selected cohort (remaining "+(cohort.Population-100)+"), one locally manufactured kit, "+c.Embark.Food+" food and "+string.Join(", ",c.Embark.Materials!.Select(x=>MaterialName(x.ResourceDefinitionId)+" "+x.Amount))+" onto this transport. Source safeguards apply.");});
                embark.SetEnabled(ValidCohort());if(!ValidCohort())Text(body,"No eligible source cohort. Embarking settlers requires an active established owned world with residents; supply delivery and founding evacuation remain separate actions.");
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
        internal void PresentStatus(string? text){if(!disposed&&text!=null)status.text=text;}
        internal void PresentReviewedOrder(ColonisationCommandDto command,string caption,ColonisationEligibilitySnapshot eligibility)
        {
            if(!Live||eligibility.Actor!=observation.Actor||eligibility.Turn!=observation.Turn||eligibility.Revision!=observation.Revision||command.ActorId!=observation.Actor||command.Turn!=observation.Turn||command.Revision!=observation.Revision||eligibility.Action!=command.Action)return;
            var token=++reviewEpoch;review.Clear();review.style.display=DisplayStyle.Flex;Text(review,caption);Text(review,eligibility.Available?"Available at the observed campaign state. Confirm revalidates the order.":eligibility.Reason);
            if(eligibility.Available)Button(review,"Confirm order","colonisation-order-confirm",()=>{if(token==reviewEpoch)Send(command);});Button(review,"Back","colonisation-order-back",()=>DismissReview(token));
        }
        internal void PresentReviewedActivity(CampaignActivityCommandDto command,string caption,CampaignActivityEligibilityDto eligibility)
        {if(science!=null){science.PresentReviewedOrder(command,caption,eligibility);return;}if(!Live||eligibility.ActorId!=observation.Actor||eligibility.Turn!=observation.Turn||eligibility.Revision!=observation.Revision)return;
         var token=++reviewEpoch;review.Clear();review.style.display=DisplayStyle.Flex;Text(review,caption);Text(review,eligibility.Accepted?"Available at this observed state.":eligibility.Reason);if(eligibility.Accepted)Button(review,"Confirm order","colonisation-order-confirm",()=>{if(token==reviewEpoch)SendActivity(command);});Button(review,"Back","colonisation-order-back",()=>DismissReview(token));}
        private void DismissReview(int token){if(token!=reviewEpoch)return;reviewEpoch++;review.Clear();review.style.display=DisplayStyle.None;}
        public void Dispose(){if(disposed)return;disposed=true;reviewEpoch++;currentInspector=null;science?.Dispose();science=null;ReleaseHull();review.Clear();}
    }
}
