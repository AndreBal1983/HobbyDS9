using System.Text.Json;
using System.Text.Json.Serialization;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;

public sealed class PopulationFoundationTests
{
    private static ScenarioDto Scenario() => VerticalSliceFixture.Scenario();
    private static ColonyStateDto Earth(ScenarioDto s) => s.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol");
    private static ColonyStateDto Mars(ScenarioDto s) => s.InitialState.Colonies!.Single(c => c.ColonyId == "colony.mars");
    private static WorldEconomySnapshot View(ScenarioDto s, ColonyStateDto c) => WorldEconomy.Project(c, s.InitialState, s.Definitions);
    private static string Json(object? o) => JsonSerializer.Serialize(o);
    private static long Total(ScenarioDto s) => s.InitialState.Colonies!.Sum(PopulationRules.Population);
    private static void NoFleets(ScenarioDto s)
    {
        var ids = s.InitialState.Fleets!.Select(f => f.FleetId).Concat(s.InitialState.Ships!.Select(p => p.ShipId)).ToHashSet();
        s.InitialState.Fleets!.Clear(); s.InitialState.Ships!.Clear(); s.InitialState.EntityIdentities.RemoveAll(e => ids.Contains(e.EntityId));
    }
    private static void Reverse(ScenarioDto s)
    {
        s.Definitions.Planets!.Reverse(); s.Definitions.Species!.Reverse(); s.Definitions.Resources!.Reverse(); s.Definitions.Infrastructure!.Reverse();
        foreach (var p in s.Definitions.Planets) { p.Habitats!.Reverse(); p.Deposits!.Reverse(); }
        foreach (var p in s.Definitions.Species) { p.Suitabilities!.Reverse(); p.Atmospheres!.Reverse(); p.TraitTags!.Reverse(); }
        foreach (var p in s.Definitions.Infrastructure) { p.Extraction!.Reverse(); p.AccessibleHabitats!.Reverse(); }
        s.InitialState.Planets!.Reverse(); s.InitialState.Colonies!.Reverse(); s.InitialState.EntityIdentities.Reverse();
        foreach (var c in s.InitialState.Colonies) { c.Cohorts!.Reverse(); c.Infrastructure!.Reverse(); c.Stockpiles!.Reverse(); c.DesiredStockpiles!.Reverse(); foreach (var p in c.Cohorts) { p.Memories!.Reverse(); p.ExternalAffinities!.Reverse(); } }
    }
    [Theory]
    [InlineData("share-sum")]
    [InlineData("share-negative")]
    [InlineData("habitat-duplicate")]
    [InlineData("capacity")]
    [InlineData("suitability")]
    [InlineData("missing-suitability")]
    [InlineData("duplicate-species")]
    [InlineData("planet-system")]
    [InlineData("cohort-species")]
    [InlineData("cohort-id")]
    [InlineData("negative-population")]
    [InlineData("legal")]
    [InlineData("rating")]
    [InlineData("affinity")]
    [InlineData("memory")]
    [InlineData("resource")]
    [InlineData("stockpile")]
    [InlineData("deposit")]
    [InlineData("infrastructure")]
    [InlineData("duplicate-planet-colony")]
    [InlineData("missing-population")]
    [InlineData("missing-species")]
    [InlineData("missing-statistics")]
    public void InvalidNewSchemaRejectsWithoutMutatingInput(string fault)
    {
        var s = Scenario(); var earth = Earth(s); var planet = s.Definitions.Planets!.Single(p => p.DefinitionId == "planet-definition.sol"); var cohort = earth.Cohorts![0];
        switch (fault)
        {
            case "share-sum": planet.Habitats![0].ShareBps++; break;
            case "share-negative": planet.Habitats![0].ShareBps = -1; break;
            case "habitat-duplicate": planet.Habitats![0].Kind = planet.Habitats[1].Kind; break;
            case "capacity": planet.Habitats![0].NaturalCapacity = -1; break;
            case "suitability": s.Definitions.Species![0].Suitabilities![0].SuitabilityBps = 10001; break;
            case "missing-suitability": s.Definitions.Species![0].Suitabilities!.RemoveAt(0); break;
            case "duplicate-species": s.Definitions.Species!.Add(s.Definitions.Species[0]); break;
            case "planet-system": planet.SystemDefinitionId = "star-system.missing"; break;
            case "cohort-species": cohort.SpeciesDefinitionId = "species.missing"; break;
            case "cohort-id": earth.Cohorts.Add(PopulationData.Copy(cohort)); break;
            case "negative-population": cohort.Population = -1; break;
            case "legal": cohort.LegalStatus = (LegalStatus)0; break;
            case "rating": cohort.CivicAlignment = 10001; break;
            case "affinity": cohort.ExternalAffinities!.Add(new() { CivilizationId = "civ.missing", Rating = 1000 }); break;
            case "memory": cohort.Memories!.Add(new() { Kind = "repressed", Magnitude = 1000, StartTurn = 2, ExpiryTurn = 1 }); break;
            case "resource": s.Definitions.ShipDesigns![0].ResourceCosts![0].ResourceDefinitionId = "resource.missing"; break;
            case "stockpile": earth.Stockpiles![0].Amount = -1; break;
            case "deposit": s.InitialState.Planets![0].RemainingDeposits![0].Amount = long.MaxValue; break;
            case "infrastructure": earth.Infrastructure![0].Count = 0; break;
            case "duplicate-planet-colony": Mars(s).PlanetId = earth.PlanetId; break;
            case "missing-population": earth.Cohorts = null; break;
            case "missing-species": s.Definitions.Species = null; break;
            case "missing-statistics": s.InitialState.PopulationStatistics = null; break;
        }
        var before = Json(s); Assert.False(FoundationValidator.ValidateScenario(s).IsValid); Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(s)); Assert.Equal(before, Json(s));
    }
    [Fact]
    public void HabitatAllocationUsesSharedCapacityAndFavorsSpeciesFitWithIndependentSurfaceShares()
    {
        var s = Scenario(); var earth = Earth(s); var p = s.Definitions.Planets!.Single(p => p.DefinitionId == "planet-definition.sol");
        foreach (var h in p.Habitats!) h.NaturalCapacity = 0;
        p.Habitats.Single(h => h.Kind == HabitatKind.Temperate).NaturalCapacity = 500;
        p.Habitats.Single(h => h.Kind == HabitatKind.Arid).NaturalCapacity = 200;
        p.Habitats.Single(h => h.Kind == HabitatKind.Polar).NaturalCapacity = 100;
        var view = View(s, earth);
        Assert.Equal(800, view.EffectiveCapacity); Assert.Equal(200, view.Crowding); Assert.Equal(800, view.Allocations.Sum(a => a.Population));
        Assert.Contains(view.Allocations, a => a.CohortId == "cohort.sol.vulcan" && a.Habitat == HabitatKind.Arid && a.Population == 200);
        Assert.Contains(view.Allocations, a => a.CohortId == "cohort.sol.andorian" && a.Habitat == HabitatKind.Polar && a.Population == 100);
        var before = Json(view); p.Habitats.ForEach(h => h.ShareBps = 0); p.Habitats[0].ShareBps = 10000;
        Assert.Equal(before, Json(View(s, earth))); // Physical share does not set capacity.
    }
    [Fact]
    public void ArtificialHabitatsAddCapacityWithoutChangingNaturalSpeciesOrMosaic()
    {
        var s = Scenario(); var earth = Earth(s); var before = View(s, earth); var content = Json(s.Definitions.Planets); var species = Json(s.Definitions.Species);
        earth.Infrastructure!.Add(new() { DefinitionId = "infrastructure.sealed-habitat", Count = 2 }); var after = View(s, earth);
        Assert.Equal(before.EffectiveCapacity + 500, after.EffectiveCapacity); Assert.Equal(before.NaturalCapacity, after.NaturalCapacity);
        Assert.Equal(content, Json(s.Definitions.Planets)); Assert.Equal(species, Json(s.Definitions.Species));
        s.Definitions.Infrastructure!.Single(i => i.DefinitionId == "infrastructure.settlement").AccessibleHabitats!.Clear();
        Assert.Equal(500, View(s, earth).EffectiveCapacity);
    }
    [Fact]
    public void UnsuitableAtmosphereDoesNotBecomeSuitableThroughSpeciesIdentityOrNaturalCapacity()
    {
        var s = Scenario(); var p = s.Definitions.Planets!.Single(p => p.DefinitionId == "planet-definition.sol"); p.Atmosphere = "atmosphere.toxic";
        var v = View(s, Earth(s)); Assert.Equal(0, v.AverageHabitability); Assert.Equal(v.Population, v.Crowding);
        Earth(s).Infrastructure!.Add(new() { DefinitionId = "infrastructure.sealed-habitat", Count = 4 });
        Assert.Equal(8000, View(s, Earth(s)).AverageHabitability);
    }
    [Fact]
    public void PopulationMajorityDominatesStabilityAndSpeciesAloneDoesNotCausePoliticalEffects()
    {
        var s = Scenario(); var earth = Earth(s); foreach (var c in earth.Cohorts!) { c.Satisfaction = 9000; c.CivicAlignment = 9000; c.Radicalisation = 0; }
        earth.Cohorts[0].Population = 10000; earth.Cohorts[1].Population = 1; earth.Cohorts[1].Satisfaction = 0; earth.Cohorts[1].CivicAlignment = 0; earth.Cohorts[1].Radicalisation = 10000;
        Assert.True(View(s, earth).TargetStability > 9000);
        var before = View(s, earth).TargetStability; earth.Cohorts[1].SpeciesDefinitionId = "species.ferengi"; Assert.Equal(before, View(s, earth).TargetStability);
        earth.Cohorts[0].ExternalAffinities!.Add(new() { CivilizationId = "civ.cardassian", Rating = 10000 }); Assert.Equal(before, View(s, earth).TargetStability);
    }
    [Fact]
    public void StabilityHasInertiaAndNoAutomaticRevoltOrOwnershipChange()
    {
        var s = Scenario(); var earth = Earth(s); earth.Stability = 1000; foreach (var c in earth.Cohorts!) { c.Satisfaction = 9000; c.CivicAlignment = 9000; }
        EconomyPhase.Execute(s.InitialState, s.Definitions); Assert.Equal(1250, earth.Stability); Assert.Equal("civ.federation", earth.OwnerCivilizationId);
    }
    [Fact]
    public void IndustryTracksInfrastructureAndPowerWhileCreditsTrackPopulationAndProsperity()
    {
        var s = Scenario(); var earth = Earth(s); var first = View(s, earth);
        foreach (var c in earth.Cohorts!) c.Population *= 10; var populous = View(s, earth);
        Assert.Equal(first.Industry, populous.Industry); Assert.True(populous.CreditOutput > first.CreditOutput); Assert.True(populous.ResearchOutput > first.ResearchOutput);
        earth.Prosperity = 0; Assert.Equal(0, View(s, earth).CreditOutput); Assert.Equal(first.Industry, View(s, earth).Industry);
        earth.Automation = 10000; Assert.Equal(first.Industry * 2, View(s, earth).Industry);
        s.Definitions.Infrastructure!.Single(i => i.DefinitionId == "infrastructure.settlement").PowerSupply = 0; Assert.Equal(0, View(s, earth).Industry);
        Assert.True(View(s, Mars(s)).Population < first.Population);
    }
    [Fact]
    public void ResearchAggregatesInstitutionAndSkilledPopulationOutputsFromEveryOwnedWorld()
    {
        var s = Scenario(); var owner = s.InitialState.Civilizations!.Single(c => c.CivilizationId == "civ.federation");
        var expected = s.InitialState.Colonies!.Where(c => c.OwnerCivilizationId == owner.CivilizationId).Sum(c => View(s, c).ResearchOutput);
        ResearchPhase.Execute(s.InitialState, s.Definitions); Assert.Equal(expected, owner.ResearchReserve);
    }
    [Fact]
    public void ShortageReducesSatisfactionBeforeDeathsAndFoodDemandUsesCeilingDivision()
    {
        var s = Scenario(); var earth = Earth(s); NoFleets(s); earth.Food = 0; s.Definitions.Infrastructure!.ForEach(i => i.FoodOutput = 0); Mars(s).Food = 0; PopulationFixture.RemoveMars(s);
        var original = PopulationRules.Population(earth); var satisfaction = earth.Cohorts![0].Satisfaction;
        EconomyPhase.Execute(s.InitialState, s.Definitions); Assert.Equal(original, PopulationRules.Population(earth)); Assert.True(earth.Cohorts[0].Satisfaction < satisfaction);
        EconomyPhase.Execute(s.InitialState, s.Definitions); Assert.Equal(0, s.InitialState.PopulationStatistics!.Deaths);
        EconomyPhase.Execute(s.InitialState, s.Definitions); Assert.True(s.InitialState.PopulationStatistics.Deaths > 0);
        Assert.Equal((PopulationRules.Population(earth) + 99) / 100, View(s, earth).FoodDemand);
    }
    [Fact]
    public void ExtractionIsBoundedByRemainingDepositAndQueueReservesLocalDilithiumOnly()
    {
        var s = Scenario(); var planet = s.InitialState.Planets!.Single(p => p.PlanetId == "planet.sol"); planet.RemainingDeposits![0].Amount = 1;
        var session = VerticalSliceFixture.Session(s); var old = session.CaptureSnapshot();
        Assert.True(VerticalSliceFixture.Enqueue(session).Accepted); Assert.Equal(8, PopulationFixture.Dilithium(VerticalSliceFixture.Colony(session)));
        Assert.Equal(10, PopulationFixture.Dilithium(old.Colonies.Single(c => c.ColonyId.Value == "colony.sol")));
        VerticalSliceFixture.Advance(session); Assert.Equal(0, session.ExportSave().GameState.Planets!.Single(p => p.PlanetId == "planet.sol").RemainingDeposits![0].Amount);
    }
    [Theory]
    [InlineData(10000, 10)]
    [InlineData(5000, 5)]
    [InlineData(0, 0)]
    public void LogisticsDeliveredIsLimitedByReceivingStabilityAndConservesPhysicalStock(int stability, long delivered)
    {
        var s = Scenario(); NoFleets(s); var from = Earth(s); var to = Mars(s); from.Food = 100; to.Food = 0;
        foreach (var c in to.Cohorts!) c.Population = 500; to.Stability = stability;
        var before = s.InitialState.Colonies!.Sum(c => c.Food); PopulationLogistics.Execute(s.InitialState, s.Definitions);
        var receipt = Assert.Single(s.InitialState.LastDeliveries!, r => r.ResourceDefinitionId == "resource.food");
        Assert.Equal(delivered, receipt.Delivered); Assert.Equal("planet.sol", receipt.OriginPlanetId); Assert.Equal("planet.mars", receipt.DestinationPlanetId); Assert.Equal(new[] { "system.sol" }, receipt.RouteNodeIds);
        Assert.Equal(before, s.InitialState.Colonies!.Sum(c => c.Food)); Assert.Equal(delivered, to.Food);
    }
    [Theory]
    [InlineData("clear", 10000)]
    [InlineData("near", 5000)]
    [InlineData("blockade", 0)]
    [InlineData("disconnected", 0)]
    public void LogisticsPathIntegrityExplainsHostileInterferenceAndDisconnectedGraph(string mode, int expected)
    {
        var s = Scenario(); var fleet = s.InitialState.Fleets!.Single(f => f.OwnerCivilizationId == "civ.cardassian");
        fleet.LocationNodeId = mode == "blockade" ? "system.sol" : mode == "near" ? "system.bajor" : "system.cardassia";
        if (mode == "disconnected") s.Definitions.NavigationConnections!.RemoveAll(e => e.EndpointA == "system.sol" || e.EndpointB == "system.sol");
        var route = PopulationLogistics.Path(s.InitialState, PopulationLogistics.Graph(s.InitialState, s.Definitions), "system.sol", mode == "disconnected" ? "system.gamma-exit" : "system.sol", "civ.federation");
        Assert.Equal(expected, route.integrity); if (mode == "disconnected") Assert.Empty(route.path);
    }
    [Fact]
    public void ImportedDilithiumCanBeUsedOnlyAfterPhysicalDelivery()
    {
        var s = Scenario(); NoFleets(s); var app = CampaignApplication.StartScenario(s); var initial = app.CaptureSnapshot();
        Assert.Equal(CommandErrorCode.InsufficientStrategicResource, StrategicOptions.Production(initial, new("civ.federation"), new("colony.mars")).Single(o => o.Design.DefinitionId.Value == "ship-design.scout").UnavailableReason);
        PopulationLogistics.Execute(s.InitialState, s.Definitions); Assert.True(PopulationFixture.Dilithium(Mars(s)) >= 2);
        app = CampaignApplication.StartScenario(s); Assert.True(app.SubmitCommand(new() { CommandId = "cmd.imported", ActorCivilizationId = "civ.federation", ExpectedTurn = 1, ExpectedRevision = 0, Kind = CommandKind.EnqueueProduction, EnqueueProduction = new() { ColonyId = "colony.mars", DefinitionId = "ship-design.scout" } }).Accepted);
    }
    [Fact]
    public void LargestRemainderIsExactCappedAndOrderingIndependent()
    {
        var input = new[] { ("c", 1L, 100L), ("a", 1L, 100L), ("b", 1L, 100L) };
        var a = PopulationMigration.Allocate(5, input); var b = PopulationMigration.Allocate(5, input.Reverse());
        Assert.Equal(new long[] { 2, 2, 1 }, a.OrderBy(p => p.Key).Select(p => p.Value)); Assert.Equal(Json(a), Json(b));
        var capped = PopulationMigration.Allocate(100, new[] { ("a", 100L, 1L), ("b", 1L, 100L) }); Assert.Equal(1, capped["a"]); Assert.Equal(99, capped["b"]);
        var huge = PopulationMigration.Allocate(long.MaxValue, new[] { ("a", long.MaxValue, long.MaxValue), ("b", long.MaxValue, long.MaxValue) });
        Assert.Equal(long.MaxValue, huge.Values.Sum());
    }
    [Fact]
    public void NormalMigrationConservesPopulationAndCannotUseBlockedRoutes()
    {
        var s = Scenario(); NoFleets(s); var before = Total(s); Earth(s).Stability = 1000; Earth(s).Prosperity = 1000; Mars(s).Stability = 10000; Mars(s).Prosperity = 10000;
        PopulationMigration.Execute(s.InitialState, s.Definitions); Assert.Equal(before, Total(s)); Assert.True(s.InitialState.PopulationStatistics!.Migrated > 0); Assert.True(s.InitialState.PopulationStatistics.Migrated <= 12);
        var blocked = Scenario(); Earth(blocked).Stability = 1000; Earth(blocked).Prosperity = 1000; Mars(blocked).Stability = 10000; Mars(blocked).Prosperity = 10000;
        blocked.InitialState.Fleets!.Single(f => f.OwnerCivilizationId == "civ.cardassian").LocationNodeId = "system.sol";
        PopulationMigration.Execute(blocked.InitialState, blocked.Definitions); Assert.Equal(0, blocked.InitialState.PopulationStatistics!.Migrated);
    }
    [Fact]
    public void RefugeeFlowIsSeparateAndPreservesStatusOriginAndMemoryAcrossReload()
    {
        var s = Scenario(); NoFleets(s); var source = Earth(s); var cohort = source.Cohorts!.Single(c => c.SpeciesDefinitionId == "species.human");
        var request = new DisplacementDto { DisplacementId = "displacement.fixture", SourcePlanetId = source.PlanetId, CohortId = cohort.CohortId, Population = 100, Cause = "civilian-casualties", ResponsibleEntityId = "civ.cardassian" };
        s.InitialState.Displacements!.Add(request); s.InitialState.EntityIdentities.Add(new() { EntityId = request.DisplacementId }); var before = Total(s);
        PopulationMigration.Execute(s.InitialState, s.Definitions); Assert.Equal(before, Total(s)); Assert.Equal(100, s.InitialState.PopulationStatistics!.RefugeesMoved); Assert.Empty(s.InitialState.Displacements);
        var arrivals = s.InitialState.Colonies!.SelectMany(c => c.Cohorts!).Where(c => c.LegalStatus == LegalStatus.Refugee).ToList();
        Assert.Equal(100, arrivals.Sum(c => c.Population)); Assert.All(arrivals, c => { Assert.Equal("civ.federation", c.OriginCivilizationId); Assert.Contains(c.Memories!, m => m.Kind == "accepted-as-refugees"); });
        var save = VerticalSliceFixture.Save(s); var session = new SimulationSession(save, s.Definitions, VerticalSliceFixture.Compatibility(s)); var json = Json(session.ExportSave());
        Assert.Equal(json, Json(new SimulationSession(JsonSerializer.Deserialize<SaveEnvelopeDto>(json)!, s.Definitions, VerticalSliceFixture.Compatibility(s)).ExportSave()));
    }
    [Fact]
    public void MemoriesDecayAndExpireDeterministically()
    {
        var s = Scenario(); PopulationFixture.RemoveMars(s); var cohort = Earth(s).Cohorts![0]; cohort.Memories!.Add(new() { Kind = "prosperity-boom", Magnitude = 100, StartTurn = 1, ExpiryTurn = 3, DecayPerTurn = 10 });
        EconomyPhase.Execute(s.InitialState, s.Definitions); Assert.Equal(90, cohort.Memories[0].Magnitude);
        s.InitialState.TurnNumber = 3; EconomyPhase.Execute(s.InitialState, s.Definitions); Assert.Empty(cohort.Memories);
    }
    [Fact]
    public void HardCompactionPreservesPopulationAndWeightedRatingsIndependentOfInputOrdering()
    {
        var s = Scenario(); var earth = Earth(s); earth.Cohorts!.Clear(); s.InitialState.EntityIdentities.RemoveAll(e => e.EntityId.StartsWith("cohort.sol.", StringComparison.Ordinal));
        for (var i = 0; i < 40; i++)
        { var c = new PopulationCohortDto { CohortId = "cohort.test-" + i.ToString("d2"), SpeciesDefinitionId = "species.human", Population = 100, LegalStatus = LegalStatus.Citizen, Satisfaction = i * 200, Integration = 5000, CivicAlignment = 5000, Radicalisation = 0, ExternalAffinities = new(), Memories = new() }; earth.Cohorts.Add(c); s.InitialState.EntityIdentities.Add(new() { EntityId = c.CohortId, DefinitionId = c.SpeciesDefinitionId }); }
        var other = JsonSerializer.Deserialize<ScenarioDto>(Json(s))!; Earth(other).Cohorts!.Reverse();
        CohortCompaction.Execute(earth, s.InitialState); CohortCompaction.Execute(Earth(other), other.InitialState);
        Assert.True(earth.Cohorts.Count <= 32); Assert.Equal(4000, PopulationRules.Population(earth)); Assert.Equal(Json(earth), Json(Earth(other)));
        Assert.Equal(15600000L, earth.Cohorts.Sum(c => c.Population * c.Satisfaction));
    }
    [Fact]
    public void ReorderedPopulationContentAndStateHaveIdenticalTwentyFiveTurnSaves()
    {
        var scenario = Scenario(); var other = JsonSerializer.Deserialize<ScenarioDto>(Json(scenario))!; Reverse(other);
        var a = VerticalSliceFixture.Session(scenario); var b = VerticalSliceFixture.Session(other);
        for (var i = 0; i < 25; i++) { VerticalSliceFixture.Advance(a); VerticalSliceFixture.Advance(b); Assert.Equal(Json(a.ExportSave()), Json(b.ExportSave())); }
        Assert.Equal(Json(a.CaptureSnapshot()), Json(b.CaptureSnapshot()));
    }
    [Theory]
    [InlineData("Food")]
    [InlineData("Stockpile")]
    [InlineData("Credits")]
    public void OverflowRollsBackTheWholeTurnIncludingDemographicsAndReceipts(string target)
    {
        var s = Scenario(); if (target == "Food") Earth(s).Food = long.MaxValue; if (target == "Stockpile") PopulationFixture.SetDilithium(Earth(s), long.MaxValue); if (target == "Credits") s.InitialState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits = long.MaxValue;
        var session = VerticalSliceFixture.Session(s); var before = Json(session.ExportSave()); Assert.Throws<OverflowException>(() => VerticalSliceFixture.Advance(session)); Assert.Equal(before, Json(session.ExportSave()));
    }
    [Theory]
    [InlineData("return")]
    [InlineData("exception")]
    [InlineData("invariant")]
    public void LateFailureDiscardsMigrationResourcesStabilityAndEntityAllocation(string failure)
    {
        var s = Scenario(); var session = VerticalSliceFixture.Session(s, (phase, state) =>
        {
            if (phase != StrategicTurnPhase.Research) return true;
            Assert.True(state.PopulationStatistics!.Migrated > 0);
            if (failure == "exception") throw new ApplicationException("injected");
            if (failure == "invariant") state.Colonies![0].Stability = 10001;
            return failure != "return";
        }); var before = Json(session.ExportSave());
        if (failure == "return") Assert.Equal(TurnErrorCode.PhaseFailed, VerticalSliceFixture.Advance(session).ErrorCode);
        else if (failure == "exception") Assert.Throws<ApplicationException>(() => VerticalSliceFixture.Advance(session));
        else Assert.Throws<InvalidOperationException>(() => VerticalSliceFixture.Advance(session));
        Assert.Equal(before, Json(session.ExportSave()));
    }
    [Fact]
    public void NewFormatHasNoObsoleteResourceOrScalarPopulationAndHostsCanRejectOldFields()
    {
        Assert.Null(typeof(ColonyStateDto).GetProperty("Materials")); Assert.Null(typeof(ColonyStateDto).GetProperty("Population")); Assert.Null(typeof(ShipDesignDefinitionDto).GetProperty("MaterialCost"));
        var options = new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ColonyStateDto>("{\"Materials\":123}", options));
        var s = Scenario(); var save = VerticalSliceFixture.Save(s); save.SaveFormatVersion = 9; Assert.False(FoundationValidator.ValidateSave(save, s.Definitions, VerticalSliceFixture.Compatibility(s)).IsValid);
    }
    [Theory]
    [InlineData("amount")]
    [InlineData("duplicate")]
    [InlineData("endpoint")]
    [InlineData("path")]
    [InlineData("factor")]
    [InlineData("resource")]
    public void TamperedDeliveryFactsAreRejected(string fault)
    {
        var scenario = Scenario(); NoFleets(scenario); PopulationLogistics.Execute(scenario.InitialState, scenario.Definitions);
        var receipt = scenario.InitialState.LastDeliveries![0];
        switch (fault)
        {
            case "amount": receipt.Delivered++; break;
            case "duplicate": scenario.InitialState.LastDeliveries.Add(PopulationData.Copy(receipt)); break;
            case "endpoint": receipt.DestinationPlanetId = receipt.OriginPlanetId; break;
            case "path": receipt.RouteNodeIds = new() { "system.cardassia" }; break;
            case "factor": receipt.StabilityFactor = 10001; break;
            case "resource": receipt.ResourceDefinitionId = "resource.absent"; break;
        }
        Assert.False(FoundationValidator.ValidateScenario(scenario).IsValid);
    }
    [Theory]
    [InlineData("species")]
    [InlineData("planet")]
    [InlineData("infrastructure")]
    [InlineData("resource")]
    [InlineData("cohort")]
    [InlineData("habitat")]
    [InlineData("memory")]
    [InlineData("affinity")]
    [InlineData("deposit")]
    [InlineData("stock")]
    [InlineData("displacement")]
    [InlineData("delivery")]
    public void NullNestedRowsProduceValidationIssues(string collection)
    {
        var s = Scenario(); var world = Earth(s);
        switch (collection)
        {
            case "species": s.Definitions.Species!.Add(null!); break;
            case "planet": s.InitialState.Planets!.Add(null!); break;
            case "infrastructure": world.Infrastructure!.Add(null!); break;
            case "resource": s.Definitions.Resources!.Add(null!); break;
            case "cohort": world.Cohorts!.Add(null!); break;
            case "habitat": s.Definitions.Planets![0].Habitats![0] = null!; break;
            case "memory": world.Cohorts![0].Memories!.Add(null!); break;
            case "affinity": world.Cohorts![0].ExternalAffinities!.Add(null!); break;
            case "deposit": s.InitialState.Planets![0].RemainingDeposits!.Add(null!); break;
            case "stock": world.Stockpiles!.Add(null!); break;
            case "displacement": s.InitialState.Displacements!.Add(null!); break;
            case "delivery": s.InitialState.LastDeliveries!.Add(null!); break;
        }
        Assert.False(FoundationValidator.ValidateScenario(s).IsValid);
    }
    [Fact]
    public void NewPopulationSnapshotsRemainDetachedFromExportsAndFutureTurns()
    {
        var scenario = Scenario(); var app = CampaignApplication.StartScenario(scenario); var snapshot = app.CaptureSnapshot(); var before = Json(snapshot);
        var exported = app.ExportSave(); exported.GameState.Colonies![0].Cohorts![0].Satisfaction = 0;
        exported.GameState.Planets![0].RemainingDeposits![0].Amount = 0;
        scenario.Definitions.Species![0].Suitabilities![0].SuitabilityBps = 0;
        Assert.Equal(before, Json(app.CaptureSnapshot()));
        app.AdvanceTurn(snapshot.Campaign.TurnNumber, snapshot.Campaign.StateRevision);
        Assert.Equal(before, Json(snapshot));
        Assert.Throws<NotSupportedException>(() => ((IList<PopulationCohortSnapshot>)snapshot.Campaign.Colonies[0].State.Cohorts).Clear());
    }
    [Fact]
    public void ResourceAndPopulationConservationHoldAcrossProductionAndCombat()
    {
        var scenario = Scenario(); var initialPopulation = Total(scenario);
        long Physical(GameStateDto state) => state.Planets!.Sum(p => p.RemainingDeposits!.Sum(r => r.Amount))
            + state.Colonies!.Sum(c => c.Stockpiles!.Sum(r => r.Amount) + c.ProductionQueue!.Sum(q => q.PaidResources!.Sum(r => r.Amount)));
        var initialResource = Physical(scenario.InitialState); var session = VerticalSliceFixture.Session(scenario);
        for (var turn = 0; turn < 25; turn++)
        {
            VerticalSliceFixture.Advance(session); var state = session.ExportSave().GameState; var stats = state.PopulationStatistics!;
            Assert.Equal(initialPopulation + stats.Births - stats.Deaths, state.Colonies!.Sum(PopulationRules.Population));
            Assert.Equal(initialResource, Physical(state) + stats.ResourceConsumed);
        }
    }
    [Fact]
    public void RefugeePolicyAndHostileRoutesCanLeaveDisplacementPendingWithoutLosingPeople()
    {
        var s = Scenario(); var source = Earth(s); var cohort = source.Cohorts![0];
        foreach (var world in s.InitialState.Colonies!) world.AcceptsRefugees = false;
        var request = new DisplacementDto { DisplacementId = "displacement.pending", SourcePlanetId = source.PlanetId, CohortId = cohort.CohortId, Population = 100, Cause = "homeland-destroyed" };
        s.InitialState.Displacements!.Add(request); s.InitialState.EntityIdentities.Add(new() { EntityId = request.DisplacementId });
        var total = Total(s); PopulationMigration.Execute(s.InitialState, s.Definitions);
        Assert.Equal(total, Total(s)); Assert.Equal(100, Assert.Single(s.InitialState.Displacements).Population); Assert.Equal(0, s.InitialState.PopulationStatistics!.RefugeesMoved);
    }

    [Theory]
    [InlineData("diaspora")]
    [InlineData("habitat")]
    [InlineData("distance")]
    public void MigrationWeightsRespondToDiasporaHabitatAndRouteDistance(string preference)
    {
        var s = Scenario(); NoFleets(s); var source = Earth(s); var near = Mars(s); var far = s.InitialState.Colonies!.Single(c => c.ColonyId == "colony.bajor");
        var human = source.Cohorts!.Single(c => c.SpeciesDefinitionId == "species.human"); human.Population = 10000; human.Satisfaction = 0;
        source.Cohorts = new() { human }; source.Stability = 0; source.Prosperity = 0;
        foreach (var world in new[] { near, far })
        {
            world.OwnerCivilizationId = "civ.federation"; world.Stability = 10000; world.Prosperity = 10000;
            world.Infrastructure = new() { new() { DefinitionId = "infrastructure.settlement", Count = 1 } };
            world.Cohorts = new() { new() { CohortId = world.ColonyId + ".resident", SpeciesDefinitionId = "species.vulcan", Population = 100, LegalStatus = LegalStatus.Citizen, Satisfaction = 10000, Integration = 10000, CivicAlignment = 10000, ExternalAffinities = new(), Memories = new() } };
            var planet = WorldEconomy.Planet(world, s.InitialState, s.Definitions);
            foreach (var habitat in planet.Habitats!) { habitat.NaturalCapacity = 10000; habitat.ShareBps = habitat.Kind == HabitatKind.Temperate ? 10000 : 0; }
        }
        if (preference != "distance") far.SystemId = near.SystemId;
        if (preference == "diaspora") near.Cohorts![0].SpeciesDefinitionId = "species.human";
        if (preference == "habitat")
        {
            var planet = WorldEconomy.Planet(far, s.InitialState, s.Definitions);
            foreach (var habitat in planet.Habitats!) habitat.ShareBps = habitat.Kind == HabitatKind.Polar ? 10000 : 0;
            s.Definitions.Species!.Single(p => p.DefinitionId == "species.human").Suitabilities!.Single(h => h.Kind == HabitatKind.Polar).SuitabilityBps = 1000;
        }
        PopulationMigration.Execute(s.InitialState, s.Definitions);
        long Arrivals(ColonyStateDto world) => world.Cohorts!.Where(c => c.SpeciesDefinitionId == "species.human" && c.OriginCivilizationId == "civ.federation").Sum(c => c.Population);
        Assert.True(Arrivals(near) > Arrivals(far)); Assert.Equal(100, Arrivals(near) + Arrivals(far));
    }

    [Fact]
    public void CompactionRetainsDisplacementReferencesWhenAnArrivalExceedsTheCeiling()
    {
        var s = Scenario(); var world = Earth(s); world.Cohorts!.Clear();
        for (var index = 0; index < 32; index++)
        {
            var cohort = new PopulationCohortDto { CohortId = "cohort.protected-" + index.ToString("d2"), SpeciesDefinitionId = "species.human", Population = 10, LegalStatus = LegalStatus.Citizen, ExternalAffinities = new(), Memories = new() };
            world.Cohorts.Add(cohort); s.InitialState.EntityIdentities.Add(new() { EntityId = cohort.CohortId, DefinitionId = cohort.SpeciesDefinitionId });
            s.InitialState.Displacements!.Add(new() { DisplacementId = "displacement.protected-" + index.ToString("d2"), SourcePlanetId = world.PlanetId, CohortId = cohort.CohortId, Population = 5, Cause = "repressed" });
        }
        var arrival = PopulationData.Copy(world.Cohorts[0]); arrival.CohortId = "cohort.arrival"; world.Cohorts.Add(arrival); s.InitialState.EntityIdentities.Add(new() { EntityId = arrival.CohortId, DefinitionId = arrival.SpeciesDefinitionId });
        CohortCompaction.Execute(world, s.InitialState);
        Assert.Equal(32, world.Cohorts.Count); Assert.Equal(330, PopulationRules.Population(world));
        Assert.All(s.InitialState.Displacements!, r => Assert.Contains(world.Cohorts, c => c.CohortId == r.CohortId && c.Population >= r.Population));
    }

}
