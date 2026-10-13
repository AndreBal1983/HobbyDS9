using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignWeaponExplanationTests
{
    private const string A="civ.federation",B="civ.cardassian";
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    private static T Copy<T>(T value)=>JsonSerializer.Deserialize<T>(Json(value))!;
    private static ICampaignTacticalReviewSession Start(CampaignTacticalReviewPackageDto p)=>CampaignTacticalReviewApplication.StartReviewPackage(p);
    private static string Allocate(ICampaignTacticalReviewSession app)
    {var s=app.CaptureStatus();Assert.True(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);return Assert.Single(app.GetEncounters(A));}
    private static CommitCampaignTacticalRoundDto Command(ICampaignTacticalReviewSession app,string id,string actor)
    {var v=app.Observe(id,actor);return new(){ActorId=actor,EncounterId=id,CommandId="command.explain."+actor+"."+v.Round,
        ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new()};}
    private static void Round(ICampaignTacticalReviewSession app,string id)
    {var a=Command(app,id,A);var b=Command(app,id,B);Assert.True(app.CommitRound(a).Accepted);Assert.True(app.CommitRound(b).Accepted);}
    [Theory]
    [InlineData("unpowered",CampaignWeaponOwnBlockers.PowerIncomplete)]
    [InlineData("empty",CampaignWeaponOwnBlockers.NoAmmunition)]
    [InlineData("cadence",CampaignWeaponOwnBlockers.CadenceWait)]
    [InlineData("damage",CampaignWeaponOwnBlockers.NoDamage)]
    public void ActualOwnConstraintExplainsWhyNoBatteryOpportunityIsGranted(string defect,CampaignWeaponOwnBlockers reason)
    {
        var p=CampaignTacticalReviewTests.Package();
        foreach(var fit in p.Campaign!.Definitions.AuthoredTacticalEquipment!.Fits!)
        foreach(var weapon in fit.Weapons!.Where(w=>w.Class==TacticalWeaponClass.Torpedo))
        {
            if(defect=="unpowered")weapon.AllocatedPower=0;
            if(defect=="cadence")weapon.InitialNextFireRound=2;
            if(defect=="damage")weapon.Damage=0;
        }
        if(defect=="empty")foreach(var pool in p.Campaign.Definitions.AuthoredTacticalEquipment.InitialShipProvisions!)foreach(var ammo in pool.InitialAmmunition!)ammo.Stock=0;
        var app=Start(p);var id=Allocate(app);var view=app.Observe(id,A);var weaponView=view.OwnShips.SelectMany(s=>s.Weapons).Single(w=>w.Class==TacticalWeaponClass.Torpedo);
        Assert.Equal(CampaignWeaponAvailabilityStatus.OwnStateBlocked,weaponView.Availability.Status);Assert.Equal(reason,weaponView.Availability.OwnBlockers);Assert.Equal(1,weaponView.Availability.Round);
        Assert.True(view.OwnShips.Single(s=>s.Condition.ShipId=="ship.equipment-surveyor").Unarmed);
        Round(app,id);var resolved=app.Observe(id,A);
        Assert.DoesNotContain(resolved.Reports,r=>r.Round==1&&r.OwnWeaponId==weaponView.WeaponId&&(r.Kind=="FiringOpportunity"||r.Kind=="WeaponDischarged"));
        if(defect=="cadence")Assert.Equal(CampaignWeaponAvailabilityStatus.TargetDependent,resolved.OwnShips.SelectMany(s=>s.Weapons).Single(w=>w.Class==TacticalWeaponClass.Torpedo).Availability.Status);
    }
    [Fact]
    public void PlanningAndCompleteHaveNoReadinessPromiseAndCadenceComesFromTheCurrentBoundary()
    {
        var p=CampaignTacticalReviewTests.Package();var app=Start(p);
        Assert.All(app.InspectOwnShips(A).SelectMany(s=>s.Weapons),w=>{Assert.Equal(CampaignWeaponAvailabilityStatus.OutsideEncounter,w.Availability.Status);Assert.Null(w.Availability.Round);});
        var id=Allocate(app);var opening=app.Observe(id,A);
        Assert.All(opening.OwnShips.SelectMany(s=>s.Weapons),w=>Assert.Equal(CampaignWeaponAvailabilityStatus.TargetDependent,w.Availability.Status));
        Round(app,id);var torp=app.Observe(id,A).OwnShips.SelectMany(s=>s.Weapons).Single(w=>w.Class==TacticalWeaponClass.Torpedo);
        Assert.Equal(CampaignWeaponOwnBlockers.CadenceWait,torp.Availability.OwnBlockers);Assert.Equal(2,torp.Availability.Round);
        Round(app,id);Round(app,id);Assert.True(app.Observe(id,A).Complete);
        Assert.All(app.Observe(id,A).OwnShips.SelectMany(s=>s.Weapons),w=>{Assert.Equal(CampaignWeaponAvailabilityStatus.OwnStateBlocked,w.Availability.Status);Assert.True(w.Availability.OwnBlockers.HasFlag(CampaignWeaponOwnBlockers.EncounterComplete));});
        Assert.Equal(CampaignWeaponAvailabilityStatus.TargetDependent,opening.OwnShips.SelectMany(s=>s.Weapons).First().Availability.Status); // immutable old boundary.
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HiddenTargetAndAllHiddenSidesDoNotChangeOwnAvailabilityOrExposeOpponentPackages(bool cloakOwn)
    {
        var p=CampaignTacticalReviewTests.Package();
        if(cloakOwn)
        {
            const string unarmed="ship.equipment-surveyor";var game=p.Campaign!.InitialState;
            game.Ships!.RemoveAll(s=>s.ShipId==unarmed);game.EntityIdentities.RemoveAll(e=>e.EntityId==unarmed);
            game.Fleets!.Single(f=>f.FleetId=="fleet.federation-scout").ShipIds!.Remove(unarmed);
            p.Campaign.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.RemoveAll(s=>s.ShipId==unarmed);
        }
        var visible=Start(p);var hidden=Start(p);var status=hidden.CaptureStatus();
        Assert.True(hidden.SetApproachCloak(B,"command.hide.b","fleet.cardassian-scout",true,status.Turn,status.CampaignRevision).Accepted);
        if(cloakOwn){status=hidden.CaptureStatus();Assert.True(hidden.SetApproachCloak(A,"command.hide.a","fleet.federation-scout",true,status.Turn,status.CampaignRevision).Accepted);}
        var one=visible.Observe(Allocate(visible),A);var id=Allocate(hidden);var two=hidden.Observe(id,A);
        Assert.Empty(two.VisibleEnemies);Assert.Empty(two.Relations);Assert.Empty(two.LegalFocusFleetIds);
        var own1=one.OwnShips.SelectMany(s=>s.Weapons).Select(w=>(w.WeaponId,w.Availability.Status,w.Availability.OwnBlockers));
        var own2=two.OwnShips.SelectMany(s=>s.Weapons).Select(w=>(w.WeaponId,w.Availability.Status,w.Availability.OwnBlockers));Assert.Equal(own1,own2);
        Assert.DoesNotContain("ship.cardassian-scout",Json(two));Assert.DoesNotContain("fleet.cardassian-scout",Json(two));
        Assert.All(two.Reports,r=>Assert.Null(r.FocusFallbackCause));
        if(cloakOwn){Assert.Empty(hidden.Observe(id,B).VisibleEnemies);Assert.All(two.OwnShips,s=>Assert.True(s.Cloaked));}
        Round(hidden,id);Assert.DoesNotContain(hidden.Observe(id,A).Reports,r=>r.Kind=="FocusFallback");
        if(cloakOwn)foreach(var actor in new[]{A,B})Assert.DoesNotContain(hidden.Observe(id,actor).Reports,r=>r.Kind=="WeaponDischarged");
    }
    [Theory]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    [InlineData("tr-TR")]
    public void RejectedCommandsAndReloadKeepTypedObservationsExactlyStable(string culture)
    {
        var prior=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);var p=CampaignTacticalReviewTests.Package();var app=Start(p);var id=Allocate(app);Round(app,id);
            var before=Json(app.Observe(id,A));var save=Json(app.ExportSave());var bad=Command(app,id,A);bad.ExpectedRound--;
            Assert.False(app.CommitRound(bad).Accepted);Assert.Equal(save,Json(app.ExportSave()));Assert.Equal(before,Json(app.Observe(id,A)));
            var reload=CampaignTacticalReviewApplication.LoadReviewSave(p,Copy(app.ExportSave()));Assert.Equal(before,Json(reload.Observe(id,A)));
            CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;Assert.Equal(before,Json(reload.Observe(id,A)));
        }
        finally{CultureInfo.CurrentCulture=prior;}
    }
    [Fact]
    public void WeaponAdmissionPredicateMatchesEveryCombinationWithoutNewOpportunityRules()
    {
        foreach(var equipped in new[]{false,true})foreach(var power in new long[]{0,1})foreach(var damage in new long[]{0,1})
        foreach(var cadence in new[]{1,2})foreach(var ammo in new long[]{0,1})foreach(var kind in new[]{TacticalWeaponClass.Beam,TacticalWeaponClass.Torpedo})
        {
            var w=new TacticalWeaponV2Dto {Equipped=equipped,RequiredPower=1,AllocatedPower=power,Damage=damage,NextFireRound=cadence,Ammunition=ammo,Class=kind};
            var original=equipped&&power==1&&damage>0&&cadence<=1&&(kind!=TacticalWeaponClass.Torpedo||ammo>0);
            Assert.Equal(original,TacticalWeaponOwnConstraints.Blockers(w,1)==CampaignWeaponOwnBlockers.None);
        }
    }
}
