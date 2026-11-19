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
    }
}
