using System.Globalization;
using System.Numerics;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ScaledGalaxyTests
{
    static List<CivilizationDefinitionDto> Catalog() => new() { new() {DefinitionId="civilization.federation"}, new() {DefinitionId="civilization.cardassian"} };
    static ScaledGalaxySettingsDto Settings(string seed="42",int count=250) => new() {Seed=seed,SystemCount=count,EmptyRegionCount=count,
        Width=count==1000?5000000:2500000,Height=count==1000?4000000:2000000,HomeCivilizationDefinitionIds=Catalog().Select(c=>c.DefinitionId).ToList()};
    static string Json<T>(T d) => JsonSerializer.Serialize(d);
    static readonly Lazy<ScaledGalaxyReview> HeldReview = new(()=>ScaledGalaxyApplication.Generate(Settings(),Catalog()));
    static readonly Lazy<ScaledGalaxyDocumentDto> Baseline = new(()=>HeldReview.Value.Export());
    static ScaledGalaxyDocumentDto Clone() => JsonSerializer.Deserialize<ScaledGalaxyDocumentDto>(Json(Baseline.Value))!;
    static GeneratedGalaxyDocumentDto Geometry(ScaledGalaxyDocumentDto d) => new() {Width=d.Layout.Width,Height=d.Layout.Height,
        Regions=d.Layout.Regions,Systems=d.Layout.Systems,Connections=d.Layout.Connections};

    [Fact]
    public void EightSeedReviewMapsHaveExactAnchorsGeometryCostsAndMeasuredQuietArea()
    {
        var ratios=new List<double>();
        for(int seed=0;seed<8;seed++) {
            var d=ScaledGalaxyApplication.Generate(Settings(seed.ToString(CultureInfo.InvariantCulture)),Catalog()).Export();
            GeneratedGalaxyTests.ValidateGeometryIndependently(Geometry(d));
            Assert.Equal(500,d.Layout.Regions.Count);Assert.Equal(250,d.Layout.Systems.Count);Assert.Equal(275,d.Features.Count);
            foreach(var f in d.Features.Where(f=>f.NodeId!=null)) {
                var region=d.Layout.Regions.Single(r=>r.NodeId==f.NodeId);
                Assert.Equal(Json(f.Position),Json(region.Center));Assert.Equal(f.SystemId,region.SystemId);
            }
            Assert.All(d.Layout.Connections,c=>Assert.Equal(1,c.BaseTraversalCost));
            foreach(var r in d.RegionInfluences) {
                var site=d.Layout.Regions.Single(n=>n.NodeId==r.NodeId).Center;
                foreach(var c in r.Contributions) {
                    var f=d.Features.Single(f=>f.FeatureId==c.FeatureId);
                    BigInteger dx=site.X-f.Position.X,dy=site.Y-f.Position.Y;
                    BigInteger q=dx*dx*1000000/(f.RadiusX*(long)f.RadiusX)+dy*dy*1000000/(f.RadiusY*(long)f.RadiusY);
                    int expected=BigInteger.Abs(dx)>=f.RadiusX||BigInteger.Abs(dy)>=f.RadiusY||q>=1000000?0:(int)(f.DensityWeight*(1000000-q)/1000000);
                    Assert.Equal(expected,c.Amount);
                }
                Assert.Equal(Math.Min(1000,100+r.Contributions.Sum(c=>c.Amount)),r.TargetDensity);
            }
            using var metrics=JsonDocument.Parse(Json(ScaledGalaxyDiagnostics.Measure(d)));
            var root=metrics.RootElement;
            Assert.Equal(250,root.GetProperty("QuietEmptyNodeIds").GetArrayLength()+root.GetProperty("DisturbedEmptyNodeIds").GetArrayLength());
            ratios.Add(root.GetProperty("QuietEmptySystemMeanAreaRatio").GetDouble());
            var quiet=root.GetProperty("QuietEmptyNodeIds").EnumerateArray().Select(n=>n.GetString()).ToHashSet();
            var componentNodes=root.GetProperty("QuietComponents").EnumerateArray().SelectMany(c=>c.GetProperty("NodeIds").EnumerateArray()).Select(n=>n.GetString()).ToArray();
            Assert.Equal(quiet.Count,componentNodes.Distinct().Count());Assert.True(quiet.SetEquals(componentNodes));
        }
        // Statistical review envelope, not an exact-per-cell or approved balance claim.
        Assert.InRange(ratios.Average(),1.7,2.5);Assert.All(ratios,r=>Assert.InRange(r,1.4,3.0));
    }

    [Theory]
    [InlineData("0")][InlineData("18446744073709551615")][InlineData("42")]
    public void RepeatReorderedStrictRoundtripAndDetachedStateAreExact(string seed)
    {
        var s=Settings(seed);var catalog=Catalog();var review=ScaledGalaxyApplication.Generate(s,catalog);string expected=Json(review.Export());
        s.HomeCivilizationDefinitionIds.Reverse();catalog.Reverse();Assert.Equal(expected,Json(ScaledGalaxyApplication.Generate(s,catalog).Export()));
        var d=review.Export();d.Layout.Regions.Reverse();d.Layout.Systems.Reverse();d.Layout.Connections.Reverse();d.Layout.HomeCandidates.Reverse();
        d.Features.Reverse();d.RegionInfluences.Reverse();d.Settings.HomeCivilizationDefinitionIds.Reverse();foreach(var r in d.RegionInfluences)r.Contributions.Reverse();
        var opened=ScaledGalaxyJson.Read(Json(d),catalog);Assert.Equal(expected,Json(opened.Export()));
        var homes=d.Layout.HomeCandidates;Assert.Equal(Json(review.FindRoute(homes[0].NodeId,homes[1].NodeId)),Json(opened.FindRoute(homes[0].NodeId,homes[1].NodeId)));
        d.Layout.Regions[0].Boundary[0].X++;d.Features[0].Position.X++;d.Tuning.Environment.BlackHoleCount++;d.Settings.HomeCivilizationDefinitionIds.Clear();
        Assert.Equal(expected,Json(review.Export()));Assert.Equal(expected,Json(opened.Export()));
    }

    [Theory]
    [InlineData("42")][InlineData("18446744073709551615")]
    public void ThousandSystemFullGenerateStrictOpenFitsBudgetsAndPreservesExactGeometry(string seed)
    {
        var review=ScaledGalaxyApplication.Generate(Settings(seed,1000),Catalog());var d=review.Export();
        Assert.Equal(2000,d.Layout.Regions.Count);Assert.Equal(1100,d.Features.Count);Assert.Equal(40,d.Tuning.Environment.BlackHoleCount);Assert.Equal(60,d.Tuning.Environment.NebulaCount);
        var json=ScaledGalaxyJson.Write(review);Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(json),1,ScaledGalaxyIdentity.MaximumJsonBytes);
        var reordered=JsonSerializer.Deserialize<ScaledGalaxyDocumentDto>(json)!;
        reordered.Layout.Regions.Reverse();reordered.Features.Reverse();reordered.RegionInfluences.Reverse();reordered.Settings.HomeCivilizationDefinitionIds.Reverse();
        var reopened=ScaledGalaxyJson.Read(Json(reordered),Catalog().AsEnumerable().Reverse());Assert.Equal(Json(d),Json(reopened.Export()));
        if(seed=="42")Assert.Equal("b7a10c946954cfdf51a28e90bccda98ee8a1d4dcbd6e35c23c811fd94878a834",d.ContentHash);
        GeneratedGalaxyTests.ValidateGeometryIndependently(Geometry(d));
        var h=d.Layout.HomeCandidates;var route=review.FindRoute(h[0].NodeId,h[1].NodeId);
        Assert.True(route.Success);Assert.Equal((long)route.Steps.Count,route.TotalCost);Assert.True(route.Steps.Count>=5);
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)]
    public void FamilyOffComparisonsRetainEveryFeatureInput(int family)
    {
        var s=Settings();if(family==0)s.StellarInfluenceEnabled=false;else if(family==1)s.BlackHoleInfluenceEnabled=false;else s.NebulaInfluenceEnabled=false;
        var off=ScaledGalaxyApplication.Generate(s,Catalog()).Export();Assert.Equal(Json(Baseline.Value.Features),Json(off.Features));Assert.Equal(Json(Baseline.Value.Tuning),Json(off.Tuning));
        Assert.NotEqual(Baseline.Value.ContentHash,off.ContentHash);
        var kind=(GalaxyEnvironmentFeatureKind)(family+1);var ids=off.Features.Where(f=>f.Kind==kind).Select(f=>f.FeatureId).ToHashSet();
        Assert.All(off.RegionInfluences,r=>Assert.DoesNotContain(r.Contributions,c=>ids.Contains(c.FeatureId)));
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)][InlineData(11)]
    public void TamperingForgedRehashAndMalformedRowsRejectWithoutReplacingReview(int mutation)
    {
        var d=Clone();string original=Json(Baseline.Value);
        switch(mutation) {
            case 0:d.Features[0].Position.X++;break;
            case 1:d.Layout.Connections[0].BaseTraversalCost++;break;
            case 2:d.Settings.EmptyRegionCount++;break;
            case 3:d.Tuning.ClusterRadius++;break;
            case 4:d.RegionInfluences[0].TargetDensity++;break;
            case 5:d.FormatVersion=2;break;
            case 6:d.Layout.Regions[0].Boundary=Enumerable.Repeat(d.Layout.Regions[0].Boundary[0],65).ToList();break;
            case 7:d.Features[0].Position=null!;break;
            case 8:d.Settings.Seed="01";break;
            case 9:d.Layout.Regions[0].Center.X=5000001;break;
            case 10:d.Settings.QuietEmptyAreaTargetPermille=2500;break;
            case 11:d.Layout.Systems[0].NodeId="nav.foreign";break;
        }
        if(mutation<=4) {
            var hash=typeof(ScaledGalaxyReviewSession).Assembly.GetType("HobbyDS9.Simulation.ScaledGalaxyData")!.GetMethod("Hash",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
            d.ContentHash=(string)hash.Invoke(null,new object[]{d})!;
        }
        Assert.Throws<ArgumentException>(()=>ScaledGalaxyApplication.Open(d,Catalog()));Assert.Equal(original,Json(HeldReview.Value.Export()));
    }

    [Fact]
    public void StrictCodecRejectsDuplicateMissingUnknownOversizedAndCrossFormatInputs()
    {
        string json=Json(Baseline.Value);
        foreach(string bad in new[]{json.Replace("\"Seed\":\"42\"","\"Seed\":\"42\",\"Seed\":\"42\""),json.Replace("\"Seed\":\"42\",",""),json.Replace("\"Seed\":\"42\"","\"Extra\":0,\"Seed\":\"42\""),"null",new string(' ',ScaledGalaxyIdentity.MaximumJsonBytes+1)})
            Assert.Throws<ArgumentException>(()=>ScaledGalaxyJson.Read(bad,Catalog()));
        Assert.Throws<ArgumentException>(()=>EnvironmentGalaxyJson.Read(json,Catalog()));Assert.Throws<ArgumentException>(()=>GeneratedGalaxyJson.Read(json,Catalog()));
        var old=EnvironmentGalaxyApplication.Generate(new(){Map=new(){Seed="42",SystemCount=40,HomeCivilizationDefinitionIds=Catalog().Select(c=>c.DefinitionId).ToList()},StellarInfluenceEnabled=true,BlackHoleInfluenceEnabled=true,NebulaInfluenceEnabled=true},Catalog());
        Assert.Equal("404d1e15057f42531a6f1da078efd78663d902dcff22e5484e6e3ba2cc8d68fe",old.Export().ContentHash);
        Assert.Throws<ArgumentException>(()=>ScaledGalaxyJson.Read(EnvironmentGalaxyJson.Write(old),Catalog()));
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public void ImpossibleOrUnboundedSettingsRejectBeforeGeneration(int mutation)
    {
        var s=Settings();switch(mutation){case 0:s.SystemCount=1001;break;case 1:s.EmptyRegionCount=2000;break;case 2:s.Width=5000001;break;case 3:s.Height=500000;break;case 4:s.BlackHolesPerBaselineArea=5;break;case 5:s.NebulaePerBaselineArea=-1;break;case 6:s.HomeCivilizationDefinitionIds[0]="civilization.foreign";break;case 7:s.Seed="18446744073709551616";break;}
        Assert.Throws<ArgumentException>(()=>ScaledGalaxyApplication.Generate(s,Catalog()));
    }

    [Fact]
    public void NewVectorAndExplicitRouteErrorsAreStable()
    {
        Assert.Equal("006ab67d08f1cd3c3d9e73da2fa335fb5e4ba1cee1acd824d63dbd44135c1937",Baseline.Value.ContentHash);
        var review=ScaledGalaxyApplication.Open(Clone(),Catalog());string node=Baseline.Value.Layout.Regions[0].NodeId;
        Assert.Equal(0,review.FindRoute(node,node).TotalCost);Assert.Equal(RouteErrorCode.InvalidSystemId,review.FindRoute("",node).ErrorCode);
        Assert.Equal(RouteErrorCode.UnknownSystem,review.FindRoute("nav.foreign",node).ErrorCode);
    }

    [Theory]
    [InlineData(1000)][InlineData(2000)][InlineData(3000)]
    public void BoundedNonDefaultCountsExtentsAndFeatureDensitiesRoundtrip(int target)
    {
        var s=Settings("0");s.SystemCount=30;s.EmptyRegionCount=15;s.Width=1000000;s.Height=600000;
        s.BlackHolesPerBaselineArea=4;s.NebulaePerBaselineArea=6;s.QuietEmptyAreaTargetPermille=target;
        var review=ScaledGalaxyApplication.Generate(s,Catalog());var d=review.Export();
        Assert.Equal(45,d.Layout.Regions.Count);Assert.Equal(2,d.Tuning.Environment.BlackHoleCount);Assert.Equal(4,d.Tuning.Environment.NebulaCount);
        Assert.Equal(1+target/1000,d.Tuning.StarClusterSize);GeneratedGalaxyTests.ValidateGeometryIndependently(Geometry(d));
        Assert.Equal(Json(d),Json(ScaledGalaxyJson.Read(ScaledGalaxyJson.Write(review),Catalog()).Export()));
        s.BlackHolesPerBaselineArea=0;s.NebulaePerBaselineArea=0;s.StellarInfluenceEnabled=false;
        var empty=ScaledGalaxyApplication.Generate(s,Catalog()).Export();Assert.Equal(30,empty.Features.Count);
        Assert.All(empty.RegionInfluences,r=>{Assert.Empty(r.Contributions);Assert.Equal(100,r.TargetDensity);});
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public void AggregateVertexAndContributionBudgetsRejectBeforeRegeneration(bool contributions)
    {
        var d=Clone();
        if(contributions)foreach(var r in d.RegionInfluences)r.Contributions=Enumerable.Repeat(new EnvironmentGalaxyContributionDto{FeatureId=d.Features[0].FeatureId,Amount=1},49).ToList();
        else foreach(var r in d.Layout.Regions)r.Boundary=Enumerable.Repeat(r.Boundary[0],41).ToList();
        Assert.Throws<ArgumentException>(()=>ScaledGalaxyApplication.Open(d,Catalog()));
        Assert.Throws<ArgumentException>(()=>ScaledGalaxyJson.Read(Json(d),Catalog()));
    }

    [Fact]
    public void ControlledFamiliesIncreaseAggregateLocalExtraSiteCountsWithFixedFeatures()
    {
        var totals=new Dictionary<GalaxyEnvironmentFeatureKind,(int On,int Off)>();
        for(int seed=0;seed<4;seed++) {
            var s=Settings(seed.ToString(CultureInfo.InvariantCulture));var on=ScaledGalaxyApplication.Generate(s,Catalog()).Export();
            foreach(var kind in new[]{GalaxyEnvironmentFeatureKind.Star,GalaxyEnvironmentFeatureKind.BlackHole,GalaxyEnvironmentFeatureKind.Nebula}) {
                s.StellarInfluenceEnabled=kind!=GalaxyEnvironmentFeatureKind.Star;s.BlackHoleInfluenceEnabled=kind!=GalaxyEnvironmentFeatureKind.BlackHole;s.NebulaInfluenceEnabled=kind!=GalaxyEnvironmentFeatureKind.Nebula;
                var off=ScaledGalaxyApplication.Generate(s,Catalog()).Export();Assert.Equal(Json(on.Features),Json(off.Features));
                var anchors=on.Features.Where(f=>f.NodeId!=null).Select(f=>f.NodeId).ToHashSet();var family=on.Features.Where(f=>f.Kind==kind).ToArray();
                bool Core(GeneratedGalaxyPointDto p,EnvironmentGalaxyFeatureDto f) {
                    long dx=p.X-f.Position.X,dy=p.Y-f.Position.Y;
                    if(Math.Abs(dx)>=f.RadiusX||Math.Abs(dy)>=f.RadiusY)return false;
                    long q=dx*dx*1000000/((long)f.RadiusX*f.RadiusX)+dy*dy*1000000/((long)f.RadiusY*f.RadiusY);
                    return q<1000000&&f.DensityWeight*(1000000-q)/1000000*2>=f.DensityWeight;
                }
                int Count(ScaledGalaxyDocumentDto d)=>d.Layout.Regions.Count(r=>!anchors.Contains(r.NodeId)&&family.Any(f=>Core(r.Center,f)));
                var prior=totals.GetValueOrDefault(kind);totals[kind]=(prior.On+Count(on),prior.Off+Count(off));
            }
        }
        Assert.All(totals,t=>Assert.True(t.Value.On>t.Value.Off,$"{t.Key}: {t.Value}"));
    }
}
