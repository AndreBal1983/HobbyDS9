using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedCampaignLabelTests
{
    [Fact] public void PriorityThenOrdinalPlacementIsIndependentOfInputOrder()
    {
        var candidates=new[]{new GeneratedLabelCandidate("normal",40,40,30,12,0,8),new GeneratedLabelCandidate("selected",40,40,30,12,100,8)};
        var a=GeneratedCampaignLabelLayout.Place(candidates,Array.Empty<GeneratedLabelRect>(),100,80);
        var b=GeneratedCampaignLabelLayout.Place(candidates.Reverse(),Array.Empty<GeneratedLabelRect>(),100,80);
        Assert.Equal(48,a["selected"].X);Assert.Equal(2,a["normal"].X);
        Assert.False(a["selected"].Intersects(a["normal"]));
        foreach(var key in a.Keys){Assert.Equal(a[key].X,b[key].X);Assert.Equal(a[key].Y,b[key].Y);}
    }
    [Fact] public void GlyphObstacleAndViewportRequireAnotherSideOrHideLabel()
    {
        var candidate=new[]{new GeneratedLabelCandidate("known",40,40,30,12,70,8)};
        var glyphs=new[]{new GeneratedLabelRect(48,34,30,12)};
        var placed=GeneratedCampaignLabelLayout.Place(candidate,glyphs,100,80);
        Assert.Equal(2,placed["known"].X);Assert.False(placed["known"].Intersects(glyphs[0]));
        Assert.Empty(GeneratedCampaignLabelLayout.Place(candidate,new[]{new GeneratedLabelRect(0,0,100,80)},100,80));
        Assert.Empty(GeneratedCampaignLabelLayout.Place(new[]{new GeneratedLabelCandidate("too-wide",10,10,200,12,100)},glyphs,100,80));
    }
    [Fact] public void DenseKnownOnlyCandidatesStayBoundedClippedAndNonoverlapping()
    {
        var candidates=Enumerable.Range(0,2000).Select(i=>new GeneratedLabelCandidate("known-"+i.ToString("D4"),20+(i%40)*25,20+(i/40)*15,50,18,i==1999?100:0)).ToArray();
        var glyphs=candidates.Select(c=>new GeneratedLabelRect(c.X-4,c.Y-4,8,8)).ToArray();
        var placed=GeneratedCampaignLabelLayout.Place(candidates,glyphs,1100,850);
        Assert.InRange(placed.Count,1,64);
        foreach(var r in placed.Values){Assert.InRange(r.X,0,1100-r.Width);Assert.InRange(r.Y,0,850-r.Height);Assert.DoesNotContain(glyphs,g=>r.Intersects(g));}
        var values=placed.Values.ToArray();for(int i=0;i<values.Length;i++)for(int j=i+1;j<values.Length;j++)Assert.False(values[i].Intersects(values[j]));
        var reverse=GeneratedCampaignLabelLayout.Place(candidates.Reverse(),glyphs,1100,850);Assert.Equal(placed.Keys.Order(),reverse.Keys.Order());
    }
    [Fact] public void InvalidProjectionCannotProduceAVisibleLabel()
    {
        var candidates=new[]{new GeneratedLabelCandidate("nan",double.NaN,20,50,18,100),new GeneratedLabelCandidate("outside",-20,20,50,18,100)};
        Assert.Empty(GeneratedCampaignLabelLayout.Place(candidates,Array.Empty<GeneratedLabelRect>(),100,100));
        Assert.Empty(GeneratedCampaignLabelLayout.Place(new[]{new GeneratedLabelCandidate("valid",20,20,50,18,100)},Array.Empty<GeneratedLabelRect>(),double.NaN,100));
        Assert.Empty(GeneratedCampaignLabelLayout.Place(new[]{new GeneratedLabelCandidate("valid",20,20,50,18,100)},new GeneratedLabelRect[4097],100,100));
    }
}
