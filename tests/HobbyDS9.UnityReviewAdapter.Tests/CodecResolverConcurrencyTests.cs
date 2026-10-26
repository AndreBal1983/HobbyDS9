using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class CodecResolverConcurrencyTests
{
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    static CampaignColonisationPackageDto Base(ReviewNewtonsoftCodec c)
    {
        byte[] Read(string f,string n)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",f,n+".json"));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),c);
        return ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),c);
    }
    static CampaignColonisationExpansionPackageDto Package(ReviewNewtonsoftCodec c,CampaignColonisationPackageDto p)
    {
        byte[] Read(string f,string n)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",f,n+".json"));
        return ColonisationExpansionPlaytestOptions.CreatePackage(ColonisationPlaytestOptions.CreateRosterPackage(p,Read("colonisation-roster-v2","roster"),c),Read("colonisation-expansion-v1","planner"),c);
    }
    [Fact] public async Task ConcurrentCrossInstanceMixedCarriersRemainByteExactAndDetachedFromSessions()
    {
        var c=new ReviewNewtonsoftCodec();var p=Base(c);
        var legacy=CampaignColonisationApplication.StartGame(p,Settings());
        var expansion=CampaignColonisationExpansionApplication.StartGame(Package(c,p),Settings());
        var oldBytes=c.EncodeColonisationRoot(legacy.ExportColonisationSave());
        var newBytes=c.EncodeColonisationExpansionRoot(expansion.ExportColonisationExpansionSave());
        using var physical=new UnityReviewFixture();var physicalBytes=c.EncodeRoot(physical.RootSave());
        await Task.WhenAll(Enumerable.Range(0,8).Select(_=>Task.Run(()=>
        {
            var codec=new ReviewNewtonsoftCodec();
            for(int i=0;i<2;i++)
            {
                Assert.Equal(physicalBytes,codec.EncodeRoot(codec.DecodeRoot(physicalBytes)));
                Assert.Equal(oldBytes,codec.EncodeColonisationRoot(codec.CopySave(codec.DecodeColonisationRoot(oldBytes))));
                var a=codec.DecodeColonisationExpansionRoot(newBytes);var b=codec.CopyExpansionSave(a);
                Assert.NotSame(a.BaseCampaign,b.BaseCampaign);Assert.NotSame(a.AiPlans,b.AiPlans);
                Assert.Null(Assert.Single(a.AiPlans!).TargetPlanetId);
                Assert.Equal(newBytes,codec.EncodeColonisationExpansionRoot(b));
                Assert.Single(b.AiPlans!).HoldReason="test-only detached mutation";
                Assert.Equal(newBytes,codec.EncodeColonisationExpansionRoot(a));
            }
        })));
        Assert.Equal(oldBytes,c.EncodeColonisationRoot(legacy.ExportColonisationSave()));
        Assert.Equal(newBytes,c.EncodeColonisationExpansionRoot(expansion.ExportColonisationExpansionSave()));
        Assert.Equal(physicalBytes,c.EncodeRoot(physical.RootSave()));
    }
    [Fact] public void WarmContractsStillRejectNestedMissingNullUnknownDuplicateAndWrongTypesWithSafeErrors()
    {
        var c=new ReviewNewtonsoftCodec();var p=Base(c);
        var root=CampaignColonisationExpansionApplication.StartGame(Package(c,p),Settings()).ExportColonisationExpansionSave();
        var bytes=c.EncodeColonisationExpansionRoot(root);Assert.Equal(bytes,c.EncodeColonisationExpansionRoot(c.DecodeColonisationExpansionRoot(bytes)));
        byte[] Bad(Action<JObject> mutate){var j=JObject.Parse(Encoding.UTF8.GetString(bytes));mutate(j);return Encoding.UTF8.GetBytes(j.ToString(Formatting.None));}
        var bad=new[]{Bad(j=>((JObject)j["AiPlans"]![0]!).Property("Stage")!.Remove()),Bad(j=>j["AiPlans"]![0]!["Stage"]=null),Bad(j=>j["AiPlans"]![0]!["Stage"]=true),Bad(j=>j["AiPlans"]![0]!["untrusted-field"]="private-marker"),Encoding.UTF8.GetBytes("{\"FormatVersion\":12,"+Encoding.UTF8.GetString(bytes).Substring(1))};
        foreach(var value in bad)
        {
            var error=Assert.Throws<InvalidDataException>(()=>new ReviewNewtonsoftCodec().DecodeColonisationExpansionRoot(value));
            Assert.Equal("Invalid colonisation campaign carrier.",error.Message);
        }
        using var physical=new UnityReviewFixture();var old=c.EncodeRoot(physical.RootSave());
        c.DecodeRoot(old);var malformed=JObject.Parse(Encoding.UTF8.GetString(old));
        malformed.Descendants().OfType<JProperty>().First(x=>x.Name=="StateRevision").Remove();
        Assert.Equal("Invalid physical review data.",Assert.Throws<InvalidDataException>(()=>new ReviewNewtonsoftCodec().DecodeRoot(Encoding.UTF8.GetBytes(malformed.ToString(Formatting.None)))).Message);
        Assert.Equal(bytes,c.EncodeColonisationExpansionRoot(c.DecodeColonisationExpansionRoot(bytes)));
    }
}
