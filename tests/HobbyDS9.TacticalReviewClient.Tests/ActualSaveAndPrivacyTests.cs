using System.Globalization;
using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;
using static HobbyDS9.TacticalReviewClient.Tests.ActualReviewFixture;

namespace HobbyDS9.TacticalReviewClient.Tests;

public sealed class ActualSaveAndPrivacyTests
{
    [Fact]
    public void SavedAcceptedCommitmentRestoresOwnWaitingOrdersWithoutOpposingLeak()
    {
        var f=new ActualReviewFixture(); f.Allocate();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,FleetB)); f.Send(f.A);
        var a=ReviewTextRenderer.Render(f.A); f.B.Refresh(); var b=ReviewTextRenderer.Render(f.B);
        var saved=f.A.SaveCommitted(); Assert.True(f.A.LoadCommitted(saved)); f.RebindB();
        Assert.Equal(a,ReviewTextRenderer.Render(f.A)); Assert.Equal(b,ReviewTextRenderer.Render(f.B));
        Assert.Equal(ReviewMode.Waiting,f.A.Mode); f.Send(f.B); f.A.Refresh();
        Assert.Equal(2,f.A.Frame.Battle!.Round);
    }
    [Fact]
    public void CommittedSaveKeepsUnsentDraftLocallyButSuccessfulLoadRequiresDiscardConsent()
    {
        var f=new ActualReviewFixture(); f.Allocate();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Flank,FleetB));
        var saved=f.A.SaveCommitted(); Assert.True(f.A.Dirty);
        Assert.False(f.A.LoadCommitted(saved)); Assert.True(f.A.Dirty);
        Assert.True(f.A.LoadCommitted(saved,true)); Assert.False(f.A.Dirty);
        Assert.Equal(ReviewMode.Orders,f.A.Mode); Assert.Empty(f.A.Frame.Battle!.OwnCommittedOrders);
        Assert.DoesNotContain("Unsent",ReviewTextRenderer.Render(f.A));
    }
    [Fact]
    public void FailedSavePreservesDraftAndThePriorValidCheckpoint()
    {
        var f=new ActualReviewFixture(); f.Allocate(); var saved=f.A.SaveCommitted();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Flank,FleetB));
        var frame=f.A.Frame; f.Codec.FailSaveCopy=true;
        Assert.Throws<IOException>(()=>f.A.SaveCommitted()); Assert.Same(frame,f.A.Frame); Assert.True(f.A.Dirty);
        f.Codec.FailSaveCopy=false; Assert.True(f.A.LoadCommitted(saved,true)); Assert.False(f.A.Dirty);
    }
    [Fact]
    public void InvalidSerializedLoadPreservesRunningSessionDraftAndGeneration()
    {
        var f=new ActualReviewFixture(); f.Allocate(); var saved=f.A.SaveCommitted();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,FleetB));
        var frame=f.A.Frame; f.Codec.ForgeSaveIdentity=true;
        Assert.Throws<ArgumentException>(()=>f.A.LoadCommitted(saved,true));
        Assert.Same(frame,f.A.Frame); Assert.True(f.A.Dirty); Assert.True(f.A.Refresh());
        Assert.Equal(frame.Generation,f.A.Frame.Generation);
        f.Codec.ForgeSaveIdentity=false; f.Send(f.A); Assert.Equal(ReviewMode.Waiting,f.A.Mode);
    }
    [Fact]
    public void SuccessfulLoadStartsANewGenerationAndIgnoresPriorCallbacksAndWork()
    {
        var f=new ActualReviewFixture(); f.Allocate(); var saved=f.A.SaveCommitted();
        var work=Begin(f.A); var receipt=f.Host.Execute(work); Assert.True(f.A.Receive(receipt));
        var generation=f.A.Frame.Generation; Assert.True(f.A.LoadCommitted(saved));
        Assert.Equal(generation+1,f.A.Frame.Generation); Assert.Equal(ReviewMode.Orders,f.A.Mode);
        Assert.False(f.A.Receive(receipt)); Assert.Throws<InvalidOperationException>(()=>f.Host.Execute(work));
        Assert.False(f.A.Frame.Battle!.OwnCommitmentAccepted);
    }
    [Fact]
    public void NavigationKeepsDraftUntilExplicitResetAndReviewCancelKeepsIt()
    {
        var f=new ActualReviewFixture(); var encounter=f.Allocate();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Flank,FleetB));
        Assert.False(f.A.SelectEncounter(encounter)); Assert.True(f.A.Dirty);
        Assert.True(f.A.ReviewRound()); Assert.True(f.A.CancelReview()); Assert.True(f.A.Dirty);
        Assert.True(f.A.ResetDraft()); Assert.False(f.A.Dirty); Assert.True(f.A.SelectEncounter(encounter));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SavedRoundReportsAndStocksMatchAfterActualSerializationAndReload(int rounds)
    {
        var f=new ActualReviewFixture(); f.Allocate();
        for(var r=0;r<rounds;r++) f.Round();
        var render=ReviewTextRenderer.Render(f.A); var report=ReviewTextRenderer.RenderReport(f.A);
        var save=f.A.SaveCommitted(); Assert.True(f.A.LoadCommitted(save));
        Assert.Equal(render,ReviewTextRenderer.Render(f.A)); Assert.Equal(report,ReviewTextRenderer.RenderReport(f.A));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoActualBattlesPreserveDepletedStocksWithoutBetweenBattleRefill(bool reload)
    {
        var f=new ActualReviewFixture(); f.Allocate(); f.Complete(); f.Finalize();
        var stock=JsonSerializer.Serialize(f.A.Frame.OwnShips.Select(x=>new{x.Condition.ShipId,x.Condition.Hull,x.Condition.Shields,Ammo=x.Weapons.Select(w=>w.Ammunition)}));
        var torpedo=f.A.Frame.OwnShips.Single(x=>x.Condition.ShipId == "ship.equipment-cruiser").Weapons.Single(w=>w.Class == TacticalWeaponClass.Torpedo);
        Assert.True(torpedo.Ammunition < 7);
        if(reload) { var save=f.A.SaveCommitted(); Assert.True(f.A.LoadCommitted(save)); f.RebindB(); }
        f.B.Refresh(); f.Allocate();
        Assert.Equal(stock,JsonSerializer.Serialize(f.A.Frame.OwnShips.Select(x=>new{x.Condition.ShipId,x.Condition.Hull,x.Condition.Shields,Ammo=x.Weapons.Select(w=>w.Ammunition)})));
        Assert.Equal(TacticalRange.Distant,Assert.Single(f.A.Frame.Battle!.Relations).Range);
        f.Complete(); f.Finalize();
        Assert.Equal(0,f.A.Frame.OwnShips.Single(x=>x.Condition.ShipId == "ship.equipment-cruiser").Weapons.Single(w=>w.Class == TacticalWeaponClass.Torpedo).Ammunition);
    }
    [Theory]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    [InlineData("tr-TR")]
    public void ActualCanonicalReverseRepeatedAndReloadedSessionsHaveIdenticalPrivateSaves(string culture)
    {
        var old=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
            var runs=new[]{new ActualReviewFixture(),new ActualReviewFixture(),new ActualReviewFixture(),new ActualReviewFixture()};
            foreach(var f in runs) f.Allocate();
            for(var round=1;round<=3;round++)
            {
                runs[0].Round(maneuver:round == 2 ? CampaignFleetManeuver.Flank : null);
                runs[1].Round(true,round == 2 ? CampaignFleetManeuver.Flank : null);
                runs[2].Round(maneuver:round == 2 ? CampaignFleetManeuver.Flank : null);
                var resumed=runs[3]; resumed.A.Refresh(); resumed.B.Refresh();
                if(round == 2)
                {
                    Assert.True(resumed.A.EditFleet(FleetA,CampaignFleetManeuver.Flank,resumed.A.Frame.Battle!.StandingOrders.Single().FocusFleetId));
                    Assert.True(resumed.B.EditFleet(FleetB,CampaignFleetManeuver.Flank,resumed.B.Frame.Battle!.StandingOrders.Single().FocusFleetId));
                }
                resumed.Send(resumed.A); var wait=resumed.A.SaveCommitted(); resumed.A.LoadCommitted(wait); resumed.RebindB();
                // The other actor's unsent local edit is deliberately not in a campaign save.
                if(round == 2) Assert.True(resumed.B.EditFleet(FleetB,CampaignFleetManeuver.Flank,resumed.B.Frame.Battle!.StandingOrders.Single().FocusFleetId));
                resumed.Send(resumed.B); resumed.A.Refresh();
                foreach(var f in runs) f.A.SaveCommitted();
                Assert.All(runs,f=>Assert.Equal(runs[0].Codec.LastPrivateSerializedSave,f.Codec.LastPrivateSerializedSave));
                Assert.All(runs,f=>Assert.Equal(ReviewTextRenderer.Render(runs[0].A),ReviewTextRenderer.Render(f.A)));
                Assert.All(runs,f=>Assert.Equal(ReviewTextRenderer.RenderReport(runs[0].A),ReviewTextRenderer.RenderReport(f.A)));
            }
            foreach(var f in runs) { f.Finalize(); f.A.SaveCommitted(); }
            Assert.All(runs,f=>Assert.Equal(runs[0].Codec.LastPrivateSerializedSave,f.Codec.LastPrivateSerializedSave));
        }
        finally { CultureInfo.CurrentCulture=old; }
    }
    [Fact]
    public void RendererShowsBackendSuppliedOwnPackagesAndFourSectorsWithoutPooling()
    {
        var f=new ActualReviewFixture(); f.Allocate(); var text=ReviewTextRenderer.Render(f.A);
        Assert.Contains("Fore 18/",text); Assert.Contains("Port 25/",text); Assert.Contains("Starboard 12/",text); Assert.Contains("Aft 0/",text);
        Assert.Contains("Supplied ammunition 7/12",text); Assert.Contains("No installed weapon packages.",text);
        Assert.Contains("Readiness explanation unavailable",text);
        f.Round(); var current=f.A.Frame.OwnShips.Single(s=>s.Condition.ShipId == "ship.equipment-cruiser").Weapons.Single(w=>w.Class == TacticalWeaponClass.Torpedo);
        Assert.True(current.Ammunition < 7); Assert.Contains($"Supplied ammunition {current.Ammunition}/{current.MagazineCapacity}",ReviewTextRenderer.Render(f.A));
    }
    [Theory]
    [InlineData("none")]
    [InlineData("mixed")]
    [InlineData("full")]
    [InlineData("mutual")]
    public void RealCloakObservationsFilterHiddenShipsFocusAndReportsAcrossReload(string cloak)
    {
        var f=new ActualReviewFixture(cloak == "full" || cloak == "mutual" ? RemoveSurveyor : null);
        if(cloak != "none") f.Cloak(ActorA,FleetA);
        if(cloak == "mutual") f.Cloak(ActorB,FleetB);
        f.Allocate();
        var a=ReviewTextRenderer.Render(f.A); var b=ReviewTextRenderer.Render(f.B);
        if(cloak == "mixed")
        {
            Assert.Equal("ship.equipment-surveyor",Assert.Single(f.B.Frame.Battle!.VisibleEnemies).ShipId);
            Assert.DoesNotContain("ship.equipment-cruiser",b);
            Assert.True(f.B.InspectShip("ship.equipment-surveyor")); Assert.False(f.B.InspectShip("ship.equipment-cruiser"));
        }
        if(cloak == "full" || cloak == "mutual")
        {
            Assert.Empty(f.B.Frame.Battle!.VisibleEnemies); Assert.Empty(f.B.Frame.Battle.LegalFocusFleetIds);
            Assert.DoesNotContain(FleetA,b); Assert.DoesNotContain("ship.equipment-cruiser",b);
            Assert.Contains("No visible hostile ships",b);
        }
        if(cloak == "mutual")
        {
            Assert.Empty(f.A.Frame.Battle!.VisibleEnemies); Assert.DoesNotContain(FleetB,a);
        }
        var ammo=f.A.Frame.OwnShips.SelectMany(s=>s.Weapons).Single(w=>w.Class == TacticalWeaponClass.Torpedo).Ammunition;
        var saved=f.A.SaveCommitted(); f.A.LoadCommitted(saved); f.RebindB();
        Assert.Equal(a,ReviewTextRenderer.Render(f.A));
        f.Round();
        Assert.All(f.A.Frame.OwnShips,s=>Assert.False(s.Cloaked));
        Assert.Contains(f.B.Frame.Battle!.VisibleEnemies,s=>s.ShipId == "ship.equipment-cruiser");
        if(cloak != "none")
            Assert.DoesNotContain("ship.equipment-cruiser",ReviewTextRenderer.RenderReport(f.B,1));
        if(cloak == "mutual")
            Assert.Equal(ammo,f.A.Frame.OwnShips.SelectMany(s=>s.Weapons).Single(w=>w.Class == TacticalWeaponClass.Torpedo).Ammunition);
        else if(cloak == "full" || cloak == "mixed")
            Assert.Contains("IncomingTorpedo",ReviewTextRenderer.RenderReport(f.B,1));
    }
    [Fact]
    public void EnemyInspectionAndReportsNeverRenderItsPackageReserveOrFreshOrder()
    {
        var f=new ActualReviewFixture(); f.Allocate(); f.Round();
        var enemy=Assert.Single(f.A.Frame.Battle!.VisibleEnemies);
        Assert.True(f.A.InspectShip(enemy.ShipId));
        var text=ReviewTextRenderer.RenderInspection(f.A,enemy.ShipId);
        foreach(var weapon in f.B.Frame.OwnShips.SelectMany(s=>s.Weapons))
            Assert.DoesNotContain(weapon.WeaponId,text);
        Assert.DoesNotContain("ammunition",text); Assert.DoesNotContain("power",text);
        Assert.DoesNotContain("maneuver",text); Assert.DoesNotContain("package",text);
        Assert.DoesNotContain("Own accepted "+FleetB,text);
        Assert.Equal(new[]{"FleetId","Hull","MaxHull","Shields","ShipId","Type"},typeof(CampaignTacticalCondition).GetProperties().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal));
        Assert.All(f.A.Frame.Battle.Reports.Where(e=>e.OwnWeaponId != null),e=>Assert.Contains(f.A.Frame.OwnShips,s=>s.Weapons.Any(w=>w.WeaponId == e.OwnWeaponId)));
    }
    [Fact]
    public void PrivateSaveCarrierIsNotExposedByPresentationFramesReceiptsOrHandles()
    {
        var types=new[]{typeof(ReviewFrame),typeof(ReviewController),typeof(ReviewReceipt),typeof(ReviewSaveHandle)};
        Assert.All(types,t=>Assert.DoesNotContain(t.GetProperties(BindingFlags.Public|BindingFlags.Instance),p=>p.PropertyType == typeof(CampaignTacticalReviewSaveDto)));
        var f=new ActualReviewFixture(); f.Allocate(); var saved=f.A.SaveCommitted();
        Assert.Equal("Private committed review checkpoint",saved.ToString());
        Assert.DoesNotContain("RandomState",ReviewTextRenderer.Render(f.A));
        Assert.DoesNotContain("Commitments",ReviewTextRenderer.Render(f.A));
        Assert.Contains("Campaign",f.Codec.LastPrivateSerializedSave!); // Trusted test-host evidence only.
    }
    [Fact]
    public void PublicClientAssemblyIsPortableAndNeverReferencesSimulationOrUnityDirectly()
    {
        var assembly=typeof(ReviewController).Assembly;
        var references=assembly.GetReferencedAssemblies().Select(r=>r.Name).ToArray();
        Assert.DoesNotContain("HobbyDS9.Simulation",references);
        Assert.DoesNotContain(references,s=>s!.StartsWith("Unity",StringComparison.Ordinal));
        Assert.Contains("HobbyDS9.Application",references);
        Assert.Contains("NETStandard,Version=v2.1",assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()!.FrameworkName);
    }
}
