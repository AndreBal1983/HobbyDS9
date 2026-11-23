using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class GeneratedCampaignGeometryTests
{
    [Theory] [InlineData(700,500)] [InlineData(1200,700)] [InlineData(2400,1000)]
    public void FitKnownFramesRealWorldBoundsAndKeepsExactInverseAfterZoomPan(int width,int height)
    {
        var viewport=new GeneratedGalaxyViewport{MaximumZoom=64};viewport.SetMap(2500000,2000000);viewport.Resize(width,height);
        viewport.FitWorldBounds(1200000,800000,1450000,1030000);
        Assert.InRange(viewport.X(1200000),29,width-29);Assert.InRange(viewport.X(1450000),29,width-29);
        Assert.InRange(viewport.Y(800000),29,height-29);Assert.InRange(viewport.Y(1030000),29,height-29);
        Assert.True(viewport.Zoom>1);
        var x=viewport.X(1327000);var y=viewport.Y(927000);viewport.ZoomAt(1.5,x,y);
        Assert.Equal(x,viewport.X(1327000),7);Assert.Equal(y,viewport.Y(927000),7);
        viewport.Pan(80,-40);Assert.Equal(1327000,viewport.WorldX(viewport.X(1327000)),6);
        Assert.Equal(927000,viewport.WorldY(viewport.Y(927000)),6);
        viewport.Fit();Assert.Equal(1,viewport.Zoom);
    }
    [Fact] public void CosmeticProjectionHasOnlyReceivedKnownFeaturesAndNoGenerationInputsOrHomeCandidates()
    {
        var region=new GeneratedGalaxyRegionDto{NodeId="nav.known",DisplayName="Known region",SystemId="system.known",Center=new(){X=10,Y=10},
            Boundary=new(){new(){X=0,Y=0},new(){X=20,Y=0},new(){X=20,Y=20},new(){X=0,Y=20}}};
        var star=new EnvironmentGalaxyFeatureDto{FeatureId="feature.known",Kind=GalaxyEnvironmentFeatureKind.Star,NodeId=region.NodeId,
            SystemId=region.SystemId,DisplayName="Known system",Position=new(){X=12,Y=11}};
        var observation=new CampaignGeneratedMapDto{ActorId="civ.human",MapContentHash=new string('a',64),Width=100,Height=100,
            ObservationTurn=4,ObservedNodeIds=new(){region.NodeId},Regions=new(){region},Connections=new(),Features=new(){star}};
        var presentation=new GeneratedGalaxyMapData(observation);
        Assert.Null(presentation.Natural);Assert.Null(presentation.Scaled);Assert.Null(presentation.Environment);
        Assert.Empty(presentation.HomeCandidates);Assert.Empty(presentation.Settings.HomeCivilizationDefinitionIds);
        Assert.Equal("0",presentation.Settings.Seed);Assert.Equal(0,presentation.FormatVersion); // Not a serializable Natural document.
        Assert.Single(presentation.Regions);Assert.Single(presentation.Features);Assert.Single(presentation.Systems);
        Assert.Equal(12,presentation.SystemPosition(region.SystemId).X);Assert.Equal(11,presentation.SystemPosition(region.SystemId).Y);
        Assert.Equal(observation.MapContentHash,presentation.ContentHash);
    }
}
