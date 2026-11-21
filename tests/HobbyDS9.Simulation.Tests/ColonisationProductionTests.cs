using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationProductionTests
{
    const string Human="civ.federation";
    static string Json(object x)=>JsonSerializer.Serialize(x);
    static GameStateDto Game(CampaignColonisationSaveDto root)=>root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
    static void Advance(CampaignTacticalReviewSession s)
    {var f=s.CaptureStatus();var r=s.AdvanceTurn(f.Turn,f.CampaignRevision);Assert.True(r.Accepted,r.Error+" "+s.LastPhysicalFailure);}
    [Fact]public void RealMixedQueueManufacturesFiniteEquipmentWithoutCreditsAndReplaysPendingAndCompleted()
    {
        var p=ColonisationInventoryTests.Package();var s=CampaignTacticalReviewSession.StartColonisation(p,PublishedConstructionProfileTests.Settings(250));
        var initial=s.ExportColonisationSave();var credits=Game(initial).Civilizations!.Single(c=>c.CivilizationId==Human).Credits;
        var f=s.CaptureStatus();var q=s.SubmitColonisation(new(){ActorId=Human,CommandId="command.kit.actual",Turn=f.Turn,Revision=f.CampaignRevision,
            Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}});Assert.True(q.Accepted,q.Error+" "+s.LastPhysicalFailure);
        var pending=s.ExportColonisationSave();Assert.Single(pending.Production!);Assert.Equal(credits,Game(pending).Civilizations!.Single(c=>c.CivilizationId==Human).Credits);
        Assert.Equal(Json(pending),Json(CampaignTacticalReviewSession.LoadColonisation(p,pending).ExportColonisationSave()));
        for(int i=0;i<8&&!s.ExportColonisationSave().ProductionHistory!.Any(r=>r.Completed&&r.Order!.Kind==ColonisationProductionKind.SettlementKit);i++)Advance(s);
        var completed=s.ExportColonisationSave();var receipt=Assert.Single(completed.ProductionHistory!,r=>r.Completed&&r.Order!.Kind==ColonisationProductionKind.SettlementKit);
        Assert.Equal(60,receipt.Order!.CompletedIndustry);Assert.Equal(6,Assert.Single(receipt.Order.ConsumedResources!).Amount);
        Assert.Equal(1,Game(completed).Colonies!.Single(c=>c.ColonyId=="colony.sol").Stockpiles!.Single(r=>r.ResourceDefinitionId==p.Settlement!.EquipmentResourceId).Amount);
        Assert.Equal(Json(completed),Json(CampaignTacticalReviewSession.LoadColonisation(p,completed).ExportColonisationSave()));
        var tampered=JsonSerializer.Deserialize<CampaignColonisationSaveDto>(Json(completed))!;tampered.FrozenPlanets!.Definitions![0].Gravity++;
        tampered.FrozenPlanets.ContentHash=ColonisationData.InventoryHash(tampered.FrozenPlanets);
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadColonisation(p,tampered));Assert.Equal(Json(completed),Json(s.ExportColonisationSave()));
        var malformed=JsonSerializer.Deserialize<CampaignColonisationSaveDto>(Json(completed))!;
        malformed.Journal![0]=new ColonisationOperationDto{Existing=new CampaignActivityOperationDto()};
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadColonisation(p,malformed));
    }
}
