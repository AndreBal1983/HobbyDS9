using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class AiPlanningPhase
    {
        internal static void Execute(Func<GameStateDto> read, DefinitionCatalogDto definitions, GalaxyGraph galaxy,
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
                    .UnlockedResearchDefinitionIds!.Contains(id, StringComparer.Ordinal));
                if (actor.ResearchTargetDefinitionId == null)
                {
                    var target = definitions.ResearchNodes!.Where(n => !actor.UnlockedResearchDefinitionIds!.Contains(n.DefinitionId, StringComparer.Ordinal)
                        && ResearchSatisfied(n.PrerequisiteDefinitionIds!)).OrderBy(n => n.ResearchCost).ThenBy(n => n.DefinitionId, StringComparer.Ordinal).FirstOrDefault();
                    if (target != null)
                    {
                        var command = Envelope(CommandKind.SetResearchTarget, 0); command.SetResearchTarget = new SetResearchTargetPayload { TargetDefinitionId = target.DefinitionId };
                        results.Add(submit(command));
                    }
                }
                var colonies = read().Colonies!.Where(c => c.OwnerCivilizationId == actorId).Select(c => c.ColonyId).OrderBy(id => id, StringComparer.Ordinal).ToList();
                for (var i = 0; i < colonies.Count; i++)
                {
                    var colony = read().Colonies!.Single(c => c.ColonyId == colonies[i]);
                    if (colony.ProductionQueue!.Count != 0) continue;
                    var owner = read().Civilizations!.Single(c => c.CivilizationId == actorId);
                    var design = definitions.ShipDesigns!.Where(d => ResearchSatisfied(d.RequiredResearchDefinitionIds!) && owner.Credits >= d.CreditCost && d.ResourceCosts!.All(r => PopulationRules.Stock(colony.Stockpiles!, r.ResourceDefinitionId) >= r.Amount) && WorldEconomy.Infrastructure(colony, definitions).Any(i => i.definition.Shipbuilding))
                        .OrderBy(d => d.IndustryCost).ThenBy(d => d.DefinitionId, StringComparer.Ordinal).FirstOrDefault();
                    if (design == null) continue;
                    var command = Envelope(CommandKind.EnqueueProduction, i); command.EnqueueProduction = new EnqueueProductionPayload { ColonyId = colony.ColonyId, DefinitionId = design.DefinitionId };
                    results.Add(submit(command));
                }
                var fleetIds = read().Fleets!.Where(f => f.OwnerCivilizationId == actorId).Select(f => f.FleetId).OrderBy(id => id, StringComparer.Ordinal).ToList();
                for (var i = 0; i < fleetIds.Count; i++)
                {
                    var fleet = read().Fleets!.Single(f => f.FleetId == fleetIds[i]);
                    if (fleet.RouteSystemIds!.Count != 0) continue;
                    var systems = read().StarSystems!.ToDictionary(n => n.SystemId, StringComparer.Ordinal);
                    var candidates = galaxy.GetNeighbors(new EntityId(fleet.LocationSystemId)).Select(id => systems[id.Value])
                        .Where(n => n.OwnerCivilizationId != actorId).Select(n => (n.SystemId, Score: n.OwnerCivilizationId == null ? 1 : 2)).ToList();
                    if (candidates.Count == 0) continue;
                    var bestScore = candidates.Max(n => n.Score);
                    var best = candidates.Where(n => n.Score == bestScore).OrderBy(n => n.SystemId, StringComparer.Ordinal).ToList();
                    var rng = SplitMix64Random.FromDto(read().RngState);
                    var choice = best.Count == 1 ? 0 : (int)rng.NextBelow((ulong)best.Count);
                    var command = Envelope(CommandKind.SetFleetRoute, i); command.SetFleetRoute = new SetFleetRoutePayload { FleetId = fleet.FleetId, RouteSystemIds = new List<string> { best[choice].SystemId } };
                    var result = submit(command); results.Add(result);
                    // A failed route command consumes no campaign draw. A later
                    // phase failure discards this draw with the whole staging state.
                    if (result.Accepted && best.Count > 1) read().RngState = rng.ToDto();
                }
            }
        }
    }
}
