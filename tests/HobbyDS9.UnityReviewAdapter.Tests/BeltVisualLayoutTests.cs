using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class BeltVisualLayoutTests
{
    static SystemAsteroidBeltDto Belt(string system="system.a",string id="belt.a")=>new(){SystemId=system,FeatureId=id,InnerRadiusMilliAu=2200,OuterRadiusMilliAu=2700,VisualSeed=new string('a',64)};
    static CampaignStaticAstronomySnapshot Sky(params SystemAsteroidBeltDto[] belts)=>new("human","map",3,7,Array.Empty<CampaignNebulaVisualProxy>(),belts);
    [Fact]public void OnlyMatchingDisclosedSystemAndStampedEditionProduceVisualSlots()
    {
        var sky=Sky(Belt("system.b","belt.b"),Belt());var slot=Assert.Single(BeltVisualLayout.ForSystem(sky,"human",3,7,"system.a"));
        Assert.Equal("belt.a",slot.Feature.FeatureId);Assert.Equal(20.8,slot.Inner,8);Assert.Equal(22.8,slot.Outer,8);Assert.True(slot.Envelope>slot.Outer);
        Assert.Empty(BeltVisualLayout.ForSystem(sky,"opponent",3,7,"system.a"));Assert.Empty(BeltVisualLayout.ForSystem(sky,"human",4,7,"system.a"));Assert.Empty(BeltVisualLayout.ForSystem(sky,"human",3,8,"system.a"));Assert.Empty(BeltVisualLayout.ForSystem(sky,"human",3,7,"system.hidden"));
        Assert.Empty(BeltVisualLayout.ForSystem(new("human","map",3,7,Array.Empty<CampaignNebulaVisualProxy>()),"human",3,7,"system.a"));
        Assert.Throws<ArgumentException>(()=>BeltVisualLayout.ForSystem(Sky(Enumerable.Range(0,9).Select(i=>Belt(id:"belt."+i)).ToArray()),"human",3,7,"system.a"));
    }
    [Fact]public void ArtWitnessAndPersistedSeedHaveStablePrefixScatterAndBoundedEnvelopes()
    {
        Assert.Equal(342353800u,BeltVisualLayout.Seed("system.example","feature.belt.example"));
        var witness=Assert.Single(BeltVisualLayout.Rocks(342353800u,1));Assert.Equal(.05731369256973266,witness.Turns,12);Assert.Equal(.5946665406227112,witness.Radial,12);Assert.Equal(3,witness.Prototype);Assert.Equal(.007212636377556606,witness.Radius,12);
        var seed=BeltVisualLayout.Seed("system.a","belt.a",new string('a',64));var full=BeltVisualLayout.Rocks(seed,320);var prefix=BeltVisualLayout.Rocks(seed,120);
        Assert.Equal(full.Take(120).Select(p=>p.Turns),prefix.Select(p=>p.Turns));Assert.NotEqual(seed,BeltVisualLayout.Seed("system.a","belt.a",new string('b',64)));
        Assert.All(full,p=>{Assert.InRange(p.Turns,0,1);Assert.InRange(p.Radial,0,1);Assert.InRange(p.Height,-.006,.006);Assert.InRange(p.Radius,.006,.016);Assert.InRange(p.Prototype,0,3);Assert.InRange(p.Sx,.8,1.3);});
        Assert.Throws<ArgumentOutOfRangeException>(()=>BeltVisualLayout.Rocks(seed,385));Assert.Throws<ArgumentException>(()=>BeltVisualLayout.Seed("system.a\nother","belt.a"));
    }
    [Fact]public void WorkBudgetsAndHysteresisStayBoundedWithoutRerolls()
    {
        Assert.Equal(320,BeltVisualLayout.RockBudget(0,1,false));
        for(var n=2;n<=8;n++){Assert.Equal(384,Enumerable.Range(0,n).Sum(i=>BeltVisualLayout.RockBudget(i,n,false)));Assert.Equal(120,Enumerable.Range(0,n).Sum(i=>BeltVisualLayout.RockBudget(i,n,true)));}
        Assert.Equal(0,BeltVisualLayout.Detail(230,-1));Assert.Equal(0,BeltVisualLayout.Detail(200,0));Assert.Equal(1,BeltVisualLayout.Detail(180,0));Assert.Equal(1,BeltVisualLayout.Detail(240,1));Assert.Equal(2,BeltVisualLayout.Detail(70,1));Assert.Equal(2,BeltVisualLayout.Detail(100,2));Assert.Equal(1,BeltVisualLayout.Detail(110,2));
    }
}
