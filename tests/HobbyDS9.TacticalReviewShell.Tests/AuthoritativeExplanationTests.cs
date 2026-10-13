using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.TacticalReviewClient.Tests;
using Xunit;
using static HobbyDS9.TacticalReviewClient.Tests.ActualReviewFixture;

namespace HobbyDS9.TacticalReviewShell.Tests;

public sealed class AuthoritativeExplanationTests
{
    [Theory]
    [InlineData("power",CampaignWeaponOwnBlockers.PowerIncomplete)]
    [InlineData("stock",CampaignWeaponOwnBlockers.NoAmmunition)]
    [InlineData("cadence",CampaignWeaponOwnBlockers.CadenceWait)]
    [InlineData("damage",CampaignWeaponOwnBlockers.NoDamage)]
    public void ActualOwnBlockedFlagsAppearInBothPresentationAndOwnInspection(string defect,CampaignWeaponOwnBlockers expected)
    {
        var f=new ActualReviewFixture(p=>{
            foreach(var fit in p.Campaign!.Definitions.AuthoredTacticalEquipment!.Fits!)
            foreach(var weapon in fit.Weapons!.Where(w=>w.Class == TacticalWeaponClass.Torpedo))
            {
                if(defect == "power") weapon.AllocatedPower=0;
                if(defect == "cadence") weapon.InitialNextFireRound=2;
                if(defect == "damage") weapon.Damage=0;
            }
            if(defect == "stock")
            foreach(var provision in p.Campaign.Definitions.AuthoredTacticalEquipment.InitialShipProvisions!)
            foreach(var ammo in provision.InitialAmmunition!) ammo.Stock=0;
        });
        f.Allocate();
        var torpedo=f.A.Frame.OwnShips.SelectMany(s=>s.Weapons).Single(w=>w.Class == TacticalWeaponClass.Torpedo);
        Assert.Equal(CampaignWeaponAvailabilityStatus.OwnStateBlocked,torpedo.Availability.Status);
        Assert.Equal(expected,torpedo.Availability.OwnBlockers);
        foreach(var text in new[]{ReviewTextRenderer.Render(f.A),ReviewTextRenderer.RenderInspection(f.A,"ship.equipment-cruiser")})
        { Assert.Contains("Own availability OwnStateBlocked",text); Assert.Contains(expected.ToString(),text); Assert.Contains("observed boundary",text); }
        Assert.False(f.A.Frame.OwnShips.Single(s=>s.Condition.ShipId == "ship.equipment-cruiser").Unarmed);
        Assert.Contains("Authoritative Unarmed=False",ReviewTextRenderer.RenderInspection(f.A,"ship.equipment-cruiser"));
    }
    [Fact]
    public void OutsideEncounterAndUnarmedAreActualFactsWithoutLiveReadiness()
    {
        var f=new ActualReviewFixture();
        Assert.All(f.A.Frame.OwnShips.SelectMany(s=>s.Weapons),w=>Assert.Equal(CampaignWeaponAvailabilityStatus.OutsideEncounter,w.Availability.Status));
        var own=ReviewTextRenderer.RenderInspection(f.A,"ship.equipment-cruiser");
        Assert.Contains("Outside encounter: no battle-round context or live firing readiness",own);
        var unarmed=f.A.Frame.OwnShips.Single(s=>s.Condition.ShipId == "ship.equipment-surveyor");
        Assert.True(unarmed.Unarmed); Assert.Contains("Authoritative Unarmed=True",ReviewTextRenderer.RenderInspection(f.A,unarmed.Condition.ShipId));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TargetDependentNeverPromisesAShotEvenWhenAllEnemyTargetsAreHidden(bool hide)
    {
        var f=new ActualReviewFixture();
        if(hide) f.Cloak(ActorB,FleetB);
        f.Allocate();
        if(hide) Assert.Empty(f.A.Frame.Battle!.VisibleEnemies);
        Assert.All(f.A.Frame.OwnShips.SelectMany(s=>s.Weapons),w=>Assert.Equal(CampaignWeaponAvailabilityStatus.TargetDependent,w.Availability.Status));
        var text=ReviewTextRenderer.Render(f.A);
        Assert.Contains("Target dependent: no listed own-state blocker; no shot promised.",text);
        Assert.DoesNotContain("backend does not supply this projection",text);
        Assert.DoesNotContain("shots available",text); Assert.DoesNotContain("ready to fire",text);
        if(hide) Assert.DoesNotContain("ship.cardassian-scout",text);
    }
    [Fact]
    public void CompletedEncounterDisplaysItsSuppliedCompletionBlockerAndHasNoFurtherRound()
    {
        var f=new ActualReviewFixture(); f.Allocate(); f.Complete();
        Assert.All(f.A.Frame.OwnShips.SelectMany(s=>s.Weapons),w=>Assert.True(w.Availability.OwnBlockers.HasFlag(CampaignWeaponOwnBlockers.EncounterComplete)));
        var text=ReviewTextRenderer.Render(f.A);
        Assert.Contains("Encounter complete; no further round",text); Assert.Contains("EncounterComplete",text);
        Assert.False(f.A.ReviewRound());
    }
    [Fact]
    public void GenericTypedHistoricalFallbackIsRenderedWithoutInferringDestructionHidingRangeOrArc()
    {
        var f=new ActualReviewFixture(p=>{
            var g=p.Campaign!.InitialState; var catalogue=p.Campaign.Definitions.AuthoredTacticalEquipment!;
            var ship=Clone(g.Ships!.Single(s=>s.ShipId == "ship.cardassian-scout"));
            ship.ShipId="ship.additional-opponent"; ship.FleetId="fleet.additional-opponent"; ship.OwnerCivilizationId="civ.dominion"; g.Ships!.Add(ship);
            var fleet=Clone(g.Fleets!.Single(s=>s.FleetId == FleetB)); fleet.FleetId=ship.FleetId; fleet.OwnerCivilizationId=ship.OwnerCivilizationId;
            fleet.ShipIds=new List<string>{ship.ShipId}; g.Fleets!.Add(fleet);
            foreach(var pair in new[]{("ship.cardassian-scout",ship.ShipId),(FleetB,fleet.FleetId)})
            { var id=Clone(g.EntityIdentities.Single(e=>e.EntityId == pair.Item1)); id.EntityId=pair.Item2; g.EntityIdentities.Add(id); }
            var provision=Clone(catalogue.InitialShipProvisions!.Single(s=>s.ShipId == "ship.cardassian-scout"));
            provision.ShipId=ship.ShipId; provision.OwnerCivilizationId=ship.OwnerCivilizationId; catalogue.InitialShipProvisions!.Add(provision);
            g.Ships!.Single(s=>s.ShipId == "ship.cardassian-scout").CurrentHull=1;
            foreach(var fit in catalogue.Fits!) { fit.EvasionBps=0; foreach(var w in fit.Weapons!) { w.AccuracyBps=10000; w.CadenceRounds=1; } }
            p.Rules!.DistantTorpedoHitPenaltyBps=0; p.Rules.HoldDefenseBps=0;
        });
        var encounter=f.Allocate(); var third=new ReviewController(f.Host,"civ.dominion"); third.SelectEncounter(encounter);
        f.A.EditFleet(FleetA,CampaignFleetManeuver.Hold,FleetB);
        f.Send(f.A); f.Send(f.B); f.Send(third); f.A.Refresh(); third.Refresh();
        f.Send(f.A); f.Send(third); f.A.Refresh();
        var entry=f.A.Frame.Battle!.Reports.First(e=>e.FocusFallbackCause.HasValue);
        Assert.Equal(CampaignFocusFallbackCause.PreferredFocusUnavailableForOpportunity,entry.FocusFallbackCause);
        var text=ReviewTextRenderer.RenderReport(f.A);
        Assert.Contains("Preferred focus unavailable for that recorded opportunity; a lawful alternative was used",text);
        Assert.Contains("No shot promised; specific historical reason not supplied",text);
        Assert.DoesNotContain("focus destroyed",text,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("focus hidden",text,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("focus-fallback:",text);
        static T Clone<T>(T v)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v))!;
    }
    [Fact]
    public void EnemyInspectionCannotAcquireOwnAvailabilityBlockersOrPackageStocks()
    {
        var f=new ActualReviewFixture(); f.Allocate();
        var enemy=f.A.Frame.Battle!.VisibleEnemies.Single();
        var text=ReviewTextRenderer.RenderInspection(f.A,enemy.ShipId);
        Assert.DoesNotContain("availability",text,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unarmed",text); Assert.DoesNotContain("ammunition",text); Assert.DoesNotContain("package",text);
        Assert.Equal(new[]{"FleetId","Hull","MaxHull","Shields","ShipId","Type"},typeof(CampaignTacticalCondition).GetProperties().Select(p=>p.Name).OrderBy(p=>p,StringComparer.Ordinal));
    }
}
