#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // Presentation only. The Application composes/validates player and AI plans,
    // resolves frozen requests, and supplies immutable buffered battle reports.
    public sealed class CombatPanelView : VisualElement
    {
        public CombatPanelView() { name = "combat-panel"; }
        public void Present(CampaignSnapshot snapshot, StrategicClient client,
            Dictionary<string, List<FleetCombatPlanDto>> drafts, Action<Func<string>> run, CombatOrchestrator? orchestration = null)
        {
            Clear();
            var requests = client.Pending(); var reports = client.RecentReports;
            var humans = snapshot.Campaign.Civilizations.Where(c => c.ControllerKind == ControllerKind.Human)
                .Select(c => c.CivilizationId.Value).ToList();
            Text(this, snapshot.Campaign.Phase == SessionPhase.AwaitingCombat ? "Awaiting combat" : "Recent Combat Reports", true);
            if (orchestration?.Error != null) Text(this, orchestration.Error);
            if (reports.Count == 0 && requests.Count == 0) Text(this, "No recent battles in this client session.");
            Text(this, "Free-for-all: every different civilization is hostile. No alliances or diplomacy are assumed.");
            Text(this, snapshot.Campaign.Phase == SessionPhase.AwaitingCombat
                ? $"Pending: {requests.Count} • Buffered: {client.Reports().Count}. Review human battles before finalizing the turn."
                : $"Recent reports: {reports.Count}. These battles have already been finalized.");
            var next = requests.Where(r => CombatOrchestrator.HumanInvolved(snapshot, r))
                .OrderBy(r => r.EncounterId, StringComparer.Ordinal).FirstOrDefault();
            if (next != null && orchestration?.ReportToReview == null)
            {
                var request = next;
                Text(this, CombatPresentation.Heading(snapshot, request.Participants.Select(p => p.CivilizationId)), true);
                Text(this, "Location: " + NodeName(snapshot, request.LocationNodeId));
                if (!drafts.TryGetValue(request.EncounterId, out var plans))
                { plans = client.PlayerPlans(snapshot, request).ToList(); drafts[request.EncounterId] = plans; }
                foreach (var participant in request.Participants.OrderBy(p => p.FleetId, StringComparer.Ordinal))
                {
                    var section = new VisualElement { name = "fleet-plan-" + participant.FleetId }; Add(section);
                    Text(section, CombatPresentation.OwnerName(snapshot, participant.CivilizationId) + " • " + participant.FleetId, true);
                    foreach (var ship in participant.Ships) Text(section, $"{ship.ShipId} • Hull {ship.Hull}/{ship.MaxHull} • Shields {ship.CombatProfile!.ShieldCapacity}");
                    var plan = humans.Contains(participant.CivilizationId) ? plans.SingleOrDefault(p => p.FleetId == participant.FleetId) : null;
                    if (plan == null)
                    {
                        Text(section, "Read-only: this fleet's deterministic plan is supplied by Application, not the player.");
                        continue;
                    }
                    Tactic(section, "Long range", plan.LongRangeTactic, value => plan.LongRangeTactic = value);
                    Tactic(section, "Medium range", plan.MediumRangeTactic, value => plan.MediumRangeTactic = value);
                    Tactic(section, "Close range", plan.CloseRangeTactic, value => plan.CloseRangeTactic = value);
                    var priorities = Enum.GetValues(typeof(CombatTargetPriority)).Cast<CombatTargetPriority>().ToList();
                    var priority = new DropdownField("Target priority", priorities.Select(v => v.ToString()).ToList(), priorities.IndexOf(plan.TargetPriority));
                    priority.AddToClassList("combat-plan-field");
                    priority.RegisterValueChangedCallback(_ => plan.TargetPriority = priorities[priority.index]); section.Add(priority);
                    var destinations = snapshot.Navigation.Nodes.OrderBy(n => n.NodeId.Value, StringComparer.Ordinal).ToList();
                    var retreat = new IntegerField("Retreat hull % (0 disables)") { value = plan.RetreatHullPercent };
                    var destination = new DropdownField("Retreat region", destinations.Select(n => NodeName(snapshot, n.NodeId.Value)).ToList(), Math.Max(0, destinations.FindIndex(n => n.NodeId.Value == plan.RetreatNodeId)));
                    destination.AddToClassList("combat-plan-field"); retreat.AddToClassList("combat-plan-field");
                    destination.SetEnabled(plan.RetreatHullPercent > 0);
                    void UpdateRetreat()
                    {
                        plan.RetreatHullPercent = Math.Max(0, Math.Min(100, retreat.value)); retreat.SetValueWithoutNotify(plan.RetreatHullPercent);
                        destination.SetEnabled(plan.RetreatHullPercent > 0);
                        plan.RetreatNodeId = plan.RetreatHullPercent == 0 ? null : destinations[destination.index].NodeId.Value;
                    }
                    retreat.RegisterValueChangedCallback(_ => UpdateRetreat()); destination.RegisterValueChangedCallback(_ => UpdateRetreat());
                    section.Add(retreat); section.Add(destination);
                }
                Text(this, "Only your fleets have editable controls. Retreat destinations still require authoritative adjacency/occupancy validation.");
                void Resolve(IReadOnlyList<FleetCombatPlanDto>? selected) => run(() =>
                {
                    var result = orchestration == null ? client.Resolve(snapshot, request, selected) : orchestration.ResolveHuman(snapshot, request, selected).Submission;
                    if (result.Status != CombatSubmissionStatus.Buffered) throw new InvalidOperationException("Combat rejected: " + result.ErrorCode);
                    return "Combat result buffered. Review the battle report before finalizing.";
                });
                Add(new Button(() => Resolve(null)) { name = "resolve-defaults", text = "Resolve with defaults" });
                Add(new Button(() => Resolve(plans)) { name = "resolve-player-plans", text = "Resolve with selected human plans" });
            }
            foreach (var report in reports)
            {
                Text(this, report.HumanInvolved ? "Human-involved battle" : "AI-only battle");
                Report(snapshot, report.Report);
            }
            if (orchestration?.ReportToReview != null && requests.Count > 0)
                Add(new Button(() => run(() => { orchestration.Continue(); return orchestration.Error ?? "Continue to the next encounter."; }))
                    { name = "continue-combat", text = "Continue to next encounter" });
            if (requests.Count == 0 && snapshot.Campaign.Phase == SessionPhase.AwaitingCombat)
                Add(new Button(() => run(() =>
                {
                    var result = orchestration == null ? client.FinalizeCombats(snapshot) : orchestration.Complete(snapshot);
                    if (result.Status != TurnStatus.Completed) throw new InvalidOperationException(result.ErrorCode.ToString());
                    drafts.Clear(); return "Combat finalized. Next planning turn opened.";
                })) { name = "finalize-combat", text = "Finalize combat and complete turn" });
        }
        private void Report(CampaignSnapshot snapshot, BattleReportSnapshot report)
        {
            var section = new Foldout { name = "battle-report-" + report.EncounterId,
                text = "Battle report • " + NodeName(snapshot, report.LocationNodeId.Value), value = true };
            section.AddToClassList("battle-report"); Add(section);
            Text(section, CombatPresentation.Heading(snapshot, report.Fleets.Select(f => f.CivilizationId)), true);
            Text(section, $"Turn {report.TurnNumber} • {report.EncounterId}");
            Text(section, $"Surviving fleets: {report.Fleets.Count(f => f.Disposition == FleetCombatDisposition.Held)} • Destroyed: {report.Fleets.Count(f => f.Disposition == FleetCombatDisposition.Destroyed)} • Retreated: {report.Fleets.Count(f => f.Disposition == FleetCombatDisposition.Retreated)}");
            Text(section, $"Shots: {report.Shots} • Hits: {report.Hits} • Misses: {report.Misses} • Phases fought: {report.PhasesFought}");
            Text(section, $"Shield damage: {report.ShieldDamage} • Hull damage: {report.HullDamage} • Destroyed ships: {report.DestroyedShips}");
            foreach (var civilization in report.Fleets.GroupBy(f => f.CivilizationId).OrderBy(g => g.Key, StringComparer.Ordinal))
                Text(section, CombatPresentation.OwnerName(snapshot, civilization.Key) + $": held {civilization.Count(f => f.Disposition == FleetCombatDisposition.Held)}, destroyed {civilization.Count(f => f.Disposition == FleetCombatDisposition.Destroyed)}, retreated {civilization.Count(f => f.Disposition == FleetCombatDisposition.Retreated)}");
            foreach (var fleet in report.Fleets)
            {
                Text(section, CombatPresentation.OwnerName(snapshot, fleet.CivilizationId) + " • " + fleet.FleetId + " • " + fleet.Disposition
                    + (fleet.RetreatNodeId == null ? "" : " → " + NodeName(snapshot, fleet.RetreatNodeId)));
                foreach (var ship in fleet.Ships)
                {
                    var design = snapshot.Campaign.ShipDesigns.SingleOrDefault(d => d.DefinitionId.Value == ship.DesignId);
                    Text(section, $"{design?.DisplayName ?? ship.DesignId} • {ship.ShipId}: hull {ship.StartingHull} → {ship.RemainingHull}" + (ship.RemainingHull == 0 ? " • Destroyed" : ""));
                }
            }
            foreach (var phase in report.Phases)
            {
                var name = phase.Phase == CombatPhaseKind.LongRange ? "Long" : phase.Phase == CombatPhaseKind.MediumRange ? "Medium" : "Close";
                var events = new Foldout { name = "events-" + phase.Phase, text = $"{name} phase • Attacks {phase.Attacks} • Hits {phase.Hits} • Misses {phase.Misses}", value = false };
                section.Add(events);
                if (phase.Events.Count == 0) Text(events, "Phase not executed / no events.");
                foreach (var e in phase.Events)
                    Text(events, $"#{e.Sequence} {e.Kind}" + (e.SourceShipId == null ? "" : " • " + e.SourceFleetId + "/" + e.SourceShipId + " →")
                        + (e.ShipId == null ? "" : " " + e.ShipId) + (e.FleetId == null ? "" : " • Fleet " + e.FleetId)
                        + (e.Amount.HasValue ? " • Damage " + e.Amount.Value : ""));
            }
        }
        private static void Text(VisualElement parent, string text, bool heading = false)
        {
            var label = new Label(text); label.AddToClassList(heading ? "combat-heading" : "combat-text"); parent.Add(label);
        }
        private static string NodeName(CampaignSnapshot snapshot, string id) => GalaxyProjection.Nodes(snapshot).FirstOrDefault(n => n.NodeId.Value == id)?.Label ?? id;
        private static void Tactic(VisualElement parent, string label, CombatTactic initial, Action<CombatTactic> set)
        {
            var values = Enum.GetValues(typeof(CombatTactic)).Cast<CombatTactic>().ToList();
            var field = new DropdownField(label, values.Select(v => v.ToString()).ToList(), values.IndexOf(initial));
            field.AddToClassList("combat-plan-field"); field.RegisterValueChangedCallback(_ => set(values[field.index])); parent.Add(field);
        }
    }
}
