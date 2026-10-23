using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class FleetDefendBindingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefendUsesOwnedStationaryCurrentBoundaryAndExistingSharedCommand(bool generated)
    {
        var codec=new ReviewNewtonsoftCodec();
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        // No save is written by this capability/command test.
        var unused=Path.Combine(Path.GetTempPath(),"hobby-defend-unused-"+Guid.NewGuid().ToString("N"));
        var model=generated?new ReviewUiModel(template,new NaturalGalaxySettingsDto{Map=new(){Seed="42",SystemCount=250,EmptyRegionCount=250,
            Width=2500000,Height=2000000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}},codec,new ReviewGeneratedCampaignSlotStore(unused,codec))
            :new ReviewUiModel(template,codec,new ReviewPlayableFleetSlotStore(unused,codec));
        var original=model.Current.Frame;var fleet=original.OwnStrategicFleets.First().FleetId.Value;
        Assert.True(model.CanDefendFleet(original,fleet));
        Assert.False(model.CanDefendFleet(original,"fleet.not-owned"));
        model.OrganizationShips.Add(original.Strategy!.Ships.First().ShipId.Value);
        Assert.False(model.CanDefendFleet(original,fleet));model.ResetOrganization();
        Assert.True(model.DraftTravel(fleet,CampaignTravelIntent.StayEngage,null,null));
        Assert.True(model.SendTravel("command.defend.binding"));
        Assert.Equal(CampaignTravelIntent.StayEngage,model.Current.Frame.Travel!.OwnOrders.Single(o=>o.FleetId==fleet).Intent);
        Assert.False(model.CanDefendFleet(original,fleet)); // Old modal stamp.
        Assert.True(model.CanDefendFleet(model.Current.Frame,fleet)); // Does not require CanOrganize/Stay.
        var offer=model.Current.Frame.Strategy!.Navigation.Nodes.Select(n=>model.Current.FindKnownRoute(fleet,n.NodeId.Value))
            .Where(r=>r?.Route.Success==true&&r.Route.TotalCost>0).OrderBy(r=>r!.Route.TotalCost).First()!;
        Assert.True(model.SendPreviewedTravel(offer,"command.defend.travel"));
        Assert.False(model.CanDefendFleet(model.Current.Frame,fleet)); // Accepted route is not overwritten.
        Assert.False(Directory.Exists(unused));
    }
}
