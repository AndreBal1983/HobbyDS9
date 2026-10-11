using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Simulation.Tests;

internal static class VerticalSliceFixture
{
    internal static ScenarioDto Scenario(bool humans = true, bool weighted = false)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json"));
        var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
        scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!weighted)
        {
            foreach(var civilization in scenario.InitialState.Civilizations!) { civilization.ScienceAllocationBps=0; civilization.ScienceFocus=ScienceDiscipline.Construction; }
            foreach(var application in scenario.Definitions.EngineeringApplications!) { application.ScienceRequirements=new();application.BaseEngineeringCost=9; }
            // Historical unit-cost mechanics fixtures stay isolated from authored weighted balance.
            var association=scenario.Definitions.NavigationRegions!.Where(n=>n.AssociatedSystemId!=null).ToDictionary(n=>n.NodeId,n=>n.AssociatedSystemId!);
            foreach(var fleet in scenario.InitialState.Fleets!){fleet.LocationNodeId=association[fleet.LocationNodeId];fleet.RouteNodeIds=fleet.RouteNodeIds!.Select(n=>association[n]).ToList();}
            NavigationFixture.Definitions(scenario.Definitions,scenario.InitialState.StarSystems!.Select(n=>n.SystemId).ToArray());
            NavigationFixture.State(scenario.InitialState,scenario.Definitions);
            foreach(var design in scenario.Definitions.ShipDesigns!)design.MovementBudget=1;
        }
        if(humans)
        {
            foreach (var c in scenario.InitialState.Civilizations!) c.ControllerKind = ControllerKind.Human;
        }
        return scenario;
    }
    internal static SaveCompatibility Compatibility(ScenarioDto s) => new(s.ScenarioId, s.RulesVersion, s.ContentHash);
    internal static SaveEnvelopeDto Save(ScenarioDto s) => new() { SaveFormatVersion = FoundationVersions.SaveFormatVersion,
        ScenarioId = s.ScenarioId, RulesVersion = s.RulesVersion, ContentHash = s.ContentHash, GameState = s.InitialState };
    internal static SimulationSession Session(ScenarioDto? scenario = null, Func<StrategicTurnPhase, GameStateDto, bool>? probe = null)
    { var s = scenario ?? Scenario(); return new(Save(s), s.Definitions, Compatibility(s), new Dictionary<CommandKind, ICommandHandler>(), probe); }
    internal static string Json(SimulationSession s) => JsonSerializer.Serialize(s.ExportSave());
    internal static TurnResult Advance(SimulationSession s) { var p = s.CaptureSnapshot(); return CampaignCombatFixture.Complete(s, s.AdvanceTurn(p.TurnNumber, p.StateRevision)); }
    internal static CommandEnvelope Command(SimulationSession s, CommandKind kind, string id, string actor = "civ.federation")
    { var p = s.CaptureSnapshot(); return new() { CommandId = id, ActorCivilizationId = actor, ExpectedTurn = p.TurnNumber, ExpectedRevision = p.StateRevision, Kind = kind }; }
    internal static CommandResult Enqueue(SimulationSession s, string id = "cmd.enqueue", string design = "ship-design.scout", string colony = "colony.sol", string actor = "civ.federation")
    { var c = Command(s, CommandKind.EnqueueProduction, id, actor); c.EnqueueProduction = new() { ColonyId = colony, DefinitionId = design }; return s.SubmitCommand(c); }
    internal static CommandResult Cancel(SimulationSession s, string item, string id = "cmd.cancel", string colony = "colony.sol")
    { var c = Command(s, CommandKind.CancelProduction, id); c.CancelProduction = new() { ColonyId = colony, QueueItemId = item }; return s.SubmitCommand(c); }
    internal static ColonyStateDto Colony(SimulationSession s) => s.ExportSave().GameState.Colonies!.Single(c => c.ColonyId == "colony.sol");
}
