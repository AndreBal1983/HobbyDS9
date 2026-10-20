using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class MapOrderPreviewTests
{
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
