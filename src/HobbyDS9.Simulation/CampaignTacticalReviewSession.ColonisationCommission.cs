using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private FrontierCommissionSnapshot CaptureColonisationCommission(string actor)
        {
            Require(Actor(actor)==CampaignTacticalError.None,"Controlled colonisation mission required.");
                var game=state.Campaign.GameState;
                if(CampaignActivitiesData.CommissionContentFingerprint(activities!.Content)!=FrontierCommissionOfficialContent)
                    return new FrontierCommissionSnapshot(FrontierCommissionAvailability.UnsupportedContent,actor,game.TurnNumber,game.StateRevision,colonisation!.PackageHash,Array.Empty<FrontierCommissionObjective>());
                var history=colonisation!.History.Where(h=>h.Completed&&h.Order!.OwnerId==actor).OrderBy(h=>h.ResolvedTurn).ThenBy(h=>h.Order!.QueueItemId,StringComparer.Ordinal).ToList();
                var structure=history.FirstOrDefault(h=>h.Order!.Kind==ColonisationProductionKind.Infrastructure&&activities.State.Structures!.Any(s=>s.QueueItemId==h.Order.QueueItemId&&s.ColonyId==h.Order.ColonyId&&s.DefinitionId==h.Order.DefinitionId&&s.OwnerId==actor&&s.CompletedTurn==h.ResolvedTurn));
                var built=history.FirstOrDefault(h=>h.Order!.Kind==ColonisationProductionKind.PublishedShip&&published!.Records.Any(p=>p.ShipId==h.ShipId&&p.QueueItemId==h.Order.QueueItemId&&p.OwnerCivilizationId==actor&&p.BlueprintId==h.Order.Blueprint!.BlueprintId&&p.BlueprintRevision==h.Order.Blueprint.Revision&&p.TurnBuilt==h.ResolvedTurn));
                var own=activities.State.Findings!.Where(f=>f.ActorId==actor).ToList();
                bool Minor(ActivityFindingDto f)=>activities.State.Sites!.Single(s=>s.SiteId==f.SiteId).MinorActorId!=null;
                var complete=own.Where(f=>f.Stage==ActivityProgressStage.Completed&&f.ResolvedTurn.HasValue&&activities.State.Observations!.Any(o=>o.ActorId==actor&&o.SiteId==f.SiteId))
                    .OrderBy(f=>f.ResolvedTurn).ThenBy(f=>f.SiteId,StringComparer.Ordinal).ThenBy(f=>f.FindingId,StringComparer.Ordinal).ToList();
                var science=complete.FirstOrDefault(f=>!Minor(f)&&ValidScience(f));
                var society=complete.FirstOrDefault(f=>Minor(f)&&(f.ChoiceId=="choice.ilex.observe"||f.ChoiceId=="choice.namar.observe"||f.ChoiceId=="choice.ilex.channel"||f.ChoiceId=="choice.namar.channel")
                    &&ActivityDefinition(activities.State.Sites!.Single(s=>s.SiteId==f.SiteId).DefinitionId).Choices!.Any(c=>c.ChoiceId==f.ChoiceId&&c.FindingId==f.FindingId));
                FrontierCommissionEvidence? Finding(ActivityFindingDto? f)
                {if(f==null)return null;var site=activities.State.Sites!.Single(s=>s.SiteId==f.SiteId);return new FrontierCommissionEvidence(f.SiteId+"|"+f.FindingId,f.ShipId,ActivityDefinition(site.DefinitionId).Title,f.ResolvedTurn!.Value,f.Report,f.InsightApplied,f.InsightUnlearnable);}
                bool Pending(ActivityFindingDto f)=>f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim;
                var objectives=new List<FrontierCommissionObjective>{
                    new FrontierCommissionObjective("infrastructure","Establish frontier capacity","Complete one structure at an owned world using delivered materials.",colonisation!.Production.Any(q=>q.OwnerId==actor&&q.Kind==ColonisationProductionKind.Infrastructure),
                        structure==null?null:new FrontierCommissionEvidence(structure.Order!.QueueItemId,structure.Order.ColonyId,definitions.Infrastructure!.Single(s=>s.DefinitionId==structure.Order.DefinitionId).DisplayName,structure.ResolvedTurn,"Built with actual Industry and consumed local materials.")),
                    new FrontierCommissionObjective("ship","Launch an own-built class","Complete a ship from your yards. The starting vessel is not a constructed ship.",colonisation!.Production.Any(q=>q.OwnerId==actor&&q.Kind==ColonisationProductionKind.PublishedShip),
                        built==null?null:new FrontierCommissionEvidence(built.Order!.QueueItemId,built.ShipId!,game.Blueprints!.Single(p=>p.BlueprintId==built.Order.Blueprint!.BlueprintId&&p.Revision==built.Order.Blueprint.Revision).Fit!.DisplayName,built.ResolvedTurn,"Launched revision "+built.Order.Blueprint!.Revision+". Later losses do not erase this historical launch.")),
                    new FrontierCommissionObjective("science","File a finite scientific result","Complete and claim a phenomenon study. An aborted or failed attempt is not a scientific result; explore further known routes if needed.",own.Any(f=>!Minor(f)&&Pending(f)),Finding(science)),
                    new FrontierCommissionObjective("society","Assess an independent society","Complete distant study or resolve a contact request. Acceptance and refusal both count; cooperation is not required.",own.Any(f=>Minor(f)&&Pending(f)),Finding(society))};
                return new FrontierCommissionSnapshot(FrontierCommissionAvailability.Available,actor,game.TurnNumber,game.StateRevision,colonisation!.PackageHash,objectives);
        }
    }
}
