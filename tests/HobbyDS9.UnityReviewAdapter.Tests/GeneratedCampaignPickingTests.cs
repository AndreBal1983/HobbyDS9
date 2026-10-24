using HobbyDS9.UnityReview;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class GeneratedCampaignPickingTests
{
    [Fact] public void WholeTilePickingUsesOnlyKnownPolygonsAndStableBoundaryTies()
    {
        GeneratedGalaxyRegionDto Tile(string id,int x)=>new(){NodeId=id,Boundary=new(){new(){X=x,Y=0},new(){X=x+1000,Y=0},new(){X=x+1000,Y=1000},new(){X=x,Y=1000}}};
        var a=Tile("nav.a",0);var b=Tile("nav.b",1000);var known=new[]{b,a};
        Assert.Equal("nav.a",GeneratedCampaignNodePicker.ResolveRegion(known,50,900));
        Assert.Equal("nav.b",GeneratedCampaignNodePicker.ResolveRegion(known,1950,900));
        Assert.Equal("nav.a",GeneratedCampaignNodePicker.ResolveRegion(known,1000,500));
        Assert.Equal("nav.a",GeneratedCampaignNodePicker.ResolveRegion(known.Reverse(),1000,500));
        Assert.Null(GeneratedCampaignNodePicker.ResolveRegion(known,2050,500));
        Assert.Null(GeneratedCampaignNodePicker.ResolveRegion(known,double.NaN,500));
        Assert.Null(GeneratedCampaignNodePicker.ResolveRegion(new[]{a},1500,500));
        var view=new GeneratedGalaxyViewport();view.SetMap(2000,1000);view.Resize(840,480);view.ZoomAt(2,420,240);view.Pan(83,-31);
        Assert.Equal("nav.b",GeneratedCampaignNodePicker.ResolveRegion(known,view.WorldX(view.X(1750)),view.WorldY(view.Y(750))));
    }
    [Theory]
    [InlineData(1,1)]
    [InlineData(3,5)]
    public void ContentLocalAndCssBorderOriginsAgreeWithoutMovingPaintedAnchor(double border,double padding)
    {
        var viewport=new GeneratedGalaxyViewport();viewport.SetMap(5000000,4000000);viewport.Resize(900,510);
        double content=(float)viewport.X(1000000),origin=border+padding;
        var contentAnchors=new[]{new GeneratedCampaignNodeAnchor("a",content,100),new GeneratedCampaignNodeAnchor("b",content+1.5,100)};
        var localAnchors=contentAnchors.Select(a=>new GeneratedCampaignNodeAnchor(a.NodeId,a.X+origin,a.Y+origin)).ToArray();
        var localX=content+origin;var localY=100+origin;
        Assert.Equal("a",GeneratedCampaignNodePicker.Resolve(localAnchors,localX,localY));
        Assert.NotEqual("a",GeneratedCampaignNodePicker.Resolve(contentAnchors,localX,localY)); // The omitted-origin regression.
        double cssLeft=localX-border-19;
        Assert.Equal(localX,border+cssLeft+19); // Absolute child starts at border, not local origin.
        Assert.Equal(content,localX-origin); // Wheel and painter content coordinates invert that origin.
    }
    [Fact] public void CrowdedHitBoxesResolveNearestActualPointWithOrdinalTiesAndNoBlankSelection()
    {
        var known=new[]{new GeneratedCampaignNodeAnchor("nav.b",110,100),new GeneratedCampaignNodeAnchor("nav.a",100,100)};
        Assert.Equal("nav.a",GeneratedCampaignNodePicker.Resolve(known,100,100));
        Assert.Equal("nav.a",GeneratedCampaignNodePicker.Resolve(known,105,100));
        Assert.Equal("nav.a",GeneratedCampaignNodePicker.Resolve(known.Reverse(),105,100));
        Assert.Equal("nav.b",GeneratedCampaignNodePicker.Resolve(known,109.75,100));
        Assert.Null(GeneratedCampaignNodePicker.Resolve(known,130,100));
        Assert.Null(GeneratedCampaignNodePicker.Resolve(known,double.NaN,100));
        var hidden=new GeneratedCampaignNodeAnchor("nav.hidden",105,100);
        Assert.Equal("nav.a",GeneratedCampaignNodePicker.Resolve(known,hidden.X,hidden.Y)); // Hidden is not an admitted candidate.
    }
    [Fact] public void ThousandProjectedKnownAnchorsAtCompactSizeMatchIndependentActualPointerOracle()
    {
        var viewport=new GeneratedGalaxyViewport{MaximumZoom=64};viewport.SetMap(5000000,4000000);viewport.Resize(900,510);
        var known=Enumerable.Range(0,1000).Select(i=>new GeneratedCampaignNodeAnchor("nav."+i.ToString("D4"),
            (float)viewport.X(100000+(i%40)*120000),(float)viewport.Y(100000+(i/40)*145000))).ToArray();
        Assert.Contains(Enumerable.Range(0,known.Length-1),i=>Math.Abs(known[i].X-known[i+1].X)<38);
        foreach(var index in new[]{0,17,213,511,998})
        {
            // Browser/native events can quantize requested coordinates: actual received x/y are authoritative.
            var x=Math.Floor(known[index].X);var y=Math.Floor(known[index].Y);
            var expected=known.Where(a=>Math.Abs(a.X-x)<=19&&Math.Abs(a.Y-y)<=19)
                .OrderBy(a=>(a.X-x)*(a.X-x)+(a.Y-y)*(a.Y-y)).ThenBy(a=>a.NodeId,StringComparer.Ordinal).First().NodeId;
            Assert.Equal(expected,GeneratedCampaignNodePicker.Resolve(known.Reverse(),x,y));
        }
        Assert.Null(GeneratedCampaignNodePicker.Resolve(Array.Empty<GeneratedCampaignNodeAnchor>(),450,255));
    }
}
