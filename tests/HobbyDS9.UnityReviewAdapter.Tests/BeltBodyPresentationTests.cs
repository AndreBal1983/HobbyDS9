using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class BeltBodyPresentationTests
{
    static BeltBodySnapshot Body(bool current,BeltBodyObservationDto? observation=null)=>new(
        new(){BodyId="body.test",FeatureId="belt.test",SystemId="system.test",DisplayName="Test belt",InitialDeposit=999,HabitabilityBps=0},
        current,observation,null,null,Array.Empty<string>(),Array.Empty<string>());
    [Fact]public void UnobservedDefinitionDepositAndScoreAreNeverDisplayedAsKnown()
    {
        var body=Body(true);
        Assert.Equal("Unknown",BeltBodyPresentation.Ore(body));Assert.Equal("Not surveyed",BeltBodyPresentation.Habitability(body));
        Assert.Equal("Resource unknown",BeltBodyPresentation.Resource(body));Assert.Equal("Survey required",BeltBodyPresentation.ResourceStamp(body));
    }
    [Fact]public void StaleSurveyDisplaysItsRecordedQuantityAndDateWithoutRefreshingFromDefinition()
    {
        var body=Body(false,new(){BodyId="body.test",ActorId="human",SurveyedTurn=3,ObservedTurn=5,ObservedRemaining=47});
        Assert.Equal("47",BeltBodyPresentation.Ore(body));Assert.Equal("Last reported · week 5",BeltBodyPresentation.ResourceStamp(body));
        Assert.Equal("Surveyed · week 3",BeltBodyPresentation.SurveyStamp(body));Assert.Equal("0%",BeltBodyPresentation.Habitability(body));
    }
    [Fact]public void CurrentKnownZeroRemainsDistinctFromUnknown()
    {
        var body=Body(true,new(){BodyId="body.test",ActorId="human",SurveyedTurn=2,ObservedTurn=4,ObservedRemaining=0});
        Assert.Equal("0",BeltBodyPresentation.Ore(body));Assert.Equal("Current report · week 4",BeltBodyPresentation.ResourceStamp(body));
        Assert.Equal("Dilithium",BeltBodyPresentation.Resource(body));Assert.Contains("Review",BeltBodyPresentation.Reason(BeltBodyError.StaleRevision));
    }
}
