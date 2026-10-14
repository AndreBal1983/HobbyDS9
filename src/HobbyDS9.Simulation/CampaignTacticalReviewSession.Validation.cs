using System;
using System.Linq;
using System.Collections.Generic;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private void ValidateSave(CampaignTacticalReviewSaveDto s,IReadOnlyList<CombatRequestDto>? physicalRequests=null)
        {
            Require(s != null && s.FormatVersion == CampaignTacticalReviewIdentity.SaveFormat && s.ProfileId == CampaignTacticalReviewIdentity.Profile
                && Hash(s.PackageHash) && s.ResolverId == CampaignTacticalReviewIdentity.Resolver && s.ResolverVersion == CampaignTacticalReviewIdentity.ResolverVersion
                && s.Campaign != null && s.Pools != null && s.FleetSettings != null && s.Battles != null && s.AcceptedRoundCommands != null, "Invalid review save envelope.");
            FoundationValidator.ValidateSave(s.Campaign, definitions, compatibility).ThrowIfInvalid(nameof(s));
            var game = s.Campaign.GameState;
            ValidateBuiltShips(game);
            if(travel!=null)Require(game.Phase==SessionPhase.Planning&&game.PendingCombats!.Count==0,"Physical core cannot carry legacy pending encounters.");
            var requests=travel!=null?(physicalRequests??travel.Requests):game.PendingCombats!.Select(p=>p.Request).ToList();
            Require(game.Civilizations!.Count == controllers.Count && game.Civilizations.All(c => controllers.TryGetValue(c.CivilizationId,out var controller)
                && c.ControllerKind == controller), "Controller configuration differs from the pinned review package.");
            Require(game.ResolvedCombats!.Count == 0, "External v1 results cannot enter the private review lifecycle.");
            Require(s.AcceptedRoundCommands.All(id => EntityId.TryParse(id, out _) && !id.StartsWith("ai-command.", StringComparison.Ordinal))
                && s.AcceptedRoundCommands.Distinct(StringComparer.Ordinal).Count() == s.AcceptedRoundCommands.Count
                && !s.AcceptedRoundCommands.Intersect(game.AcceptedCommandIds!, StringComparer.Ordinal).Any(), "Invalid replay ledger.");
            Require(s.Pools.Count == game.Ships!.Count && s.Pools.All(p => p != null)
                && s.Pools.Select(p => p.ShipId).Distinct(StringComparer.Ordinal).Count() == s.Pools.Count, "Missing/duplicate current pools.");
            foreach (var pool in s.Pools)
            {
                var actual = game.Ships.SingleOrDefault(p => p.ShipId == pool.ShipId);
                Require(actual != null && KnownSource(pool.ShipId), "Unbound current source.");
                var fit = Fit(pool.ShipId);
                Require(pool.FitId == fit.DefinitionId && pool.FitRevision == fit.Revision && pool.ProvenanceId == fit.ProvenanceId
                    && pool.Shields != null && pool.Shields.Count == 4 && pool.Shields.All(q => q != null && q.Sector.HasValue && q.Current.HasValue)
                    && pool.Shields.Select(q => q.Sector).Distinct().Count() == 4, "Invalid pool provenance/sectors.");
                foreach (var q in pool.Shields)
                {
                    var limit = fit.ShieldLimits!.SingleOrDefault(p => p.Sector == q.Sector);
                    Require(limit != null && q.Current >= 0 && q.Current <= limit.Maximum, "Shield stock exceeds exact fit.");
                }
                var initial = Provision(pool.ShipId).InitialAmmunition!;
                Require(pool.Ammunition != null && pool.Ammunition.Count == initial.Count && pool.Ammunition.All(a => a != null)
                    && pool.Ammunition.Select(a => a.WeaponId).Distinct(StringComparer.Ordinal).Count() == pool.Ammunition.Count, "Invalid current ammunition.");
                foreach (var a in pool.Ammunition)
                {
                    var provision = initial.SingleOrDefault(p => p.WeaponId == a.WeaponId);
                    Require(provision != null && a.Stock.HasValue && a.Stock >= 0 && a.Stock <= provision.Stock
                        && a.AmmunitionDefinitionId == provision.AmmunitionDefinitionId, "Ammunition recovery/substitution is unsupported.");
                }
            }
            Require(s.FleetSettings.Count == game.Fleets!.Count && s.FleetSettings.All(f => f != null && game.Fleets.Any(p => p.FleetId == f.FleetId))
                && s.FleetSettings.Select(f => f.FleetId).Distinct(StringComparer.Ordinal).Count() == s.FleetSettings.Count, "Invalid fleet cloak settings.");
            Require(s.Battles.Count == requests.Count && s.Battles.All(b => b != null && b.State != null)
                && s.Battles.Select(b => b.State.EncounterId).Distinct(StringComparer.Ordinal).Count() == s.Battles.Count, "Invalid private battle batch.");
            foreach (var b in s.Battles)
            {
                TacticalCombatV2State.Validate(b.State);
                var canonicalBattle = TacticalCombatV2State.Copy(b.State);
                var frozen = requests.SingleOrDefault(p => p.EncounterId == b.State.EncounterId);
                Require(frozen != null && RulesKey(b.State.Rules) == RulesKey(rules)
                    && b.BoundaryRevision == b.State.NextRound - 1 && b.HiddenShipIds != null
                    && b.Commitments != null && b.Reports != null && b.ResolvedRounds != null, "Invalid frozen battle/stamp.");
                Require(b.State.Formations.OrderBy(f => f.FormationId, StringComparer.Ordinal).Select(f => (f.FormationId, f.SideId))
                    .SequenceEqual(frozen.Participants.OrderBy(f => f.FleetId, StringComparer.Ordinal).Select(f => (f.FleetId, f.CivilizationId))), "Frozen ownership differs.");
                Require(b.HiddenShipIds.Distinct(StringComparer.Ordinal).Count() == b.HiddenShipIds.Count
                    && (b.State.NextRound == 1 || b.HiddenShipIds.Count == 0), "Illegal re-cloaking.");
                foreach (var f in b.State.Formations)
                {
                    var participant = frozen.Participants.Single(p => p.FleetId == f.FormationId);
                    Require(f.Ships.Select(p => p.ShipId).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(participant.Ships.Select(p => p.ShipId).OrderBy(id => id, StringComparer.Ordinal))
                        && f.Objective == TacticalObjective.Defeat && f.ObjectiveProgress == 0 && !f.ObjectiveCompleted
                        && f.StandingOrder.ProtectedFormationId == null && f.StandingOrder.TargetPriority == TacticalTargetPriority.LowestHullRatio
                        && f.StandingOrder.PreferredRange == TacticalRange.Distant && f.Status != TacticalFormationStatus.Retreated && f.Status != TacticalFormationStatus.BrokenThrough,
                        "Invalid fleet membership, cross-fleet protection or exit.");
                    Maneuver(f.StandingOrder.Maneuver);
                    var sourceFits = f.Ships.Select(p => Fit(p.ShipId)).ToList();
                    int Median(Func<AuthoredTacticalFitDto,int> value) { var ratings = sourceFits.Select(value).OrderBy(v => v).ToList(); return ratings[(ratings.Count-1)/2]; }
                    Require(f.Speed == Median(p => p.Capabilities!.Speed!.Value) && f.Agility == Median(p => p.Capabilities!.Agility!.Value)
                        && f.Sensors == sourceFits.Max(p => p.Capabilities!.Sensors!.Value) && f.Command == 0 && f.Cohesion == 0, "Changed frozen capability projection.");
                    foreach (var ship in canonicalBattle.Formations.Single(p => p.FormationId == f.FormationId).Ships)
                    {
                        var actual = game.Ships.Single(p => p.ShipId == ship.ShipId);
                        var pool = s.Pools.Single(p => p.ShipId == ship.ShipId);
                        var expected = Compile(actual, Fit(ship.ShipId), pool);
                        Require(SameHardware(ship, expected) && ship.Hull <= actual.CurrentHull
                            && ship.Weapons.All(w => w.Ammunition <= expected.Weapons.Single(p => p.WeaponId == w.WeaponId).Ammunition), "Forged hardware, hull or ammunition.");
                        if (b.State.NextRound == 1)
                            Require(ship.Hull == expected.Hull && ship.Shields.Select(q => (q.Sector, q.Current)).SequenceEqual(expected.Shields.Select(q => (q.Sector, q.Current)))
                                && ship.Weapons.Select(w => (w.WeaponId, w.Ammunition, w.NextFireRound)).SequenceEqual(expected.Weapons.Select(w => (w.WeaponId, w.Ammunition, w.NextFireRound)))
                                && f.StandingOrder.Maneuver == TacticalManeuver.MaintainDistance && f.StandingOrder.TargetFormationId == null, "Forged opening stocks/orders.");
                    }
                }
                foreach (var id in b.HiddenShipIds)
                {
                    var fleet = b.State.Formations.SingleOrDefault(f => f.Ships.Any(p => p.ShipId == id));
                    Require(fleet != null && s.FleetSettings.Single(f => f.FleetId == fleet.FormationId).ApproachCloak
                        && cloak.Single(c => c.FitId == Fit(id).DefinitionId).Capable == true, "Hidden ship lacks authored cloak.");
                }
                if (b.State.NextRound == 1)
                {
                    Require(b.State.RandomState == frozen.RandomState, "Initial seed was not allocated by the campaign.");
                    Require(b.State.Geometry.All(g => g.Range == TacticalRange.Distant && g.FacingA == TacticalSector.Fore && g.FacingB == TacticalSector.Fore), "Changed opening geometry.");
                    var expectedHidden = b.State.Formations.Where(f => s.FleetSettings.Single(p => p.FleetId == f.FormationId).ApproachCloak)
                        .SelectMany(f => f.Ships).Where(p => cloak.Single(c => c.FitId == Fit(p.ShipId).DefinitionId).Capable == true).Select(p => p.ShipId).OrderBy(id => id, StringComparer.Ordinal);
                    Require(expectedHidden.SequenceEqual(b.HiddenShipIds.OrderBy(id => id, StringComparer.Ordinal)), "Missing initial cloak state.");
                }
                Require(b.Commitments.All(c => c != null && c.Orders != null && game.Civilizations!.Any(a => a.CivilizationId == c.ActorId)
                    && b.State.Formations.Any(f => f.SideId == c.ActorId && f.Status == TacticalFormationStatus.Active))
                    && b.Commitments.Select(c => c.CommandId).Distinct(StringComparer.Ordinal).Count() == b.Commitments.Count
                    && b.Commitments.Select(c => c.ActorId).Distinct(StringComparer.Ordinal).Count() == b.Commitments.Count,
                    "Invalid private commitments.");
                foreach (var c in b.Commitments)
                {
                    Require(EntityId.TryParse(c.CommandId, out _) && (game.Civilizations!.Single(a => a.CivilizationId == c.ActorId).ControllerKind == ControllerKind.AI
                        ? c.CommandId.StartsWith("ai-command.", StringComparison.Ordinal) : s.AcceptedRoundCommands.Contains(c.CommandId, StringComparer.Ordinal)), "Invalid private acceptance receipt.");
                    var owned = b.State.Formations.Where(f => f.SideId == c.ActorId && f.Status == TacticalFormationStatus.Active).Select(f => f.FormationId).OrderBy(id => id, StringComparer.Ordinal);
                    Require(b.State.Completion == TacticalCompletion.None && c.Orders.All(o => ValidOrder(b, c.ActorId, o))
                        && c.Orders.Select(o => o.FleetId).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(owned), "Foreign/hidden/exit loaded orders.");
                }
                foreach (var r in b.Reports)
                {
                    Require(r != null && r.Round >= 1 && r.Round < b.State.NextRound && b.State.Formations.Any(f => f.SideId == r.ActorId)
                        && (r.Detail == "" || r.Kind=="FocusFallback"&&r.Detail=="Preferred fleet unavailable for this opportunity; a visible hostile alternative was acquired.")
                        && r.Amount >= 0 && (Enum.TryParse<TacticalEventKind>(r.Kind, out var kind) && Enum.IsDefined(typeof(TacticalEventKind), kind)
                            || r.Kind == "IncomingTorpedo" || r.Kind == "FocusFallback"), "Invalid filtered report.");
                    if (r.OwnWeaponId != null) Require(r.ShipId != null && b.State.Formations.Any(f => f.SideId == r.ActorId
                        && f.Ships.Any(p => p.ShipId == r.ShipId && p.Weapons.Any(w => w.WeaponId == r.OwnWeaponId))), "Opponent equipment in report.");
                    bool own = r.ShipId != null && b.State.Formations.Any(f => f.SideId == r.ActorId && f.Ships.Any(p => p.ShipId == r.ShipId));
                    if (r.Kind == "FiringOpportunity" || r.Kind == "FocusFallback") Require(own, "Private firing opportunity disclosure.");
                    if (!own && r.Kind != "ShieldImpact" && r.Kind != "HullDamage") Require(r.Amount == 0, "Hidden offensive statistics in report.");
                    if (r.Round == 1)
                        foreach (var id in new[] { r.ShipId, r.TargetShipId }.Where(id => id != null))
                        {
                            var fleet = b.State.Formations.SingleOrDefault(f => f.Ships.Any(p => p.ShipId == id));
                            Require(fleet != null, "Unknown report participant.");
                            if (fleet.SideId != r.ActorId) Require(!s.FleetSettings.Single(f => f.FleetId == fleet.FormationId).ApproachCloak
                                || cloak.Single(c => c.FitId == Fit(id!).DefinitionId).Capable != true, "Hidden round-one identity in saved report.");
                        }
                }
                foreach (var actor in b.Reports.Select(r => r.ActorId).Distinct(StringComparer.Ordinal))
                    Require(b.Reports.Where(r => r.ActorId == actor).Select(r => r.Sequence).SequenceEqual(Enumerable.Range(0,b.Reports.Count(r => r.ActorId == actor))),
                        "Noncontiguous actor-local report chronology.");
                ValidateReplay(s,b,frozen);
            }
            var commitments = s.Battles.SelectMany(b => b.Commitments.Concat(b.ResolvedRounds.SelectMany(r => r.Commitments))).ToList();
            Require(commitments.Select(c => c.CommandId).Distinct(StringComparer.Ordinal).Count() == commitments.Count, "Repeated private command provenance.");
        }
    }
}
