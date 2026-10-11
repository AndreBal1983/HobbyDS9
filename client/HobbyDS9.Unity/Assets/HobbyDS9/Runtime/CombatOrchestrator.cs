#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityClient
{
    // Immutable receipts contain projections, not mutable request/result DTO aliases.
    // A rejection has no report. Results are preserved only after buffering succeeds.
    public sealed class CombatResolutionReceipt
    {
        public CombatSubmissionResult Submission { get; }
        public BattleReportSnapshot? Report { get; }
        public bool HumanInvolved { get; }
        internal CombatResolutionReceipt(CombatSubmissionResult submission, BattleReportSnapshot? report, bool human)
        { Submission = submission; Report = report; HumanInvolved = human; }
    }
    public sealed class RecentCombatReport
    {
        public BattleReportSnapshot Report { get; }
        public bool HumanInvolved { get; }
        internal RecentCombatReport(BattleReportSnapshot report, bool human) { Report = report; HumanInvolved = human; }
    }

    // Presentation/controller orchestration. Pump is an explicit action after
    // advancement/load/Continue, never a side effect of rendering the combat tab.
    public sealed class CombatOrchestrator
    {
        private readonly StrategicClient client;
        private readonly HashSet<string> acknowledged = new HashSet<string>(StringComparer.Ordinal);
        private long generation = -1;
        public string? ReportToReview { get; private set; }
        public string? PendingHumanEncounter { get; private set; }
        public string? Error { get; private set; }
        public string? Notice { get; private set; }
        public bool RequiresPlayer => ReportToReview != null || PendingHumanEncounter != null ||
            (client.Snapshot.Campaign.Phase == SessionPhase.AwaitingCombat && client.Reports().Any(r => IsHuman(r)));
        public CombatOrchestrator(StrategicClient client) { this.client = client; }
        public static bool HumanInvolved(CampaignSnapshot snapshot, CombatRequestDto request) => request.Participants
            .Any(p => snapshot.Campaign.Civilizations.Any(c => c.CivilizationId.Value == p.CivilizationId && c.ControllerKind == ControllerKind.Human));
        private bool IsHuman(BattleReportSnapshot report) => report.Fleets.Any(f => client.Snapshot.Campaign.Civilizations
            .Any(c => c.CivilizationId.Value == f.CivilizationId && c.ControllerKind == ControllerKind.Human));
        public void Pump()
        {
            Error = null; Notice = null; PendingHumanEncounter = null; ReportToReview = null;
            if (generation != client.SessionGeneration) { acknowledged.Clear(); generation = client.SessionGeneration; }
            if (client.Snapshot.Campaign.Phase != SessionPhase.AwaitingCombat) { acknowledged.Clear(); return; }
            try
            {
                var aiCount = 0;
                foreach (var request in client.Pending().OrderBy(r => r.EncounterId, StringComparer.Ordinal))
                {
                    if (HumanInvolved(client.Snapshot, request)) continue;
                    var receipt = client.ResolveDetailed(client.Snapshot, request);
                    if (receipt.Submission.Status != CombatSubmissionStatus.Buffered)
                        throw new InvalidOperationException("Automatic combat submission rejected: " + receipt.Submission.ErrorCode);
                    aiCount++;
                }
                var reports = client.Reports();
                ReportToReview = reports.Where(IsHuman).OrderBy(r => r.EncounterId, StringComparer.Ordinal)
                    .FirstOrDefault(r => !acknowledged.Contains(r.EncounterId))?.EncounterId;
                if (ReportToReview == null)
                    PendingHumanEncounter = client.Pending().OrderBy(r => r.EncounterId, StringComparer.Ordinal)
                        .FirstOrDefault(r => HumanInvolved(client.Snapshot, r))?.EncounterId;
                if (client.Pending().Count == 0 && !reports.Any(IsHuman))
                {
                    var count = reports.Count;
                    var completed = client.FinalizeCombats(client.Snapshot);
                    if (completed.Status != TurnStatus.Completed)
                        throw new InvalidOperationException("Automatic combat finalization rejected: " + completed.ErrorCode);
                    Notice = count + " AI battles resolved. View Combat Reports.";
                    acknowledged.Clear();
                }
                else if (aiCount > 0) Notice = aiCount + " AI battles buffered. View Combat Reports.";
            }
            catch (Exception error) { Error = error.Message; }
        }
        public CombatResolutionReceipt ResolveHuman(CampaignSnapshot observed, CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto>? plans = null)
        {
            // Check authoritative participation, not caller-edited DTOs.
            var frozen = client.Pending().Single(r => r.EncounterId == request.EncounterId);
            if (!HumanInvolved(client.Snapshot, frozen)) throw new ArgumentException("This encounter has no human participant.", nameof(request));
            var receipt = client.ResolveDetailed(observed, frozen, plans);
            if (receipt.Submission.Status == CombatSubmissionStatus.Buffered) Pump();
            return receipt;
        }
        public void Continue()
        {
            if (ReportToReview != null) acknowledged.Add(ReportToReview);
            Pump();
        }
        public TurnResult Complete(CampaignSnapshot observed)
        {
            var result = client.FinalizeCombats(observed);
            if (result.Status == TurnStatus.Completed) { acknowledged.Clear(); ReportToReview = null; PendingHumanEncounter = null; }
            return result;
        }
    }
}
