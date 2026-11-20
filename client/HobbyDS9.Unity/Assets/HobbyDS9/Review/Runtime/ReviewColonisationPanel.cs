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
    public sealed partial class ReviewColonisationPanel:VisualElement,IDisposable
    {
        private readonly ColonisationCampaignSnapshot observation;
        private readonly IReadOnlyList<ColonisationPlanetSnapshot> planets;
        private readonly CampaignActivityObservationDto activities;
        private readonly ILookup<string,string>? systemNodes;
        private readonly CampaignStaticAstronomySnapshot? astronomy;
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
        private bool solInnerFrame,solFocusFrame;
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
        {internal readonly string Page,World,Ship,Planet,SurveyShip,Blueprint,BeltSurveyShip,BeltSponsor;internal readonly string? SystemCaption,SelectedSystem;internal readonly BodySelectionKey Body;internal readonly int Revision;internal readonly bool InspectorOpen;internal readonly float SolYaw,SolZoom;internal readonly bool ReducedMotion,InnerFrame,FocusFrame;internal readonly ReviewActivitiesPanel.Selection? Science;internal readonly TransportDraft? Draft;internal Selection(string page,string world,string ship,string planet,string surveyShip,string blueprint,int revision,bool open,ReviewActivitiesPanel.Selection? science,TransportDraft? draft,string? systemCaption,float solYaw=0,float solZoom=1,bool reducedMotion=false,bool innerFrame=false,bool focusFrame=false,BodySelectionKey selectedBody=default,string? selectedSystem=null,string beltSurveyShip="",string beltSponsor=""){BeltSurveyShip=beltSurveyShip;BeltSponsor=beltSponsor;Body=selectedBody;SelectedSystem=selectedSystem;InnerFrame=innerFrame;FocusFrame=focusFrame;SolYaw=solYaw;SolZoom=solZoom;ReducedMotion=reducedMotion;SystemCaption=systemCaption;Page=page;World=world;Ship=ship;Planet=planet;SurveyShip=surveyShip;Blueprint=blueprint;Revision=revision;InspectorOpen=open;Science=science;Draft=draft?.Copy();}}
        internal Selection? CaptureSelection()=>disposed?null:new Selection(page,world,ship,planet,surveyShip,inspectedBlueprint,inspectedRevision,this.Q<Foldout>("colonisation-hull-inspector")?.value??inspectedOpen,science?.CaptureSelection(),transportDraft,inspectedSystemCaption,solYaw,solZoom,solReducedMotion,solInnerFrame,solFocusFrame,selectedBody.Kind==BodySelectionKind.None?new BodySelectionKey(BodySelectionKind.Planet,planet):selectedBody,inspectedSystemId,beltSurveyShip,beltSponsor);
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
            selectedBody=KnownBody(saved.Body)?saved.Body:new BodySelectionKey(BodySelectionKind.Planet,planet);
            inspectedSystemId=saved.SelectedSystem!=null&&DisclosedBodySystems().Contains(saved.SelectedSystem,StringComparer.Ordinal)?saved.SelectedSystem:null;
            if(activities.Ships.Any(x=>x.ShipId==saved.SurveyShip))surveyShip=saved.SurveyShip;
            beltSurveyShip=activities.Ships.Any(x=>x.ShipId==saved.BeltSurveyShip)?saved.BeltSurveyShip:"";
            beltSponsor=observation.Worlds.Any(x=>x.Id==saved.BeltSponsor)?saved.BeltSponsor:"";
            solYaw=saved.SolYaw;solZoom=saved.SolZoom;solReducedMotion=saved.ReducedMotion;solInnerFrame=saved.InnerFrame;solFocusFrame=saved.FocusFrame;transportDraft=saved.Draft?.Copy();inspectedBlueprint=saved.Blueprint;inspectedRevision=saved.Revision;inspectedOpen=saved.InspectorOpen;Show(saved.Page);science?.RestoreSelection(saved.Science);
        }
        private readonly bool supportsKitFreePassengers;
        private readonly int? environmentScale;
        public ReviewColonisationPanel(ColonisationCampaignSnapshot observation,IReadOnlyList<ColonisationPlanetSnapshot> planets,CampaignActivityObservationDto activities,
            Action<ColonisationCommandDto,string> reviewOrder,Action<ColonisationCommandDto> submit,Action<CampaignActivityCommandDto,string> reviewActivity,Action<CampaignActivityCommandDto> submitActivity,bool supportsKitFreePassengers=false, ILookup<string,string>? systemNodes=null,int? environmentScale=null,CampaignStaticAstronomySnapshot? astronomy=null,BeltBodiesSnapshot? beltBodies=null,Action<BeltBodyCommandDto,string>? reviewBelt=null,bool supportsBeltBodies=false,IReadOnlyDictionary<string,string>? disclosedSystemNames=null)
        {
            this.astronomy=astronomy;this.supportsBeltBodies=supportsBeltBodies;this.reviewBelt=reviewBelt;
            this.disclosedSystemNames=disclosedSystemNames?.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
            this.beltBodies=supportsBeltBodies&&beltBodies!=null&&beltBodies.ActorId==observation.Actor&&beltBodies.Turn==observation.Turn&&beltBodies.Revision==observation.Revision?beltBodies:null;
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
        private string? inspectedSystemCaption;
        internal bool InspectOwnedWorld(string worldId,bool planetDetails=false)
        {
            if(!Live||!observation.Worlds.Any(x=>x.Id==worldId))return false;
            world=worldId;transportDraft=null;inspectedSystemCaption=null;
            if(planetDetails){planet=observation.Worlds.Single(x=>x.Id==worldId).PlanetId;selectedBody=new BodySelectionKey(BodySelectionKind.Planet,planet);inspectedSystemId=planets.FirstOrDefault(x=>x.PlanetId==planet)?.SystemId;Show("Planet");}else Show("Worlds");
            return true;
        }
        internal void InspectSystem(string? systemId,string caption)
        {
            if(!Live)return;
            inspectedSystemCaption=caption;inspectedSystemId=systemId;
            if(!planets.Any(x=>x.PlanetId==planet&&x.SystemId==systemId)){planet=planets.FirstOrDefault(x=>x.SystemId==systemId)?.PlanetId??"";solYaw=0;}
            if(selectedBody.Kind!=BodySelectionKind.Belt||!KnownBelts.Any(b=>b.BodyId==selectedBody.Id&&b.SystemId==systemId))
                selectedBody=planet.Length>0?new BodySelectionKey(BodySelectionKind.Planet,planet):new BodySelectionKey(BodySelectionKind.Belt,KnownBelts.FirstOrDefault(b=>b.SystemId==systemId)?.BodyId??"");
            Show("Planets");
        }
        private void Planets()
        {
            Text(body,inspectedSystemCaption??"System","colonisation-system-heading").style.unityFontStyleAndWeight=FontStyle.Bold;
            if(inspectedSystemCaption!=null&&inspectedSystemId==null)
            {Text(body,supportsBeltBodies&&beltBodies==null?"Belt body information is temporarily unavailable.":"No celestial bodies have been disclosed here.","celestial-body-availability");return;}
            var systems=DisclosedBodySystems();
            var system=inspectedSystemId??(selectedBody.Kind==BodySelectionKind.Belt?KnownBelts.FirstOrDefault(x=>x.BodyId==selectedBody.Id)?.SystemId:null)
                ??planets.FirstOrDefault(x=>x.PlanetId==planet)?.SystemId??systems.FirstOrDefault();
            if(system==null||!systems.Contains(system,StringComparer.Ordinal))
            {Text(body,supportsBeltBodies&&beltBodies==null?"Belt body information is temporarily unavailable.":"No celestial bodies have been disclosed here.","celestial-body-availability");return;}
            if(supportsBeltBodies&&beltBodies==null)Text(body,"Belt body information is temporarily unavailable.","celestial-body-availability");
            inspectedSystemId=system;
            Picker(body,"Disclosed system",systems,x=>x,BodySystemName,system,id=>{
                inspectedSystemCaption=null;inspectedSystemId=id;planet=planets.FirstOrDefault(x=>x.SystemId==id)?.PlanetId??"";
                selectedBody=planet.Length>0?new BodySelectionKey(BodySelectionKind.Planet,planet):new BodySelectionKey(BodySelectionKind.Belt,KnownBelts.First(b=>b.SystemId==id).BodyId);
                solYaw=0;Show("Planets");
            },"colonisation-system-selector");
            var bodies=planets.Where(x=>x.SystemId==system).ToArray();
            if(bodies.Length==0){BeltOnlySystem(body,system);return;}
            var p=bodies.FirstOrDefault(x=>x.PlanetId==planet)??bodies[0];planet=p.PlanetId;
            if(!KnownBody(selectedBody))selectedBody=new BodySelectionKey(BodySelectionKind.Planet,planet);
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
            IReadOnlyDictionary<string,long>? distances=null;
            try
            {
                var resource=Resources.Load<TextAsset>("PlayableFleetPackages/v1/sol08-astronomy");
                if(resource!=null)distances=SolMeanOrbitData.Read(resource.text).JoinDisclosed(bodies,planets);
            }
            catch(Exception){/* Explicit schematic fallback; never synthesize a body. */}
            if(distances!=null)bodies=bodies.OrderBy(b=>distances[b.PlanetId]).ToArray();
            var cards=new Dictionary<string,PlanetBodyCard>(StringComparer.Ordinal);var epoch=controlEpoch;
            void FocusPlanet(ColonisationPlanetSnapshot target)
            {
                if(!Live||epoch!=controlEpoch||!bodies.Any(x=>ReferenceEquals(x,target)))return;
                planet=target.PlanetId;selectedBody=new BodySelectionKey(BodySelectionKind.Planet,planet);
                foreach(var beltButton in this.Query<Button>(className:"system-belt-body").ToList())beltButton.RemoveFromClassList("selected-nav");
                foreach(var pair in cards)pair.Value.SetSelected(pair.Key==planet);
                // Do not rebuild cards/markers between the first and second click.
                solOverview?.Select(planet);detail.Clear();PlanetDetails(detail,target);
            }
            void OpenPlanet(ColonisationPlanetSnapshot target)
            {
                if(!Live||epoch!=controlEpoch||!bodies.Any(x=>ReferenceEquals(x,target)))return;
                planet=target.PlanetId;selectedBody=new BodySelectionKey(BodySelectionKind.Planet,planet);Show("Planet");
            }
            solOverview=new ReviewSolOverview(bodies,p.PlanetId,solYaw,solZoom,FocusPlanet,
                (yaw,zoom)=>{if(Live&&epoch==controlEpoch){solYaw=yaw;solZoom=zoom;}},OpenPlanet,solReducedMotion,
                reduced=>{if(Live&&epoch==controlEpoch)solReducedMotion=reduced;},activation,distances,solInnerFrame,solFocusFrame,(inner,focus)=>{if(Live&&epoch==controlEpoch){solInnerFrame=inner;solFocusFrame=focus;}},BeltVisualLayout.ForSystem(astronomy,observation.Actor,observation.Turn,observation.Revision,p.SystemId));
            visual.Add(solOverview);
            var selectedBelt=selectedBody.Kind==BodySelectionKind.Belt?KnownBelts.FirstOrDefault(b=>b.BodyId==selectedBody.Id&&b.SystemId==system):null;
            if(selectedBelt!=null){solOverview.ClearSelection();BeltDetails(detail,selectedBelt);}else PlanetDetails(detail,p);
            var selectorTitle=Text(visual,"CELESTIAL BODIES","system-selector-title");selectorTitle.style.marginTop=12;selectorTitle.style.marginBottom=6;
            var strip=Row(visual);strip.name="colonisation-system-bodies";strip.AddToClassList("system-selector");
            foreach(var disclosed in bodies)
            {
                var own=observation.Worlds.Where(x=>x.PlanetId==disclosed.PlanetId).ToArray();
                var status=own.Length==0?null:string.Join(" / ",own.Select(x=>StageLabel(x.Stage)));
                var card=new PlanetBodyCard(disclosed,status,selectedBody.Kind!=BodySelectionKind.Belt&&disclosed.PlanetId==planet,()=>FocusPlanet(disclosed),()=>OpenPlanet(disclosed),activation);
                cards.Add(disclosed.PlanetId,card);strip.Add(card);
            }
            BeltButtons(strip,KnownBelts.Where(b=>b.SystemId==system),detail);
            var legend=Text(visual,"⌂ Own settlement · Survey and settlement are separate observations.","system-disclosure-legend");legend.style.marginTop=8;legend.style.flexShrink=0;
        }
        private static string StageLabel(ColonyEstablishmentStage stage)=>stage==ColonyEstablishmentStage.EmptyOutpost?"Empty outpost":stage==ColonyEstablishmentStage.Founding?"Founding camp":stage.ToString();
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
        internal void CompleteSubmission(string text)
        {if(disposed)return;submitted=false;DismissReview(reviewEpoch);science?.CompleteSubmission(text);PresentStatus(text);}
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
