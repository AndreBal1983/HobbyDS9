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
    public sealed partial class ReviewColonisationPanel
    {
        private Foldout PlanetSection(VisualElement parent,string id,string label,bool initiallyOpen)
        {
            var key=planet+"/"+id;var epoch=controlEpoch;
            var fold=new Foldout{name="colonisation-planet-"+id,text=label,value=planetSections.TryGetValue(key,out var open)?open:initiallyOpen};
            fold.style.flexShrink=0;parent.Add(fold);
            fold.RegisterValueChangedCallback(e=>{if(Live&&epoch==controlEpoch&&Contains(fold)&&fold.enabledInHierarchy&&ReferenceEquals(e.target,fold))planetSections[key]=e.newValue;});return fold;
        }
        private static string ReadableKey(string? key)
        {
            if(string.IsNullOrWhiteSpace(key))return "Unknown";
            var value=key!.Substring(key.LastIndexOf('.')+1).Replace('-',' ').Replace('_',' ');
            return value.Length==0?"Unknown":char.ToUpperInvariant(value[0])+value.Substring(1);
        }
        private static Label WorldLabel(VisualElement parent,string text,string name,string cls)
        {var label=new Label(text){name=name};label.AddToClassList(cls);parent.Add(label);return label;}
        private static void WorldChip(VisualElement parent,string text,string name,string tooltip,bool muted=false)
        {var label=WorldLabel(parent,text,name,"world-info-chip");label.tooltip=tooltip;label.EnableInClassList("world-info-muted",muted);}
        private static string WorldNumber(long? value)=>value?.ToString("N0",System.Globalization.CultureInfo.InvariantCulture)??"—";
        private static VisualElement WorldGrid(VisualElement parent,string name)
        {var grid=new VisualElement{name=name};grid.AddToClassList("world-info-grid");parent.Add(grid);return grid;}
        private static void WorldStat(VisualElement parent,string key,string label,long? value,string unit,ReviewIconKind icon)
        {
            var cell=new VisualElement{name="world-stat-"+key,tooltip=label+": "+(value.HasValue?WorldNumber(value)+" "+unit:"Not reported")};cell.AddToClassList("world-info-stat");cell.EnableInClassList("world-info-muted",!value.HasValue);parent.Add(cell);
            var heading=new VisualElement();heading.AddToClassList("world-info-stat-heading");cell.Add(heading);heading.Add(new ReviewIcon(icon,label));
            WorldLabel(heading,label,"world-stat-label-"+key,"world-info-stat-label");
            var number=new VisualElement();number.AddToClassList("world-info-stat-number");cell.Add(number);
            WorldLabel(number,WorldNumber(value),"world-stat-value-"+key,"world-info-stat-value").tooltip=cell.tooltip;
            WorldLabel(number,value.HasValue?unit:"Not reported","world-stat-unit-"+key,"world-info-stat-unit");
        }
        private static string ControlCaption(ColonisationPlanetSnapshot p,string actor)
            =>!p.KnownControl.HasValue?"Control unknown":p.KnownControl==PlanetControllerKind.Unclaimed?"Unclaimed record":p.KnownControl==PlanetControllerKind.IndependentGovernment?"Independent government":p.KnownActor==actor?"Own empire record":"Other empire record";
        private static string ObservationStamp(int? turn,int current)
            =>!turn.HasValue||turn.Value<0||turn.Value>current?"Date not supplied":turn.Value==current?"Observed week "+turn:"Last observed week "+turn;
        private void WorldQueue(VisualElement root,ColonisationWorldSnapshot own)
        {
            var orders=observation.Production.Where(x=>x.ColonyId==own.Id).ToArray();
            var caption=orders.Length==0?"Queue · Idle":"Queue · "+orders[0].Name+" · "+WorldNumber(orders[0].CompletedIndustry)+" / "+WorldNumber(orders[0].RequiredIndustry)+" Industry"+(orders.Length>1?" · +"+(orders.Length-1):"");
            var queue=PlanetSection(root,"queue-"+own.Id,caption,false);queue.AddToClassList("world-info-disclosure");queue.tooltip="Own production record · week "+observation.Turn;
            if(orders.Length==0){WorldLabel(queue,"No queued orders","world-queue-empty","world-info-secondary");return;}
            foreach(var order in orders)
            {
                var row=new VisualElement{name="world-queue-"+order.Id};row.AddToClassList("world-info-queue-row");queue.Add(row);
                row.Add(new ReviewIcon(order.Kind==ColonisationProductionKind.PublishedShip?ReviewIconKind.Propulsion:ReviewIconKind.Construction,"Queued construction"));
                WorldLabel(row,order.Name,"world-queue-name-"+order.Id,"world-info-row-name");
                WorldLabel(row,WorldNumber(order.CompletedIndustry)+" / "+WorldNumber(order.RequiredIndustry)+" Industry","world-queue-work-"+order.Id,"world-info-row-value");
                WorldLabel(queue,order.Status,"world-queue-status-"+order.Id,"world-info-secondary");
            }
        }
        private void PlanetDetails(VisualElement parent,ColonisationPlanetSnapshot p)
        {
            var root=new VisualElement{name="world-info"};root.AddToClassList("world-info-compact");parent.Add(root);
            var sheet=Resources.Load<StyleSheet>("WorldInfoCompact");if(sheet!=null)root.styleSheets.Add(sheet);
            var environmentKnown=p.Surveyed&&!string.IsNullOrWhiteSpace(p.PlanetClass);
            WorldLabel(root,p.DisplayName,"colonisation-focused-planet-name","world-info-title");
            var ownWorlds=observation.Worlds.Where(x=>x.PlanetId==p.PlanetId).ToArray();
            var identity=WorldGrid(root,"world-identity-chips");
            WorldChip(identity,environmentKnown?PlanetVisualProfile.From(p).Caption:"Type not reported","world-type-chip","Only disclosed environment classification; decorative appearance is not a survey.",!environmentKnown);
            if(ownWorlds.Length==0)WorldChip(identity,"Settlement not reported","world-settlement-chip","No own-world record does not establish an empty or uncolonised body.",true);
            else foreach(var own in ownWorlds)WorldChip(identity,StageLabel(own.Stage),"world-settlement-chip-"+own.Id,"Owned settlement: "+own.Name);
            if(page=="Planets"&&solOverview!=null)
            {
                var portrait=new Image{name="colonisation-focused-planet-portrait",scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
                portrait.style.height=180;portrait.style.flexShrink=0;root.Add(portrait);
                var portraitStatus=WorldLabel(root,"Selected-body preview unavailable.","colonisation-portrait-unavailable","world-info-secondary");
                portraitStatus.style.display=DisplayStyle.None;solOverview.BindPortrait(portrait,portraitStatus);
            }
            if(ownWorlds.Length==0)
            {
                var unknown=WorldGrid(root,"world-economy-unknown");
                WorldStat(unknown,"population","Population",null,"people",ReviewIconKind.Biotech);
                WorldStat(unknown,"industry","Industry",null,"/ week",ReviewIconKind.Construction);
                WorldStat(unknown,"research","Research",null,"/ week",ReviewIconKind.Computing);
                WorldChip(root,"Economy, food & queue not reported","world-economy-unknown-chip","Only an own-world economy observation supplies these values. Unknown is not zero.",true);
            }
            foreach(var own in ownWorlds)
            {
                var economy=new VisualElement{name="colonisation-colony-"+own.Id};root.Add(economy);
                if(ownWorlds.Length>1||own.Name!=p.DisplayName)WorldLabel(economy,own.Name,"world-economy-name-"+own.Id,"world-info-section-title");
                WorldChip(economy,"Own figures · week "+observation.Turn,"world-economy-stamp-"+own.Id,"Owned-world observation for this week; this date does not apply to environment or resource records.");
                var grid=WorldGrid(economy,"world-economy-grid-"+own.Id);
                WorldStat(grid,"population","Population",own.Population,"people",ReviewIconKind.Biotech);
                WorldStat(grid,"industry","Industry",own.Industry,"/ week",ReviewIconKind.Construction);
                WorldStat(grid,"research","Research",own.Research,"/ week",ReviewIconKind.Computing);
                WorldStat(grid,"food-output","Food output",own.FoodOutput,"/ week",ReviewIconKind.Biotech);
                WorldStat(grid,"food-demand","Food demand",own.FoodDemand,"/ week",ReviewIconKind.Biotech);
                WorldStat(grid,"food-stock","Food stored",own.Food,"stock",ReviewIconKind.Biotech);
                var balance=new VisualElement{name="world-food-balance"};balance.AddToClassList("world-info-balance");balance.EnableInClassList("world-info-warning",own.LocalFoodBalance<0);economy.Add(balance);
                balance.EnableInClassList("world-info-muted",!own.LocalFoodBalance.HasValue);
                WorldLabel(balance,"Food balance","world-food-balance-label","world-info-stat-label");
                WorldLabel(balance,own.LocalFoodBalance.HasValue?(own.LocalFoodBalance>=0?"+":"")+WorldNumber(own.LocalFoodBalance)+" / week":"Not reported","world-food-balance-value","world-info-balance-value");
                WorldQueue(economy,own);
            }
            var knowledge=WorldGrid(root,"world-knowledge-chips");
            WorldChip(knowledge,ControlCaption(p,observation.Actor),"world-control-chip","Recorded control is distinct from current settlement and access.",!p.KnownControl.HasValue);
            WorldChip(knowledge,ObservationStamp(p.ControlObservedTurn,observation.Turn),"world-control-date-chip","Control observation date only; not a date for environment, resources or economic figures.",!p.ControlObservedTurn.HasValue||p.ControlObservedTurn!=observation.Turn);
            var environment=PlanetSection(root,"environment",environmentKnown?"Environment · known record":"Environment · not reported",false);environment.AddToClassList("world-info-disclosure");
            WorldChip(environment,"Record date not supplied","world-environment-date-chip","No environment observation date is supplied by this projection.",true);
            if(environmentKnown)
            {
                WorldLabel(environment,ReadableKey(p.PlanetClass)+" · "+ReadableKey(p.Atmosphere),"world-environment-class","world-info-secondary");
                WorldLabel(environment,environmentScale.HasValue?"Gravity "+((double)p.Gravity/environmentScale.Value).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" g · Pressure "+((double)p.Pressure/environmentScale.Value).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" (reference scale)":"Gravity "+p.Gravity+" · Pressure "+p.Pressure+" (measurement units unavailable)","world-environment-measurements","world-info-secondary");
                WorldLabel(environment,"Radiation index "+p.Radiation,"world-environment-radiation","world-info-secondary");
            }
            else WorldLabel(environment,"Not reported","world-environment-unknown","world-info-secondary");
            var resources=PlanetSection(root,"resources",environmentKnown?"Initial deposits · "+(p.InitialDeposits.Count==0?"none recorded":p.InitialDeposits.Count+" types"):"Resources · not reported",false);resources.AddToClassList("world-info-disclosure");
            WorldChip(resources,"Initial record · date not supplied","world-resources-date-chip","Current remaining deposits, extraction permission and observation date are not supplied here.",true);
            if(environmentKnown)foreach(var deposit in p.InitialDeposits)WorldLabel(resources,MaterialName(deposit.ResourceDefinitionId)+" · "+WorldNumber(deposit.Amount),"world-deposit-"+deposit.ResourceDefinitionId,"world-info-secondary");
            else WorldLabel(resources,"Not reported","world-resources-unknown","world-info-secondary");
            var surveyArea=WorldGrid(root,"world-survey-status");
            if(p.ActiveSurveyId!=null)
            {
                var survey=observation.Surveys.FirstOrDefault(x=>x.Id==p.ActiveSurveyId&&x.PlanetId==p.PlanetId);
                var assigned=survey==null?null:activities.Ships.FirstOrDefault(x=>x.ShipId==survey.ShipId);
                WorldChip(surveyArea,"Survey active · "+p.CompletedSurveyWeeks+" weeks done","world-survey-chip","Required duration and completion date are not reported.");
                if(assigned!=null)WorldChip(surveyArea,assigned.DisplayName,"world-survey-ship-chip","Assigned ship in this observation.");
                var indeterminate=new VisualElement{name="colonisation-survey-indeterminate",tooltip="Required duration unavailable; no percentage or ETA"};
                indeterminate.style.height=5;indeterminate.style.backgroundColor=new Color(.16f,.24f,.32f);
                var segment=new VisualElement{pickingMode=PickingMode.Ignore};segment.style.width=18;segment.style.height=5;segment.style.backgroundColor=new Color(.86f,.65f,.35f);indeterminate.Add(segment);root.Add(indeterminate);
            }
            else
            {
                var completed=observation.Surveys.Any(s=>s.PlanetId==p.PlanetId&&s.Stage==PlanetSurveyStage.Completed);
                WorldChip(surveyArea,completed?"Survey complete":p.Surveyed?"No completed survey record":"Not surveyed","world-survey-chip","Environment knowledge alone does not establish a completed survey.",!completed);
            }
            var reports=observation.Surveys.Where(x=>x.PlanetId==p.PlanetId).ToArray();
            if(reports.Length>0){var history=PlanetSection(root,"survey-records","Survey records",false);history.AddToClassList("world-info-disclosure");foreach(var report in reports)WorldLabel(history,report.Stage+" · "+report.Weeks+" weeks done","world-survey-record-"+report.Id,"world-info-secondary");}
            var presence=observation.Presence.FirstOrDefault(x=>x.SystemId==p.SystemId);
            if(presence!=null)
            {
                var local=WorldGrid(root,"world-system-presence");
                WorldChip(local,presence.Caption,"world-presence-chip","System presence does not establish control or the absence of other governments on unobserved bodies.");
                WorldChip(local,ObservationStamp(presence.ObservationTurn,observation.Turn),"world-presence-date-chip","System presence observation date only.",presence.ObservationTurn!=observation.Turn);
            }
        }
        private void PlanetInterface()
        {
            Button(body,"Back to system","colonisation-planet-back",()=>Show("Planets"));
            var p=planets.FirstOrDefault(x=>x.PlanetId==planet);
            if(p==null){Text(body,"This planet is no longer disclosed.");return;}
            var context=Row(body);context.name="planet-detail-context";context.style.flexWrap=Wrap.NoWrap;
            var portrait=new ReviewPlanetPortrait(p,solReducedMotion,value=>solReducedMotion=value);planetPortrait=portrait;context.Add(portrait);
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
        }
    }
}
