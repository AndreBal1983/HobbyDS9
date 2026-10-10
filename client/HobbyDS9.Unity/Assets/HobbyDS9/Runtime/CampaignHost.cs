#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Newtonsoft.Json;

namespace HobbyDS9.UnityClient
{
    // Host adapter: JSON/content hashing stay out of the portable libraries.
    public sealed class CampaignHost
    {
        private readonly ICampaignSession session;
        public CampaignSnapshot Snapshot { get; private set; }
        public CampaignHost(byte[] scenarioBytes)
        {
            var scenario = JsonConvert.DeserializeObject<ScenarioDto>(System.Text.Encoding.UTF8.GetString(scenarioBytes))
                ?? throw new ArgumentException("Scenario is empty.");
            using (var hash = SHA256.Create())
                scenario.ContentHash = string.Concat(hash.ComputeHash(scenarioBytes).Select(b => b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
            session = CampaignApplication.StartScenario(scenario);
            Snapshot = session.CaptureSnapshot();
        }
        public TurnResult EndTurn()
        {
            var observed = Snapshot.Campaign;
            var result = session.AdvanceTurn(observed.TurnNumber, observed.StateRevision);
            Refresh(); return result;
        }
        public IReadOnlyList<CombatRequestDto> Pending() => session.GetPendingCombatRequests();
        public CombatSubmissionResult Resolve(CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto>? plans = null)
        {
            var observed = Snapshot.Campaign;
            var result = plans == null ? session.ResolveCombat(request) : session.ResolveCombat(request, plans);
            var submitted = session.SubmitCombatResult(result, observed.TurnNumber, observed.StateRevision);
            Refresh(); return submitted;
        }
        public TurnResult FinalizeCombats()
        {
            var observed = Snapshot.Campaign;
            var result = session.FinalizeCombat(observed.TurnNumber, observed.StateRevision);
            Refresh(); return result;
        }
        public void Refresh() => Snapshot = session.CaptureSnapshot();
    }
}
