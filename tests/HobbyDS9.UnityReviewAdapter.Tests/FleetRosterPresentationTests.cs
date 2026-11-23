using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class FleetRosterPresentationTests
{
    [Fact]public void CurrentShieldStatusRequiresEveryActualSectorAndKeepsUnknownDistinctFromZero()
    {
        Assert.False(FleetRosterPresentation.Shields(null,new long[]{5,5,5,5}).Reported);
        Assert.False(FleetRosterPresentation.Shields(new long[]{5,5},new long[]{5,5,5,5}).Reported);
        Assert.Equal("No shields",FleetRosterPresentation.Shields(new long[4],new long[4]).State);
        Assert.Equal("Full",FleetRosterPresentation.Shields(new long[]{5,5,5,5},new long[]{5,5,5,5}).State);
        var reduced=FleetRosterPresentation.Shields(new long[]{5,2,0,4},new long[]{5,5,5,5});Assert.Equal(11,reduced.Current);Assert.Equal(20,reduced.Maximum);Assert.Equal("Reduced",reduced.State);
        Assert.Equal("Depleted",FleetRosterPresentation.Shields(new long[4],new long[]{5,5,5,5}).State);
        Assert.False(FleetRosterPresentation.Shields(new long[]{6,5,5,5},new long[]{5,5,5,5}).Reported);
    }
    [Fact]public void MissionUsesAcceptedIntentRouteAndActivityRatherThanShipClass()
    {
        Assert.Equal("Not reported",FleetRosterPresentation.Mission(null,0,null,false,false));
        Assert.Equal("Scientific study",FleetRosterPresentation.Mission(CampaignTravelIntent.Stay,0,null,true,false));
        Assert.Equal("Travel → Known destination",FleetRosterPresentation.Mission(CampaignTravelIntent.Leave,2,"Known destination",false,false));
        Assert.Equal("Defend",FleetRosterPresentation.Mission(CampaignTravelIntent.StayEngage,0,null,false,false));
        Assert.Equal("Follow contact",FleetRosterPresentation.Mission(CampaignTravelIntent.Follow,0,null,false,false));
    }
    [Fact]public void FiltersMatchActualTextAndRetainHonestUnknownState()
    {
        var unknown=FleetRosterPresentation.Shields(null,null);var reduced=FleetRosterPresentation.Shields(new long[]{1,0,0,0},new long[]{5,5,5,5});
        bool Match(string q,string c,string f,bool? damage,FleetShieldSummary shield)=>FleetRosterPresentation.Matches(q,c,f,"First Fleet","Enterprise","Explorer","Sol","Remain",damage,shield);
        Assert.True(Match("explorer","All conditions","All fleets",false,unknown));Assert.False(Match("unknown ship","All conditions","All fleets",false,unknown));
        Assert.False(Match("","All conditions","Other fleet",false,unknown));Assert.False(Match("","Reduced shields","All fleets",false,unknown));
        Assert.True(Match("","Reduced shields","All fleets",false,reduced));Assert.True(Match("","Status not reported","All fleets",null,unknown));
    }
}
