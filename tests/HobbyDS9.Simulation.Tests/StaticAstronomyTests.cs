using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.Simulation.Tests;

public sealed class StaticAstronomyTests
{
    const string Human="civ.federation";
    readonly ITestOutputHelper output;
    public StaticAstronomyTests(ITestOutputHelper output)=>this.output=output;
    static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(Json(value))!;
    static CampaignPlayableFleetPackageDto Template()=>PlayableFleetTests.Package();
    static NaturalGalaxySettingsDto Settings(int count=250)=>new(){Map=new(){Seed="42",SystemCount=count,EmptyRegionCount=count,
        Width=count==1000?5000000:2500000,Height=count==1000?4000000:2000000,QuietEmptyAreaTargetPermille=2000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    static GeneratedGalaxyPointDto P(int x,int y)=>new(){X=x,Y=y};
    static GeneratedGalaxyRegionDto Box(string id,int x,int y)=>new(){NodeId=id,DisplayName=id,Center=P(x+5,y+5),
        Boundary=new(){P(x,y),P(x+10,y),P(x+10,y+10),P(x,y+10)}};
    static List<GeneratedGalaxyRegionDto> Boxes()=>new(){Box("nav.a",0,0),Box("nav.b",10,0),Box("nav.c",0,10),Box("nav.d",10,10)};
    static EnvironmentGalaxyFeatureDto Nebula(string id,int x,int y,int radius=30)=>new(){FeatureId=id,Kind=GalaxyEnvironmentFeatureKind.Nebula,
        DisplayName="Private source label",Position=P(x,y),RadiusX=radius,RadiusY=radius,DensityWeight=999};
    static void Advance(ICampaignGeneratedSession app){var status=app.CaptureStatus();var result=app.AdvanceTurn(status.Turn,status.CampaignRevision);Assert.True(result.Accepted,result.Error.ToString());}
    static void Explore(ICampaignGeneratedSession app,HashSet<string> visited)
    {
        foreach(var fleet in app.GetOwnStrategicFleets(Human))
        {
            string origin=fleet.LocationNodeId.Value;visited.Add(origin);if(fleet.RouteNodeIds.Count>0||fleet.TransitTotalCost>0)continue;
            var map=app.CaptureGeneratedMap(Human);var neighbors=map.Regions.ToDictionary(r=>r.NodeId,_=>new List<string>(),StringComparer.Ordinal);
            foreach(var edge in map.Connections){neighbors[edge.EndpointA].Add(edge.EndpointB);neighbors[edge.EndpointB].Add(edge.EndpointA);}
            var distance=new Dictionary<string,int>(StringComparer.Ordinal){{origin,0}};var queue=new Queue<string>();queue.Enqueue(origin);
            while(queue.Count>0){string current=queue.Dequeue();foreach(string next in neighbors[current])if(distance.TryAdd(next,distance[current]+1))queue.Enqueue(next);}
            string? target=distance.Where(d=>d.Value>0&&!visited.Contains(d.Key)).OrderBy(d=>d.Value).ThenBy(d=>d.Key,StringComparer.Ordinal).Select(d=>d.Key).FirstOrDefault();
            if(target==null)continue;
            var route=app.FindKnownRoute(Human,origin,target);var status=app.CaptureStatus();
            Assert.True(app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.astronomy.explore."+status.CampaignRevision,ExpectedTurn=status.Turn,
                ExpectedRevision=status.CampaignRevision,Order=new(){FleetId=fleet.FleetId.Value,Intent=CampaignTravelIntent.Leave,RouteNodeIds=route.Steps.ToList()}}).Accepted);
        }
    }

