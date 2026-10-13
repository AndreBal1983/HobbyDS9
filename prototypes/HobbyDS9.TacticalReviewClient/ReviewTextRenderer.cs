using System;
using System.Linq;
using System.Text;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public static class ReviewTextRenderer
    {
        public static string Render(ReviewController controller)
        {
            var frame=controller.Frame; var b=frame.Battle; var text=new StringBuilder();
            text.AppendLine("HEADLESS AUTHORITATIVE REVIEW CLIENT / PRODUCTION ACTIVATION DISABLED");
            text.AppendLine(FormattableString.Invariant($"Actor {frame.Actor} | Turn {frame.Status.Turn} | {frame.Status.Phase} | {controller.Mode}"));
            text.AppendLine(frame.Status.TravelModel); text.AppendLine(frame.Status.ContactAdmission);
            if(frame.Travel != null)
            {
                text.AppendLine("PHYSICAL REVIEW / PROPOSED POLICIES / COMPLETE FORMAT 2 HOST");
                foreach(var order in frame.Travel.OwnOrders)
                    text.AppendLine($"Own travel {order.FleetId}: {order.Intent} | contact {order.ContactFleetId ?? "none"} | route {string.Join(", ",order.RouteNodeIds)}");
                foreach(var contact in frame.Travel.KnownContacts)
                    text.AppendLine(FormattableString.Invariant($"Known contact {contact.FleetId}: LAST SEEN {contact.LastSeenNodeId}, turn {contact.LastSeenTurn}, fresh {contact.Fresh}; current position and destination unavailable"));
                foreach(var receipt in frame.Travel.OwnReceipts)
                    text.AppendLine(FormattableString.Invariant($"Own movement {receipt.FleetId}: turn {receipt.Turn}, end node {receipt.EndNodeId}, destination {receipt.TransitDestinationId ?? "none"}, locked progress {receipt.TransitProgress}/{receipt.TransitTotalCost}, paid {receipt.CostPaid}, edges {receipt.EdgesCompleted}, encounter stop {receipt.StoppedByEncounter}, exact time {receipt.StopTimeNumerator}/{receipt.StopTimeDenominator}"));
                if(controller.TravelDraft != null)
                    text.AppendLine($"UNSENT travel {controller.TravelDraft.Fleet}: {controller.TravelDraft.Intent} | contact {controller.TravelDraft.ContactFleet ?? "none"} | route {string.Join(", ",controller.TravelDraft.Route)}");
                text.AppendLine("Travel choices: Stay, StayEngage, Leave, Follow. Backend validates ownership, contact freshness, routes and transit. Speed/ETA explanation unavailable.");
            }
            foreach(var fleet in frame.OwnStrategicFleets)
                text.AppendLine($"Own strategic fleet {fleet.FleetId.Value} | node {fleet.LocationNodeId.Value} | remaining route {string.Join(", ",fleet.RouteNodeIds.Select(n=>n.Value))}");
            if(controller.RefreshRequired) text.AppendLine("Refresh required; no automatic retry.");
            if(controller.LastBackendError.HasValue) text.AppendLine("Backend result: "+controller.LastBackendError.Value);
            if(controller.LastCheckpointError != ReviewCheckpointError.None)
                text.AppendLine("Checkpoint result: "+controller.LastCheckpointError);
            if(controller.DraftInvalidated) text.AppendLine("Draft invalidated by a new authoritative stamp or accepted commitment; review new orders.");
            if(b == null)
            {
                text.AppendLine("Committed campaign stocks; select an encounter for current battle inspection.");
                foreach(var encounter in frame.Encounters) text.AppendLine("Owned encounter: "+encounter);
                foreach(var setting in frame.CloakSettings)
                    text.AppendLine($"Own fleet {setting.FleetId} | approach cloak {setting.Enabled} | change available {setting.CanChange}");
            }
            else
            {
                text.AppendLine(FormattableString.Invariant($"Encounter {b.EncounterId} | Location {b.LocationNodeId} | Next round cursor {b.Round} | Complete {b.Complete}"));
                text.AppendLine("Observation precision: "+b.ObservationPolicy+" (proposed review policy)");
                if(b.Complete) text.AppendLine("Encounter complete; no further round. Batch finalization available: "+frame.Status.CanFinalizeBatch);
                else if(b.OwnCommitmentAccepted) text.AppendLine("Own orders accepted. Waiting for authoritative round result; opposing orders unavailable.");
                else text.AppendLine("Fleet maneuver and visible enemy-fleet focus only. Screening stays within the same fleet.");
                foreach(var order in b.StandingOrders)
                    text.AppendLine($"Standing {order.FleetId}: {order.Maneuver} | focus {order.FocusFleetId ?? "none"} | editable {order.CanOrder}");
                foreach(var order in b.OwnCommittedOrders)
                    text.AppendLine($"Own accepted {order.FleetId}: {order.Maneuver} | focus {order.FocusFleetId ?? "none"}");
                foreach(var edit in controller.Draft)
                    text.AppendLine($"Unsent {edit.Fleet}: {edit.Maneuver} | focus {edit.Focus ?? "none"}");
                text.AppendLine("Legal maneuvers: "+string.Join(", ",b.LegalManeuvers));
                text.AppendLine("Legal visible fleet focus: "+string.Join(", ",b.LegalFocusFleetIds));
                if(b.VisibleEnemies.Count == 0) text.AppendLine("No visible hostile ships. Identities and counts of hidden contacts are unavailable.");
                foreach(var enemy in b.VisibleEnemies) Condition(text,"Visible enemy",enemy,null);
                foreach(var relation in b.Relations)
                    text.AppendLine($"Observed relation {relation.OwnFleetId} / {relation.VisibleEnemyFleetId}: {relation.Range}, own {relation.OwnFacing}, enemy {relation.EnemyFacing}");
            }
            foreach(var own in frame.OwnShips)
            {
                Condition(text,"Own",own.Condition,own.ShieldMaximum);
                text.AppendLine(FormattableString.Invariant($"Fit {own.FitId} revision {own.FitRevision} | provenance {own.ProvenanceId} | cloak {own.Cloaked}"));
                text.AppendLine(FormattableString.Invariant($"Armor {own.Armor} | power {own.EffectivePower} | equipment budget {own.EquipmentPowerBudget} | cloak reservation {own.CloakPowerReserved}"));
                if(own.Weapons.Count == 0) text.AppendLine("No installed weapon packages.");
                foreach(var weapon in own.Weapons)
                {
                    text.AppendLine(FormattableString.Invariant($"Own package {weapon.WeaponId} | {weapon.PackageVersionId} | {weapon.Class} | mount {weapon.MountId} | slots {weapon.SlotsRequired} | coverage {weapon.Coverage}"));
                    text.AppendLine(FormattableString.Invariant($"Supplied ammunition {weapon.Ammunition}/{weapon.MagazineCapacity} | ammunition definition {weapon.AmmunitionDefinitionId}"));
                    text.AppendLine(FormattableString.Invariant($"Powered {weapon.Powered} | power allocation {weapon.AllocatedPower}/{weapon.RequiredPower} | next fire round {weapon.NextFireRound} | cadence {weapon.CadenceRounds}"));
                    text.AppendLine("Readiness explanation unavailable: backend does not supply this projection.");
                }
            }
            if(controller.SelectedShip != null && (frame.OwnShips.Any(s=>s.Condition.ShipId == controller.SelectedShip)
                || (b?.VisibleEnemies.Any(s=>s.ShipId == controller.SelectedShip) ?? false)))
                text.AppendLine("Inspection selection: "+controller.SelectedShip);
            text.AppendLine("Save committed state: unsent edits remain local and are absent from the checkpoint.");
            text.AppendLine("Successful load discards local edits after confirmation; failed load retains the running state.");
            return text.ToString();
        }
        private static void Condition(StringBuilder text, string label, CampaignTacticalCondition ship, System.Collections.Generic.IReadOnlyList<long>? max)
        {
            text.AppendLine(FormattableString.Invariant($"{label} {ship.ShipId} | fleet {ship.FleetId} | type {ship.Type} | hull {ship.Hull}/{ship.MaxHull}"));
            // Explicit sector order is part of the actual observation contract.
            for(var i=0;i<ship.Shields.Count;i++)
                text.AppendLine(FormattableString.Invariant($"{(TacticalSector)i} {ship.Shields[i]}")+(max == null ? "" : FormattableString.Invariant($"/{max[i]}")));
        }
        public static string RenderReport(ReviewController controller, int? round = null)
        {
            var text=new StringBuilder("ACTOR-SCOPED REPORT / REPLAY ONLY\n");
            var reports=controller.Frame.Battle?.Reports;
            if(reports == null) return text.AppendLine("No current-batch report projection. No permanent campaign archive.").ToString();
            foreach(var e in reports.Where(e=>!round.HasValue || e.Round == round.Value))
            {
                // No private Detail parsing, opponent package IDs, chance or reserve reconstruction.
                text.AppendLine(FormattableString.Invariant($"#{e.Sequence} Round {e.Round} {e.Kind} | ship {e.ShipId ?? "unavailable"} | target {e.TargetShipId ?? "unavailable"} | own package {e.OwnWeaponId ?? "unavailable"} | amount {e.Amount} | sector {e.Sector} | range {e.Range}"));
                if(e.Kind == "FocusFallback") text.AppendLine("Authoritative focus fallback reported; selected focus remains a preference. Detailed reason unavailable.");
            }
            return text.ToString();
        }
        public static string RenderInspection(ReviewController controller, string shipId)
        {
            var text=new StringBuilder("LAWFUL SHIP INSPECTION\n");
            var own=controller.Frame.OwnShips.SingleOrDefault(s=>s.Condition.ShipId == shipId);
            if(own != null)
            {
                Condition(text,"Own",own.Condition,own.ShieldMaximum);
                foreach(var w in own.Weapons)
                    text.AppendLine(FormattableString.Invariant($"Own package {w.WeaponId} | {w.Class} | ammunition {w.Ammunition}/{w.MagazineCapacity} | next fire round {w.NextFireRound}"));
                text.AppendLine("Readiness explanation unavailable.");
                return text.ToString();
            }
            var enemy=controller.Frame.Battle?.VisibleEnemies.SingleOrDefault(s=>s.ShipId == shipId);
            if(enemy != null) Condition(text,"Visible enemy",enemy,null);
            else text.AppendLine("No lawful ship observation available.");
            return text.ToString();
        }
    }
}
