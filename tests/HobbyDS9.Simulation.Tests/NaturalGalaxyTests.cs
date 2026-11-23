using System.Globalization;
using System.Numerics;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class NaturalGalaxyTests
{
    static List<CivilizationDefinitionDto> Catalog()=>new(){new(){DefinitionId="civilization.federation"},new(){DefinitionId="civilization.cardassian"}};
    static NaturalGalaxySettingsDto Settings(string seed="42",int n=250)=>new(){Map=new(){Seed=seed,SystemCount=n,EmptyRegionCount=n,Width=n==1000?5000000:2500000,Height=n==1000?4000000:2000000,
        HomeCivilizationDefinitionIds=Catalog().Select(c=>c.DefinitionId).ToList()}};
    static string Json<T>(T d)=>JsonSerializer.Serialize(d);
    static readonly Lazy<NaturalGalaxyReview> Held=new(()=>NaturalGalaxyApplication.Generate(Settings(),Catalog()));
    static NaturalGalaxyDocumentDto Clone()=>JsonSerializer.Deserialize<NaturalGalaxyDocumentDto>(Json(Held.Value.Export()))!;
    static long Squared(GeneratedGalaxyPointDto a,GeneratedGalaxyPointDto b){long dx=a.X-b.X,dy=a.Y-b.Y;return dx*dx+dy*dy;}
    static void Geometry(NaturalGalaxyDocumentDto d)=>GeneratedGalaxyTests.ValidateGeometryIndependently(new(){Width=d.Layout.Width,Height=d.Layout.Height,Regions=d.Layout.Regions,Systems=d.Layout.Systems,Connections=d.Layout.Connections});
    static void GroupsAndAnchors(NaturalGalaxyDocumentDto d)
    {
        var stars=d.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star).ToArray();
        Assert.Equal(d.Settings.Map.SystemCount,stars.Length);Assert.Equal(stars.Select(f=>f.FeatureId).Order(),d.StarGroups.SelectMany(g=>g.StarFeatureIds).Order());
        Assert.Equal(stars.Length,d.StarGroups.SelectMany(g=>g.StarFeatureIds).Distinct().Count());
        int expectedIsolated=(stars.Length*d.Settings.IsolatedStarSharePermille+999)/1000;
        Assert.Equal(expectedIsolated,d.StarGroups.Count(g=>g.Kind==NaturalStarGroupKind.Isolated));
        foreach(var group in d.StarGroups){
            foreach(string id in group.StarFeatureIds){var star=stars.Single(f=>f.FeatureId==id);
                if(group.Kind==NaturalStarGroupKind.Isolated){Assert.Equal(Json(group.Center),Json(star.Position));Assert.All(stars.Where(f=>f.FeatureId!=id),other=>Assert.True(Squared(other.Position,star.Position)>=(long)d.Tuning.IsolatedStarSeparation*d.Tuning.IsolatedStarSeparation));}
                else {BigInteger dx=star.Position.X-group.Center.X,dy=star.Position.Y-group.Center.Y;
                    Assert.True(dx*dx*1000000/((long)group.RadiusX*group.RadiusX)+dy*dy*1000000/((long)group.RadiusY*group.RadiusY)<1000000);}
            }
        }
        foreach(var f in d.Features.Where(f=>f.NodeId!=null)){var row=d.Layout.Regions.Single(r=>r.NodeId==f.NodeId);Assert.Equal(Json(f.Position),Json(row.Center));Assert.Equal(f.SystemId,row.SystemId);}
        Assert.All(d.Layout.Connections,e=>Assert.Equal(1,e.BaseTraversalCost));
    }

    [Fact]
    public void EightSeedsHaveRealIsolatedStarsVariedAssociationsAndExactGeometry()
    {
        for(int seed=0;seed<8;seed++){
            var d=NaturalGalaxyApplication.Generate(Settings(seed.ToString(CultureInfo.InvariantCulture)),Catalog()).Export();
            GroupsAndAnchors(d);Geometry(d);Assert.Equal(500,d.Layout.Regions.Count);
            var groups=d.StarGroups.Where(g=>g.Kind==NaturalStarGroupKind.LooseAssociation).ToArray();
            Assert.True(groups.Select(g=>g.StarFeatureIds.Count).Distinct().Count()>=4);
            Assert.True(groups.Select(g=>(g.RadiusX,g.RadiusY)).Distinct().Count()>=4);
            using var stats=JsonDocument.Parse(Json(NaturalGalaxyDiagnostics.Measure(d)));
            Assert.True(stats.RootElement.GetProperty("ActualStarsWithNearestAtLeastIsolatedSeparation").GetInt32()>=25);
            Assert.True(stats.RootElement.GetProperty("NearestDistanceP90").GetDouble()>stats.RootElement.GetProperty("NearestDistanceP10").GetDouble()*2);
        }
    }

    [Theory]
    [InlineData("0")][InlineData("18446744073709551615")][InlineData("42")]
    public void FullReorderRepeatStrictOpenAndDetachedProvenanceAreExact(string seed)
    {
        var s=Settings(seed);var catalog=Catalog();var review=NaturalGalaxyApplication.Generate(s,catalog);var d=review.Export();string original=Json(d);
        s.Map.HomeCivilizationDefinitionIds.Reverse();catalog.Reverse();Assert.Equal(original,Json(NaturalGalaxyApplication.Generate(s,catalog).Export()));
        d.Features.Reverse();d.StarGroups.Reverse();foreach(var g in d.StarGroups)g.StarFeatureIds.Reverse();d.RegionInfluences.Reverse();foreach(var r in d.RegionInfluences)r.Contributions.Reverse();
        d.Layout.Regions.Reverse();d.Layout.Connections.Reverse();d.Layout.Systems.Reverse();d.Layout.HomeCandidates.Reverse();
        var reopened=NaturalGalaxyJson.Read(Json(d),catalog);Assert.Equal(original,Json(reopened.Export()));
        var h=d.Layout.HomeCandidates;Assert.Equal(Json(review.FindRoute(h[0].NodeId,h[1].NodeId)),Json(reopened.FindRoute(h[0].NodeId,h[1].NodeId)));
        d.StarGroups[0].Center.X++;d.StarGroups[0].StarFeatureIds.Clear();d.Settings.Map.HomeCivilizationDefinitionIds.Clear();d.Features[0].Position.Y++;d.Tuning.Environment.BaseDensity++;
        Assert.Equal(original,Json(review.Export()));Assert.Equal(original,Json(reopened.Export()));
    }

    [Theory]
    [InlineData(1000)][InlineData(3000)]
    public void AreaTargetChangesPressureButCannotChangeStarGroupArityOrPositions(int target)
    {
        var baseline=Held.Value.Export();var s=Settings();s.Map.QuietEmptyAreaTargetPermille=target;var d=NaturalGalaxyApplication.Generate(s,Catalog()).Export();
        Assert.Equal(Json(baseline.Features),Json(d.Features));Assert.Equal(Json(baseline.StarGroups),Json(d.StarGroups));
        Assert.Equal(target/2,d.Tuning.StellarPlacementPressurePermille);Assert.NotEqual(Json(baseline.Layout),Json(d.Layout));
        Assert.Equal(Json(d),Json(NaturalGalaxyApplication.Open(d,Catalog()).Export()));
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)]
    public void ControlledFeatureSwitchesCannotRegenerateStarsOrProvenance(int family)
    {
        var s=Settings();if(family==0)s.Map.StellarInfluenceEnabled=false;else if(family==1)s.Map.BlackHoleInfluenceEnabled=false;else s.Map.NebulaInfluenceEnabled=false;
        var d=NaturalGalaxyApplication.Generate(s,Catalog()).Export();var original=Held.Value.Export();Assert.Equal(Json(original.Features),Json(d.Features));Assert.Equal(Json(original.StarGroups),Json(d.StarGroups));
        var ids=d.Features.Where(f=>f.Kind==(GalaxyEnvironmentFeatureKind)(family+1)).Select(f=>f.FeatureId).ToHashSet();
        Assert.All(d.RegionInfluences,r=>Assert.DoesNotContain(r.Contributions,c=>ids.Contains(c.FeatureId)));
    }

    [Theory]
    [InlineData("42")][InlineData("18446744073709551615")]
    public void ThousandSystemPublicGenerateStrictOpenAndGeometryAreSupported(string seed)
    {
        var review=NaturalGalaxyApplication.Generate(Settings(seed,1000),Catalog());var d=review.Export();
        if(seed=="42")Assert.Equal("265f4247468e493e0cab64edd7c28970865ce955efe950880742313a8d857ac4",d.ContentHash);
        Assert.Equal(2000,d.Layout.Regions.Count);Assert.Equal(1100,d.Features.Count);GroupsAndAnchors(d);Geometry(d);
        string json=NaturalGalaxyJson.Write(review);Assert.True(System.Text.Encoding.UTF8.GetByteCount(json)<ScaledGalaxyIdentity.MaximumJsonBytes);
        Assert.Equal(Json(d),Json(NaturalGalaxyJson.Read(json,Catalog().AsEnumerable().Reverse()).Export()));
        var h=d.Layout.HomeCandidates;var plan=review.FindRoutePlan(new(){MapContentHash=d.ContentHash,OriginNodeId=h[0].NodeId,
            Targets=new(){new(){NodeId=h[1].NodeId,Intent=GalaxyReviewTargetIntent.SystemLocation,SystemId=h[1].SystemId}},
            Filter=new(){PermittedNodeIds=d.Layout.Regions.Select(r=>r.NodeId).ToList(),PermittedConnectionIds=d.Layout.Connections.Select(e=>e.ConnectionId).ToList()}});
        Assert.True(plan.Success);Assert.Equal(review.FindRoute(h[0].NodeId,h[1].NodeId).TotalCost,plan.TotalCost);
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)][InlineData(11)][InlineData(12)][InlineData(13)]
    public void CorruptedOrForgedGroupsFeaturesSettingsAndBudgetsRejectWithoutReplacingReview(int mutation)
    {
        var d=Clone();string original=Json(Held.Value.Export());
        switch(mutation){case 0:d.StarGroups[0].Center.X++;break;case 1:d.StarGroups[^1].StarFeatureIds[0]="feature.foreign";break;case 2:d.Tuning.StellarPlacementPressurePermille++;break;
            case 3:d.Settings.AssociationSpreadPermille++;break;case 4:d.Features[0].Position.X++;break;case 5:d.Layout.Connections[0].BaseTraversalCost++;break;
            case 6:d.StarGroups[0].Center=null!;break;case 7:d.StarGroups[0].StarFeatureIds=null!;break;case 8:d.Settings.IsolatedStarSharePermille=99;break;
            case 9:d.FormatVersion=3;break;case 10:d.Settings.Map.Seed="01";break;case 11:d.StarGroups=Enumerable.Repeat(d.StarGroups[0],1001).ToList();break;
            case 12:d.StarGroups=Enumerable.Range(0,125).Select(i=>new NaturalStarGroupDto{GroupId="g"+i,Kind=NaturalStarGroupKind.LooseAssociation,Center=new(){X=100,Y=100},RadiusX=100,RadiusY=100,StarFeatureIds=Enumerable.Repeat(d.Features[0].FeatureId,9).ToList()}).ToList();break;
            case 13:d.StarGroups[0].StarFeatureIds=Enumerable.Repeat(d.Features[0].FeatureId,10).ToList();break;}
        if(mutation<=5)d.ContentHash=(string)typeof(NaturalGalaxyReviewSession).Assembly.GetType("HobbyDS9.Simulation.NaturalGalaxyData")!.GetMethod("Hash",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.Invoke(null,new object[]{d})!;
        Assert.Throws<ArgumentException>(()=>NaturalGalaxyApplication.Open(d,Catalog()));Assert.Equal(original,Json(Held.Value.Export()));
        if(mutation>=6)Assert.Throws<ArgumentException>(()=>NaturalGalaxyJson.Read(Json(d),Catalog()));
    }

    [Fact]
    public void StrictNewSchemaRejectsUnknownDuplicateMissingOversizedAndLegacyRoots()
    {
        string json=Json(Clone());foreach(string bad in new[]{json.Replace("\"IsolatedStarSharePermille\":100","\"IsolatedStarSharePermille\":100,\"IsolatedStarSharePermille\":100"),json.Replace("\"IsolatedStarSharePermille\":100,",""),json.Replace("\"IsolatedStarSharePermille\":100","\"Unknown\":true,\"IsolatedStarSharePermille\":100"),"null",new string(' ',ScaledGalaxyIdentity.MaximumJsonBytes+1)})
            Assert.Throws<ArgumentException>(()=>NaturalGalaxyJson.Read(bad,Catalog()));
        Assert.Throws<ArgumentException>(()=>ScaledGalaxyJson.Read(json,Catalog()));Assert.Throws<ArgumentException>(()=>EnvironmentGalaxyJson.Read(json,Catalog()));Assert.Throws<ArgumentException>(()=>GeneratedGalaxyJson.Read(json,Catalog()));
        var old=ScaledGalaxyApplication.Generate(Settings().Map,Catalog());Assert.Equal("006ab67d08f1cd3c3d9e73da2fa335fb5e4ba1cee1acd824d63dbd44135c1937",old.Export().ContentHash);
        Assert.Throws<ArgumentException>(()=>NaturalGalaxyJson.Read(ScaledGalaxyJson.Write(old),Catalog()));
    }

    [Fact]
    public void NewNaturalGoldenAndNeutralQueryCannotChangeCurrentMap()
    {
        var review=Held.Value;var d=review.Export();Assert.Equal("4ea9b0d4581edb60460413e9fee0203257bd47e5b6cb43fc044ee5b2cda01976",d.ContentHash);
        var h=d.Layout.HomeCandidates;string before=Json(d);var q=new GalaxyReviewRouteQueryDto{MapContentHash=d.ContentHash,OriginNodeId=h[0].NodeId,
            Targets=new(){new(){NodeId=h[1].NodeId}},Filter=new(){PermittedNodeIds=d.Layout.Regions.Select(r=>r.NodeId).ToList(),PermittedConnectionIds=d.Layout.Connections.Select(e=>e.ConnectionId).ToList()}};
        Assert.True(review.FindRoutePlan(q).Success);Assert.Equal(before,Json(review.Export()));
    }
}
