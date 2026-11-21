using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationRepeatTests
{
    readonly Xunit.Abstractions.ITestOutputHelper output;
    public ColonisationRepeatTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    internal static CampaignColonisationRepeatPackageDto Package()=>CampaignTacticalReviewSession.CreateRepeatPackage(ColonisationGrowthTests.Package());
    static string Wire(object root)=>JsonSerializer.Serialize(root);
    [Fact]public void ExplicitOwnedSourceInitialReplayAndPartialExportsRemainClosed()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartRepeatGame(p,PublishedConstructionProfileTests.Settings(30));var root=s.ExportRepeatSave();var before=Wire(root);
        Assert.Equal(13,root.FormatVersion);Assert.Single(root.RepeatGoals!);Assert.Equal(before,Wire(CampaignTacticalReviewSession.LoadRepeatSave(p,root).ExportRepeatSave()));
        Assert.Throws<ArgumentException>(()=>s.ExportGrowthSave());Assert.Throws<ArgumentException>(()=>s.ExportColonisationExpansionSave());Assert.Throws<ArgumentException>(()=>s.ExportColonisationSave());
        var wrong=Package();wrong.PackageHash=new string('0',64);Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartRepeatGame(wrong,PublishedConstructionProfileTests.Settings(30)));
        p.Policy!.AdditionalColonies=2;p.BaseGrowth!.PlannerPolicy!.MaximumAccepted=1;p.BaseGrowth.BaseRosterV2!.StartingRoster!.ClassId="changed";
        Assert.Equal(before,Wire(s.ExportRepeatSave()));root.RepeatGoals![0].Assignment!.HomeColonyId="foreign";Assert.Equal(before,Wire(s.ExportRepeatSave()));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadRepeatSave(Package(),root));
        var app=HobbyDS9.Application.CampaignColonisationRepeatApplication.StartRepeatGame(Package(),PublishedConstructionProfileTests.Settings(30));var stamp=app.CaptureStatus();
        var fork=Assert.IsAssignableFrom<HobbyDS9.Application.ICampaignColonisationRepeatSession>(app.ForkOperations(stamp.Turn,stamp.CampaignRevision));
        Assert.True(fork.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(before,Wire(app.ExportRepeatSave()));
    }
    [Fact]public void Actual250SecondColonyCheckpointsReplayAndNoThirdSettlement()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartRepeatGame(p,PublishedConstructionProfileTests.Settings(250));var checkpoints=new HashSet<string>();
        for(var n=0;n<=128;n++)
        {
            var root=s.ExportRepeatSave();var first=Assert.Single(root.AiPlans!);var goal=Assert.Single(root.RepeatGoals!);var second=goal.Assignment!;
            output.WriteLine($"turn{Game(root.BaseCampaign!).TurnNumber} first={first.Stage}/{first.HoldReason} repeat={goal.State}/{second.Stage}/{second.HoldReason} target={second.TargetPlanetId} manifest={goal.SettlementManifestId} supply={goal.SupplyCampId}");
            Assert.True(first.LatestActions!.Count<=8);Assert.True(first.LatestActions.Count(a=>a.Accepted)<=4);Assert.Empty(second.LatestActions!);
            void Check(string name){if(checkpoints.Add(name))Assert.Equal(Wire(root),Wire(CampaignTacticalReviewSession.LoadRepeatSave(p,root).ExportRepeatSave()));}
            if(goal.State==ColonisationRepeatState.Active&&root.BaseCampaign!.Production!.Any(q=>q.QueueItemId==second.KitQueueId))Check("queue");
            var cargo=root.BaseCampaign!.Colonisation!.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==goal.SettlementManifestId);
            if(cargo?.Stage==SettlerManifestStage.Loaded){Check("loaded");var ship=Game(root.BaseCampaign).Ships!.Single(x=>x.ShipId==cargo.ShipId);var fleet=Game(root.BaseCampaign).Fleets!.Single(f=>f.FleetId==ship.FleetId);var physical=root.BaseCampaign.Campaign!.Integrated.Physical!;var movement=physical.LastMovement.SingleOrDefault(r=>r.FleetId==fleet.FleetId&&r.EdgesCompleted>0);if(movement!=null){var start=physical.MovementProof!.Start.Fleets!.Single(f=>f.FleetId==fleet.FleetId);Assert.NotEqual(start.LocationNodeId,movement.EndNodeId);Assert.Contains(physical.MovementProof.Orders,o=>o.FleetId==fleet.FleetId&&o.RouteNodeIds.Contains(second.TargetNodeId!));output.WriteLine($"second physical crossing turn={Game(root.BaseCampaign).TurnNumber} sourceSystem={Game(root.BaseCampaign).Colonies!.Single(w=>w.ColonyId==second.HomeColonyId).SystemId} targetSystem={root.BaseCampaign.FrozenPlanets!.Bodies!.Single(o=>o.PlanetId==second.TargetPlanetId).SystemId} startNode={start.LocationNodeId} endNode={movement.EndNodeId} targetNode={second.TargetNodeId} edges={movement.EdgesCompleted}");Check("travel-boundary");}}
            if(root.BaseCampaign.Colonisation.ManifestRecords!.Any(m=>m.OwnerId==first.ActorId&&m.Kind==SettlerManifestKind.SupplyOnly&&m.Stage==SettlerManifestStage.Loaded)){Assert.NotNull(goal.SupplyCampId);Assert.Contains(root.BaseCampaign.Colonisation.CampSources!,c=>c.CampId==goal.SupplyCampId&&c.OwnerId==first.ActorId);Check("supply");}
            if(root.BaseCampaign.Colonisation.CampSources!.Any(c=>c.CampId==second.CampId&&c.Stage==ColonyEstablishmentStage.Founding))Check("founding");
            if(goal.State==ColonisationRepeatState.Established)
            {
                Assert.Equal(ColonisationAiStage.GrowthClosed,first.Stage);Assert.NotEqual(first.EstablishedColonyId,second.EstablishedColonyId);
                var camp=root.BaseCampaign!.Colonisation!.CampSources!.Single(c=>c.CampId==second.CampId);Assert.Equal(4,camp.FedWeeks);Assert.Equal(16,camp.CompletedIndustry);Assert.Equal(0,camp.Deaths);
                var manifest=root.BaseCampaign.Colonisation.ManifestRecords!.Single(m=>m.ManifestId==goal.SettlementManifestId);Assert.Equal(100,manifest.PopulationTransferred);Assert.Equal(1,manifest.EquipmentConsumed);
                Check("established");Assert.Equal(new[]{"established","founding","loaded","queue","supply","travel-boundary"},checkpoints.OrderBy(x=>x).ToArray());
                var original=root.BaseCampaign.Colonisation.ManifestOrigins!.Single(o=>o.ManifestId==manifest.ManifestId);Assert.Equal(100,original.EmbarkedPopulation);Assert.Equal(20,original.LoadedFood);Assert.Contains(original.LoadedMaterials!,r=>r.ResourceDefinitionId=="resource.settlement-kit"&&r.Amount==1);
                var firstCamp=root.BaseCampaign.Colonisation.CampSources!.Single(c=>c.CampId==first.CampId);Assert.NotEqual(firstCamp.ManifestId,camp.ManifestId);
                var sameShip=manifest.ShipId;Assert.Contains(root.BaseCampaign.Colonisation.ManifestRecords!,m=>m.ShipId==sameShip&&m.Kind==SettlerManifestKind.ImmigrationOnly&&m.PopulationTransferred==100);
                for(int i=0;i<4;i++)Advance(s);var later=s.ExportRepeatSave();Assert.Equal(goal.SettlementManifestId,later.RepeatGoals![0].SettlementManifestId);Assert.Equal(2,later.BaseCampaign!.Colonisation!.ManifestRecords!.Count(m=>m.OwnerId==first.ActorId&&m.Kind==SettlerManifestKind.Settlement));
                Assert.Equal(Wire(later),Wire(CampaignTacticalReviewSession.LoadRepeatSave(p,later).ExportRepeatSave()));return;
            }
            if(n==128)break;Advance(s);
        }
        Assert.Fail("Actual unmodified public250 second colony did not establish within128 advances.");
    }
    [Fact]public void RepeatEditionAndBothAssignmentsRollbackAtEveryPhysicalAndLateCopyBoundary()
    {
        var s=CampaignTacticalReviewSession.StartRepeatGame(Package(),PublishedConstructionProfileTests.Settings(30));var before=Wire(s.ExportRepeatSave());
        foreach(var phase in Enum.GetValues<StrategicTurnPhase>())
        {
            bool seen=false;s.PhysicalPhaseProbe=p=>{if(p==phase){seen=true;return false;}return true;};var f=s.CaptureStatus();
            Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Wire(s.ExportRepeatSave()));
        }
        s.PhysicalPhaseProbe=null;
        foreach(var point in new[]{"after-command","before-copy","after-transcript-copy","after-cache-copy"})
        {
            bool seen=false;s.PublishedTransactionProbe=p=>{if(p==point){seen=true;throw new InvalidOperationException("Repeat fixture fault");}};var f=s.CaptureStatus();
            Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Wire(s.ExportRepeatSave()));
        }
        s.PublishedTransactionProbe=null;Advance(s);var root=s.ExportRepeatSave();Assert.Contains(root.AiPlans![0].LatestActions!,a=>a.Accepted);
        Assert.Equal(Wire(root),Wire(CampaignTacticalReviewSession.LoadRepeatSave(Package(),root).ExportRepeatSave()));
    }
    [Fact]public void LoadedSourceAndEveryExportLayerRemainDetachedAndWrongEditionCannotAdmit()
    {
        var package=Package();var initial=CampaignTacticalReviewSession.StartRepeatGame(package,PublishedConstructionProfileTests.Settings(30)).ExportRepeatSave();
        var loaded=CampaignTacticalReviewSession.LoadRepeatSave(package,initial);var before=Wire(loaded.ExportRepeatSave());
        package.BaseGrowth!.BaseRosterV2!.BaseV1!.Settlement!.MaximumFood=19;package.Policy!.SettlementAttempts=2;
        initial.BaseCampaign!.Colonisation!.FoodConsumed++;initial.AiPlans![0].HoldReason="mutated";initial.RepeatGoals![0].Assignment!.OwnAttemptedTargets!.Add("foreign");
        Assert.Equal(before,Wire(loaded.ExportRepeatSave()));
        var export=loaded.ExportRepeatSave();export.RepeatGoals![0].SupplyCampId="foreign";export.BaseCampaign!.Production!.Clear();Assert.Equal(before,Wire(loaded.ExportRepeatSave()));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadGrowthSave(ColonisationGrowthTests.Package(),new(){ProfileId=CampaignColonisationRepeatIdentity.Profile,BaseCampaign=loaded.ExportRepeatSave().BaseCampaign,AiPlans=loaded.ExportRepeatSave().AiPlans}));
        var bad=loaded.ExportRepeatSave();bad.RepeatGoals![0].State=ColonisationRepeatState.Established;bad.RepeatGoals[0].StartedTurn=1;bad.RepeatGoals[0].ClosedTurn=1;Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadRepeatSave(Package(),bad));
    }
}
