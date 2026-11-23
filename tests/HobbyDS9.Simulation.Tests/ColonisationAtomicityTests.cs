using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationAtomicityTests
{
    [Fact] public void ChartedNeighborsDoNotDiscloseBodiesAndOwnedViewsAreDetached()
    {
        var s=CampaignTacticalReviewSession.StartColonisation(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings());
        var snapshot=s.ExportColonisationSave();var homeSystems=Game(snapshot).Colonies!.Where(c=>c.OwnerCivilizationId==Human).Select(c=>c.SystemId).ToHashSet();
        var known=s.CaptureColonisationPlanets(Human);Assert.NotEmpty(known);Assert.All(known,p=>Assert.Contains(p.SystemId,homeSystems));
        Assert.Contains(s.CaptureGeneratedMap(Human).Regions,r=>r.SystemId!=null&&!homeSystems.Contains(r.SystemId));
        var before=Json(snapshot);known[0].Environment!.Gravity=9999;known[0].KnownControl!.ActorId="actor.forged";
        Assert.Equal(before,Json(s.ExportColonisationSave()));Assert.Throws<ArgumentException>(()=>s.CaptureColonisationPlanets("civ.cardassian"));
    }
    [Fact] public void EveryActualPhysicalPhaseFaultRollsBackTheWholeNewContextAndJournal()
    {
        var s=CampaignTacticalReviewSession.StartColonisation(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings());
        foreach(var phase in Enum.GetValues<StrategicTurnPhase>())
        {
            var before=Json(s.ExportColonisationSave());var seen=false;s.PhysicalPhaseProbe=p=>{if(p==phase){seen=true;return false;}return true;};
            var stamp=s.CaptureStatus();Assert.False(s.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Json(s.ExportColonisationSave()));
        }
        s.PhysicalPhaseProbe=null;Advance(s);
    }
    [Fact] public void LateEmbarkCopyFaultAndStaleDuplicateCommandsCannotDebitOrAllocate()
    {
        var x=Manufacture();var s=x.Session;var root=s.ExportColonisationSave();var home=Game(root).Colonies!.Single(c=>c.ColonyId=="colony.sol");
        var command=Command(s,ColonisationAction.EmbarkSettlers);command.Embark=new(){ShipId=x.Ship,ColonyId=home.ColonyId,CohortId=home.Cohorts!.First(c=>c.Population>=400).CohortId,Population=100,Food=20,Materials=new()};
        var before=Json(root);s.PublishedTransactionProbe=stage=>{if(stage=="after-cache-copy")throw new InvalidOperationException("Deliberate late colonisation copy fault.");};
        Assert.False(s.SubmitColonisation(command).Accepted);Assert.Equal(before,Json(s.ExportColonisationSave()));s.PublishedTransactionProbe=null;
        Assert.True(s.SubmitColonisation(command).Accepted,s.LastPhysicalFailure);var after=Json(s.ExportColonisationSave());
        Assert.False(s.SubmitColonisation(command).Accepted);Assert.Equal(after,Json(s.ExportColonisationSave()));
        var stale=Command(s,ColonisationAction.UnloadTransport);stale.Revision--;stale.Unload=new(){ManifestId=s.ExportColonisationSave().Colonisation!.ManifestRecords!.Single().ManifestId,ColonyId=home.ColonyId};
        Assert.Equal(CampaignTacticalError.StaleRevision,s.SubmitColonisation(stale).Error);Assert.Equal(after,Json(s.ExportColonisationSave()));
    }
}
