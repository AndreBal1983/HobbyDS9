using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class PlayableFleetWalkthrough
{
    internal static CampaignPlayableFleetSaveDto Run(CampaignPlayableFleetPackageDto package, bool reload = false, bool reverse = false)
    {
        const string human = "civ.federation";
        var app = CampaignPlayableFleetApplication.StartPackage(package);
        void Require(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); }
        void Reload()
        {
            if (!reload) return;
            var before = JsonSerializer.Serialize(app.ExportPlayableSave());
            app = CampaignPlayableFleetApplication.LoadSave(package, JsonSerializer.Deserialize<CampaignPlayableFleetSaveDto>(before)!);
            Require(before == JsonSerializer.Serialize(app.ExportPlayableSave()), "Whole-root reload changed state.");
        }
        void Advance()
        {
            var s = app.CaptureStatus(); var result = app.AdvanceTurn(s.Turn, s.CampaignRevision);
            Require(result.Accepted, "Advance: " + result.Error); Reload();
        }
        for (int i = 0; i < 2; i++)
        {
            var s = app.CaptureStatus();
            Require(app.SubmitStrategicCommand(new CommandEnvelope { ActorCivilizationId = human,
                CommandId = "command.playable.buy." + i, ExpectedTurn = s.Turn, ExpectedRevision = s.CampaignRevision,
                Kind = CommandKind.EnqueueProduction, EnqueueProduction = new EnqueueProductionPayload {
                    ColonyId = "colony.sol", DefinitionId = "ship-design.source-cruiser" } }).Accepted, "Paid construction");
            Reload();
        }
        for (int i = 0; i < 4; i++) Advance();
        var built = app.ExportPlayableSave().Integrated!.BuiltShips!.Where(s => s.OwnerCivilizationId == human).ToList();
        Require(built.Count == 2, "Two paid ships must complete.");
        var ships = app.CaptureStrategy(human).Ships.Select(s => s.ShipId.Value).OrderBy(id => id, StringComparer.Ordinal).ToList();
        if (reverse) ships.Reverse();
        var status = app.CaptureStatus();
        var organized = app.SubmitFleetOrganization(new SubmitCampaignFleetOrganizationDto {
            ActorId = human, CommandId = "command.playable.assemble", ExpectedTurn = status.Turn,
            ExpectedRevision = status.CampaignRevision, Kind = CampaignFleetOrganizationKind.Create,
            DisplayName = "Bajor Task Force", ShipIds = ships });
        Require(organized.Accepted, "Assemble: " + organized.Error); Reload();
        var fleet = app.GetOwnStrategicFleets(human).Single();
        var contact = app.GetKnownContacts(human).Where(c => c.Fresh).OrderBy(c => c.FleetId, StringComparer.Ordinal).First();
        var route = app.FindKnownRoute(human, fleet.LocationNodeId.Value, contact.LastSeenNodeId);
        Require(route.Success, "Known route to actual last-seen contact");
        status = app.CaptureStatus();
        Require(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto { ActorId = human,
            CommandId = "command.playable.follow", ExpectedTurn = status.Turn, ExpectedRevision = status.CampaignRevision,
            Order = new CampaignTravelOrderDto { FleetId = organized.FleetId!, Intent = CampaignTravelIntent.Follow,
                ContactFleetId = contact.FleetId, ExpectedContactSeenTurn = contact.LastSeenTurn, RouteNodeIds = route.Steps.ToList() } }).Accepted,
            "Lawful Follow"); Reload();
        for (int step = 0; step < 12 && app.CaptureStatus().Phase == SessionPhase.Planning; step++) Advance();
        Require(app.CaptureStatus().Phase == SessionPhase.AwaitingCombat, "Fixture Follow did not meet opponent.");
        foreach (var id in app.GetEncounters(human).OrderBy(id => id, StringComparer.Ordinal))
        {
            Require(app.CaptureBattlePlayback(id, human).Rounds.Count == 0, "Uncommitted rounds must not play.");
            while (!app.Observe(id, human).Complete)
            {
                var observation = app.Observe(id, human);
                var orders = observation.StandingOrders.Select(o => new CampaignTacticalFleetOrderDto {
                    FleetId = o.FleetId, Maneuver = CampaignFleetManeuver.Charge,
                    FocusFleetId = observation.LegalFocusFleetIds.FirstOrDefault() }).ToList();
                if (reverse) orders.Reverse();
                Require(app.CommitRound(new CommitCampaignTacticalRoundDto { ActorId = human, CommandId = "command.playable.round." + observation.Round,
                    EncounterId = id, ExpectedTurn = observation.Turn, ExpectedRound = observation.Round,
                    ExpectedBoundaryRevision = observation.BoundaryRevision, Replacements = orders }).Accepted, "Joint human/AI round");
                var playback = app.CaptureBattlePlayback(id, human);
                Require(playback.Rounds.Count == observation.Round && playback.Rounds.Count <= 3, "Only actual committed rounds");
                var before = JsonSerializer.Serialize(app.ExportPlayableSave());
                Require(JsonSerializer.Serialize(playback) == JsonSerializer.Serialize(app.CaptureBattlePlayback(id, human)), "Repeat playback changed.");
                Require(before == JsonSerializer.Serialize(app.ExportPlayableSave()), "Playback mutated authority.");
                Reload();
            }
        }
        var complete = app.GetEncounters(human).Select(id => JsonSerializer.Serialize(app.CaptureBattlePlayback(id, human))).ToList();
        status = app.CaptureStatus();
        Require(app.FinalizeBatch(status.Turn, status.CampaignRevision).Accepted, "Atomic aftermath");
        Reload();
        Require(complete.SequenceEqual(app.GetBattleHistory(human).Select(b => JsonSerializer.Serialize(b))), "Aftermath or reload changed observed battle history.");
        Require(app.CaptureStatus().Phase == SessionPhase.Planning, "Planning must resume.");
        return app.ExportPlayableSave();
    }
}