    [Theory]
    [InlineData(5,5,1)][InlineData(10,5,2)][InlineData(10,10,4)][InlineData(0,0,1)][InlineData(20,20,1)][InlineData(15,15,1)]
    public void ExactClosedCentreMembershipIncludesEveryBoundaryOwner(int x,int y,int memberships)
    {
        var regions=Boxes();var feature=Nebula("feature.nebula.one",x,y);
        var index=GeneratedStaticAstronomyIndex.Build(regions,new[]{feature},20,20);
        Assert.Equal(memberships,index.Memberships);
        Assert.Equal(memberships,regions.Count(r=>index.Visible(new[]{r.NodeId}).Count==1));
        Assert.Single(index.Visible(regions.Select(r=>r.NodeId)));
        Assert.Empty(index.Visible(Array.Empty<string>()));Assert.Empty(index.Visible(new[]{"nav.unknown"}));
        Assert.Null(feature.NodeId);Assert.Null(feature.SystemId);
        regions.Reverse();Assert.Equal(Json(index.Visible(regions.Select(r=>r.NodeId))),Json(GeneratedStaticAstronomyIndex.Build(regions,new[]{feature},20,20).Visible(regions.Select(r=>r.NodeId))));
    }
    [Fact]
    public void InfluenceOverlapDoesNotRevealUnknownCentreAndInputsCannotMutateIndex()
    {
        var regions=Boxes();var feature=Nebula("feature.nebula.hidden",15,5,100);
        var index=GeneratedStaticAstronomyIndex.Build(regions,new[]{feature},20,20);
        Assert.Empty(index.Visible(new[]{"nav.a"}));Assert.Single(index.Visible(new[]{"nav.b"}));
        feature.Position.X=1;feature.RadiusX=999;regions[1].Boundary.Clear();
        var visible=Assert.Single(index.Visible(new[]{"nav.b"}));Assert.Equal(15,visible.CentreX);Assert.Equal(100,visible.VisualReferenceRadiusX);
        var snapshot=new CampaignStaticAstronomySnapshot(Human,new string('a',64),1,0,index.Visible(new[]{"nav.b"}));
        Assert.Throws<NotSupportedException>(()=>((IList<CampaignNebulaVisualProxy>)snapshot.Nebulae).Clear());
        Assert.All(typeof(CampaignNebulaVisualProxy).GetProperties(),p=>Assert.Null(p.SetMethod));
        string json=Json(snapshot);foreach(string secret in new[]{"DensityWeight","NodeId","SystemId","StarGroups","Private source label"})Assert.DoesNotContain(secret,json);
    }
    [Fact]
    public void OptInNewIdentityRoundtripsWithoutChangingV1OrItsCapability()
    {
        var old=CampaignGeneratedApplication.StartGame(Template(),Settings());
        var app=CampaignStaticAstronomyApplication.StartGame(Template(),Settings());
        Assert.False(old is ICampaignStaticAstronomySession);
        var save=app.ExportGeneratedSave();var oldSave=old.ExportGeneratedSave();
        Assert.Equal(7,save.FormatVersion);Assert.Equal(CampaignStaticAstronomyIdentity.Profile,save.ProfileId);Assert.Equal(CampaignStaticAstronomyIdentity.Policy,save.PolicyId);
        Assert.Equal(CampaignStaticAstronomyIdentity.EmbeddedCoreProfile,save.Campaign.Integrated.Physical!.Core.ProfileId);
        Assert.NotEqual(oldSave.PackageHash,save.PackageHash);Assert.Equal(Json(oldSave.Map),Json(save.Map));
        Assert.Equal(Json(oldSave.Campaign.Integrated.Physical!.Core.Campaign.GameState),Json(save.Campaign.Integrated.Physical.Core.Campaign.GameState));
        var snapshot=app.CaptureKnownAstronomy(Human);Assert.Empty(snapshot.Nebulae);Assert.Equal(app.CaptureStatus().CampaignRevision,snapshot.CampaignRevision);
        Assert.Equal(CampaignStaticAstronomyIdentity.ObservationPolicy,snapshot.ObservationPolicy);
        Assert.Throws<ArgumentException>(()=>CampaignGeneratedApplication.LoadSave(Template(),save));
        Assert.Throws<ArgumentException>(()=>CampaignStaticAstronomyApplication.LoadSave(Template(),oldSave));
        Advance(app);save=app.ExportGeneratedSave();Assert.Equal(Json(save),Json(CampaignStaticAstronomyApplication.LoadSave(Template(),Clone(save)).ExportGeneratedSave()));
    }
    [Theory]
    [InlineData("awaiting-combat.json","02fdb4e0a3113d259128f46f1a513854ab95a11bc228b90ba8fb92cb720fa8a5")]
    [InlineData("aftermath.json","8f026214856d5195e7761945b2c03c9a4e8a5e38befc5081389368b97c6b1be3")]
    public void AcceptedV1ActualCombatFixturesRemainByteExact(string file,string expected)
    {
        byte[] bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures/generated-campaign-v1",file));
        Assert.Equal(expected,Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        var root=JsonSerializer.Deserialize<CampaignGeneratedSaveDto>(bytes)!;
        var app=CampaignGeneratedApplication.LoadSave(Template(),root);
        Assert.Equal(Encoding.UTF8.GetString(bytes),Json(app.ExportGeneratedSave()));
        Assert.False(app is ICampaignStaticAstronomySession);
    }

    [Fact]
    public void ActualArrivalChartsCoarseNebulaAndKnowledgeSurvivesDepartureAndLoad()
    {
        var app=CampaignStaticAstronomyApplication.StartGame(Template(),Settings());var visited=new HashSet<string>(StringComparer.Ordinal);
        int revealedTurn=0;var ever=new HashSet<string>(StringComparer.Ordinal);string? first=null;
        for(int i=0;i<68&&app.CaptureStatus().Phase==SessionPhase.Planning;i++)
        {
            Explore(app,visited);Advance(app);var snapshot=app.CaptureKnownAstronomy(Human);
            var now=snapshot.Nebulae.Select(n=>n.FeatureId).ToHashSet(StringComparer.Ordinal);Assert.True(ever.IsSubsetOf(now));ever=now;
            if(first==null&&now.Count>0){first=now.Order(StringComparer.Ordinal).First();revealedTurn=app.CaptureStatus().Turn;}
            if(first!=null&&app.CaptureStatus().Turn>=revealedTurn+5)break;
        }
        Assert.NotNull(first);Assert.True(app.CaptureStatus().Turn>=revealedTurn+5);
        var save=app.ExportGeneratedSave();string original=Json(save);
        var snapshot1=app.CaptureKnownAstronomy(Human);var snapshot2=app.CaptureKnownAstronomy(Human);
        Assert.Equal(Json(snapshot1),Json(snapshot2));Assert.Equal(original,Json(app.ExportGeneratedSave()));
        Assert.NotSame(snapshot1.Nebulae[0],snapshot2.Nebulae[0]);
        var loaded=CampaignStaticAstronomyApplication.LoadSave(Template(),Clone(save));
        Assert.Equal(original,Json(loaded.ExportGeneratedSave()));Assert.Equal(Json(snapshot1),Json(loaded.CaptureKnownAstronomy(Human)));
        Assert.Contains(loaded.CaptureKnownAstronomy(Human).Nebulae,n=>n.FeatureId==first);
        output.WriteLine("Actual first charted nebula turn="+revealedTurn+" retained through="+app.CaptureStatus().Turn+" count="+snapshot1.Nebulae.Count);
    }

    [Theory]
    [InlineData("civ.unknown")][InlineData("civ.cardassian")][InlineData("")]
    public void UnknownOrUncontrolledActorCannotInspectPrivateIndex(string actor)
    {
        var app=CampaignStaticAstronomyApplication.StartGame(Template(),Settings());string before=Json(app.ExportGeneratedSave());
        Assert.Throws<ArgumentException>(()=>app.CaptureKnownAstronomy(actor));Assert.Equal(before,Json(app.ExportGeneratedSave()));
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)]
    public void MixedProfilesMarkersHashesAndRootsRejectWithoutReplacement(int mutation)
    {
        var app=CampaignStaticAstronomyApplication.StartGame(Template(),Settings());Advance(app);var good=app.ExportGeneratedSave();string original=Json(good);var bad=Clone(good);
        switch(mutation){case 0:bad.ProfileId=CampaignGeneratedIdentity.Profile;break;case 1:bad.PolicyId=CampaignGeneratedIdentity.Policy;break;
            case 2:bad.Campaign.Integrated.Physical!.Core.ProfileId=CampaignGeneratedIdentity.EmbeddedCoreProfile;break;
            case 3:bad.PackageHash=new string('a',64);break;case 4:bad.Map.ContentHash=new string('a',64);break;
            case 5:bad.Campaign.Transcript.Clear();break;case 6:bad.FormatVersion=8;break;}
        Assert.Throws<ArgumentException>(()=>CampaignStaticAstronomyApplication.LoadSave(Template(),bad));Assert.Equal(original,Json(app.ExportGeneratedSave()));
    }

