using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedGalaxyCodecTests
{
    // Wire-shape checks only; full semantic acceptance must go through portable Open.
    private static GeneratedGalaxyDocumentDto Shape()=>new()
    {
        FormatVersion=GeneratedGalaxyIdentity.FormatVersion,ProfileId=GeneratedGalaxyIdentity.Profile,
        GeneratorVersion=GeneratedGalaxyIdentity.GeneratorVersion,TuningVersion=GeneratedGalaxyIdentity.TuningVersion,
        ContentHash=new string('a',64),Width=1000000,Height=800000,
        Settings=new(){Seed="18446744073709551615",SystemCount=40,HomeCivilizationDefinitionIds=new(){"def.a","def.b"}},
        Regions=new(),Systems=new(),Connections=new(),HomeCandidates=new()
    };
    [Fact] public void CodecRetainsFullUnsignedSeedAsTextWithoutPrecisionLoss()
    {
        var codec=new ReviewNewtonsoftCodec();var decoded=codec.DecodeGeneratedGalaxy(codec.EncodeGeneratedGalaxy(Shape()));
        Assert.Equal("18446744073709551615",decoded.Settings.Seed);Assert.Equal(1000000,decoded.Width);
    }
    [Theory]
    [InlineData("\"Width\":1000000","\"Width\":\"1000000\"")]
    [InlineData("\"Width\":1000000","\"Width\":1000000,\"Width\":42")]
    [InlineData("\"Width\":1000000,","")]
    [InlineData("\"Seed\":\"18446744073709551615\"","\"Seed\":18446744073709551615")]
    [InlineData("\"ProfileId\":","\"Unknown\":0,\"ProfileId\":")]
    public void CorruptShapeIsRejectedBeforePortableOpen(string before,string after)
    {
        var codec=new ReviewNewtonsoftCodec();var text=Encoding.UTF8.GetString(codec.EncodeGeneratedGalaxy(Shape()));
        Assert.Contains(before,text);
        Assert.Throws<InvalidDataException>(()=>codec.DecodeGeneratedGalaxy(Encoding.UTF8.GetBytes(text.Replace(before,after))));
    }

    private static byte[] EnvironmentSample()
    {
        var codec=new ReviewNewtonsoftCodec();var catalog=codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1/scenario.json"))).Definitions.Civilizations!;
        var map=HobbyDS9.Application.EnvironmentGalaxyApplication.Generate(new EnvironmentGalaxySettingsDto
        {
            Map=new(){Seed="42",SystemCount=40,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}},
            StellarInfluenceEnabled=true,BlackHoleInfluenceEnabled=true,NebulaInfluenceEnabled=true
        },catalog).Export();
        return codec.EncodeEnvironmentGalaxy(map);
    }

    [Fact] public void OuterFormatDispatchAndEnvironmentRoundtripRetainAllFields()
    {
        var codec=new ReviewNewtonsoftCodec();var bytes=EnvironmentSample();Assert.Equal(2,codec.GeneratedGalaxyFormat(bytes));
        Assert.Equal(bytes,codec.EncodeEnvironmentGalaxy(codec.DecodeEnvironmentGalaxy(bytes)));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeGeneratedGalaxy(bytes));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeEnvironmentGalaxy(codec.EncodeGeneratedGalaxy(Shape())));
        Assert.Throws<InvalidDataException>(()=>codec.GeneratedGalaxyFormat(Encoding.UTF8.GetBytes("{\"FormatVersion\":\"2\"}")));
        Assert.Throws<InvalidDataException>(()=>codec.GeneratedGalaxyFormat(Encoding.UTF8.GetBytes("{\"FormatVersion\":2,\"FormatVersion\":1}")));
    }

    [Theory]
    [InlineData("boolean-string")]
    [InlineData("missing-nullable")]
    [InlineData("extra-field")]
    [InlineData("feature-bound")]
    [InlineData("contribution-bound")]
    [InlineData("integer-string")]
    public void EnvironmentDecoderRejectsInvalidWireShape(string kind)
    {
        var root=Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString(EnvironmentSample()));
        if(kind=="boolean-string")root["Settings"]!["StellarInfluenceEnabled"]="true";
        if(kind=="missing-nullable")((Newtonsoft.Json.Linq.JObject)root["Features"]![0]!).Remove("SystemId");
        if(kind=="extra-field")root["Layout"]!["FormatVersion"]=1;
        if(kind=="integer-string")root["Features"]![0]!["RadiusX"]="1000";
        if(kind=="feature-bound"){var rows=(Newtonsoft.Json.Linq.JArray)root["Features"]!;while(rows.Count<56)rows.Add(rows[0].DeepClone());}
        if(kind=="contribution-bound"){var rows=(Newtonsoft.Json.Linq.JArray)root["RegionInfluences"]![0]!["Contributions"]!;while(rows.Count<56)rows.Add(new Newtonsoft.Json.Linq.JObject{{"FeatureId","unknown"},{"Amount",1}});}
        Assert.Throws<InvalidDataException>(()=>new ReviewNewtonsoftCodec().DecodeEnvironmentGalaxy(Encoding.UTF8.GetBytes(root.ToString())));
    }
}
