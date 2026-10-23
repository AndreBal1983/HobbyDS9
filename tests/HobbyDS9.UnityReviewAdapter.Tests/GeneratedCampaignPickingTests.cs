using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class GeneratedCampaignPickingTests
{
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
