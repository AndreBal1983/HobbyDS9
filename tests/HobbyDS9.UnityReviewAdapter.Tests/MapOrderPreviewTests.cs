using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class MapOrderPreviewTests
{
    [Fact]
    public void AppendedWaypointRetainsAcceptedPrefixAndSurvivesSaveContinue()
    {
        using var fixture=new UnityReviewFixture();var model=new ReviewUiModel(fixture.Package,fixture.Codec,fixture.Store,A);
        var first=model.Current.FindKnownRoute(FA,"nav.bajor")!;
        Assert.True(model.SendPreviewedTravel(first,ReviewUiModel.NewCommandId()));
        var frame=model.Current.Frame;var prefix=Assert.Single(frame.Travel!.OwnOrders).RouteNodeIds.ToArray();
        var append=model.Current.FindKnownRoute(FA,"nav.sol",true)!;
        Assert.True(append.Route.Success);Assert.Equal(prefix,append.Route.Steps.Take(prefix.Length));Assert.Equal("nav.sol",append.Destination);
        Assert.Same(frame,model.Current.Frame);Assert.True(model.SendPreviewedTravel(append,ReviewUiModel.NewCommandId()));
        Assert.Equal(append.Route.Steps,Assert.Single(model.Current.Frame.Travel!.OwnOrders).RouteNodeIds);
        Assert.True(model.Save("waypoints"));Assert.True(model.Load("waypoints",true));
        Assert.Equal(append.Route.Steps,Assert.Single(model.Current.Frame.Travel!.OwnOrders).RouteNodeIds);
        Assert.False(model.SendPreviewedTravel(append,ReviewUiModel.NewCommandId()));
    }

    [Fact]
    public void PlainDestinationReplacesRatherThanAppendsAndBadWaypointLeavesOrderUntouched()
    {
        using var fixture=new UnityReviewFixture();var model=new ReviewUiModel(fixture.Package,fixture.Codec,fixture.Store,A);
        var first=model.Current.FindKnownRoute(FA,"nav.bajor")!;Assert.True(model.SendPreviewedTravel(first,ReviewUiModel.NewCommandId()));
        var frame=model.Current.Frame;var bad=model.Current.FindKnownRoute(FA,"nav.missing",true)!;
        Assert.False(bad.Route.Success);Assert.False(model.SendPreviewedTravel(bad,ReviewUiModel.NewCommandId()));Assert.Same(frame,model.Current.Frame);
        var replace=model.Current.FindKnownRoute(FA,"nav.bajoran-wormhole")!;Assert.True(replace.Route.Success);
        Assert.True(model.SendPreviewedTravel(replace,ReviewUiModel.NewCommandId()));Assert.Equal(replace.Route.Steps,Assert.Single(model.Current.Frame.Travel!.OwnOrders).RouteNodeIds);
    }

    [Fact]
    public void PreviewIsReadOnlyAndConfirmedFollowUsesTheExactObservedContactAndRoute()
    {
        using var fixture=new UnityReviewFixture();
        var model=new ReviewUiModel(fixture.Package,fixture.Codec,fixture.Store,A);
        var frame=model.Current.Frame;
        var contact=Assert.Single(frame.Travel!.KnownContacts);
        var offer=model.Current.FindKnownRoute(FA,contact.LastSeenNodeId)!;
        Assert.Same(frame,model.Current.Frame);Assert.False(model.HasLocalEdits);
        Assert.True(model.SendPreviewedFollow(offer,contact.FleetId,contact.LastSeenTurn,ReviewUiModel.NewCommandId()));
        var order=Assert.Single(model.Current.Frame.Travel!.OwnOrders);
        Assert.Equal(CampaignTravelIntent.Follow,order.Intent);
        Assert.Equal(contact.FleetId,order.ContactFleetId);
        Assert.Equal(contact.LastSeenTurn,order.ExpectedContactSeenTurn);
        Assert.Equal(offer.Route.Steps,order.RouteNodeIds);
        var accepted=model.Current.Frame;
        Assert.False(model.SendPreviewedFollow(offer,contact.FleetId,contact.LastSeenTurn,ReviewUiModel.NewCommandId()));
        Assert.Same(accepted,model.Current.Frame);
    }

    [Theory]
    [InlineData("wrong-seen")]
    [InlineData("unknown-contact")]
    [InlineData("wrong-destination")]
    [InlineData("dirty")]
    [InlineData("replacement-session")]
    public void ObsoleteOrUnavailablePreviewCannotSubmitOrDiscardLocalEdits(string problem)
    {
        using var fixture=new UnityReviewFixture();
        var model=new ReviewUiModel(fixture.Package,fixture.Codec,fixture.Store,A);
        var contact=Assert.Single(model.Current.Frame.Travel!.KnownContacts);
        var offer=model.Current.FindKnownRoute(FA,problem=="wrong-destination"?"nav.bajoran-wormhole":contact.LastSeenNodeId)!;
        if(problem=="dirty")model.LocalDesignDirty=true;
        if(problem=="replacement-session")
        {Assert.True(model.Save("map-before"));Assert.True(model.Load("map-before",true));}
        var frame=model.Current.Frame;var dirty=model.HasLocalEdits;
        Assert.False(model.SendPreviewedFollow(offer,problem=="unknown-contact"?"fleet.unknown":contact.FleetId,
            problem=="wrong-seen"?contact.LastSeenTurn+1:contact.LastSeenTurn,ReviewUiModel.NewCommandId()));
        Assert.Same(frame,model.Current.Frame);Assert.Equal(dirty,model.HasLocalEdits);
    }
}
