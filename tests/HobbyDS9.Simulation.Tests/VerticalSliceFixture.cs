using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Simulation.Tests;

internal static class VerticalSliceFixture
{
    internal static ScenarioDto Scenario(bool humans = true, bool weighted = false, bool fullEngineeringTree = false)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json"));
        var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
        scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        // Historical mechanics fixtures retain their original one-project catalogue.
        // Full-tree acceptance uses the actual authored content without this reduction.
        if (!fullEngineeringTree)
        {
            scenario.Definitions.EngineeringApplications!.RemoveAll(a => a.DefinitionId != "engineering.advanced-starship-engineering");
            scenario.Definitions.ShipDesignCatalogue!.Components!.RemoveAll(c => c.VersionNumber > 1);
            scenario.Definitions.ShipDesignCatalogue.Hulls!.RemoveAll(h => h.VersionNumber > 1);
            scenario.Definitions.ShipDesignVersionAccess!.RemoveAll(g => g.RequiredEngineeringApplicationDefinitionIds!.Count > 0);
        }
        if (!weighted)
        {
            foreach(var civilization in scenario.InitialState.Civilizations!) { civilization.ScienceAllocationUnits=0; }
            foreach(var application in scenario.Definitions.EngineeringApplications!) { application.ScienceRequirements=new();application.BaseEngineeringCost=9*600; }
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
    // Content additions change the envelope identity. Normalize ONLY that identity to
    // compare every gameplay byte with the previously accepted 25-turn golden.
    internal static string LegacyGameplayHash(SaveEnvelopeDto save)
    {
        var copy = JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(save))!;
        copy.ContentHash = "3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59";
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(copy))).ToLowerInvariant();
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
