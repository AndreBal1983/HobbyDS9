using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class PoliticsTests
{
    private static ScenarioDto Scenario(){var s=VerticalSliceFixture.Scenario();PopulationFixture.RemoveMars(s);return s;}
    private static ColonyStateDto World(ScenarioDto s)=>s.InitialState.Colonies!.Single(c=>c.ColonyId=="colony.sol");
    private static string Json(object value)=>JsonSerializer.Serialize(value);
    private static CommandEnvelope Command(ICampaignSession app,string policy="governance.autonomy")
    {var snapshot=app.CaptureSnapshot().Campaign;return new(){CommandId="cmd.policy",ActorCivilizationId="civ.federation",ExpectedTurn=snapshot.TurnNumber,ExpectedRevision=snapshot.StateRevision,Kind=CommandKind.SetWorldGovernancePolicy,SetWorldGovernancePolicy=new(){ColonyId="colony.sol",PolicyDefinitionId=policy}};}
    [Fact]
    public void PolicyCommandUsesRevisionLedgerAndDetachedSnapshot()
    {
        var s=Scenario();var app=CampaignApplication.StartScenario(s);var old=app.CaptureSnapshot();var envelope=Command(app);
        Assert.True(app.SubmitCommand(envelope).Accepted);Assert.Equal(1,app.CaptureSnapshot().Campaign.StateRevision);
        Assert.Equal("governance.autonomy",app.CaptureSnapshot().Campaign.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").State.Politics.GovernancePolicyDefinitionId);
        Assert.Equal("governance.integration",old.Campaign.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").State.Politics.GovernancePolicyDefinitionId);
        var before=Json(app.ExportSave());Assert.Equal(CommandErrorCode.DuplicateCommand,app.SubmitCommand(envelope).ErrorCode);Assert.Equal(before,Json(app.ExportSave()));
        envelope.CommandId="cmd.stale";Assert.Equal(CommandErrorCode.StaleRevision,app.SubmitCommand(envelope).ErrorCode);Assert.Equal(before,Json(app.ExportSave()));
    }
    [Theory]
    [InlineData("unknown",CommandErrorCode.UnknownGovernancePolicy)][InlineData("foreign",CommandErrorCode.ColonyNotOwned)]
    [InlineData("unchanged",CommandErrorCode.GovernancePolicyUnchanged)][InlineData("missing",CommandErrorCode.UnknownColony)]
    public void InvalidPoliciesAreOrdinaryAtomicRejections(string fault,CommandErrorCode expected)
    {
        var app=CampaignApplication.StartScenario(Scenario());var command=Command(app);var payload=command.SetWorldGovernancePolicy!;
        if(fault=="unknown")payload.PolicyDefinitionId="governance.absent";
        if(fault=="foreign")payload.ColonyId="colony.cardassia";
        if(fault=="unchanged")payload.PolicyDefinitionId="governance.integration";
        if(fault=="missing")payload.ColonyId="colony.absent";
        var before=Json(app.ExportSave());Assert.Equal(expected,app.SubmitCommand(command).ErrorCode);Assert.Equal(before,Json(app.ExportSave()));
    }
    [Theory]
    [InlineData("rate")][InlineData("integration")][InlineData("factor")][InlineData("duplicate")][InlineData("cycle")]
    [InlineData("missing-policy")][InlineData("invalid-resistance")][InlineData("timer")][InlineData("missing-state")][InlineData("missing-effects")]
    public void InvalidPoliticalStateAndContentReject(string error)
    {
        var s=Scenario();var policy=s.Definitions.GovernancePolicies![0];var world=World(s);
        switch(error)
        {
            case "rate":policy.SatisfactionChange=501;break;
            case "integration":policy.IntegrationChange=26;break;
            case "factor":policy.LogisticsFactor=-1;break;
            case "duplicate":s.Definitions.GovernancePolicies.Add(policy);break;
            case "cycle":policy.LegalTransitions=new(){new(){From=LegalStatus.Resident,To=LegalStatus.Citizen,RequiredTurns=1},new(){From=LegalStatus.Citizen,To=LegalStatus.Resident,RequiredTurns=1}};break;
            case "missing-policy":world.Politics!.GovernancePolicyDefinitionId="governance.absent";break;
            case "invalid-resistance":world.Politics!.Resistance=(ResistanceState)10;break;
            case "timer":world.Politics!.EscalationTurns=3;break;
            case "missing-state":world.Politics=null;break;
            case "missing-effects":s.Definitions.PoliticalMemoryEffects=null;break;
        }
        Assert.False(FoundationValidator.ValidateScenario(s).IsValid);
    }
    [Fact]
    public void TreatmentChangesRatingsWithoutSpeciesOrAffinityPenalties()
    {
        var s=Scenario();var world=World(s);foreach(var c in world.Cohorts!){c.Satisfaction=7000;c.Integration=7000;c.CivicAlignment=7000;c.Radicalisation=3000;}
        var other=JsonSerializer.Deserialize<ScenarioDto>(Json(s))!;
        foreach(var c in World(other).Cohorts!){c.SpeciesDefinitionId="species.ferengi";c.ExternalAffinities!.Add(new(){CivilizationId="civ.cardassian",Rating=10000});}
        // Equalize environmental suitability: biology may change environmental experience, never political identity.
        foreach(var species in s.Definitions.Species!)foreach(var habitat in species.Suitabilities!)habitat.SuitabilityBps=8000;
        foreach(var species in other.Definitions.Species!)foreach(var habitat in species.Suitabilities!)habitat.SuitabilityBps=8000;
        PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,true);PoliticsRules.ExecuteWorld(World(other),other.InitialState,other.Definitions,true);
        Assert.Equal(world.Cohorts.Select(c=>(c.Satisfaction,c.Integration,c.CivicAlignment,c.Radicalisation)),World(other).Cohorts!.Select(c=>(c.Satisfaction,c.Integration,c.CivicAlignment,c.Radicalisation)));
        Assert.All(world.Cohorts,c=>{Assert.InRange(c.Integration-7000,0,25);Assert.True(c.CivicAlignment>7000);Assert.True(c.Radicalisation<3000);});
    }
    [Fact]
    public void PoorTreatmentAndActiveMemoriesDriveExplicitPressureReasons()
    {
        var s=Scenario();var world=World(s);world.Stability=1500;world.Prosperity=1000;world.Politics!.GovernancePolicyDefinitionId="governance.extraction";
        foreach(var c in world.Cohorts!){c.Satisfaction=2000;c.Integration=3000;c.CivicAlignment=2000;c.Radicalisation=5000;c.LegalStatus=LegalStatus.Occupied;c.Memories!.Add(new(){Kind="repressed",Magnitude=10000,StartTurn=1,ExpiryTurn=20,DecayPerTurn=100});}
        PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,false);
        Assert.All(world.Cohorts,c=>{Assert.True(c.Satisfaction<2000);Assert.True(c.Radicalisation>5000);Assert.True(c.CivicAlignment<2000);});
        Assert.All(world.Politics.CohortReports!,r=>{Assert.Contains(r.Reasons!,v=>v.Code=="memory.repressed");Assert.Contains(r.Reasons!,v=>v.Code=="legal-treatment");});
        Assert.True(world.Politics.Pressure>5000);
    }
    [Fact]
    public void ExpiredMemoriesHaveNoPoliticalEffectAndMagnitudeScales()
    {
        var s=Scenario();var world=World(s);var c=world.Cohorts![0];c.Memories!.Add(new(){Kind="repressed",Magnitude=5000,StartTurn=1,ExpiryTurn=2});
        PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,true);
        Assert.Equal(-75,world.Politics!.CohortReports!.Single(r=>r.CohortId==c.CohortId).Reasons!.Single(r=>r.Code=="memory.repressed").SatisfactionChange);
        s.InitialState.TurnNumber=2;PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,true);
        Assert.DoesNotContain(world.Politics.CohortReports!.SelectMany(r=>r.Reasons!),r=>r.Code=="memory.repressed");
    }
    [Fact]
    public void LegalInclusionRequiresSustainedEligibilityAndResetsWhenConditionsFail()
    {
        var s=Scenario();var world=World(s);var c=world.Cohorts![0];c.LegalStatus=LegalStatus.Refugee;c.Satisfaction=8000;c.Integration=8000;c.CivicAlignment=8000;
        for(var turn=0;turn<7;turn++){PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,true);Assert.Equal(LegalStatus.Refugee,c.LegalStatus);}
        world.Stability=1000;PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,true);Assert.Equal(0,c.LegalStatusEligibleTurns);
        world.Stability=8000;for(var turn=0;turn<8;turn++)PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,true);
        Assert.Equal(LegalStatus.Resident,c.LegalStatus);Assert.Equal(0,c.LegalStatusEligibleTurns);
    }
    [Fact]
    public void PressureWeightsPopulationAndResistanceRequiresSustainedConditions()
    {
        var s=Scenario();var world=World(s);var majority=world.Cohorts![0];majority.Population=100000;majority.Satisfaction=10000;majority.CivicAlignment=10000;majority.Radicalisation=0;
        foreach(var minority in world.Cohorts.Skip(1)){minority.Population=1;minority.Satisfaction=0;minority.CivicAlignment=0;minority.Radicalisation=10000;}
        Assert.True(PoliticsRules.Pressure(world)<1000);
        foreach(var c in world.Cohorts){c.Satisfaction=0;c.CivicAlignment=0;c.Radicalisation=10000;}
        world.Politics!.GovernancePolicyDefinitionId="governance.extraction";world.Stability=0;
        for(var i=0;i<2;i++){PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,false);Assert.Equal(ResistanceState.Quiet,world.Politics.Resistance);}
        PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,false);Assert.Equal(ResistanceState.Tense,world.Politics.Resistance);
        for(var i=0;i<9;i++)PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,false);
        Assert.Equal(ResistanceState.OrganizedResistance,world.Politics.Resistance);Assert.True(world.Politics.Disruptions>0);
        var rng=s.InitialState.RngState.State;PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,false);Assert.Equal(rng,s.InitialState.RngState.State);
        foreach(var c in world.Cohorts){c.Satisfaction=10000;c.CivicAlignment=10000;c.Radicalisation=0;}
        world.Stability=10000;world.Politics.GovernancePolicyDefinitionId="governance.integration";
        for(var i=0;i<4;i++)PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,true);Assert.Equal(ResistanceState.OrganizedResistance,world.Politics.Resistance);
        PoliticsRules.ExecuteWorld(world,s.InitialState,s.Definitions,true);Assert.Equal(ResistanceState.Unrest,world.Politics.Resistance);
    }
    [Fact]
    public void ReorderedPoliticalInputsAndReloadPreserveFullState()
    {
        var s=Scenario();var b=JsonSerializer.Deserialize<ScenarioDto>(Json(s))!;b.Definitions.GovernancePolicies!.Reverse();b.Definitions.PoliticalMemoryEffects!.Reverse();
        foreach(var c in b.InitialState.Colonies!){c.Cohorts!.Reverse();foreach(var row in c.Cohorts){row.Memories!.Reverse();row.ExternalAffinities!.Reverse();}}
        var a=VerticalSliceFixture.Session(s);var reordered=VerticalSliceFixture.Session(b);
        for(var turn=0;turn<15;turn++){VerticalSliceFixture.Advance(a);VerticalSliceFixture.Advance(reordered);Assert.Equal(VerticalSliceFixture.Json(a),VerticalSliceFixture.Json(reordered));reordered=new SimulationSession(reordered.ExportSave(),b.Definitions,VerticalSliceFixture.Compatibility(b));}
    }
    [Fact]
    public void LateFailureRollsBackPoliticalHistoryTimersAndPolicyReports()
    {
        var s=Scenario();var session=VerticalSliceFixture.Session(s,(phase,_)=>phase!=StrategicTurnPhase.Movement);var before=VerticalSliceFixture.Json(session);
        Assert.Equal(TurnErrorCode.PhaseFailed,VerticalSliceFixture.Advance(session).ErrorCode);Assert.Equal(before,VerticalSliceFixture.Json(session));
    }
}