    [Fact]
    public void ReorderedRepeatQueriesAndV1V2AiRoutesResourcesContactsRngStayExact()
    {
        var v1=CampaignGeneratedApplication.StartGame(Template(),Settings());var v2=CampaignStaticAstronomyApplication.StartGame(Template(),Settings());
        var reversed=Template();reversed.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Fleets!.Reverse();reversed.Integrated.Physical.CorePackage.Campaign.InitialState.Civilizations!.Reverse();
        var settings=Settings();settings.Map.HomeCivilizationDefinitionIds.Reverse();var repeat=CampaignStaticAstronomyApplication.StartGame(reversed,settings);
        for(int i=0;i<12;i++)
        {
            string before=Json(v2.ExportGeneratedSave());for(int q=0;q<3;q++)v2.CaptureKnownAstronomy(Human);Assert.Equal(before,Json(v2.ExportGeneratedSave()));
            Advance(v1);Advance(v2);Advance(repeat);
            var old=v1.ExportGeneratedSave().Campaign.Integrated.Physical!;var current=v2.ExportGeneratedSave().Campaign.Integrated.Physical!;
            Assert.Equal(Json(old.Core.Campaign.GameState),Json(current.Core.Campaign.GameState));Assert.Equal(Json(old.Orders),Json(current.Orders));Assert.Equal(Json(old.Contacts),Json(current.Contacts));
            Assert.Equal(Json(v2.ExportGeneratedSave()),Json(repeat.ExportGeneratedSave()));
        }
    }

