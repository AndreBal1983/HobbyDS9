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
}
