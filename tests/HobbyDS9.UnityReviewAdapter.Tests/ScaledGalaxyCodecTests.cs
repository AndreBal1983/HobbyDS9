using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ScaledGalaxyCodecTests
{
    // Deliberately empty wire-shape fixture, NOT a generated/semantically valid map or art sample.
    private static ScaledGalaxyDocumentDto Shape()=>new()
    {
        FormatVersion=3,ProfileId=ScaledGalaxyIdentity.Profile,GeneratorVersion=ScaledGalaxyIdentity.GeneratorVersion,
        TuningVersion=ScaledGalaxyIdentity.TuningVersion,ContentHash=new string('a',64),
        Settings=new(){Seed="18446744073709551615",HomeCivilizationDefinitionIds=new(){"def.a","def.b"}},
        Tuning=new(){Environment=new()},Features=new(),RegionInfluences=new(),
        Layout=new(){Width=2500000,Height=2000000,Regions=new(),Systems=new(),Connections=new(),HomeCandidates=new()}
    };
    private static JObject Root()=>JObject.Parse(Encoding.UTF8.GetString(new ReviewNewtonsoftCodec().EncodeScaledGalaxy(Shape())));
    private static byte[] Bytes(JObject value)=>Encoding.UTF8.GetBytes(value.ToString(Newtonsoft.Json.Formatting.None));
    private static JObject Point()=>new(){{"X",1},{"Y",1}};
    private static JObject Region()=>new(){{"NodeId","n"},{"DisplayName","N"},{"SystemId",null},{"Center",Point()},{"Boundary",new JArray()}};
    private static JObject Feature()=>new(){{"FeatureId","f"},{"Kind",1},{"DisplayName","F"},{"Position",Point()},
        {"RadiusX",1},{"RadiusY",1},{"DensityWeight",1},{"SystemId",null},{"NodeId",null}};
    private static JObject Influence()=>new(){{"NodeId","n"},{"BaseDensity",100},{"TargetDensity",100},{"Contributions",new JArray()}};

    [Fact] public void DedicatedFormat3ShapeRoundtripsButDoesNotEnterLegacyDecoders()
    {
        var codec=new ReviewNewtonsoftCodec();var bytes=codec.EncodeScaledGalaxy(Shape());
        Assert.Equal(3,codec.GeneratedGalaxyFormat(bytes));var decoded=codec.DecodeScaledGalaxy(bytes);
        Assert.Equal("18446744073709551615",decoded.Settings.Seed);Assert.Equal(bytes,codec.EncodeScaledGalaxy(decoded));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeGeneratedGalaxy(bytes));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeEnvironmentGalaxy(bytes));
    }

    [Fact] public void InvalidFormat3SemanticsCannotReplaceCurrentMapOrDraft()
    {
        var directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-ScaledWire-"+Guid.NewGuid().ToString("N"));
        try
        {
            var codec=new ReviewNewtonsoftCodec();var catalog=codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
                "scenarios/playable-fleet-v1/scenario.json"))).Definitions.Civilizations!;
            var store=new GeneratedGalaxyMapStore(directory);var workspace=new GeneratedGalaxyWorkspace(catalog,store);
            Assert.True(workspace.Generate());var current=workspace.Document;var original=workspace.ExportBytes();
            workspace.Seed="98765";store.Write(1,codec.EncodeScaledGalaxy(Shape()),false);
            Assert.False(workspace.Open());Assert.Same(current,workspace.Document);Assert.Equal("98765",workspace.Seed);
            Assert.True(workspace.Unexported);Assert.Equal(original,workspace.ExportBytes());
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }

    [Theory]
    [InlineData("format")][InlineData("missing")][InlineData("unknown")][InlineData("missing-nullable")]
    [InlineData("integer-string")][InlineData("bool-string")][InlineData("seed-long")][InlineData("text-long")]
    [InlineData("regions")][InlineData("systems")][InlineData("edges")][InlineData("features")]
    [InlineData("vertices-per-cell")][InlineData("vertices-total")][InlineData("influences")]
    [InlineData("contributions-per-row")][InlineData("contributions-total")]
    public void StrictShapeAndIndividualAndAggregateBudgetsRejectBeforePortableOpen(string kind)
    {
        var root=Root();var layout=root["Layout"]!;
        switch(kind)
        {
            case "format":root["FormatVersion"]=2;break;
            case "missing":((JObject)root["Settings"]!).Remove("EmptyRegionCount");break;
            case "unknown":root["Tuning"]!["Invented"]=0;break;
            case "missing-nullable":var feature=Feature();feature.Remove("NodeId");((JArray)root["Features"]!).Add(feature);break;
            case "integer-string":root["Settings"]!["Width"]="2500000";break;
            case "bool-string":root["Settings"]!["NebulaInfluenceEnabled"]="true";break;
            case "seed-long":root["Settings"]!["Seed"]=new string('1',21);break;
            case "text-long":root["ProfileId"]=new string('x',129);break;
            case "regions":layout["Regions"]=Repeat(Region(),2001);break;
            case "systems":layout["Systems"]=Repeat(new JObject{{"SystemId","s"},{"DisplayName","S"},{"NodeId","n"}},1001);break;
            case "edges":layout["Connections"]=Repeat(new JObject{{"ConnectionId","e"},{"EndpointA","a"},{"EndpointB","b"},{"Kind",0},{"BaseTraversalCost",1}},6001);break;
            case "features":root["Features"]=Repeat(Feature(),1251);break;
            case "vertices-per-cell":var region=Region();region["Boundary"]=Repeat(Point(),65);((JArray)layout["Regions"]!).Add(region);break;
            case "vertices-total":var many=Region();many["Boundary"]=Repeat(Point(),64);layout["Regions"]=Repeat(many,313);break;
            case "influences":root["RegionInfluences"]=Repeat(Influence(),2001);break;
            case "contributions-per-row":var influence=Influence();influence["Contributions"]=Repeat(new JObject{{"FeatureId","f"},{"Amount",1}},1);((JArray)root["RegionInfluences"]!).Add(influence);break;
            case "contributions-total":root["Features"]=Repeat(Feature(),13);var all=Influence();all["Contributions"]=Repeat(new JObject{{"FeatureId","f"},{"Amount",1}},13);root["RegionInfluences"]=Repeat(all,1847);break;
        }
        Assert.Throws<InvalidDataException>(()=>new ReviewNewtonsoftCodec().DecodeScaledGalaxy(Bytes(root)));
    }

    [Fact] public void DuplicateDepthByteLimitsAndLegacyTwoMiBCapRemainEnforced()
    {
        var codec=new ReviewNewtonsoftCodec();var bytes=codec.EncodeScaledGalaxy(Shape());var text=Encoding.UTF8.GetString(bytes);
        Assert.Throws<InvalidDataException>(()=>codec.DecodeScaledGalaxy(Encoding.UTF8.GetBytes(text.Replace("\"FormatVersion\":3","\"FormatVersion\":3,\"FormatVersion\":3"))));
        Assert.Throws<InvalidDataException>(()=>codec.GeneratedGalaxyFormat(Encoding.UTF8.GetBytes("{\"FormatVersion\":3,\"x\":"+new string('[',20)+"0"+new string(']',20)+"}")));
        var padded=Encoding.UTF8.GetBytes(text+new string(' ',GeneratedGalaxyMapStore.LegacyMaximumBytes));
        Assert.Equal(3,codec.GeneratedGalaxyFormat(padded));Assert.Equal(250,codec.DecodeScaledGalaxy(padded).Settings.SystemCount);
        foreach(var format in new[]{1,2})
        {
            var old=Encoding.UTF8.GetBytes("{\"FormatVersion\":"+format+"}"+new string(' ',GeneratedGalaxyMapStore.LegacyMaximumBytes));
            Assert.Throws<InvalidDataException>(()=>codec.GeneratedGalaxyFormat(old));
            Assert.Throws<InvalidDataException>(()=>codec.DecodeGeneratedGalaxy(old));
            Assert.Throws<InvalidDataException>(()=>codec.DecodeEnvironmentGalaxy(old));
        }
        Assert.Throws<InvalidDataException>(()=>codec.DecodeScaledGalaxy(new byte[ScaledGalaxyIdentity.MaximumJsonBytes+1]));
    }

    private static JArray Repeat(JToken value,int count)=>new(Enumerable.Range(0,count).Select(_=>value.DeepClone()));
}
