using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class CampaignTacticalReviewData
    {
        internal static void Require([DoesNotReturnIf(false)] bool condition, string message)
        { if (!condition) throw new ArgumentException(message); }
        internal static bool Hash(string? hash) => hash != null && hash.Length == 64 && hash.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        internal static void ValidatePackage(CampaignTacticalReviewPackageDto p)
        {
            Require(p != null && p.FormatVersion == CampaignTacticalReviewIdentity.PackageFormat && Hash(p.PackageHash)
                && p.ProfileId == CampaignTacticalReviewIdentity.Profile && p.PolicyStatus == "proposed"
                && p.DefaultActivationAllowed == false && p.Campaign != null && p.Rules != null && p.CloakSources != null, "Unknown or ungated review package.");
            FoundationValidator.ValidateScenario(p.Campaign!).ThrowIfInvalid(nameof(p));
            Require(p.Campaign!.Definitions.ContentFormatVersion == AuthoredTacticalEquipmentIdentity.ContentFormatVersion, "Review requires explicit authored equipment content.");
            var fits = p.Campaign.Definitions.AuthoredTacticalEquipment!.Fits!;
            Require(p.CloakSources!.Count == fits.Count && p.CloakSources.All(c => c != null)
                && p.CloakSources.Select(c => c.FitId).Distinct(StringComparer.Ordinal).Count() == fits.Count, "Incomplete cloak source catalogue.");
            foreach (var c in p.CloakSources)
            {
                var fit = fits.SingleOrDefault(f => f.DefinitionId == c.FitId && f.Revision == c.FitRevision);
                Require(fit != null && c.Capable.HasValue && c.ConcurrentPowerRequired.HasValue && c.ConcurrentPowerRequired >= 0
                    && (c.Capable == true ? c.ConcurrentPowerRequired > 0 : c.ConcurrentPowerRequired == 0), "Invalid explicit cloak source.");
                var allocated = fit!.EssentialReservations!.Aggregate(BigInteger.Zero, (v, e) => v + e.AllocatedPower!.Value)
                    + fit.Weapons!.Aggregate(BigInteger.Zero, (v, e) => v + e.AllocatedPower!.Value) + fit.ShieldRechargeAllocatedPower!.Value;
                Require(allocated + c.ConcurrentPowerRequired!.Value <= Math.Min(fit.ReactorOutput!.Value, fit.DistributionLimit!.Value), "Concurrent cloak and ordinary combat cannot be powered.");
            }
            Require(p.Campaign.InitialState.Ships!.All(s => p.Campaign.Definitions.AuthoredTacticalEquipment.InitialShipProvisions!.Any(a => a.ShipId == s.ShipId)),
                "Mixed/legacy ships cannot enter the review lifecycle.");
            var r = p.Rules!;
            Require(new[] { r.SpeedWeight, r.AgilityWeight, r.SensorsWeight, r.CommandWeight, r.CohesionWeight }.All(v => v >= 0 && v <= 100)
                && r.SpeedWeight + r.AgilityWeight > 0 && r.ManeuverTieMargin >= 0 && r.ManeuverTieMargin <= 100000
                && r.ArmorNumerator >= 0 && r.ArmorNumerator <= 1000000 && r.ArmorDenominator > 0 && r.ArmorDenominator <= 1000000
                && new[] { r.DistantTorpedoHitPenaltyBps, r.DistantInterceptionBonusBps, r.InterceptionBaseBps, r.ScreenDefenseBps,
                    r.WithdrawalDefenseBps, r.HoldDefenseBps, r.ChargeDefenseBps }.All(v => v >= 0 && v <= 10000), "Invalid frozen review rules.");
        }
        internal static TacticalRulesV2Dto CopyRules(TacticalRulesV2Dto r) => new TacticalRulesV2Dto {
            SpeedWeight = r.SpeedWeight, AgilityWeight = r.AgilityWeight, SensorsWeight = r.SensorsWeight, CommandWeight = r.CommandWeight,
            CohesionWeight = r.CohesionWeight, ManeuverTieMargin = r.ManeuverTieMargin, ArmorNumerator = r.ArmorNumerator,
            ArmorDenominator = r.ArmorDenominator, DistantTorpedoHitPenaltyBps = r.DistantTorpedoHitPenaltyBps,
            DistantInterceptionBonusBps = r.DistantInterceptionBonusBps, InterceptionBaseBps = r.InterceptionBaseBps,
            ScreenDefenseBps = r.ScreenDefenseBps, WithdrawalDefenseBps = r.WithdrawalDefenseBps, HoldDefenseBps = r.HoldDefenseBps,
            ChargeDefenseBps = r.ChargeDefenseBps, SuppressRechargeOnHitSector = r.SuppressRechargeOnHitSector };
        internal static string RulesKey(TacticalRulesV2Dto r) => string.Join(",", new long[] { r.SpeedWeight, r.AgilityWeight, r.SensorsWeight, r.CommandWeight,
            r.CohesionWeight, r.ManeuverTieMargin, r.ArmorNumerator, r.ArmorDenominator, r.DistantTorpedoHitPenaltyBps, r.DistantInterceptionBonusBps,
            r.InterceptionBaseBps, r.ScreenDefenseBps, r.WithdrawalDefenseBps, r.HoldDefenseBps, r.ChargeDefenseBps, r.SuppressRechargeOnHitSector ? 1 : 0 });
        internal static CampaignTacticalPoolDto Copy(CampaignTacticalPoolDto p) => new CampaignTacticalPoolDto {
            ShipId = p.ShipId, FitId = p.FitId, FitRevision = p.FitRevision, ProvenanceId = p.ProvenanceId,
            Shields = p.Shields.OrderBy(q => q.Sector).Select(q => new AuthoredTacticalShieldStockDto { Sector = q.Sector, Current = q.Current }).ToList(),
            Ammunition = p.Ammunition.OrderBy(q => q.WeaponId, StringComparer.Ordinal).Select(q => new AuthoredTacticalAmmoStockDto {
                WeaponId = q.WeaponId, AmmunitionDefinitionId = q.AmmunitionDefinitionId, Stock = q.Stock }).ToList() };
        internal static CampaignTacticalFleetOrderDto Copy(CampaignTacticalFleetOrderDto o) =>
            new CampaignTacticalFleetOrderDto { FleetId = o.FleetId, Maneuver = o.Maneuver, FocusFleetId = o.FocusFleetId };
        internal static CampaignTacticalCommitmentDto Copy(CampaignTacticalCommitmentDto c) => new CampaignTacticalCommitmentDto {
            ActorId = c.ActorId, CommandId = c.CommandId, Orders = c.Orders.OrderBy(o => o.FleetId, StringComparer.Ordinal).Select(Copy).ToList() };
        internal static CampaignTacticalActorReportDto Copy(CampaignTacticalActorReportDto r) =>
            new CampaignTacticalActorReportDto { ActorId = r.ActorId, Sequence = r.Sequence, Round = r.Round, Kind = r.Kind, ShipId = r.ShipId,
                TargetShipId = r.TargetShipId, OwnWeaponId = r.OwnWeaponId, Amount = r.Amount, Sector = r.Sector, Range = r.Range, Detail = r.Detail };
        internal static CampaignTacticalBattleCheckpointDto Copy(CampaignTacticalBattleCheckpointDto b) =>
            new CampaignTacticalBattleCheckpointDto { State = TacticalCombatV2State.Copy(b.State), BoundaryRevision = b.BoundaryRevision,
                HiddenShipIds = b.HiddenShipIds.OrderBy(s => s, StringComparer.Ordinal).ToList(),
                Commitments = b.Commitments.OrderBy(c => c.ActorId, StringComparer.Ordinal).Select(c => new CampaignTacticalCommitmentDto {
                    ActorId = c.ActorId, CommandId = c.CommandId, Orders = c.Orders.OrderBy(o => o.FleetId, StringComparer.Ordinal).Select(Copy).ToList() }).ToList(),
                ResolvedRounds = b.ResolvedRounds.Select(r => new CampaignTacticalResolvedRoundDto { Round = r.Round,
                    Commitments = r.Commitments.OrderBy(c => c.ActorId,StringComparer.Ordinal).Select(Copy).ToList() }).ToList(),
                Reports = b.Reports.Select(Copy).ToList() };
        internal static SaveEnvelopeDto Copy(SaveEnvelopeDto s) => new SaveEnvelopeDto { SaveFormatVersion = s.SaveFormatVersion,
            ScenarioId = s.ScenarioId, RulesVersion = s.RulesVersion, ContentHash = s.ContentHash, GameState = SimulationSession.CopyState(s.GameState) };
        internal static CampaignTacticalReviewSaveDto Copy(CampaignTacticalReviewSaveDto s) => new CampaignTacticalReviewSaveDto {
            FormatVersion = s.FormatVersion, ProfileId = s.ProfileId, PackageHash = s.PackageHash, ResolverId = s.ResolverId, ResolverVersion = s.ResolverVersion,
            Campaign = Copy(s.Campaign), Pools = s.Pools.OrderBy(p => p.ShipId, StringComparer.Ordinal).Select(Copy).ToList(),
            FleetSettings = s.FleetSettings.OrderBy(f => f.FleetId, StringComparer.Ordinal).Select(f => new CampaignTacticalFleetSettingDto { FleetId = f.FleetId, ApproachCloak = f.ApproachCloak }).ToList(),
            Battles = s.Battles.OrderBy(b => b.State.EncounterId, StringComparer.Ordinal).Select(Copy).ToList(),
            AcceptedRoundCommands = s.AcceptedRoundCommands.OrderBy(s2 => s2, StringComparer.Ordinal).ToList() };
        internal static TacticalManeuver Maneuver(CampaignFleetManeuver m) => m == CampaignFleetManeuver.Hold ? TacticalManeuver.MaintainDistance
            : m == CampaignFleetManeuver.Charge ? TacticalManeuver.Charge : m == CampaignFleetManeuver.Flank ? TacticalManeuver.Flank
            : m == CampaignFleetManeuver.Screen ? TacticalManeuver.Screen : TacticalManeuver.FightingWithdrawal;
        internal static CampaignFleetManeuver Maneuver(TacticalManeuver m)
        {
            Require(new[] { TacticalManeuver.MaintainDistance, TacticalManeuver.Charge, TacticalManeuver.Flank, TacticalManeuver.Screen, TacticalManeuver.FightingWithdrawal }.Contains(m),
                "Excluded standing exit maneuver.");
            return m == TacticalManeuver.MaintainDistance ? CampaignFleetManeuver.Hold : m == TacticalManeuver.Charge ? CampaignFleetManeuver.Charge
                : m == TacticalManeuver.Flank ? CampaignFleetManeuver.Flank : m == TacticalManeuver.Screen ? CampaignFleetManeuver.Screen : CampaignFleetManeuver.FightingWithdrawal;
        }
        internal static TacticalShipV2Dto Compile(ShipStateDto ship, AuthoredTacticalFitDto fit, CampaignTacticalPoolDto pool)
        {
            return new TacticalShipV2Dto { ShipId = ship.ShipId, Hull = ship.CurrentHull, MaxHull = fit.MaxHull!.Value, Role = fit.Role!.Value,
                Armor = fit.Armor!.Value, EvasionBps = fit.EvasionBps!.Value,
                PowerBudget = Math.Min(fit.ReactorOutput!.Value, fit.DistributionLimit!.Value) - fit.EssentialReservations!.Sum(e => e.AllocatedPower!.Value),
                ShieldPowerRequired = fit.ShieldRechargeRequiredPower!.Value, ShieldPowerAllocated = fit.ShieldRechargeAllocatedPower!.Value,
                ShieldRechargePerRound = fit.ShieldRechargePerRound!.Value,
                Shields = fit.ShieldLimits!.OrderBy(q => q.Sector).Select(q => new TacticalShieldV2Dto { Sector = q.Sector!.Value, Maximum = q.Maximum!.Value,
                    Current = pool.Shields.Single(s => s.Sector == q.Sector).Current!.Value }).ToList(),
                // Class-specific compiled views share a physical mount; source validation already
                // checks total SlotsRequired across all classes, including disabled packages.
                Mounts = fit.Mounts!.SelectMany(m => m.AllowedWeaponClasses!.Select(c => new TacticalMountV2Dto {
                    MountId = m.MountId + "." + c.ToString().ToLowerInvariant(), WeaponClass = c, Slots = m.Slots!.Value, Coverage = m.Coverage!.Value })).OrderBy(m => m.MountId, StringComparer.Ordinal).ToList(),
                Weapons = fit.Weapons!.OrderBy(w => w.WeaponId, StringComparer.Ordinal).Select(w => new TacticalWeaponV2Dto {
                    WeaponId = w.WeaponId!, MountId = w.MountId + "." + w.Class!.Value.ToString().ToLowerInvariant(), Class = w.Class.Value,
                    Coverage = w.Coverage!.Value, Equipped = true, RequiredPower = w.RequiredPower!.Value, AllocatedPower = w.AllocatedPower!.Value,
                    Damage = w.Damage!.Value, AccuracyBps = w.AccuracyBps!.Value, TrackingBps = w.TrackingBps!.Value,
                    OpportunitiesPerRound = w.OpportunitiesPerRound!.Value, CadenceRounds = w.CadenceRounds!.Value, NextFireRound = w.InitialNextFireRound!.Value,
                    MagazineCapacity = w.MagazineCapacity!.Value,
                    Ammunition = w.Class == TacticalWeaponClass.Torpedo ? pool.Ammunition.Single(a => a.WeaponId == w.WeaponId).Stock!.Value : 0 }).ToList() };
        }
        internal static bool SameHardware(TacticalShipV2Dto a, TacticalShipV2Dto b) =>
            (a.ShipId, a.Role, a.MaxHull, a.Armor, a.EvasionBps, a.PowerBudget, a.ShieldPowerRequired, a.ShieldPowerAllocated, a.ShieldRechargePerRound)
                == (b.ShipId, b.Role, b.MaxHull, b.Armor, b.EvasionBps, b.PowerBudget, b.ShieldPowerRequired, b.ShieldPowerAllocated, b.ShieldRechargePerRound)
            && a.Shields.Select(q => (q.Sector, q.Maximum)).SequenceEqual(b.Shields.Select(q => (q.Sector, q.Maximum)))
            && a.Mounts.Select(m => (m.MountId, m.WeaponClass, m.Coverage, m.Slots)).SequenceEqual(b.Mounts.Select(m => (m.MountId, m.WeaponClass, m.Coverage, m.Slots)))
            && a.Weapons.Select(w => (w.WeaponId, w.MountId, w.Class, w.Coverage, w.Equipped, w.RequiredPower, w.AllocatedPower, w.Damage,
                w.AccuracyBps, w.TrackingBps, w.OpportunitiesPerRound, w.CadenceRounds, w.MagazineCapacity))
            .SequenceEqual(b.Weapons.Select(w => (w.WeaponId, w.MountId, w.Class, w.Coverage, w.Equipped, w.RequiredPower, w.AllocatedPower, w.Damage,
                w.AccuracyBps, w.TrackingBps, w.OpportunitiesPerRound, w.CadenceRounds, w.MagazineCapacity)));
    }
}
