using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private sealed class ExpansionSource
        {
            internal readonly string Hash;
            internal readonly ColonisationPlannerPolicyDto Policy;
            internal ExpansionSource(CampaignColonisationExpansionPackageDto p)
            {Require(p!=null&&p.PackageHash==ColonisationData.ExpansionHash(p),"Different approved expansion package.");Hash=p!.PackageHash;Policy=ColonisationData.Copy(p.PlannerPolicy!);}
        }
        private sealed class ExpansionContext
        {
            internal readonly ExpansionSource Source;
            internal List<ColonisationAiPlanDto> Plans;
            internal ExpansionContext(ExpansionSource source,IEnumerable<ColonisationAiPlanDto> plans){Source=source;Plans=plans.Select(ColonisationData.Copy).ToList();}
            internal ExpansionContext Copy()=>new ExpansionContext(Source,Plans);
        }
        private ExpansionContext? expansion;
        public static CampaignColonisationExpansionPackageDto CreateColonisationExpansionPackage(CampaignColonisationRosterPackageDto roster)
        {
            var p=new CampaignColonisationExpansionPackageDto{BaseRosterV2=roster};p.PackageHash=ColonisationData.ExpansionHash(p);return p;
        }
        public static CampaignTacticalReviewSession StartColonisationExpansion(CampaignColonisationExpansionPackageDto package,NaturalGalaxySettingsDto settings)
        {
            var source=new ExpansionSource(package);var roster=package.BaseRosterV2!;var p=roster.BaseV1!;
            var civs=p.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=GeneratedCampaignFactory.Frozen(NaturalGalaxyGenerator.Generate(settings,civs),civs);
            return StartActivitiesFrozen(p.Activities,map,null,p,null,new ColonisationRosterSource(roster),source);
        }
        public static CampaignTacticalReviewSession LoadColonisationExpansion(CampaignColonisationExpansionPackageDto package,CampaignColonisationExpansionSaveDto root)
        {
            var source=new ExpansionSource(package);var roster=package.BaseRosterV2!;
            ColonisationRootBudget.ValidateExpansion(root,roster.BaseV1!.Settlement!);
            Require(root.PackageHash==source.Hash,"Different expansion source.");
            var replay=LoadColonisationCore(roster.BaseV1,root.BaseCampaign!,new ColonisationRosterSource(roster),source);
            Require(FrontierSame(root,replay.ExportColonisationExpansionSave()),"Expansion plan differs from complete ordered replay.");return replay;
        }
        public CampaignColonisationExpansionSaveDto ExportColonisationExpansionSave()
        {
            lock(gate){Require(expansion!=null,"Explicit expansion root required.");return new CampaignColonisationExpansionSaveDto{
                PackageHash=expansion!.Source.Hash,BaseCampaign=ExportColonisationCore(),AiPlans=expansion.Plans.Select(ColonisationData.Copy).ToList()};}
        }
        private void ValidateExpansionState(GameStateDto game)
        {
            if(expansion==null)return;
            var plans=expansion.Plans;var actors=game.Civilizations!.Where(c=>c.ControllerKind==ControllerKind.AI).Select(c=>c.CivilizationId).OrderBy(id=>id,StringComparer.Ordinal).ToList();
            Require(plans.Count==actors.Count&&plans.Select(p=>p.ActorId).OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(actors),"Exact fixed AI goal census required.");
            foreach(var p in plans)
            {
                bool World(string? id)=>id==null||game.Colonies!.Any(c=>c.ColonyId==id&&c.OwnerCivilizationId==p.ActorId);
                bool Ship(string? id)=>id==null||game.Ships!.Any(s=>s.ShipId==id&&s.OwnerCivilizationId==p.ActorId)
                    ||published!.Records.Any(r=>r.ShipId==id&&r.OwnerCivilizationId==p.ActorId)
                    ||published.Package.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Ships!.Any(s=>s.ShipId==id&&s.OwnerCivilizationId==p.ActorId);
                bool Queue(string? id)=>id==null||colonisation!.Production.Any(q=>q.QueueItemId==id&&q.OwnerId==p.ActorId)||colonisation!.History.Any(h=>h.Order!.QueueItemId==id&&h.Order.OwnerId==p.ActorId);
                Require(p.GoalId=="establish-first-frontier-colony-v1"&&Enum.IsDefined(typeof(ColonisationAiStage),p.Stage)&&p.LastPlannedTurn>=0&&p.LastPlannedTurn<=game.TurnNumber
                    &&p.DisputedBoundaries>=0&&p.DisputedBoundaries<=expansion.Source.Policy.DisputedWeeks&&p.HoldReason!=null&&p.HoldReason.Length<=128,"Invalid AI goal stage/report.");
                Require(World(p.HomeColonyId)&&World(p.YardColonyId)&&World(p.EstablishedColonyId)&&Ship(p.ScoutShipId)&&Ship(p.TransportShipId)&&Queue(p.KitQueueId)&&Queue(p.TransportQueueId),"Foreign or fabricated AI assignment.");
                Require(p.SourceCohortId==null||game.Colonies!.Where(c=>c.OwnerCivilizationId==p.ActorId&&c.ColonyId==p.HomeColonyId).SelectMany(c=>c.Cohorts!).Any(c=>c.CohortId==p.SourceCohortId)
                    ||colonisation!.State.ManifestOrigins!.Any(o=>o.SourceCohortId==p.SourceCohortId&&o.SourceColonyId==p.HomeColonyId&&colonisation.State.ManifestRecords!.Any(m=>m.ManifestId==o.ManifestId&&m.OwnerId==p.ActorId)),"Foreign or fabricated AI source cohort.");
                Require((p.TargetPlanetId==null)==(p.TargetNodeId==null),"Incomplete AI target binding.");
                if(p.TargetPlanetId!=null)
                {
                    var observed=ColonisationPlanetsForActor(game,p.ActorId).SingleOrDefault(o=>o.PlanetId==p.TargetPlanetId);
                    Require(observed!=null&&physicalNavigation!.NodeForSystem(observed.SystemId)==p.TargetNodeId&&physicalNavigation.KnownNode(game,p.ActorId,p.TargetNodeId!),"Unobserved AI body or mismatched known node.");
                }
                Require(p.Stage==ColonisationAiStage.Preparing||p.HomeColonyId!=null,"Active AI goal needs an actual source world.");
                Require(p.Stage!=ColonisationAiStage.Assembling||p.TransportShipId!=null&&p.TargetPlanetId!=null,"Incomplete assembling assignment.");
                Require(p.Stage!=ColonisationAiStage.Loaded&&p.Stage!=ColonisationAiStage.Deploying||p.ManifestId!=null&&p.TargetPlanetId!=null,"Incomplete loaded assignment.");
                Require(p.Stage!=ColonisationAiStage.Supporting||p.CampId!=null,"Supporting requires retained actual settlement.");
                Require(p.BlueprintId==null?p.BlueprintRevision==0:game.Blueprints!.Any(b=>b.BlueprintId==p.BlueprintId&&b.Revision==p.BlueprintRevision&&b.OwnerCivilizationId==p.ActorId),"Foreign AI source revision.");
                Require(p.ManifestId==null||colonisation!.State.ManifestRecords!.Any(m=>m.ManifestId==p.ManifestId&&m.OwnerId==p.ActorId),"Foreign AI manifest.");
                Require(p.CampId==null||colonisation!.State.CampSources!.Any(c=>c.CampId==p.CampId&&c.OwnerId==p.ActorId),"Foreign AI camp.");
                Require(p.OwnAttemptedTargets!=null&&p.OwnAttemptedTargets.Count<=expansion.Source.Policy.FailedTargets
                    &&p.OwnAttemptedTargets.Distinct(StringComparer.Ordinal).Count()==p.OwnAttemptedTargets.Count
                    &&p.OwnAttemptedTargets.All(id=>colonisation!.State.Surveys!.Any(s=>s.ActorId==p.ActorId&&s.PlanetId==id)),"Unobserved or duplicate AI attempted target.");
                Require(p.LatestActions!=null&&p.LatestActions.Count<=expansion.Source.Policy.MaximumAttempts&&p.LatestActions.Count(a=>a.Accepted)<=expansion.Source.Policy.MaximumAccepted,"AI action bound.");
                Require(p.LatestActions!.Select(a=>a.CommandId).Distinct(StringComparer.Ordinal).Count()==p.LatestActions.Count,"Duplicate AI action receipt.");
                foreach(var a in p.LatestActions!)Require(Enum.IsDefined(typeof(ColonisationAiAction),a.Action)&&Enum.IsDefined(typeof(CampaignTacticalError),a.Error)&&a.AfterRevision<=game.StateRevision&&EntityId.TryParse(a.CommandId,out _)&&a.CommandId.Length<=128&&a.CommandId.StartsWith("ai-command.colonisation.",StringComparison.Ordinal)
                    &&a.CommandId=="ai-command.colonisation.a"+plans.OrderBy(x=>x.ActorId,StringComparer.Ordinal).Select(x=>x.ActorId).ToList().IndexOf(p.ActorId).ToString(System.Globalization.CultureInfo.InvariantCulture)+".t"+p.LastPlannedTurn.ToString(System.Globalization.CultureInfo.InvariantCulture)+".n"+p.LatestActions.IndexOf(a).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    &&a.BeforeRevision>=0&&a.AfterRevision>=a.BeforeRevision&&(a.Accepted?(System.Numerics.BigInteger)a.AfterRevision==(System.Numerics.BigInteger)a.BeforeRevision+1&&a.Error==CampaignTacticalError.None:a.AfterRevision==a.BeforeRevision&&a.Error!=CampaignTacticalError.None),"Invalid actual AI action receipt.");
            }
        }
    }
}
