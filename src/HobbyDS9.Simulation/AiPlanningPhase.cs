using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class AiPlanningPhase
    {
        internal static void Execute(Func<GameStateDto> read, DefinitionCatalogDto definitions, WeightedNavigationGraph galaxy,
            Func<CommandEnvelope, CommandResult> submit, List<CommandResult> results)
        {
            // Actor ordinal is its position among ALL canonical campaign actors,
            // so controller changes cannot change IDs of other actors this turn.
            var actorIds = read().Civilizations!.Select(c => c.CivilizationId).OrderBy(id => id, StringComparer.Ordinal).ToList();
            for (var actorOrdinal = 0; actorOrdinal < actorIds.Count; actorOrdinal++)
            {
                var actorId = actorIds[actorOrdinal];
                var actor = read().Civilizations!.Single(c => c.CivilizationId == actorId);
                if (actor.ControllerKind != ControllerKind.AI) continue;
                CommandEnvelope Envelope(CommandKind kind, int ordinal)
                {
                    var state = read();
                    return new CommandEnvelope
                    {
                        CommandId = "ai-command.t" + state.TurnNumber.ToString("d10", CultureInfo.InvariantCulture)
                        + ".a" + actorOrdinal.ToString("d8", CultureInfo.InvariantCulture) + ".k" + ((int)kind).ToString(CultureInfo.InvariantCulture)
                        + ".n" + ordinal.ToString("d8", CultureInfo.InvariantCulture),
                        ActorCivilizationId = actorId,
                        ExpectedTurn = state.TurnNumber,
                        ExpectedRevision = state.StateRevision,
                        Kind = kind
                    };
                }
                bool ResearchSatisfied(IEnumerable<string> prerequisites) => prerequisites.All(id => read().Civilizations!.Single(c => c.CivilizationId == actorId)
                    .MatureEngineeringApplicationDefinitionIds!.Contains(id, StringComparer.Ordinal));
                if(actor.ScienceFocus==null||actor.Disciplines!.Single(d=>d.Discipline==actor.ScienceFocus).Level==10)
                {
                    var candidates=actor.Disciplines!.Where(d=>d.Level<10).OrderBy(d=>d, System.Collections.Generic.Comparer<ScienceDisciplineStateDto>.Create((a,b)=>
                    {
                        int level=a.Level.CompareTo(b.Level); if(level!=0)return level;
                        int fraction=((System.Numerics.BigInteger)a.ProgressTowardNextLevel*ScienceRules.IntervalCost(b.Level,read().ResearchPaceFactor!.Value)).CompareTo((System.Numerics.BigInteger)b.ProgressTowardNextLevel*ScienceRules.IntervalCost(a.Level,read().ResearchPaceFactor!.Value));
                        return fraction!=0?fraction:a.Discipline.CompareTo(b.Discipline);
                    })).ToList();
                    if(candidates.Count>0){var focusCommand=Envelope(CommandKind.SetScienceFocus,0);focusCommand.SetScienceFocus=new SetScienceFocusPayload{Discipline=candidates[0].Discipline};results.Add(submit(focusCommand));}
                }
                actor=read().Civilizations!.Single(c=>c.CivilizationId==actorId);
                if (actor.EngineeringTargetDefinitionId == null)
                {
                    var target=definitions.EngineeringApplications!.Where(a=>EngineeringRules.Availability(actor,a,read().ResearchPaceFactor!.Value)==CommandErrorCode.None)
                        .OrderBy(a=>ScienceRules.AdjustCost(a.BaseEngineeringCost,read().ResearchPaceFactor!.Value)-(actor.EngineeringProgress!.SingleOrDefault(p=>p.DefinitionId==a.DefinitionId)?.Progress??0))
                        .ThenBy(a=>a.DefinitionId,StringComparer.Ordinal).FirstOrDefault();
                    if(target!=null){var command=Envelope(CommandKind.SetEngineeringTarget,0);command.SetEngineeringTarget=new SetEngineeringTargetPayload{TargetDefinitionId=target.DefinitionId};results.Add(submit(command));}
                }
                var colonies = read().Colonies!.Where(c => c.OwnerCivilizationId == actorId).Select(c => c.ColonyId).OrderBy(id => id, StringComparer.Ordinal).ToList();
                for (var i = 0; i < colonies.Count; i++)
                {
                    var colony = read().Colonies!.Single(c => c.ColonyId == colonies[i]);
                    // Shared stamped policy command; species and affinities do not select policy.
                    var pressure = colony.Politics!.Pressure;
                    var policy = definitions.GovernancePolicies!.OrderByDescending(p => pressure >= 6000 ? p.Suppression : 0)
                        .ThenByDescending(p => p.SatisfactionChange + 2L*p.IntegrationChange + p.AlignmentChange - 2L*p.RadicalisationChange)
                        .ThenBy(p => p.DefinitionId,StringComparer.Ordinal).First();
                    if (policy.DefinitionId != colony.Politics.GovernancePolicyDefinitionId)
                    {
                        var policyCommand = Envelope(CommandKind.SetWorldGovernancePolicy,i);
                        policyCommand.SetWorldGovernancePolicy = new SetWorldGovernancePolicyPayload { ColonyId=colony.ColonyId, PolicyDefinitionId=policy.DefinitionId };
                        results.Add(submit(policyCommand));
                        colony = read().Colonies!.Single(c=>c.ColonyId==colonies[i]);
                    }
                    if (colony.ProductionQueue!.Count != 0) continue;
                    var owner = read().Civilizations!.Single(c => c.CivilizationId == actorId);
                    var design = definitions.ShipDesigns!.Where(d => ResearchSatisfied(d.RequiredEngineeringApplicationDefinitionIds!) && owner.Credits >= d.CreditCost && d.ResourceCosts!.All(r => PopulationRules.Stock(colony.Stockpiles!, r.ResourceDefinitionId) >= r.Amount) && WorldEconomy.Infrastructure(colony, definitions).Any(i => i.definition.Shipbuilding))
                        .OrderBy(d => d.IndustryCost).ThenBy(d => d.DefinitionId, StringComparer.Ordinal).FirstOrDefault();
                    if (design == null) continue;
                    var command = Envelope(CommandKind.EnqueueProduction, i); command.EnqueueProduction = new EnqueueProductionPayload { ColonyId = colony.ColonyId, DefinitionId = design.DefinitionId };
                    results.Add(submit(command));
                }
                var fleetIds = read().Fleets!.Where(f => f.OwnerCivilizationId == actorId).Select(f => f.FleetId).OrderBy(id => id, StringComparer.Ordinal).ToList();
                for (var i = 0; i < fleetIds.Count; i++)
                {
                    var fleet = read().Fleets!.Single(f => f.FleetId == fleetIds[i]);
                    if (fleet.RouteNodeIds!.Count != 0) continue;
                    var systems = read().StarSystems!.ToDictionary(n => n.SystemId, StringComparer.Ordinal);
                    var candidates = galaxy.Regions.Where(n => n.AssociatedSystemId != null && systems[n.AssociatedSystemId].OwnerCivilizationId != actorId)
                        .Select(n => (Node: n.NodeId, Score: systems[n.AssociatedSystemId!].OwnerCivilizationId == null ? 1 : 2,
                            Route: galaxy.FindRoute(read(), actorId, fleet.LocationNodeId, n.NodeId)))
                        .Where(n => n.Route.Success && n.Route.Steps.Count > 0).ToList();
                    if (candidates.Count == 0) continue;
                    var bestCost = candidates.Min(n => n.Route.TotalCost!.Value);
                    var bestScore = candidates.Where(n => n.Route.TotalCost == bestCost).Max(n => n.Score);
                    var best = candidates.Where(n => n.Score == bestScore && n.Route.TotalCost == bestCost).OrderBy(n => n.Node, StringComparer.Ordinal).ToList();
                    var rng = SplitMix64Random.FromDto(read().RngState);
                    var choice = best.Count == 1 ? 0 : (int)rng.NextBelow((ulong)best.Count);
                    var command = Envelope(CommandKind.SetFleetRoute, i); command.SetFleetRoute = new SetFleetRoutePayload { FleetId = fleet.FleetId, RouteNodeIds = best[choice].Route.Steps.ToList() };
                    var result = submit(command); results.Add(result);
                    // A failed route command consumes no campaign draw. A later
                    // phase failure discards this draw with the whole staging state.
                    if (result.Accepted && best.Count > 1) read().RngState = rng.ToDto();
                }
            }
        }
    }
}
