using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class CombatPlaytestTests
{
    readonly ReviewNewtonsoftCodec codec=new();
    readonly Xunit.Abstractions.ITestOutputHelper output;
    public CombatPlaytestTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    byte[][] Inputs(string folder="combat-playtest-v1")=>CombatPlaytestOptions.InputNames().Select(n=>File.ReadAllBytes(
        Path.Combine(AppContext.BaseDirectory,"scenarios",folder,n+".json"))).ToArray();
    CampaignSingleplayerDemoPackageDto Package(ulong? seed=null)
    {
        var bytes=Inputs();
        if(seed.HasValue)
        {
            // Separate test-only source bytes/identity; shipped starting RNG is retained.
            var scenario=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes[4]));
            scenario["InitialState"]!["RngState"]!["State"]=new Newtonsoft.Json.Linq.JValue(seed.Value);
            bytes[4]=System.Text.Encoding.UTF8.GetBytes(scenario.ToString());
        }
        return CombatPlaytestOptions.CreatePackage(bytes,codec);
    }
    byte[] Wire(ICampaignSingleplayerDemoSession app)=>codec.EncodeDemoRoot(app.ExportDemoSave());

    byte[] Journey(bool reload,ulong? seed=null)
    {
        var p=Package(seed);var app=CampaignSingleplayerDemoApplication.StartPackage(p);var human=p.HumanActorId!;
        Assert.Single(app.GetKnownContacts(human));
        Assert.Equal(CampaignTravelIntent.Leave,Assert.Single(app.GetOwnTravelOrders(human)).Intent);
        Assert.Equal(new[]{"nav.bajor"},Assert.Single(app.GetOwnTravelOrders(human)).RouteNodeIds);
        int advances=0;
        while(app.CaptureStatus().Phase==SessionPhase.Planning&&advances<3)
        {
            var stamp=app.CaptureStatus();Assert.True(app.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);advances++;
        }
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);Assert.InRange(advances,1,3);
        var id=Assert.Single(app.GetEncounters(human));var pending=app.ExportDemoSave();var battle=Assert.Single(pending.Integrated!.Physical!.Core.Battles);
        Assert.Equal(id,battle.State.EncounterId);Assert.Equal(p.OpponentActorId,Assert.Single(battle.Commitments).ActorId);
        var observed=app.Observe(id,human);Assert.False(observed.OwnCommitmentAccepted);Assert.Empty(observed.Reports);
        Assert.False(app.CaptureStatus().CanFinalizeBatch);
        var startingAmmo=battle.State.Formations.SelectMany(f=>f.Ships).SelectMany(s=>s.Weapons).Sum(w=>w.Ammunition);
        var startingHull=battle.State.Formations.SelectMany(f=>f.Ships).Sum(s=>s.Hull);Assert.True(startingAmmo>0);
        int rounds=0;
        while(!app.Observe(id,human).Complete&&rounds<3)
        {
            if(reload)
            {
                var before=Wire(app);app=CampaignSingleplayerDemoApplication.LoadSave(p,codec.DecodeDemoRoot(before));Assert.Equal(before,Wire(app));
            }
            var o=app.Observe(id,human);Assert.False(o.OwnCommitmentAccepted);
            Assert.True(app.CommitRound(new(){ActorId=human,CommandId="command.combat-test.round."+o.Round,EncounterId=id,
                ExpectedTurn=o.Turn,ExpectedRound=o.Round,ExpectedBoundaryRevision=o.BoundaryRevision,
                Replacements=o.StandingOrders.Select(s=>new CampaignTacticalFleetOrderDto{FleetId=s.FleetId,Maneuver=CampaignFleetManeuver.Charge,
                    FocusFleetId=o.LegalFocusFleetIds.FirstOrDefault()}).ToList()}).Accepted);
            rounds++;
        }
        var final=app.Observe(id,human);Assert.True(final.Complete);Assert.InRange(rounds,1,3);
        var end=Assert.Single(app.ExportDemoSave().Integrated!.Physical!.Core.Battles).State;
        var ammo=end.Formations.SelectMany(f=>f.Ships).SelectMany(s=>s.Weapons).Sum(w=>w.Ammunition);Assert.True(ammo<startingAmmo);
        Assert.All(end.Formations.SelectMany(f=>f.Ships).SelectMany(s=>s.Weapons),w=>Assert.True(w.Ammunition>=0));
        Assert.True(end.Formations.SelectMany(f=>f.Ships).Sum(s=>s.Hull)<startingHull);
        Assert.Contains(final.Reports,r=>r.Kind=="ShieldImpact"&&r.Amount>0&&r.Sector.HasValue);
        Assert.Contains(final.Reports,r=>r.Kind=="HullDamage"&&r.Amount>0);
        Assert.True(app.CaptureStatus().CanFinalizeBatch);
        if(reload){var before=Wire(app);app=CampaignSingleplayerDemoApplication.LoadSave(p,codec.DecodeDemoRoot(before));Assert.Equal(before,Wire(app));}
        var status=app.CaptureStatus();Assert.True(app.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted);
        Assert.Equal(SessionPhase.Planning,app.CaptureStatus().Phase);Assert.Empty(app.GetEncounters(human));
        var persisted=app.ExportDemoSave().Integrated!.Physical!.Core;
        foreach(var ship in end.Formations.SelectMany(f=>f.Ships).Where(s=>s.Hull>0))
        {
            Assert.Equal(ship.Hull,persisted.Campaign.GameState.Ships!.Single(s=>s.ShipId==ship.ShipId).CurrentHull);
            var pool=persisted.Pools.Single(p=>p.ShipId==ship.ShipId);
            foreach(var stock in pool.Ammunition)Assert.Equal((long?)ship.Weapons.Single(w=>w.WeaponId==stock.WeaponId).Ammunition,stock.Stock);
            foreach(var shield in pool.Shields)Assert.Equal((long?)ship.Shields.Single(s=>s.Sector==shield.Sector).Current,shield.Current);
        }
        var after=Wire(app);Assert.Equal(after,Wire(CampaignSingleplayerDemoApplication.LoadSave(p,codec.DecodeDemoRoot(after))));
        output.WriteLine("Package {0}",p.PackageHash);
        output.WriteLine("Ordinary advances={0}; allocated={1}; joint rounds={2}; finite ammunition spent={3}; hull lost={4}",advances,id,rounds,
            startingAmmo-ammo,startingHull-end.Formations.SelectMany(f=>f.Ships).Sum(s=>s.Hull));
        return after;
    }
    [Fact]public void OrdinaryTravelAllocatesCombatAndHumanRoundsSpendRealStocksWithReplayParity()
        =>Assert.Equal(Journey(false),Journey(true));

    [Theory][InlineData(42UL)][InlineData(43UL)]
    public void SeedVariantsKeepTheContactWindowAndJointRoundReplay(ulong seed)
        =>Assert.Equal(Journey(false,seed),Journey(true,seed));

    [Fact]public void ActualUiModelColdSaveRetainsThePendingHumanPlannerInItsSeparateStore()
    {
        var folder=Path.Combine(Path.GetTempPath(),"hobby-combat-test-"+Guid.NewGuid().ToString("N"));
        try
        {
            var package=Package();var store=new ReviewDemoSlotStore(Path.Combine(folder,CombatPlaytestOptions.SaveDirectory),codec);
            var model=new ReviewUiModel(package,codec,store);int turns=0;
            while(model.Current.Frame.Status.Phase==SessionPhase.Planning&&turns++<3)Assert.True(model.Advance());
            var battle=model.Current.Frame.Battle!;Assert.NotNull(battle);Assert.False(battle.OwnCommitmentAccepted);
            Assert.True(model.Save("combat-1"));var root=codec.EncodeDemoRoot(store.Read("combat-1"));
            var cold=new ReviewUiModel(package,codec,store);Assert.True(cold.Load("combat-1",true));
            Assert.Equal(battle.Round,cold.Current.Frame.Battle!.Round);Assert.False(cold.Current.Frame.Battle.OwnCommitmentAccepted);
            Assert.True(cold.Save("combat-1",true));Assert.Equal(root,codec.EncodeDemoRoot(store.Read("combat-1")));
        }
        finally
        {
            if(Directory.Exists(folder)&&Path.GetFullPath(folder).StartsWith(Path.Combine(Path.GetTempPath(),"hobby-combat-test-"),StringComparison.Ordinal))
                Directory.Delete(folder,true);
        }
    }

    [Fact]public void ExplicitProfileNeverRebindsNormalDemoSaves()
    {
        var standard=Inputs("singleplayer-demo-v1");Assert.Throws<ArgumentException>(()=>CombatPlaytestOptions.CreatePackage(standard,codec));
        var old=ReviewExactPackageLoader.LoadDemo(standard[0],standard[1],standard[2],standard[3],standard[4],codec);
        var p=Package();Assert.NotEqual(old.PackageHash,p.PackageHash);
        var original=CampaignSingleplayerDemoApplication.StartPackage(old).ExportDemoSave();
        Assert.Throws<ArgumentException>(()=>CampaignSingleplayerDemoApplication.LoadSave(p,original));
        Assert.Throws<ArgumentException>(()=>CampaignSingleplayerDemoApplication.LoadSave(old,CampaignSingleplayerDemoApplication.StartPackage(p).ExportDemoSave()));
        Assert.Equal("HobbyDS9CombatPlaytestOnly-v1",CombatPlaytestOptions.SaveDirectory);
    }
}
