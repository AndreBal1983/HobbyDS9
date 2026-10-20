using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class PlayableFleetBindingTests : IDisposable
{
    private const string Actor="civ.federation";
    private readonly string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"hobby-playable-binding-"+Guid.NewGuid().ToString("N")));
    private readonly ReviewNewtonsoftCodec codec=new();
    private static string Folder=>Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1");
    private static CampaignPlayableFleetPackageDto Package()=>PlayableFleetPackageReader.Read(Folder);
    private ReviewPlayableFleetSlotStore Store(Action<UnityReviewWriteStage>? probe=null)=>new(root,codec,probe);
    private ReviewUiModel Model()=>new(Package(),codec,Store());
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    [Fact] public void ActualUnityByteLoaderMatchesBackendAndKeepsDisjointCompleteRoot()
    {
        byte[] Bytes(string name)=>File.ReadAllBytes(Path.Combine(Folder,name+".json"));
        var p=ReviewExactPackageLoader.LoadPlayableFleet(Bytes("playable"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
        Assert.Equal(Json(Package()),Json(p));
        Assert.Equal("87e57e759ad9ddbe0eebcecef86fb04a9c55c025ea28031205ac323c2c9c41c7",p.PackageHash);
        var model=new ReviewUiModel(p,codec,Store());Assert.True(model.PlayableFleet);Assert.True(model.Integrated);
        Assert.False(model.SwitchActor("civ.cardassian"));Assert.True(model.Save("one"));
        var saved=Store().Read("one");Assert.Equal(6,saved.FormatVersion);
        Assert.ThrowsAny<Exception>(()=>codec.DecodeDemoRoot(codec.EncodePlayableFleetRoot(saved)));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeFrontierRoot(codec.EncodePlayableFleetRoot(saved)));
        Assert.True(model.Load("one"));Assert.Equal(Json(saved),Json(Store().Read("one")));
        Assert.Single(Directory.GetFiles(root,"unity-playable-fleet-review-v6-*.json"));
    }
    [Fact] public void OrganizationUsesReviewedBoundaryAndReloadPreservesNamesAndStocks()
    {
        var m=Model();var old=m.Current.Frame;
        var ships=old.Strategy!.Ships.Select(s=>s.ShipId.Value).ToArray();
        Assert.True(m.OrganizeFleet(old,CampaignFleetOrganizationKind.Create,ships,null,"Test Task Force","command.binding.create"));
        Assert.False(m.OrganizeFleet(old,CampaignFleetOrganizationKind.Create,ships,null,"Stale","command.binding.stale"));
        var fleet=Assert.Single(m.NamedFleets());Assert.Equal("Test Task Force",fleet.DisplayName);
        var condition=Json(m.Current.Frame.OwnShips);
        Assert.True(m.Save("one"));var original=m.Current;Assert.True(m.Load("one"));
        Assert.NotSame(original,m.Current);Assert.Equal("Test Task Force",Assert.Single(m.NamedFleets()).DisplayName);
        Assert.Equal(condition,Json(m.Current.Frame.OwnShips));
        Assert.False(m.OrganizeFleet(old,CampaignFleetOrganizationKind.Rename,Array.Empty<string>(),fleet.Fleet.FleetId.Value,"Old session","command.binding.old"));
    }
    [Fact] public void SavesRetainUnsentResearchAndFailedReplacementKeepsCurrentSession()
    {
        var m=Model();var draft=m.ResearchInputs()!;draft.Adjust(Enum.GetValues<ScienceDiscipline>()[0],6);
        Assert.True(m.HasLocalEdits);Assert.True(m.Save("one"));Assert.Same(draft,m.ResearchDraft);Assert.True(m.HasLocalEdits);
        var original=m.Current;
        Assert.False(m.Load("absent",true));Assert.Same(original,m.Current);Assert.Same(draft,m.ResearchDraft);
        var bytes=File.ReadAllBytes(Path.Combine(root,"unity-playable-fleet-review-v6-one.json"));
        var broken=Store().Read("one");broken.Names.Add(new CampaignFleetNameDto {FleetId="fleet.federation-scout",DisplayName="Forged"});
        Store().Write("broken",broken,false);
        Assert.False(m.Load("broken",true));Assert.Same(original,m.Current);Assert.Same(draft,m.ResearchDraft);
        Assert.Equal(bytes,File.ReadAllBytes(Path.Combine(root,"unity-playable-fleet-review-v6-one.json")));
        Assert.True(m.Load("one",true));Assert.False(m.HasLocalEdits);
    }
    [Fact] public void FullPaidFleetBattleHistoryRoundTripsThroughActualCodecAndHost()
    {
        var package=Package();var final=PlayableFleetWalkthrough.Run(package,true);
        Store().Write("journey",final,false);
        var opened=PhysicalReviewHost.OpenPlayableFleetFromSlot(package,codec,Store(),"journey",Actor);
        Assert.True(opened.Succeeded);
        var expected=CampaignPlayableFleetApplication.LoadSave(package,final).GetBattleHistory(Actor);
        Assert.NotEmpty(expected);
        var before=File.ReadAllBytes(Path.Combine(root,"unity-playable-fleet-review-v6-journey.json"));
        Assert.Equal(Json(expected),Json(opened.Host!.CaptureBattleHistory(opened.Frame!)));
        Assert.Equal(Json(expected),Json(opened.Host.CaptureBattleHistory(opened.Frame!)));
        Assert.True(opened.Host.SaveCommittedToSlot("journey",true).Succeeded);
        Assert.Equal(before,File.ReadAllBytes(Path.Combine(root,"unity-playable-fleet-review-v6-journey.json")));
    }
    [Fact] public void ActualPlayerLoopRequiresEachHumanRoundAndPlaybackAcknowledgmentNeverMutatesAuthority()
    {
        var m=Model();
        Assert.True(m.Enqueue("colony.sol","ship-design.source-cruiser","command.ui.buy.1"));
        Assert.True(m.Enqueue("colony.sol","ship-design.source-cruiser","command.ui.buy.2"));
        for(int i=0;i<4;i++)Assert.True(m.Advance());
        var ships=m.Current.Frame.Strategy!.Ships.Select(s=>s.ShipId.Value).ToArray();
        Assert.True(m.OrganizeFleet(m.Current.Frame,CampaignFleetOrganizationKind.Create,ships,null,"Player task force","command.ui.assemble"));
        var fleet=Assert.Single(m.NamedFleets()).Fleet.FleetId.Value;
        var contact=m.Current.Frame.Travel!.KnownContacts.First(c=>c.Fresh);
        var offer=m.Current.FindKnownRoute(fleet,contact.LastSeenNodeId)!;
        Assert.True(m.SendPreviewedFollow(offer,contact.FleetId,contact.LastSeenTurn,"command.ui.follow"));
        for(int i=0;i<12&&m.Current.Frame.Status.Phase==SessionPhase.Planning;i++)Assert.True(m.Advance());
        Assert.Equal(SessionPhase.AwaitingCombat,m.Current.Frame.Status.Phase);
        var flow=new ReviewCombatFlow(m);
        Assert.Empty(flow.Playback!.Rounds);Assert.Null(flow.PendingPlayback);Assert.True(flow.CanCommitRound);
        int rounds=0;
        while(!m.Current.Frame.Battle!.Complete)
        {
            var observed=m.Current.Frame.Battle;
            foreach(var order in observed.StandingOrders.Where(o=>o.CanOrder))
                Assert.True(flow.DraftFleet(order.FleetId,CampaignFleetManeuver.Charge,observed.LegalFocusFleetIds.FirstOrDefault(),false));
            Assert.True(flow.CommitRound("command.ui.round."+observed.Round,true));rounds++;
            Assert.Equal(rounds,flow.Playback!.Rounds.Count);
            Assert.NotNull(flow.PendingPlayback);Assert.False(flow.CanCommitRound);Assert.False(flow.CanFinalize);
            Assert.False(flow.PlanNextRound());
            var playback=flow.Playback;var round=flow.PendingPlayback!.Round;
            Assert.True(m.Save("round",true));var before=Json(Store().Read("round"));
            Assert.True(flow.AcknowledgePlayback(playback!,round));
            Assert.False(flow.AcknowledgePlayback(playback!,round));
            Assert.True(m.Save("round",true));Assert.Equal(before,Json(Store().Read("round")));
            if(!m.Current.Frame.Battle.Complete){Assert.True(flow.PlanNextRound());Assert.True(flow.CanCommitRound);}
        }
        Assert.InRange(rounds,1,3);Assert.True(flow.CanFinalize);
        Assert.True(flow.FinalizeAftermath(true));Assert.False(flow.FinalizeAftermath(true));
        var history=Json(m.History());Assert.NotEmpty(m.History());
        Assert.True(m.Save("done"));Assert.True(m.Load("done"));Assert.Equal(history,Json(m.History()));
        Assert.Equal(SessionPhase.Planning,m.Current.Frame.Status.Phase);
    }
    [Fact] public void OrganizationDraftSurvivesSaveAndNavigationButNotSuccessfulSessionReplacement()
    {
        var m=Model();var workspace=new ReviewWorkspace(new ReviewCombatFlow(m));
        m.OrganizationName="Unsent name";m.OrganizationShips.Add(m.Current.Frame.Strategy!.Ships[0].ShipId.Value);
        Assert.True(m.HasLocalEdits);Assert.True(workspace.Navigate(ReviewWorkspacePage.Research));
        Assert.True(m.Save("one"));Assert.Equal("Unsent name",m.OrganizationName);Assert.Single(m.OrganizationShips);
        Assert.False(m.Advance());Assert.False(m.Load("one"));Assert.True(m.Load("one",true));
        Assert.False(m.HasLocalEdits);Assert.Empty(m.OrganizationShips);Assert.Equal("",m.OrganizationName);
    }
    [Theory]
    [InlineData(UnityReviewWriteStage.AfterPartialWrite)]
    [InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void FailedAtomicWritePreservesPriorSlot(UnityReviewWriteStage stage)
    {
        var m=Model();Assert.True(m.Save("one"));var prior=Store().Read("one");
        var path=Path.Combine(root,"unity-playable-fleet-review-v6-one.json");var bytes=File.ReadAllBytes(path);
        var failing=Store(s=>{if(s==stage)throw new IOException("Injected");});
        Assert.Throws<ReviewCheckpointStoreException>(()=>failing.Write("one",prior,true));
        Assert.Equal(bytes,File.ReadAllBytes(path));Assert.Empty(Directory.GetFiles(root,"*.tmp"));
    }
    [Fact] public void EliminatedHumanQueuesAllActualAiRoundsOnceAndLoadInvalidatesOldPlaybackCallbacks()
    {
        var m=new ReviewUiModel(ThreeSidePackage(true),codec,Store());Assert.True(m.Advance());
        var flow=new ReviewCombatFlow(m);Assert.True(flow.CommitRound("command.ui.eliminated",true));
        Assert.True(m.Current.Frame.Battle!.Complete);Assert.Equal(3,flow.Playback!.Rounds.Count);
        Assert.False(flow.CanCommitRound);Assert.False(flow.CanFinalize);
        var old=flow.Playback;Assert.True(flow.AcknowledgePlayback(old,1));Assert.False(flow.CanFinalize);
        Assert.True(m.Save("one"));Assert.True(m.Load("one"));
        Assert.False(flow.AcknowledgePlayback(old,2));
        var current=flow.Playback!;
        for(int round=1;round<=3;round++)
        {
            Assert.Equal(round,flow.PendingPlayback!.Round);Assert.False(flow.CanFinalize);
            Assert.True(flow.AcknowledgePlayback(current,round));Assert.False(flow.AcknowledgePlayback(current,round));
        }
        Assert.Null(flow.PendingPlayback);Assert.True(flow.CanFinalize);Assert.True(flow.FinalizeAftermath(true));
        Assert.False(flow.CanCommitRound);Assert.False(flow.FinalizeAftermath(true));
    }
    [Fact] public void VisualBindingUsesOnlyTheExactBoundaryDisclosureAndNeverInfersFromDisplayNames()
    {
        var own=new CampaignTacticalCondition("ship.own","fleet.own","Cruiser",10,10,new long[]{1,1,1,1});
        var enemy=new CampaignTacticalCondition("ship.hidden","fleet.enemy","Cruiser",10,10,new long[]{1,1,1,1});
        var identity=new CampaignPlaybackShipIdentity(enemy.ShipId,"civilization.cardassian","ship-design.travel-target");
        var before=new CampaignPlaybackBoundary(new[]{own},Array.Empty<CampaignTacticalCondition>(),Array.Empty<CampaignTacticalRelation>());
        var after=new CampaignPlaybackBoundary(new[]{own},new[]{enemy},Array.Empty<CampaignTacticalRelation>(),new[]{identity});
        Assert.Null(ReviewBattleVisualBinding.Identity(before,enemy.ShipId));Assert.Null(ReviewBattleVisualBinding.Identity(before,own.ShipId));
        Assert.Equal(Tuple.Create("civilization.cardassian","ship-design.travel-target"),ReviewBattleVisualBinding.Identity(after,enemy.ShipId));
        Assert.Null(ReviewBattleVisualBinding.Identity(before,enemy.ShipId));
        var inconsistent=new CampaignPlaybackBoundary(new[]{own},Array.Empty<CampaignTacticalCondition>(),Array.Empty<CampaignTacticalRelation>(),new[]{identity});
        Assert.Null(ReviewBattleVisualBinding.Identity(inconsistent,enemy.ShipId));
    }
    private static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(Json(value))!;
    static CampaignPlayableFleetPackageDto MultiEncounterPackage(bool aiOnly)
    {
        // Independent admission fixture: a dormant authored AI fights at a second location.
        // No scenario, balance or new gameplay class is added to the delivered package.
        var p = Package(); var physical = p.Integrated!.Physical!; var campaign = physical.CorePackage!.Campaign!;
        var game = campaign.InitialState; var equipment = campaign.Definitions.AuthoredTacticalEquipment!;
        game.Fleets!.Single(f => f.FleetId == "fleet.cardassian-scout").LocationNodeId = "nav.bajor";
        game.Fleets!.Single(f => f.FleetId == "fleet.federation-scout").LocationNodeId = aiOnly ? "nav.sol" : "nav.bajor";
        foreach (var original in game.Ships!.ToList())
        {
            if (aiOnly && original.OwnerCivilizationId == Actor) continue;
            var ship = Clone(original); ship.ShipId += ".capacity"; ship.FleetId += ".capacity";
            if (ship.OwnerCivilizationId != Actor) ship.OwnerCivilizationId = "civ.dominion";
            var fleet = Clone(game.Fleets!.Single(f => f.FleetId == original.FleetId));
            fleet.FleetId = ship.FleetId; fleet.OwnerCivilizationId = ship.OwnerCivilizationId;
            fleet.LocationNodeId = aiOnly ? "nav.bajor" : "nav.sol"; fleet.ShipIds = new() { ship.ShipId };
            game.Ships!.Add(ship); game.Fleets!.Add(fleet);
            foreach (var pair in new[] { (original.ShipId, ship.ShipId), (original.FleetId, fleet.FleetId) })
            {
                var identity = Clone(game.EntityIdentities.Single(i => i.EntityId == pair.Item1));
                identity.EntityId = pair.Item2; game.EntityIdentities.Add(identity);
            }
            var provision = Clone(equipment.InitialShipProvisions!.Single(s => s.ShipId == original.ShipId));
            provision.ShipId = ship.ShipId; provision.OwnerCivilizationId = ship.OwnerCivilizationId;
            equipment.InitialShipProvisions!.Add(provision);
            physical.InitialOrders!.Add(new CampaignTravelOrderDto { FleetId = fleet.FleetId,
                Intent = CampaignTravelIntent.StayEngage, RouteNodeIds = new() });
        }
        physical.InitialOrders!.ForEach(o => o.Intent = CampaignTravelIntent.StayEngage);
        physical.Hostilities!.Add(new CampaignDeclaredHostilityDto {
            CivilizationA = aiOnly ? "civ.cardassian" : Actor, CivilizationB = "civ.dominion" });
        // Contact grids are authored lawful fixture knowledge, including both independent locations.
        physical.InitialContacts!.Clear();
        foreach (var fleet in game.Fleets!)
        foreach (var civ in game.Civilizations!)
            if (physical.Hostilities.Any(h => h.CivilizationA == civ.CivilizationId && h.CivilizationB == fleet.OwnerCivilizationId
                || h.CivilizationB == civ.CivilizationId && h.CivilizationA == fleet.OwnerCivilizationId))
                physical.InitialContacts.Add(new CampaignContactKnowledgeDto {
                    ActorId = civ.CivilizationId, FleetId = fleet.FleetId, LastSeenNodeId = fleet.LocationNodeId, LastSeenTurn = 1,
                    TrackingNodeIds = campaign.Definitions.NavigationRegions!.Select(n => n.NodeId).ToList() });
        return p;
    }
    static CampaignPlayableFleetPackageDto ThreeSidePackage(bool eliminate)
    {
        var p = MultiEncounterPackage(true); p.ReplayOperationLimit = 5;
        var physical = p.Integrated!.Physical!; var campaign = physical.CorePackage!.Campaign!;
        var game = campaign.InitialState;
        game.Fleets!.Single(f => f.OwnerCivilizationId == Actor).LocationNodeId = "nav.bajor";
        if (eliminate) game.Ships!.Single(s => s.OwnerCivilizationId == Actor).CurrentHull = 1;
        var equipment = campaign.Definitions.AuthoredTacticalEquipment!;
        if (eliminate) equipment.InitialShipProvisions!.Single(s => s.OwnerCivilizationId == Actor).InitialShields!.ForEach(s => s.Current = 0);
        equipment.Fits!.ForEach(f => { f.EvasionBps = 0; f.Weapons!.ForEach(w => w.AccuracyBps = 10000); });
        physical.Hostilities!.Add(new CampaignDeclaredHostilityDto { CivilizationA = Actor, CivilizationB = "civ.dominion" });
        foreach (var pair in new[] { (Actor, "fleet.cardassian-scout.capacity"), ("civ.dominion", "fleet.federation-scout") })
            physical.InitialContacts!.Add(new CampaignContactKnowledgeDto {
                ActorId = pair.Item1, FleetId = pair.Item2, LastSeenNodeId = "nav.bajor", LastSeenTurn = 1,
                TrackingNodeIds = campaign.Definitions.NavigationRegions!.Select(n => n.NodeId).ToList() });
        return p;
    }
    public void Dispose()
    {
        var parent=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if(Path.GetDirectoryName(root)!=parent||!Path.GetFileName(root).StartsWith("hobby-playable-binding-",StringComparison.Ordinal))
            throw new InvalidOperationException("Unsafe cleanup path");
        if(Directory.Exists(root))Directory.Delete(root,true);
    }
}
