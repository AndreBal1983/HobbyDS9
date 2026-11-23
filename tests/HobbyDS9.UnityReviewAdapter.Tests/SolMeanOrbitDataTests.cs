using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class SolMeanOrbitDataTests
{
    string Source()=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"scenarios/sol-eight-planets-v1/astronomy.json"));
    static ColonisationPlanetSnapshot Body(string id,string system="generated.home")=>new(new ColonisationPlanetObservationDto{PlanetId=id,BodyId="body."+id,SystemId=system,DisplayName=id,Surveyed=false});
    [Fact]public void MeanRadiiJoinOnlyTheObservedSolSystemAndPreserveSourceRatios()
    {
        var earth=Body("planet.sol");var mars=Body("planet.mars");var neptune=Body("planet.neptune");
        var data=SolMeanOrbitData.Read(Source());var joined=data.JoinDisclosed(new[]{earth,mars,neptune},new[]{earth,mars,neptune})!;
        Assert.Equal(3,joined.Count);Assert.Equal(149600000,joined[earth.PlanetId]);Assert.Equal(4515000000,joined[neptune.PlanetId]);
        Assert.Equal(228000000d/149600000,(double)joined[mars.PlanetId]/joined[earth.PlanetId]);
        Assert.DoesNotContain("planet.mercury",joined.Keys);Assert.Null(data.JoinDisclosed(new[]{Body("planet.mercury","other.system")},new[]{earth}));
        Assert.Null(data.JoinDisclosed(new[]{mars},new[]{mars}));Assert.Null(data.JoinDisclosed(new[]{Body("planet.unlisted")},new[]{earth}));
    }
    [Fact]public void InvalidUnitsOrDuplicateRowsCannotMasqueradeAsARealDistanceLayout()
    {
        Assert.Throws<ArgumentException>(()=>SolMeanOrbitData.Read(Source().Replace("\"km\"","\"AU\"")));
        Assert.Throws<ArgumentException>(()=>SolMeanOrbitData.Read(Source().Replace("planet.venus","planet.mercury")));
    }
    [Theory][InlineData("planet.mercury","mercury")][InlineData("planet.venus","venus_atmosphere")][InlineData("planet.jupiter","jupiter")]
    [InlineData("planet.saturn","saturn")][InlineData("planet.uranus","uranus")][InlineData("planet.neptune","neptune")]
    public void AdditionalSourceArtRequiresTheRealDisclosedSurveyedIdentity(string id,string map)
    {
        Assert.Null(PlanetVisualProfile.From(Body(id)).AuthoredMap);
        var known=new ColonisationPlanetSnapshot(new ColonisationPlanetObservationDto{PlanetId=id,BodyId="body."+id,SystemId="home",Surveyed=true,Environment=new PlanetDefinitionDto{PlanetClass="Unclassified"}});
        Assert.Equal(map,PlanetVisualProfile.From(known).AuthoredMap);
    }
}
