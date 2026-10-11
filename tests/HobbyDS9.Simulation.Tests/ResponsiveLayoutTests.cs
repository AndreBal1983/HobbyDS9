using HobbyDS9.Application;
using HobbyDS9.UnityClient;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;

namespace HobbyDS9.Simulation.Tests;
public sealed class ResponsiveLayoutTests
{
    [Theory]
    [InlineData(1280,720,LayoutMode.Compact)]
    [InlineData(1600,900,LayoutMode.Standard)]
    [InlineData(1920,1080,LayoutMode.Standard)]
    [InlineData(2560,1440,LayoutMode.Standard)]
    [InlineData(3440,1440,LayoutMode.Wide)]
    [InlineData(5120,1440,LayoutMode.Wide)]
    public void AcceptanceViewportHasReadableExpectedMode(double width, double height, LayoutMode expected)
    {
        var plan = ResponsiveLayoutPolicy.ForViewport(width,height); Assert.Equal(expected,plan.Mode);
        var scale = height / LcarsMetrics.ReferenceHeight * ResponsiveLayoutPolicy.PanelScaleMultiplier(height);
        Assert.True(scale >= LcarsMetrics.MinimumReadableScale);
        if (height >= 972) Assert.Equal(1,ResponsiveLayoutPolicy.PanelScaleMultiplier(height));
    }
    [Fact]
    public void SameAspectResizeChangesReadabilityFloorButNormalDisplaysRemainHeightMatched()
    {
        Assert.Equal(1.35,ResponsiveLayoutPolicy.PanelScaleMultiplier(720),10);
        Assert.Equal(1.08,ResponsiveLayoutPolicy.PanelScaleMultiplier(900),10);
        Assert.Equal(1,ResponsiveLayoutPolicy.PanelScaleMultiplier(1080));Assert.Equal(1,ResponsiveLayoutPolicy.PanelScaleMultiplier(1440));
        Assert.Equal(LayoutMode.Compact,ResponsiveLayoutPolicy.ForViewport(1280,720).Mode);
        Assert.Equal(LayoutMode.Standard,ResponsiveLayoutPolicy.ForViewport(1920,1080).Mode);
    }
    [Theory]
    [InlineData(1699.99,LayoutMode.Compact)] [InlineData(1700,LayoutMode.Standard)]
    [InlineData(2299.99,LayoutMode.Standard)] [InlineData(2300,LayoutMode.Wide)]
    public void LogicalBreakpointsAreExactAndRepeatable(double width,LayoutMode expected)
    { Assert.Equal(expected,ResponsiveLayoutPolicy.ForLogicalSize(width,1080).Mode); Assert.Equal(expected,ResponsiveLayoutPolicy.ForLogicalSize(width,1080).Mode); }
    [Fact]
    public void WideHardwareGainsMapSpaceWithIdenticalFurnitureScale()
    {
        var standard=ResponsiveLayoutPolicy.ForViewport(2560,1440);var wide=ResponsiveLayoutPolicy.ForViewport(5120,1440);
        Assert.Equal(standard.RailWidth,wide.RailWidth);Assert.Equal(standard.DetailsWidth,wide.DetailsWidth);
        var map=2300-wide.RailWidth-wide.DetailsWidth-LcarsMetrics.Production-80;
        Assert.True(map>=LcarsMetrics.MinimumMap);
        Assert.True(wide.ProductionAutoDock); Assert.False(standard.ProductionAutoDock);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExplicitProductionChoiceSurvivesEveryResize(bool open)
    {
        var visibility=new ProductionVisibility();visibility.Choose(open);
        foreach(var width in new[]{1400,1920,2580,3840,1200,2580})
            Assert.Equal(open,visibility.IsOpen(ResponsiveLayoutPolicy.ForLogicalSize(width,1080)));
        Assert.Equal(open,visibility.UserChoice);
    }
    [Fact]
    public void ProductionDefaultsAndToggleAreIndependentFromSectionSelection()
    {
        var visibility=new ProductionVisibility();var standard=ResponsiveLayoutPolicy.ForLogicalSize(1920,1080);var wide=ResponsiveLayoutPolicy.ForLogicalSize(2580,1080);
        Assert.False(visibility.IsOpen(standard));Assert.True(visibility.IsOpen(wide));Assert.Null(visibility.UserChoice);
        visibility.Toggle(wide);Assert.False(visibility.IsOpen(standard));Assert.False(visibility.IsOpen(wide));
        visibility.Toggle(standard);Assert.True(visibility.IsOpen(wide));
    }
    [Theory]
    [InlineData(0,64)] [InlineData(20,20)] [InlineData(104,64)] [InlineData(180,90)]
    public void ElbowGeometryClampsThicknessAndCurvesToCurrentContentRect(double width,double height)
    {
        var elbow=new ElbowMetrics(width,height);
        Assert.InRange(elbow.Thickness,0,Math.Min(width,height)*0.4);
        Assert.InRange(elbow.RadiusX,0,width);Assert.InRange(elbow.RadiusY,0,height);
        if(width>0){Assert.True(elbow.CanDraw);Assert.True(elbow.RadiusX>elbow.Thickness);Assert.True(elbow.RadiusY>elbow.Thickness);}else Assert.False(elbow.CanDraw);
    }
    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void UnusableDimensionsFailExplicitlyRatherThanOscillating(double width)
    { Assert.Throws<ArgumentOutOfRangeException>(()=>ResponsiveLayoutPolicy.ForLogicalSize(width,1080));Assert.Throws<ArgumentOutOfRangeException>(()=>ResponsiveLayoutPolicy.PanelScaleMultiplier(width)); }
    [Theory] [InlineData(1099,true)] [InlineData(1100,false)]
    public void ExtremelyNarrowContextDrawerBoundaryIsCentralized(double width,bool drawer)
    { Assert.Equal(drawer,ResponsiveLayoutPolicy.ForLogicalSize(width,1080).ContextDrawer); }
    [Fact]
    public void ResizingAndProductionOverridesHaveNoAuthoritativeSaveEffects()
    {
        var app=CampaignApplication.StartScenario(VerticalSliceFixture.Scenario(false,true));var before=Json(app.ExportSave());var visibility=new ProductionVisibility();
        foreach(var width in new[]{1422,1920,2580,3840,1400,2580})
        {var plan=ResponsiveLayoutPolicy.ForLogicalSize(width,1080);visibility.Toggle(plan);_ = visibility.IsOpen(plan);}
        Assert.Equal(before,Json(app.ExportSave()));
    }
}
