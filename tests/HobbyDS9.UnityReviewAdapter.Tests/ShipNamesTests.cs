using System.Globalization;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ShipNamesTests
{
    [Fact] public void CatalogueSelectionUsesTheImmutableConstructionSequence()
    {
        var name=ReviewShipNames.Ship("ship.spawn.0000000000000017","civ.federation")!;
        Assert.Equal("U.S.S. Thunderchild 2",name.Name);
        Assert.Equal("NCC-100023",name.Registry);
        Assert.Equal(name.Label,ReviewShipNames.Ship("ship.spawn.0000000000000017","civilization.federation")!.Label);
        Assert.Equal("U.S.S. Enterprise",ReviewShipNames.Ship("ship.equipment-cruiser","civ.federation")!.Name);
    }

    [Fact] public void RegistriesAndNamesDoNotCollideOrDependOnEnumerationOrderOrCulture()
    {
        var previous=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("ar-SA");
            var ids=Enumerable.Range(1,10000).Select(i=>"ship.spawn."+i.ToString("x16",CultureInfo.InvariantCulture)).ToArray();
            var forward=ids.ToDictionary(id=>id,id=>ReviewShipNames.Ship(id,"civ.federation")!.Label);
            Assert.Equal(ids.Length,forward.Values.Distinct().Count());
            Assert.Equal(ids.Length,ids.Select(id=>ReviewShipNames.Ship(id,"civ.federation")!.Registry).Distinct().Count());
            foreach(var id in ids.Reverse())Assert.Equal(forward[id],ReviewShipNames.Ship(id,"civ.federation")!.Label);
            Assert.Equal("NCC-18446744073709651615",ReviewShipNames.Ship("ship.spawn.ffffffffffffffff","civ.federation")!.Registry);
        }
        finally { CultureInfo.CurrentCulture=previous; }
    }

    [Theory]
    [InlineData("ship.spawn.0000000000000000","civ.federation")]
    [InlineData("ship.spawn.17","civ.federation")]
    [InlineData("ship.unrecognised","civ.federation")]
    [InlineData("ship.spawn.0000000000000017","civ.cardassian")]
    public void NeverAssignsStarfleetIdentityToForeignOrUnknownAuthoredVessels(string ship,string actor)
        =>Assert.Null(ReviewShipNames.Ship(ship,actor));
}
