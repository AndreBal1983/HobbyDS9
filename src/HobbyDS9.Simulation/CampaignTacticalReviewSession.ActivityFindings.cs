using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command)
        {
            lock(gate)
            {
                Require(activities!=null&&command!=null&&Actor(command.ActorId)==CampaignTacticalError.None,"Controlled activity eligibility required.");
                // Candidate validation never grants permission or changes the live frame.
                // Foreign identities return the same ordinary failure as missing ones.
                var game=state.Campaign.GameState;var candidate=ForkPublished();candidate.publishedExecuting=false;
                var result=candidate.SubmitActivity(command);
                var observation=new CampaignActivityEligibilityDto{ActorId=command.ActorId,Turn=game.TurnNumber,Revision=game.StateRevision,
                    Accepted=result.Accepted,Error=result.Error,Reason=result.Accepted?"Ready to submit at this campaign state.":result.Error==CampaignTacticalError.Overflow?
                        "The remaining campaign or record budget cannot safely complete this order.":"This order is unavailable here. Check location, ownership, capability, resources and prior findings."};
                if(result.Accepted&&command.Kind==CampaignActivityCommandKind.EnqueueStructure)
                {
                    var colony=PopulationData.Copy(game.Colonies!.Single(c=>c.ColonyId==command.TargetId&&c.OwnerCivilizationId==command.ActorId));
                    colony.Infrastructure!.Add(new InfrastructureStateDto{DefinitionId=command.DefinitionId,Count=1});
                    var projected=WorldEconomy.Project(colony,game,definitions);observation.IndustryAfterCompletion=projected.Industry;
                    observation.ResearchAfterCompletion=projected.ResearchOutput;observation.FoodAfterCompletion=projected.FoodOutput;
                }
                return observation;
            }
        }
        private ActivitySiteDefinitionDto ActivityDefinition(string id)=>activities!.Content.Sites!
            .Concat(activities.Content.Minors!.Select(m=>m.Encounter!)).Single(d=>d.DefinitionId==id);
        private ActivitySupportedShip? ActivityCapability(GameStateDto game,ShipStateDto ship)
        {
            var reference=ship.Blueprint;
            if(reference==null&&activities!.AuthoredActivityBlueprints.TryGetValue(ship.ShipId,out var blueprint))reference=new BlueprintReferenceDto{BlueprintId=blueprint,Revision=1};
            if(reference==null)return null;
            var record=CampaignBlueprints.Find(game,reference);
            if(record==null||record.OwnerCivilizationId!=ship.OwnerCivilizationId)return null;
            // No caller snapshot is accepted as capability. Recompile the exact frozen
            // publication against the complete private v3 support package.
            return PublishedTacticalSupportCompiler.EvaluateActivities(new ShipDesignCatalogueSnapshot(record.FrozenCatalogue!),CampaignBlueprints.Restore(record),
                CampaignBlueprints.FrozenAccess(record.FrozenCatalogue!),activities!.Support).Ship;
        }
        private void ObserveActivitySites(GameStateDto game)
        {
            foreach(var actor in game.Civilizations!.OrderBy(c=>c.CivilizationId,StringComparer.Ordinal))
            {
                var coverage=GeneratedCoverage(game,actor.CivilizationId).ToHashSet(StringComparer.Ordinal);
                foreach(var site in activities!.State.Sites!.Where(s=>coverage.Contains(s.NodeId)))
                    if(!activities.State.Observations!.Any(o=>o.ActorId==actor.CivilizationId&&o.SiteId==site.SiteId))
                        activities.State.Observations!.Add(new ActivitySiteObservationDto{ActorId=actor.CivilizationId,SiteId=site.SiteId,FirstObservedTurn=game.TurnNumber});
            }
        }
        public CampaignActivityObservationDto CaptureActivities(string actor)
        {
            lock(gate)
            {
                Require(activities!=null&&Actor(actor)==CampaignTacticalError.None,"Controlled activity actor required.");
                var game=state.Campaign.GameState;var copy=CampaignActivitiesData.Copy(activities!.State);
                var colonies=game.Colonies!.Where(c=>c.OwnerCivilizationId==actor).Select(c=>c.ColonyId).ToHashSet(StringComparer.Ordinal);
                var known=copy.Observations!.Where(o=>o.ActorId==actor).Select(o=>o.SiteId).ToHashSet(StringComparer.Ordinal);
                var result=new CampaignActivityObservationDto{ActorId=actor,Turn=game.TurnNumber,Revision=game.StateRevision,
                    Production=copy.Production!.Where(q=>colonies.Contains(q.ColonyId)).ToList(),Structures=copy.Structures!.Where(q=>q.OwnerId==actor).ToList(),
                    Findings=copy.Findings!.Where(f=>f.ActorId==actor).ToList(),Credits=game.Civilizations!.Single(c=>c.CivilizationId==actor).Credits};
                foreach(var colony in game.Colonies!.Where(c=>c.OwnerCivilizationId==actor).OrderBy(c=>c.ColonyId,StringComparer.Ordinal))
                {var output=WorldEconomy.Project(colony,game,definitions);result.Colonies.Add(new ActivityOwnColonyDto{ColonyId=colony.ColonyId,
                    DisplayName=definitions.StarSystems!.Single(s=>s.DefinitionId==game.StarSystems!.Single(s=>s.SystemId==colony.SystemId).DefinitionId).DisplayName,
                    Industry=output.Industry,Research=output.ResearchOutput,Food=output.FoodOutput});}
                foreach(var ship in game.Ships!.Where(s=>s.OwnerCivilizationId==actor).OrderBy(s=>s.ShipId,StringComparer.Ordinal))
                {
                    var fleet=game.Fleets!.Single(f=>f.FleetId==ship.FleetId);var supported=ActivityCapability(game,ship);
                    var reference=ship.Blueprint;
                    if(reference==null&&activities.AuthoredActivityBlueprints.TryGetValue(ship.ShipId,out var authored))reference=new BlueprintReferenceDto{BlueprintId=authored,Revision=1};
                    var record=reference==null?null:CampaignBlueprints.Find(game,reference);
                    result.Ships.Add(new ActivityOwnShipDto{ShipId=ship.ShipId,FleetId=ship.FleetId,NodeId=fleet.LocationNodeId,
                        Stationary=fleet.TransitTotalCost==0&&fleet.RouteNodeIds!.Count==0,DisplayName=record?.Fit!.DisplayName??Equipment(ship.ShipId).DisplayName,
                        HullVersionId=record?.Fit!.HullVersionId??"",StudyWork=supported?.StudyWork??0,ContactCapacity=supported?.ContactCapacity??0});
                }
                foreach(var record in game.Blueprints!.Where(b=>b.OwnerCivilizationId==actor).OrderBy(b=>b.BlueprintId,StringComparer.Ordinal).ThenBy(b=>b.Revision))
                {
                    var support=PublishedFit(game,new BlueprintReferenceDto{BlueprintId=record.BlueprintId,Revision=record.Revision},true);
                    result.BuildOptions.Add(new ActivityBuildOptionDto{Kind=ActivityProductionKind.PublishedShip,DefinitionId=record.BlueprintId,BlueprintRevision=record.Revision,
                        DisplayName=record.Fit!.DisplayName,HullVersionId=record.Fit.HullVersionId,Cost=BlueprintCopies.Copy(record.Cost!),Supported=support.IsSupportedByCompiler,
                        Reason=support.IsSupportedByCompiler?"":"This class contains equipment outside the supported activity/tactical package."});
                }
                foreach(var structure in activities.Content.Structures!)result.BuildOptions.Add(new ActivityBuildOptionDto{Kind=ActivityProductionKind.Infrastructure,
                    DefinitionId=structure.DefinitionId,DisplayName=structure.DisplayName,Cost=BlueprintCopies.Copy(structure.Cost!),Supported=true});
                foreach(var site in copy.Sites!.Where(s=>known.Contains(s.SiteId)).OrderBy(s=>s.SiteId,StringComparer.Ordinal))
                {
                    var minor=site.MinorActorId==null?null:copy.Minors!.Single(m=>m.ActorId==site.MinorActorId);
                    var definition=minor==null?null:activities.Content.Minors!.Single(m=>m.DefinitionId==minor.DefinitionId);
                    var detached=definition==null?null:CampaignActivitiesData.Copy(activities.Content).Minors!.Single(m=>m.DefinitionId==definition.DefinitionId);
                    var relation=minor==null?(ActivityMinorRelation?)null:copy.Relations!.SingleOrDefault(r=>r.ActorId==actor&&r.MinorActorId==minor.ActorId)?.Relation??ActivityMinorRelation.Observed;
                    var disclosed=CampaignActivitiesData.Copy(ActivityDefinition(site.DefinitionId));
                    disclosed.Choices=disclosed.Choices!.Where(c=>!c.RequiredRelation.HasValue||c.RequiredRelation==relation).ToList();
                    var choices=new List<ActivityKnownChoiceDto>();
                    foreach(var choice in disclosed.Choices)
                    {
                        var preview=ScienceData.Copy(game.Civilizations!.Single(c=>c.CivilizationId==actor));
                        var insight=ScienceRules.GrantActivityInsight(preview,game.ResearchPaceFactor!.Value,actor,choice.Discipline,choice.Insight,new List<ResearchReceiptDto>());
                        choices.Add(new ActivityKnownChoiceDto{ChoiceId=choice.ChoiceId,Caption=choice.Caption,FindingId=choice.FindingId,RequiredStudy=choice.RequiredStudy,RequiredContact=choice.RequiredContact,
                            Work=choice.Work,Deadline=choice.Deadline,Credits=choice.Credits,Discipline=choice.Discipline,LearnableInsightInternalUnits=insight.LearnedInternalUnits,
                            UnlearnableInsightInternalUnits=insight.UnlearnableInternalUnits,HullRiskBps=choice.HullRiskBps,HullDamage=choice.HullDamage});
                    }
                    disclosed.Choices=new List<ActivityChoiceDefinitionDto>();
                    if(detached!=null)detached.Encounter=null;
                    result.Sites.Add(new ActivityKnownSiteDto{Site=site,Definition=disclosed,Body=copy.Bodies!.Single(b=>b.BodyId==site.BodyId),Minor=minor,MinorDefinition=detached,Relation=relation,Choices=choices});
                }
                return result;
            }
        }
        private CampaignTacticalError StartActivityStudy(GameStateDto game,CampaignActivityCommandDto command)
        {
            var a=activities!.State;
            var site=a.Sites!.SingleOrDefault(s=>s.SiteId==command.TargetId);
            if(site==null||!a.Observations!.Any(o=>o.ActorId==command.ActorId&&o.SiteId==site.SiteId))return CampaignTacticalError.InvalidOrder;
            var choice=ActivityDefinition(site.DefinitionId).Choices!.SingleOrDefault(c=>c.ChoiceId==command.DefinitionId);
            var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==command.ShipId&&s.OwnerCivilizationId==command.ActorId);
            if(choice==null||ship==null)return CampaignTacticalError.InvalidOrder;
            var fleet=game.Fleets!.Single(f=>f.FleetId==ship.FleetId);
            if(fleet.LocationNodeId!=site.NodeId||fleet.TransitTotalCost!=0||fleet.RouteNodeIds!.Count!=0||ship.CurrentHull<=0)return CampaignTacticalError.InvalidOrder;
            var capability=ActivityCapability(game,ship);
            if(capability==null||capability.StudyWork<choice.RequiredStudy||capability.ContactCapacity<choice.RequiredContact)return CampaignTacticalError.UnsupportedSource;
            if(a.Findings!.Any(f=>f.ActorId==command.ActorId&&f.SiteId==site.SiteId&&f.FindingId==choice.FindingId)
                ||a.Findings.Any(f=>f.ShipId==ship.ShipId&&(f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim)))return CampaignTacticalError.InvalidOrder;
            if(choice.RequiredRelation.HasValue)
            {
                if(site.MinorActorId==null)return CampaignTacticalError.InvalidOrder;
                var relation=a.Relations!.SingleOrDefault(r=>r.ActorId==command.ActorId&&r.MinorActorId==site.MinorActorId)?.Relation??ActivityMinorRelation.Observed;
                if(relation!=choice.RequiredRelation.Value)return CampaignTacticalError.InvalidOrder;
            }
            var owner=game.Civilizations!.Single(c=>c.CivilizationId==command.ActorId);
            if(owner.Credits<choice.Credits)return CampaignTacticalError.InvalidOrder;
            owner.Credits-=choice.Credits;
            a.Findings!.Add(new ActivityFindingDto{ActorId=command.ActorId,SiteId=site.SiteId,ChoiceId=choice.ChoiceId,FindingId=choice.FindingId,FindingBudget=choice.Insight,
                ShipId=ship.ShipId,FleetId=ship.FleetId,Stage=choice.Work==0?ActivityProgressStage.AwaitingClaim:ActivityProgressStage.Active,
                RemainingAdvances=choice.Deadline,StartedTurn=game.TurnNumber,PaidCredits=choice.Credits});
            return CampaignTacticalError.None;
        }
        private CampaignTacticalError ResolveActivity(GameStateDto game,CampaignActivityCommandDto command,bool abort)
        {
            var finding=activities!.State.Findings!.SingleOrDefault(f=>f.ActorId==command.ActorId&&f.SiteId==command.TargetId&&f.ChoiceId==command.DefinitionId);
            if(finding==null||finding.Stage!=ActivityProgressStage.Active&&finding.Stage!=ActivityProgressStage.AwaitingClaim)return CampaignTacticalError.InvalidOrder;
            if(abort){finding.Stage=ActivityProgressStage.Aborted;finding.ResolvedTurn=game.TurnNumber;finding.Report="You ended this expedition. Its consumed funding is not refunded; this attempt remains recorded.";return CampaignTacticalError.None;}
            if(finding.Stage!=ActivityProgressStage.AwaitingClaim)return CampaignTacticalError.InvalidOrder;
            var site=activities.State.Sites!.Single(s=>s.SiteId==finding.SiteId);var choice=ActivityDefinition(site.DefinitionId).Choices!.Single(c=>c.ChoiceId==finding.ChoiceId);
            var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==finding.ShipId&&s.OwnerCivilizationId==finding.ActorId&&s.FleetId==finding.FleetId);
            var fleet=game.Fleets!.SingleOrDefault(f=>f.FleetId==finding.FleetId);
            if(ship==null||ship.CurrentHull<=0||fleet==null||fleet.LocationNodeId!=site.NodeId||fleet.TransitTotalCost!=0||fleet.RouteNodeIds!.Count!=0)return CampaignTacticalError.InvalidOrder;
            var capability=ActivityCapability(game,ship);
            if(capability==null||capability.StudyWork<choice.RequiredStudy||capability.ContactCapacity<choice.RequiredContact)return CampaignTacticalError.UnsupportedSource;
            if(choice.RequiredRelation.HasValue)
            {
                var current=activities.State.Relations!.SingleOrDefault(r=>r.ActorId==command.ActorId&&r.MinorActorId==site.MinorActorId)?.Relation??ActivityMinorRelation.Observed;
                if(current!=choice.RequiredRelation.Value)return CampaignTacticalError.InvalidOrder;
            }
            var roll=Convert.ToUInt32(CampaignActivitiesData.Identity("activity-outcome-v1",published!.WrapperHash,generatedMap!.Settings.Map.Seed,finding.ActorId,site.SiteId,finding.FindingId).Substring(0,8),16)%10000;
            var accepted=roll<choice.ConsentChanceBps;
            if(site.MinorActorId!=null&&choice.AcceptedRelation.HasValue)
            {
                var relation=activities.State.Relations!.SingleOrDefault(r=>r.ActorId==command.ActorId&&r.MinorActorId==site.MinorActorId);
                if(relation==null){relation=new ActivityMinorRelationDto{ActorId=command.ActorId,MinorActorId=site.MinorActorId};activities.State.Relations!.Add(relation);}
                relation.Relation=accepted?choice.AcceptedRelation.Value:choice.RefusedRelation??ActivityMinorRelation.Refused;relation.ChangedTurn=game.TurnNumber;
                finding.ContactOutcome=accepted?"Accepted: "+choice.ContactOutcome:"Refused: "+choice.ContactOutcome;
            }
            if(accepted&&choice.Insight>0)
            {
                var receipts=new List<ResearchReceiptDto>();var owner=game.Civilizations!.Single(c=>c.CivilizationId==finding.ActorId);
                var granted=ScienceRules.GrantActivityInsight(owner,game.ResearchPaceFactor!.Value,finding.ActorId,choice.Discipline,choice.Insight,receipts);
                finding.InsightApplied=granted.LearnedInternalUnits;finding.InsightUnlearnable=granted.UnlearnableInternalUnits;
                // The old core receipt schema intentionally remains unchanged. This
                // profile retains activity/progression receipts in its typed finding.
                finding.ResearchReceipts=receipts;
            }
            var damageRoll=Convert.ToUInt32(CampaignActivitiesData.Identity("activity-hull-risk-v1",published.WrapperHash,generatedMap!.Settings.Map.Seed,finding.ActorId,site.SiteId,finding.FindingId).Substring(0,8),16)%10000;
            var damage=damageRoll<choice.HullRiskBps?Math.Min(choice.HullDamage,Math.Max(0,ship.CurrentHull-1)):0;
            ship.CurrentHull-=damage;
            finding.Stage=ActivityProgressStage.Completed;finding.ResolvedTurn=game.TurnNumber;finding.Report=accepted?choice.Report:"The society refused this approach. No cooperation, programme or reward was granted. Accepted expedition funding remains consumed.";
            if(damage>0)finding.Report+=" The vessel suffered "+damage+" hull damage.";
            return CampaignTacticalError.None;
        }
        private void TickActivityFindings(GameStateDto game,bool advance=true)
        {
            foreach(var finding in activities!.State.Findings!.Where(f=>f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim))
            {
                var site=activities.State.Sites!.Single(s=>s.SiteId==finding.SiteId);
                var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==finding.ShipId&&s.OwnerCivilizationId==finding.ActorId);
                var fleet=game.Fleets!.SingleOrDefault(f=>f.FleetId==finding.FleetId);
                if(ship==null){finding.Stage=ActivityProgressStage.Failed;finding.Report="The participating vessel was lost. No replacement or reward was created.";finding.ResolvedTurn=game.TurnNumber;continue;}
                if(fleet==null||ship.FleetId!=finding.FleetId||fleet.LocationNodeId!=site.NodeId||fleet.TransitTotalCost!=0||fleet.RouteNodeIds!.Count!=0)
                {finding.Stage=ActivityProgressStage.Aborted;finding.Report="The participating vessel left or reorganized. Unearned findings were not granted.";finding.ResolvedTurn=game.TurnNumber;continue;}
                if(finding.Stage==ActivityProgressStage.AwaitingClaim||!advance)continue;
                var choice=ActivityDefinition(site.DefinitionId).Choices!.Single(c=>c.ChoiceId==finding.ChoiceId);var capability=ActivityCapability(game,ship);
                finding.RemainingAdvances--;
                if(capability!=null&&capability.StudyWork>=choice.RequiredStudy&&capability.ContactCapacity>=choice.RequiredContact)
                    finding.WorkDone=checked(finding.WorkDone+Math.Max(1,capability.StudyWork));
                if(finding.WorkDone>=choice.Work)finding.Stage=ActivityProgressStage.AwaitingClaim;
                else if(finding.RemainingAdvances<=0){finding.Stage=ActivityProgressStage.Expired;finding.Report="The disclosed work window ended. Unearned findings were not granted; funding remains consumed.";finding.ResolvedTurn=game.TurnNumber;}
            }
            ObserveActivitySites(game);
        }
    }
}
