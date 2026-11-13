using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed partial class PlayableFleetTests
{
    static void AssertVisibleIdentityPairs(CampaignPlaybackBoundary boundary)
    {
        var own = boundary.OwnShips.Select(s => s.ShipId).ToHashSet();
        var visible = boundary.OwnShips.Concat(boundary.VisibleEnemies).Select(s => s.ShipId).OrderBy(s => s, StringComparer.Ordinal);
        Assert.Equal(visible, boundary.ShipIdentities.Select(s => s.ShipId));
        Assert.Equal(boundary.ShipIdentities.Count, boundary.ShipIdentities.Select(s => s.ShipId).Distinct(StringComparer.Ordinal).Count());
        foreach (var identity in boundary.ShipIdentities)
        {
            Assert.Equal(own.Contains(identity.ShipId) ? "civilization.federation" : "civilization.cardassian", identity.CivilizationDefinitionId);
            Assert.Equal(own.Contains(identity.ShipId) ? "ship-design.source-cruiser" : "ship-design.travel-target", identity.ShipDesignDefinitionId);
        }
    }
    [Fact]
    public void PlaybackVisualIdentityUsesExactDefinitionsForPaidShipsDespiteIdenticalDisplayLabels()
    {
        var p = Package(); var definitions = p.Integrated!.Physical!.CorePackage!.Campaign!.Definitions;
        definitions.Civilizations!.ForEach(c => c.DisplayName = "Identical label");
        definitions.ShipDesigns!.ForEach(d => d.DisplayName = "Identical label");
        var app = CampaignPlayableFleetApplication.StartPackage(p);
        Buy(app); Buy(app); for (int i = 0; i < 4; i++) Advance(app);
        var builtIds = app.ExportPlayableSave().Integrated!.BuiltShips!.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId).ToList();
        Assert.Equal(2, builtIds.Count);
        Assert.True(app.SubmitFleetOrganization(Organization(app,
            Game(app).Ships!.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId))).Accepted);
        Assert.DoesNotContain(Game(app).Fleets!, f => f.FleetId == "fleet.federation-scout");
        var fleet = Assert.Single(app.GetOwnStrategicFleets(Human));
        var contact = app.GetKnownContacts(Human).First(c => c.Fresh);
        var route = app.FindKnownRoute(Human, fleet.LocationNodeId.Value, contact.LastSeenNodeId);
        var stamp = app.CaptureStatus();
        Assert.True(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto { ActorId = Human,
            CommandId = "command.identity.follow", ExpectedTurn = stamp.Turn, ExpectedRevision = stamp.CampaignRevision,
            Order = new CampaignTravelOrderDto { FleetId = fleet.FleetId.Value, Intent = CampaignTravelIntent.Follow,
                ContactFleetId = contact.FleetId, ExpectedContactSeenTurn = contact.LastSeenTurn, RouteNodeIds = route.Steps.ToList() } }).Accepted);
        for (int i = 0; i < 6 && app.CaptureStatus().Phase == SessionPhase.Planning; i++) Advance(app);
        var id = Assert.Single(app.GetEncounters(Human));
        var initial = app.CaptureBattlePlayback(id, Human).Initial;
        AssertVisibleIdentityPairs(initial);
        Assert.All(initial.OwnShips.Concat(initial.VisibleEnemies), s => Assert.Equal("Identical label", s.Type));
        Assert.All(builtIds, ship => Assert.Contains(initial.ShipIdentities, i => i.ShipId == ship));
        while (!app.Observe(id, Human).Complete) Round(app, id);
        var playback = app.CaptureBattlePlayback(id, Human);
        foreach (var round in playback.Rounds) { AssertVisibleIdentityPairs(round.Before); AssertVisibleIdentityPairs(round.After); }
        var before = Json(app.ExportPlayableSave());
        Assert.Equal(Json(playback), Json(app.CaptureBattlePlayback(id, Human)));
        Assert.Equal(before, Json(app.ExportPlayableSave()));
        app = Reload(p, app);
        Assert.Equal(Json(playback), Json(app.CaptureBattlePlayback(id, Human)));
        var status = app.CaptureStatus();
        Assert.True(app.FinalizeBatch(status.Turn, status.CampaignRevision).Accepted);
        app = Reload(p, app);
        Assert.Equal(Json(playback), Json(Assert.Single(app.GetBattleHistory(Human))));
    }
    [Fact]
    public void PlaybackIdentityIsBoundaryLocalAndNeverBackfillsAHiddenShipAfterReveal()
    {
        // Isolated projection fixture; no caller-supplied result or visibility is imported as authority.
        var app = Pending(); var backend = Backend(app);
        var battle = app.ExportPlayableSave().Integrated!.Physical!.Core.Battles.Single().State;
        var beforeState = Json(app.ExportPlayableSave());
        var method = typeof(HobbyDS9.Simulation.CampaignTacticalReviewSession).GetMethod("PlaybackBoundary",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var hidden = battle.Formations.SelectMany(f => f.Ships).Select(s => s.ShipId).ToList();
        var before = (CampaignPlaybackBoundary)method.Invoke(backend, new object[] { battle, hidden, Human })!;
        Assert.Empty(before.VisibleEnemies);
        Assert.Equal(before.OwnShips.Select(s => s.ShipId).OrderBy(s => s, StringComparer.Ordinal), before.ShipIdentities.Select(s => s.ShipId));
        Assert.DoesNotContain(before.ShipIdentities, s => s.CivilizationDefinitionId == "civilization.cardassian");
        var frozen = Json(before);
        hidden.Clear();
        var revealed = (CampaignPlaybackBoundary)method.Invoke(backend, new object[] { battle, hidden, Human })!;
        AssertVisibleIdentityPairs(revealed);
        Assert.Contains(revealed.ShipIdentities, s => s.CivilizationDefinitionId == "civilization.cardassian");
        Assert.Equal(frozen, Json(before));
        var revealedFrozen = Json(revealed);
        hidden.AddRange(battle.Formations.Where(f => f.SideId != Human).SelectMany(f => f.Ships).Select(s => s.ShipId));
        var concealed = (CampaignPlaybackBoundary)method.Invoke(backend, new object[] { battle, hidden, Human })!;
        Assert.Equal(frozen, Json(concealed));
        Assert.Equal(revealedFrozen, Json(revealed));
        Assert.Equal(beforeState, Json(app.ExportPlayableSave()));
        Assert.Throws<NotSupportedException>(() => ((ICollection<CampaignPlaybackShipIdentity>)revealed.ShipIdentities)
            .Add(new CampaignPlaybackShipIdentity("ship.forged", "civilization.forged", "ship-design.forged")));
        var input = revealed.ShipIdentities.ToList();
        var detached = new CampaignPlaybackBoundary(revealed.OwnShips, revealed.VisibleEnemies, revealed.Relations, input);
        input.Clear();
        Assert.Equal(revealedFrozen, Json(detached));
        var legacyConstructor = new CampaignPlaybackBoundary(revealed.OwnShips, revealed.VisibleEnemies, revealed.Relations);
        Assert.Empty(legacyConstructor.ShipIdentities);
        Assert.Equal(Json(revealed.OwnShips), Json(legacyConstructor.OwnShips));
    }
    [Fact]
    public void HistoricalVisualIdentitiesSurviveDestroyedShipRemovalWithoutCurrentRosterLookup()
    {
        var p = ThreeSidePackage(true);
        var app = CampaignPlayableFleetApplication.StartPackage(p); Advance(app);
        var id = Assert.Single(app.GetEncounters(Human)); Round(app, id);
        var result = app.CaptureBattlePlayback(id, Human);
        Assert.Equal(0, Assert.Single(result.Rounds.Last().After.OwnShips).Hull);
        var identity = Assert.Single(result.Initial.ShipIdentities, s => s.ShipId == "ship.equipment-cruiser");
        Assert.Equal("civilization.federation", identity.CivilizationDefinitionId);
        Assert.Equal("ship-design.source-cruiser", identity.ShipDesignDefinitionId);
        var status = app.CaptureStatus(); Assert.True(app.FinalizeBatch(status.Turn, status.CampaignRevision).Accepted);
        Assert.DoesNotContain(Game(app).Ships!, s => s.ShipId == identity.ShipId);
        app = Reload(p, app);
        Assert.Equal(Json(result), Json(Assert.Single(app.GetBattleHistory(Human))));
    }
}
