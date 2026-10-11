using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityClient;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class CombatOrchestrationTests
{
    private static (ScenarioDto Scenario, ICampaignSession App, StrategicClient Client, CombatOrchestrator Flow) Checkpoint(
        int count = 1, int aiOnly = 0, string human = "civ.federation", bool secondHuman = false)
    {
        var scenario = CampaignCombatFixture.Scenario(count, true);
        // Disable AI policy while arranging the fixture; controllers are changed
        // on the resulting checkpoint through validated save/load, never runtime state.
        foreach (var actor in scenario.InitialState.Civilizations!) actor.ControllerKind = ControllerKind.Human;
        foreach (var fleet in scenario.InitialState.Fleets!.Where(f => f.OwnerCivilizationId == human))
            if (int.Parse(fleet.FleetId.Split('-').Last(), System.Globalization.CultureInfo.InvariantCulture) < aiOnly)
                fleet.LocationNodeId = "system.sol";
        var app = CampaignApplication.StartScenario(scenario);
        Assert.Equal(TurnStatus.AwaitingCombat, app.AdvanceTurn(1, 0).Status);
        var save = app.ExportSave();
        foreach (var actor in save.GameState.Civilizations!)
            actor.ControllerKind = actor.CivilizationId == human || (secondHuman && actor.CivilizationId == "civ.cardassian") ? ControllerKind.Human : ControllerKind.AI;
        app = CampaignApplication.LoadSave(scenario, save);
        var client = new StrategicClient(app, dto => CampaignApplication.LoadSave(scenario, dto));
        return (scenario, app, client, new CombatOrchestrator(client));
    }
    [Fact]
    public void AiOnlyBatchAutoFinalizesWithoutPlayerInterruptionAndMatchesManualDefaultPath()
    {
        var (scenario, app, client, flow) = Checkpoint(2, 2);
        var manual = CampaignApplication.LoadSave(scenario, client.ExportSave());
        Assert.All(client.Pending(), r => Assert.False(CombatOrchestrator.HumanInvolved(client.Snapshot, r)));
        foreach (var request in manual.GetPendingCombatRequests())
        {
            var stamp = manual.CaptureSnapshot().Campaign;
            Assert.Equal(CombatSubmissionStatus.Buffered, manual.SubmitCombatResult(manual.ResolveCombat(request), stamp.TurnNumber, stamp.StateRevision).Status);
        }
        var before = manual.CaptureSnapshot().Campaign;
        Assert.Equal(TurnStatus.Completed, manual.FinalizeCombat(before.TurnNumber, before.StateRevision).Status);
        flow.Pump(); Assert.Null(flow.Error); Assert.False(flow.RequiresPlayer);
        Assert.Equal(SessionPhase.Planning, client.Snapshot.Campaign.Phase); Assert.Equal(2, client.RecentReports.Count);
        Assert.All(client.RecentReports, r => Assert.False(r.HumanInvolved)); Assert.Contains("2 AI battles resolved", flow.Notice);
        Assert.Equal(Json(manual.ExportSave()), Json(client.ExportSave()));
        var saved = Json(client.ExportSave()); flow.Pump(); flow.Pump();
        Assert.Equal(saved, Json(client.ExportSave())); Assert.Equal(2, client.RecentReports.Count);
    }
    [Theory]
    [InlineData("civ.cardassian")]
    [InlineData("civ.dominion")]
    public void ClassificationAndEditablePlansFollowPersistedHumanControllerInsteadOfFederation(string human)
    {
        var (_, app, client, flow) = Checkpoint(human: human);
        var request = client.Pending().Single(); var before = Json(client.ExportSave());
        Assert.True(CombatOrchestrator.HumanInvolved(client.Snapshot, request));
        flow.Pump(); Assert.True(flow.RequiresPlayer); Assert.Equal(request.EncounterId, flow.PendingHumanEncounter);
        Assert.Equal(before, Json(client.ExportSave()));
        var plan = Assert.Single(client.PlayerPlans(client.Snapshot, request));
        Assert.Equal(human, request.Participants.Single(p => p.FleetId == plan.FleetId).CivilizationId);
        Assert.Equal(Json(app.ResolveCombat(request)), Json(app.ResolveHumanCombat(request.EncounterId)));
    }
    [Fact]
    public void MixedCheckpointAutomaticallyBuffersAiAndRequiresHumanReportAcknowledgmentBeforeNextPlanner()
    {
        var (_, _, client, flow) = Checkpoint(3, 1);
        flow.Pump(); Assert.Null(flow.Error); Assert.Single(client.Reports()); Assert.Equal(2, client.Pending().Count);
        Assert.False(client.RecentReports[0].HumanInvolved); Assert.True(flow.RequiresPlayer);
        var first = client.Pending().Single(r => r.EncounterId == flow.PendingHumanEncounter);
        var receipt = flow.ResolveHuman(client.Snapshot, first);
        Assert.Equal(CombatSubmissionStatus.Buffered, receipt.Submission.Status); Assert.True(receipt.HumanInvolved); Assert.NotNull(receipt.Report);
        Assert.Equal(first.EncounterId, flow.ReportToReview); Assert.Null(flow.PendingHumanEncounter); Assert.Single(client.Pending());
        flow.Pump(); Assert.Equal(first.EncounterId, flow.ReportToReview); Assert.Equal(2, client.RecentReports.Count);
        flow.Continue(); Assert.Null(flow.ReportToReview); Assert.NotNull(flow.PendingHumanEncounter);
        flow.ResolveHuman(client.Snapshot, client.Pending().Single());
        Assert.NotNull(flow.ReportToReview); Assert.Empty(client.Pending()); Assert.Equal(SessionPhase.AwaitingCombat, client.Snapshot.Campaign.Phase);
        Assert.Equal(TurnStatus.Completed, flow.Complete(client.Snapshot).Status);
        Assert.Equal(3, client.RecentReports.Count); Assert.Empty(client.Reports()); Assert.Equal(SessionPhase.Planning, client.Snapshot.Campaign.Phase);
    }
    [Fact]
    public void MultipleHumanControllersReceiveOnlyTheirOwnControlsAndCompleteCanonicalPlanSet()
    {
        var (_, app, client, _) = Checkpoint(secondHuman: true); var request = client.Pending().Single();
        var plans = client.PlayerPlans(client.Snapshot, request); Assert.Equal(2, plans.Count);
        var expected = HobbyDS9.Simulation.DeterministicPhasedCombatResolver.CreateDefaultPlans(request);
        foreach (var plan in plans) { plan.CloseRangeTactic = CombatTactic.Evasive; expected.Single(p => p.FleetId == plan.FleetId).CloseRangeTactic = CombatTactic.Evasive; }
        Assert.Equal(Json(app.ResolveCombat(request, expected)), Json(app.ResolveHumanCombat(request.EncounterId, plans)));
        var ai = expected.Single(p => !plans.Any(h => h.FleetId == p.FleetId));
        Assert.Throws<ArgumentException>(() => app.ResolveHumanCombat(request.EncounterId, new[] { ai }));
        Assert.Throws<ArgumentException>(() => app.ResolveHumanCombat(request.EncounterId, new[] { plans[0], plans[0] }));
    }
    [Fact]
    public void StaleReceiptHasNoReportAndDoesNotAddHistoryOrChangeCampaign()
    {
        var (_, _, client, _) = Checkpoint(2); var stamp = client.Snapshot; var requests = client.Pending();
        var first = client.ResolveDetailed(stamp, requests[0]); var save = Json(client.ExportSave());
        var rejected = client.ResolveDetailed(stamp, requests[1]);
        Assert.Equal(CombatSubmissionError.StaleRevision, rejected.Submission.ErrorCode); Assert.Null(rejected.Report);
        Assert.Single(client.RecentReports); Assert.Single(client.Pending()); Assert.Equal(save, Json(client.ExportSave()));
        Assert.NotNull(first.Report); Assert.True(first.Report!.Shots > 0);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReloadedAiCheckpointResolvesOnlyUnbufferedRequestsAndFinalizesOnce(bool buffered)
    {
        var (_, _, client, flow) = Checkpoint(2, 2);
        if (buffered) client.Resolve(client.Snapshot, client.Pending()[0]);
        var checkpoint = client.ExportSave(); client.Load(checkpoint);
        Assert.Equal(buffered ? 1 : 0, client.RecentReports.Count);
        flow.Pump(); var completed = Json(client.ExportSave());
        Assert.Equal(SessionPhase.Planning, client.Snapshot.Campaign.Phase); Assert.Equal(2, client.RecentReports.Count);
        flow.Pump(); Assert.Equal(completed, Json(client.ExportSave())); Assert.Equal(2, client.RecentReports.Count);
    }
    [Fact]
    public void ReloadedHumanCheckpointReconstructsBufferedReportWithoutResubmittingAndClearsOldHistory()
    {
        var (_, _, client, flow) = Checkpoint(2);
        client.Resolve(client.Snapshot, client.Pending()[0]); var partial = client.ExportSave();
        client.Resolve(client.Snapshot, client.Pending()[0]); flow.Complete(client.Snapshot);
        Assert.Equal(2, client.RecentReports.Count);
        client.Load(partial); Assert.Single(client.RecentReports); var before = Json(client.ExportSave());
        flow.Pump(); Assert.Equal(before, Json(client.ExportSave())); Assert.NotNull(flow.ReportToReview);
        flow.Continue(); Assert.NotNull(flow.PendingHumanEncounter); Assert.Single(client.Pending());
        var clean = CampaignApplication.StartScenario(VerticalSliceFixture.Scenario());
        var replacement = new StrategicClient(clean, dto => CampaignApplication.LoadSave(VerticalSliceFixture.Scenario(), dto));
        Assert.Empty(replacement.RecentReports);
    }
    [Fact]
    public void FailedLoadPreservesHistorySnapshotAndCoordinatorState()
    {
        var (_, _, client, flow) = Checkpoint(); flow.Pump(); flow.ResolveHuman(client.Snapshot, client.Pending().Single());
        var snapshot = client.Snapshot; var report = client.RecentReports.Single(); var before = Json(client.ExportSave());
        var bad = client.ExportSave(); bad.GameState.TurnNumber = 0;
        Assert.ThrowsAny<Exception>(() => client.Load(bad));
        Assert.Same(snapshot, client.Snapshot); Assert.Same(report, client.RecentReports.Single()); Assert.Equal(before, Json(client.ExportSave()));
        Assert.Equal(report.Report.EncounterId, flow.ReportToReview);
    }
    [Fact]
    public void AiSubmissionFailureStopsCheckpointWithoutFinalizationOrFakeReport()
    {
        var (scenario, app, original, _) = Checkpoint(aiOnly: 1); var before = Json(original.ExportSave());
        var proxy = SessionProxy.Wrap(app, (method, args) => method.Name == nameof(ICampaignSession.SubmitCombatResult)
            ? new CombatSubmissionResult(null, CombatSubmissionStatus.Rejected, CombatSubmissionError.StaleRevision, 1, 1) : null);
        var client = new StrategicClient(proxy, dto => CampaignApplication.LoadSave(scenario, dto)); var flow = new CombatOrchestrator(client);
        flow.Pump(); Assert.NotNull(flow.Error); Assert.Equal(SessionPhase.AwaitingCombat, client.Snapshot.Campaign.Phase);
        Assert.Empty(client.RecentReports); Assert.Single(client.Pending()); Assert.Equal(before, Json(client.ExportSave()));
    }
    [Fact]
    public void RecentHistoryIsBoundedOrdinalNewestFirstButRetainsWholeLatestTurnAndIsReadOnly()
    {
        var (_, app, _, _) = Checkpoint(); var reports = new List<BattleReportSnapshot>();
        for (var i = 0; i < 90; i++)
        {
            var request = Request(); request.EncounterId = "encounter." + i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture); request.TurnNumber = i / 3 + 1;
            reports.Add(new BattleReportSnapshot(new() { Request = request, Result = EmptyResult(request) }));
        }
        var proxy = SessionProxy.Wrap(app, (method, args) => method.Name == nameof(ICampaignSession.GetBattleReports) ? reports.AsReadOnly() : null);
        var client = new StrategicClient(proxy, _ => app); Assert.Equal(50, client.RecentReports.Count);
        Assert.Equal(30, client.RecentReports[0].Report.TurnNumber); Assert.Equal("encounter.087", client.RecentReports[0].Report.EncounterId);
        Assert.Throws<NotSupportedException>(() => ((IList<RecentCombatReport>)client.RecentReports).Clear());
        foreach (var report in reports.ToArray())
        {
            var request = Request(); request.EncounterId = "latest." + report.EncounterId; request.TurnNumber = 31;
            reports.Add(new BattleReportSnapshot(new() { Request = request, Result = EmptyResult(request) }));
        }
        client.Refresh(); Assert.Equal(90, client.RecentReports.Count); Assert.All(client.RecentReports, r => Assert.Equal(31, r.Report.TurnNumber));
        client.Load(app.ExportSave()); Assert.Empty(client.RecentReports);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RealScenarioTwentyFiveTurnsWithAutoOrchestrationAndCheckpointReloadMatchesExistingHost(bool reload)
    {
        var scenario = VerticalSliceFixture.Scenario(false, true); var app = CampaignApplication.StartScenario(scenario);
        var client = new StrategicClient(app, dto => CampaignApplication.LoadSave(scenario, dto)); var flow = new CombatOrchestrator(client);
        var manual = CampaignApplication.StartScenario(scenario);
        for (var turn = 0; turn < 25; turn++)
        {
            client.EndTurn(client.Snapshot); var manualResult = manual.AdvanceTurn(manual.CaptureSnapshot().Campaign.TurnNumber, manual.CaptureSnapshot().Campaign.StateRevision);
            if (client.Snapshot.Campaign.Phase == SessionPhase.AwaitingCombat && reload) client.Load(client.ExportSave());
            flow.Pump(); Assert.Null(flow.Error);
            while (client.Snapshot.Campaign.Phase == SessionPhase.AwaitingCombat)
            {
                if (flow.ReportToReview != null) flow.Continue();
                else if (flow.PendingHumanEncounter != null) flow.ResolveHuman(client.Snapshot, client.Pending().Single(r => r.EncounterId == flow.PendingHumanEncounter));
                else Assert.Equal(TurnStatus.Completed, flow.Complete(client.Snapshot).Status);
                Assert.Null(flow.Error);
            }
            if (manualResult.Status == TurnStatus.AwaitingCombat)
            {
                foreach (var request in manual.GetPendingCombatRequests().Reverse())
                { var stamp = manual.CaptureSnapshot().Campaign; manual.SubmitCombatResult(manual.ResolveCombat(request), stamp.TurnNumber, stamp.StateRevision); }
                var finalStamp = manual.CaptureSnapshot().Campaign; manual.FinalizeCombat(finalStamp.TurnNumber, finalStamp.StateRevision);
            }
            Assert.Equal(Json(manual.ExportSave()), Json(client.ExportSave()));
        }
        Assert.Equal(26, client.Snapshot.Campaign.TurnNumber); Assert.NotEmpty(client.RecentReports);
        Assert.Equal("10009857d0396dea4bd0a81001cadfb6b5ebfad2c8772223d8288bd70d3559e4",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(client.ExportSave()))).ToLowerInvariant());
    }

    public class SessionProxy : DispatchProxy
    {
        private ICampaignSession target = null!;
        private Func<MethodInfo, object?[]?, object?> intercept = null!;
        public static ICampaignSession Wrap(ICampaignSession target, Func<MethodInfo, object?[]?, object?> intercept)
        { var proxy = Create<ICampaignSession, SessionProxy>(); var instance = (SessionProxy)proxy; instance.target = target; instance.intercept = intercept; return proxy; }
        protected override object? Invoke(MethodInfo? method, object?[]? args) => intercept(method!, args) ?? method!.Invoke(target, args);
    }
}
