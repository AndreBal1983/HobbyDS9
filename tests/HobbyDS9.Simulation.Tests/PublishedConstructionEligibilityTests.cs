using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;

namespace HobbyDS9.Simulation.Tests;
public sealed partial class PublishedConstructionProfileTests
{
    [Fact] public void EligibilityUsesRealPaidCandidateWithoutChangingAuthorityAndRejectsForeignIdentities()
    {
        var source=Package();var support=JsonSerializer.Deserialize<TacticalSupportCatalogueDto>(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory,"scenarios/published-construction-v1/support.json")))!;
        var p=CampaignPublishedConstructionApplication.CreatePackage(source.BaseTemplate!,support,512);
        var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());Publish(app,1);
        var before=Json(app.ExportPublishedConstructionSave());var stamp=app.CaptureStatus();
        var a=app.CaptureConstructionEligibility(Human,Id,1,"colony.sol");
        Assert.True(a.CanQueue);Assert.True(a.Cost.Credits>0);Assert.Equal(stamp.CampaignRevision,a.CampaignRevision);
        Assert.Equal(before,Json(app.ExportPublishedConstructionSave()));
        Assert.Throws<ArgumentException>(()=>app.CaptureConstructionEligibility("civ.cardassian",Id,1,"colony.sol"));
        Assert.Throws<ArgumentException>(()=>app.CaptureConstructionEligibility(Human,Id,1,"colony.cardassia"));
        Assert.Throws<ArgumentException>(()=>app.CaptureConstructionEligibility(Human,"blueprint.unknown",1,"colony.sol"));
        Queue(app);var after=app.CaptureStrategy(Human).Civilization.Credits;
        Assert.Equal(a.Cost.Credits,JsonSerializer.Deserialize<CampaignPublishedConstructionSaveDto>(before)!.Campaign!.Integrated.Physical!.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId==Human).Credits-after);
        Assert.NotEqual(a.CampaignRevision,app.CaptureStatus().CampaignRevision);
    }
    [Fact] public void EligibilityReportsPostPaymentCapacityFailureAndDoesNotReserveCommandsIdsOrResources()
    {
        var source=Package();source.BaseTemplate!.ReplayOperationLimit=2;
        var p=CampaignPublishedConstructionApplication.CreatePackage(source.BaseTemplate,source.Support!,512);
        var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());Publish(app,1);
        var before=Json(app.ExportPublishedConstructionSave());
        var a=app.CaptureConstructionEligibility(Human,Id,1,"colony.sol");
        Assert.True(a.Supported);Assert.Equal(CommandErrorCode.None,a.OrdinaryReason);Assert.False(a.CanQueue);
        Assert.Equal(CampaignTacticalError.Overflow,a.CommandResult);Assert.Equal(before,Json(app.ExportPublishedConstructionSave()));
        Assert.Equal(Json(a),Json(app.CaptureConstructionEligibility(Human,Id,1,"colony.sol")));
        Assert.Equal(before,Json(app.ExportPublishedConstructionSave()));
    }
}
