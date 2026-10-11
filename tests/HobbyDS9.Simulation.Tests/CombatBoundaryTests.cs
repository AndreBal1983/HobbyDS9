using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CombatBoundaryTests
{
    [Fact]
    public void StreamMatchesKnownVectorsAndRejectsZeroBoundWithoutDrawing()
    {
        var random = new EncounterRandomStream(0);
        Assert.Equal(0xe220a8397b1dcdafUL, random.NextUInt64());
        Assert.Equal(0x6e789e6aa1b965f4UL, random.NextUInt64());
        Assert.Equal(0x06c45d188009454fUL, random.NextUInt64());
        var state = random.State;
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextBelow(0));
        Assert.Equal(state, random.State);
    }

    [Fact]
    public void BoundedDrawUsesRejectionSampling()
    {
        var random = new EncounterRandomStream(0);
        random.NextUInt64();
        Assert.Equal(0x788bb8a8724c81ebUL, random.NextBelow(0x8000000000000001UL));
        Assert.Equal(0x78dde6e5fd29f054UL, random.State); // Four total draws: two rejected.
    }

    [Fact]
    public void CreationAllocatesStableIdsAndIndependentSeedsInLocationOrder()
    {
        var first = Create();
        var reordered = CombatEncounterScaffold.CreateCheckpoint(7, 23, 0, 1, Templates().AsEnumerable().Reverse());
        Assert.Equal(Json(first), Json(reordered));
        Assert.Equal(new[] { "encounter.spawn.0000000000000001", "encounter.spawn.0000000000000002",
            "encounter.spawn.0000000000000003" }, first.PendingCombats.Select(p => p.Request.EncounterId));
        Assert.Equal(new[] { 0xe220a8397b1dcdafUL, 0x6e789e6aa1b965f4UL, 0x06c45d188009454fUL },
            first.PendingCombats.Select(p => p.Request.RandomState));
        Assert.Equal(0xdaa66d2c7ddf743fUL, first.CampaignRandomState);
        Assert.Equal(4UL, first.NextEntitySequence);
    }

    [Fact]
    public void EveryResolutionPermutationProducesIdenticalCanonicalResults()
    {
        var orders = new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
            new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
        string? expected = null;
        foreach (var order in orders)
        {
            var checkpoint = Create();
            var original = Json(checkpoint);
            var encounterIds = checkpoint.PendingCombats.Select(p => p.Request.EncounterId).ToArray();
            var resolver = new SeededProbeResolver();
            foreach (var index in order)
            {
                var request = CombatEncounterScaffold.GetDetachedRequest(checkpoint, encounterIds[index]);
                var result = resolver.Resolve(request);
                var previous = checkpoint;
                var previousJson = Json(previous);
                checkpoint = CombatEncounterScaffold.RecordResult(checkpoint, result);
                Assert.Equal(previousJson, Json(previous));
            }
            Assert.True(CombatEncounterScaffold.IsReadyToFinalize(checkpoint));
            Assert.Equal(0xdaa66d2c7ddf743fUL, checkpoint.CampaignRandomState);
            Assert.Equal(7, checkpoint.TurnNumber);
            Assert.Equal(26, checkpoint.StateRevision);
            Assert.Equal(3, CombatEncounterScaffold.GetCanonicalResults(checkpoint).Count);
            expected ??= Json(checkpoint);
            Assert.Equal(expected, Json(checkpoint));
            Assert.NotEqual(original, Json(checkpoint));
        }
    }

    [Fact]
    public void ExtraDrawsInOneBattleDoNotChangeOtherBattlesOrCampaignStream()
    {
        var checkpoint = Create();
        var normal = new SeededProbeResolver();
        var extra = new SeededProbeResolver(extraDrawsAtAlpha: 100);
        foreach (var pending in checkpoint.PendingCombats)
        {
            var request = CombatEncounterScaffold.GetDetachedRequest(checkpoint, pending.Request.EncounterId);
            var a = normal.Resolve(request);
            var b = extra.Resolve(request);
            if (request.LocationNodeId == "system.alpha") Assert.NotEqual(a.FinalRandomState, b.FinalRandomState);
            else Assert.Equal(Json(a), Json(b));
        }
        Assert.Equal(0xdaa66d2c7ddf743fUL, checkpoint.CampaignRandomState);
    }

    [Fact]
    public void PartialBatchSaveResumesInDifferentOrderWithoutRegeneratingSeeds()
    {
        var original = Create();
        var ids = original.PendingCombats.Select(p => p.Request.EncounterId).ToArray();
        var resolver = new SeededProbeResolver();
        var partial = Resolve(original, ids[1], resolver);
        var loaded = JsonSerializer.Deserialize<CombatCheckpointDto>(Json(partial))!;
        CombatEncounterScaffold.ValidateCheckpoint(loaded);
        var resumed = Resolve(Resolve(loaded, ids[2], resolver), ids[0], resolver);
        var uninterrupted = Resolve(Resolve(Resolve(original, ids[0], resolver), ids[1], resolver), ids[2], resolver);
        Assert.Equal(Json(uninterrupted), Json(resumed));
    }

    [Fact]
    public void DetachedInputsAndReturnedResultsCannotMutateCheckpoint()
    {
        var template = Templates()[0];
        var checkpoint = CombatEncounterScaffold.CreateCheckpoint(7, 23, 0, 1, new[] { template });
        var json = Json(checkpoint);
        template.Participants[0].Ships[0].Hull = 1;
        var request = CombatEncounterScaffold.GetDetachedRequest(checkpoint, checkpoint.PendingCombats[0].Request.EncounterId);
        request.Participants[0].Ships[0].Hull = 2;
        Assert.Equal(json, Json(checkpoint));
        checkpoint = Resolve(checkpoint, checkpoint.PendingCombats[0].Request.EncounterId, new SeededProbeResolver());
        json = Json(checkpoint);
        CombatEncounterScaffold.GetCanonicalResults(checkpoint)[0].ShipChanges[0].RemainingHull = 0;
        Assert.Equal(json, Json(checkpoint));
    }

    [Fact]
    public void DuplicateFleetInAnotherEncounterIsRejectedEvenAfterItsFirstBattleIsResolved()
    {
        var templates = Templates();
        templates[1].Participants[0].FleetId = templates[0].Participants[0].FleetId;
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.CreateCheckpoint(7, 23, 0, 1, templates));
        var checkpoint = Create();
        checkpoint = Resolve(checkpoint, checkpoint.PendingCombats[0].Request.EncounterId, new SeededProbeResolver());
        checkpoint.PendingCombats[0].Request.Participants[0].FleetId = checkpoint.ResolvedCombats[0].Request.Participants[0].FleetId;
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.ValidateCheckpoint(checkpoint));
    }

    [Fact]
    public void DuplicateShipAndLocationAreRejected()
    {
        var templates = Templates();
        templates[1].Participants[0].Ships[0].ShipId = templates[0].Participants[0].Ships[0].ShipId;
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.CreateCheckpoint(7, 23, 0, 1, templates));
        templates = Templates();
        templates[1].LocationNodeId = templates[0].LocationNodeId;
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.CreateCheckpoint(7, 23, 0, 1, templates));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Fleet.Alpha")]
    [InlineData("fleet alpha")]
    [InlineData("fléet.alpha")]
    public void NoncanonicalIdsAreRejectedAtIngress(string fleetId)
    {
        var templates = Templates();
        templates[0].Participants[0].FleetId = fleetId;
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.CreateCheckpoint(7, 23, 0, 1, templates));
    }

    [Fact]
    public void DeferredAndInvalidResultsPreserveCheckpointAndBlockFinalization()
    {
        var checkpoint = Create();
        var before = Json(checkpoint);
        var request = checkpoint.PendingCombats[0].Request;
        var deferred = new CombatResultDto { EncounterId = request.EncounterId, ResolverId = request.ResolverId,
            ResolverVersion = request.ResolverVersion, FinalRandomState = request.RandomState };
        Assert.Equal(before, Json(CombatEncounterScaffold.RecordResult(checkpoint, deferred)));
        deferred.FinalRandomState++;
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.RecordResult(checkpoint, deferred));
        Assert.Equal(before, Json(checkpoint));
        Assert.False(CombatEncounterScaffold.IsReadyToFinalize(checkpoint));
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.GetCanonicalResults(checkpoint));
        Assert.Equal("CombatPending", Assert.Throws<InvalidOperationException>(() => CombatEncounterScaffold.RequirePlanning(checkpoint)).Message);
    }

    [Fact]
    public void DuplicateResolutionIsRejectedAndPlanningWaitsForCampaignCommit()
    {
        var checkpoint = Create();
        var request = checkpoint.PendingCombats[0].Request;
        var result = new SeededProbeResolver().Resolve(request);
        checkpoint = CombatEncounterScaffold.RecordResult(checkpoint, result);
        var before = Json(checkpoint);
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.RecordResult(checkpoint, result));
        Assert.Equal(before, Json(checkpoint));
        Assert.False(CombatEncounterScaffold.IsReadyToFinalize(checkpoint));
        foreach (var id in checkpoint.PendingCombats.Select(p => p.Request.EncounterId).ToArray())
            checkpoint = Resolve(checkpoint, id, new SeededProbeResolver());
        Assert.True(CombatEncounterScaffold.IsReadyToFinalize(checkpoint));
        Assert.Throws<InvalidOperationException>(() => CombatEncounterScaffold.RequirePlanning(checkpoint));
        CombatEncounterScaffold.RequirePlanning(CombatEncounterScaffold.CreateCheckpoint(7, 0, 0, 1, Array.Empty<CombatRequestDto>()));
    }

    [Fact]
    public void RetreatCannotEnterAnyOriginalBattleLocationRegardlessOfResolutionOrder()
    {
        var checkpoint = Create();
        var alpha = checkpoint.PendingCombats[0].Request;
        var betaId = checkpoint.PendingCombats[1].Request.EncounterId;
        var result = new SeededProbeResolver().Resolve(alpha);
        result.FleetChanges.Add(new() { FleetId = alpha.Participants[0].FleetId,
            Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "system.beta" });
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.RecordResult(checkpoint, result));
        checkpoint = Resolve(checkpoint, betaId, new SeededProbeResolver());
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.RecordResult(checkpoint, result));
        result.FleetChanges[0].RetreatNodeId = "system.safe";
        var resolved = CombatEncounterScaffold.RecordResult(checkpoint, result);
        Assert.Equal(checkpoint.PendingCombats.Count - 1, resolved.PendingCombats.Count);
        Assert.Equal(checkpoint.PendingCombats.Count + checkpoint.ResolvedCombats.Count,
            resolved.PendingCombats.Count + resolved.ResolvedCombats.Count); // No new encounter from retreat.
    }

    [Fact]
    public void CombatDesignIdsRespectTheSharedDefinitionNamespace()
    {
        var templates = Templates();
        templates[0].Participants[0].Ships[0].DesignId = "design.spawn.0000000000000001";
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.CreateCheckpoint(7, 23, 0, 1, templates));
    }

    [Fact]
    public void CounterOverflowAndMalformedSaveAreRejected()
    {
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.CreateCheckpoint(7, 0, 0, ulong.MaxValue, Templates()));
        var checkpoint = Create();
        checkpoint.PendingCombats[0].Request.RandomAlgorithmId = "unknown";
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.ValidateCheckpoint(checkpoint));
        checkpoint = Create();
        checkpoint.PendingCombats[0].Request.EncounterId = "encounter.spawn.0000000000000004";
        Assert.Throws<ArgumentException>(() => CombatEncounterScaffold.ValidateCheckpoint(checkpoint));
    }

    private static CombatCheckpointDto Create() => CombatEncounterScaffold.CreateCheckpoint(7, 23, 0, 1, Templates());
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static CombatCheckpointDto Resolve(CombatCheckpointDto checkpoint, string id, ICombatResolver resolver)
        => CombatEncounterScaffold.RecordResult(checkpoint, resolver.Resolve(CombatEncounterScaffold.GetDetachedRequest(checkpoint, id)));

    private static List<CombatRequestDto> Templates() => new[] { "alpha", "beta", "gamma" }.Select(location => new CombatRequestDto
    {
        LocationNodeId = "system." + location, RulesVersion = "slice-v1", ContentHash = "fixture-hash",
        ResolverId = "seeded-probe", ResolverVersion = 1,
        Participants = new[] { "alpha", "beta" }.Select(civ => new CombatParticipantDto
        {
            CivilizationId = "civ." + civ, FleetId = "fleet." + location + "." + civ,
            Ships = new() { new() { ShipId = "ship." + location + "." + civ, DesignId = "design.scout", Hull = 100, MaxHull = 100, CombatProfile = new() } }
        }).ToList()
    }).ToList();

    // Test-only resolver exercises independent draws and ordered damage data;
    // this is not an implementation of strategic combat or its placeholder.
    private sealed class SeededProbeResolver : ICombatResolver
    {
        private readonly int extraDrawsAtAlpha;
        public SeededProbeResolver(int extraDrawsAtAlpha = 0) { this.extraDrawsAtAlpha = extraDrawsAtAlpha; }
        public CombatResultDto Resolve(CombatRequestDto request)
        {
            var random = new EncounterRandomStream(request.RandomState);
            var ship = request.Participants[0].Ships[0];
            var damage = 1 + (int)random.NextBelow(50);
            if (request.LocationNodeId == "system.alpha")
                for (var i = 0; i < extraDrawsAtAlpha; i++) random.NextUInt64();
            return new CombatResultDto
            {
                EncounterId = request.EncounterId, ResolverId = request.ResolverId, ResolverVersion = request.ResolverVersion,
                Status = CombatResolutionStatus.Resolved, FinalRandomState = random.State,
                ShipChanges = new() { new() { ShipId = ship.ShipId, RemainingHull = ship.Hull - damage } },
                Events = new()
                {
                    new() { Sequence = 0, Kind = CombatEventKind.ShipDamaged, ShipId = ship.ShipId, Amount = damage },
                    new() { Sequence = 1, Kind = CombatEventKind.EncounterResolved }
                }
            };
        }
    }
}
