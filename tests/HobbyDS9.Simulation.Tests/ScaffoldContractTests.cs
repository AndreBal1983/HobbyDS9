using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ScaffoldContractTests
{
    [Theory]
    [InlineData("HobbyDS9.Contracts")]
    [InlineData("HobbyDS9.Simulation")]
    [InlineData("HobbyDS9.Scenarios")]
    public void PortableAssemblyTargetsStandardAndOnlyAllowedDependencies(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        Assert.NotNull(framework);
        Assert.Equal(".NETStandard,Version=v2.1", framework.FrameworkName);
        var permitted = new[] { "netstandard", "HobbyDS9.Contracts" };
        foreach (var reference in assembly.GetReferencedAssemblies())
            Assert.Contains(reference.Name, permitted);
    }

    [Fact]
    public void PendingCheckpointRoundTripPreservesResumeInputs()
    {
        var checkpoint = new CombatCheckpointDto
        {
            Phase = SessionPhase.AwaitingCombat,
            TurnNumber = 7,
            StateRevision = 23,
            PendingCombats = new()
            {
                new() { Request = new()
                {
                    EncounterId = "encounter.7.system.border",
                    TurnNumber = 7,
                    LocationNodeId = "system.border",
                    RulesVersion = "slice-v1",
                    ContentHash = "fixture-content-hash",
                    ResolverId = "no-loss-v1",
                    ResolverVersion = 1,
                    RandomAlgorithmId = "splitmix64-rejection-v1",
                    RandomState = ulong.MaxValue,
                    Participants = new()
                    {
                        new() { CivilizationId = "civ.alpha", FleetId = "fleet.alpha",
                            Ships = new() { new() { ShipId = "ship.alpha", DesignId = "design.scout", Hull = 10, MaxHull = 10, CombatProfile = new() } } },
                        new() { CivilizationId = "civ.beta", FleetId = "fleet.beta",
                            Ships = new() { new() { ShipId = "ship.beta", DesignId = "design.scout", Hull = 10, MaxHull = 10, CombatProfile = new() } } }
                    }
                } }
            }
        };

        var json = JsonSerializer.Serialize(checkpoint);
        var loaded = JsonSerializer.Deserialize<CombatCheckpointDto>(json)!;
        Assert.Equal(SessionPhase.AwaitingCombat, loaded.Phase);
        Assert.Equal(7, loaded.TurnNumber);
        Assert.Equal(23, loaded.StateRevision);
        var request = Assert.Single(loaded.PendingCombats).Request;
        Assert.Equal(ulong.MaxValue, request.RandomState);
        Assert.Equal(new[] { "fleet.alpha", "fleet.beta" }, request.Participants.Select(p => p.FleetId));
        Assert.Equal("ship.beta", Assert.Single(request.Participants[1].Ships).ShipId);
        Assert.Equal(json, JsonSerializer.Serialize(loaded));
    }

    [Fact]
    public void ResultRoundTripPreservesEventOrderAndConsequences()
    {
        var result = new CombatResultDto
        {
            EncounterId = "encounter.7.system.border",
            ResolverId = "fixture",
            ResolverVersion = 1,
            Status = CombatResolutionStatus.Resolved,
            FinalRandomState = 47,
            ShipChanges = new() { new() { ShipId = "ship.alpha", RemainingHull = 6 } },
            FleetChanges = new() { new() { FleetId = "fleet.beta", Disposition = FleetCombatDisposition.Retreated,
                RetreatNodeId = "system.home" } },
            Events = new()
            {
                new() { Sequence = 0, Kind = CombatEventKind.ShipDamaged, ShipId = "ship.alpha" },
                new() { Sequence = 1, Kind = CombatEventKind.FleetRetreated, FleetId = "fleet.beta" },
                new() { Sequence = 2, Kind = CombatEventKind.EncounterResolved }
            }
        };

        var loaded = JsonSerializer.Deserialize<CombatResultDto>(JsonSerializer.Serialize(result))!;
        Assert.Equal(CombatResolutionStatus.Resolved, loaded.Status);
        Assert.Equal(new[] { 0, 1, 2 }, loaded.Events.Select(e => e.Sequence));
        Assert.Equal(new[] { CombatEventKind.ShipDamaged, CombatEventKind.FleetRetreated, CombatEventKind.EncounterResolved },
            loaded.Events.Select(e => e.Kind));
        Assert.Equal(6, Assert.Single(loaded.ShipChanges).RemainingHull);
        Assert.Equal("system.home", Assert.Single(loaded.FleetChanges).RetreatNodeId);
        Assert.Equal(47UL, loaded.FinalRandomState);
    }
}