    [Theory]
    [InlineData(250)][InlineData(1000)]
    public void RealMapIndexBuildQueryAndFrozenLoadAreMeasuredAndBounded(int count)
    {
        var watch=Stopwatch.StartNew();var app=CampaignStaticAstronomyApplication.StartGame(Template(),Settings(count));long started=watch.ElapsedMilliseconds;
        var save=app.ExportGeneratedSave();var indexWatch=Stopwatch.StartNew();var index=GeneratedStaticAstronomyIndex.Build(save.Map.Layout.Regions,save.Map.Features,save.Map.Layout.Width,save.Map.Layout.Height);
        long indexMs=indexWatch.ElapsedMilliseconds;for(int i=0;i<100;i++)app.CaptureKnownAstronomy(Human);
        long queryMs=indexWatch.ElapsedMilliseconds-indexMs;var loaded=CampaignStaticAstronomyApplication.LoadSave(Template(),Clone(save));
        Assert.Equal(Json(save),Json(loaded.ExportGeneratedSave()));Assert.Equal(Json(app.CaptureKnownAstronomy(Human)),Json(loaded.CaptureKnownAstronomy(Human)));
        Assert.InRange(index.CandidateRegionChecks,0,2500000);Assert.InRange(index.CrossProducts,0,25000000);
        output.WriteLine(count+" systems startMs="+started+" indexMs="+indexMs+" 100queriesMs="+queryMs+" candidates="+index.CandidateRegionChecks+" crosses="+index.CrossProducts+" memberships="+index.Memberships);
    }

    [Fact]
    public void AdversarialExactIngressWorkCapsRemainFiniteWithAllVertexTies()
    {
        const int scale=80,side=500*scale,centre=side/2;
        var rim=new List<GeneratedGalaxyPointDto>();
        for(int i=0;i<500;i++)rim.Add(P(i*scale,0));for(int i=0;i<500;i++)rim.Add(P(side,i*scale));
        for(int i=0;i<500;i++)rim.Add(P(side-i*scale,side));for(int i=0;i<500;i++)rim.Add(P(0,side-i*scale));
        var regions=new List<GeneratedGalaxyRegionDto>();
        for(int i=0;i<rim.Count;i++)
        {
            var a=rim[i];var b=rim[(i+1)%rim.Count];var boundary=new List<GeneratedGalaxyPointDto>{P(centre,centre),P(a.X,a.Y)};
            for(int j=1;j<8;j++)boundary.Add(P(a.X+(b.X-a.X)*j/8,a.Y+(b.Y-a.Y)*j/8));boundary.Add(P(b.X,b.Y));
            regions.Add(new(){NodeId="nav.cap."+i,DisplayName="Cap",Center=P((centre+a.X+b.X)/3,(centre+a.Y+b.Y)/3),Boundary=boundary});
        }
        var features=Enumerable.Range(0,1250).Select(i=>Nebula("feature.cap."+i,centre,centre)).ToList();
        var watch=Stopwatch.StartNew();long before=GC.GetTotalMemory(false);var index=GeneratedStaticAstronomyIndex.Build(regions,features,side,side);
        Assert.Equal(2500000,index.CandidateRegionChecks);Assert.Equal(25000000,index.CrossProducts);Assert.Equal(2500000,index.Memberships);
        Assert.Equal(1250,index.Visible(new[]{"nav.cap.0","nav.cap.1"}).Count);
        output.WriteLine("Component cap test (not generated-game fixture) ms="+watch.ElapsedMilliseconds+" managedDelta="+(GC.GetTotalMemory(false)-before));
        Assert.Throws<ArgumentException>(()=>GeneratedStaticAstronomyIndex.Build(regions.Append(regions[0]).ToArray(),features,side,side));
        Assert.Throws<ArgumentException>(()=>GeneratedStaticAstronomyIndex.Build(regions,features.Append(features[0]).ToArray(),side,side));
    }
}
