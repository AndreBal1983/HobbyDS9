using System;
using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    // Standalone encounter fixture/round-boundary schema. Never embedded in v1 saves.
    public static class TacticalCombatV2Identity
    {
        public const string ResolverId = "combat-resolver.tactical";
        public const int ResolverVersion = 2;
        public const int FormatVersion = 1;
        public const string RulesId = "tactical-combat-v2-provisional-1";
        public const string RandomAlgorithmId = "splitmix64-rejection-v1";
    }
    public enum TacticalRange { Distant = 0, Engagement = 1, Close = 2, Intermixed = 3 }
    public enum TacticalSector { Fore = 0, Port = 1, Starboard = 2, Aft = 3 }
    [Flags] public enum TacticalArc { None = 0, Fore = 1, Port = 2, Starboard = 4, Aft = 8, All = 15 }
    public enum TacticalWeaponClass { Beam = 1, Cannon = 2, Torpedo = 3 }
    public enum TacticalManeuver { MaintainDistance = 1, Charge = 2, Flank = 3, Screen = 4, Breakthrough = 5, Pursuit = 6, FightingWithdrawal = 7, Disengage = 8 }
    public enum TacticalTargetPriority { LowestId = 1, LowestHullRatio = 2, CommandSupport = 3, Screen = 4 }
    public enum TacticalShipRole { Line = 1, CommandSupport = 2, Escort = 3 }
    // These are local encounter goals; they confer no strategic consequences.
    public enum TacticalObjective { Defeat = 1, Hold = 2, Delay = 3, Breakthrough = 4, Disengage = 5 }
    public enum TacticalFormationStatus { Active = 0, Destroyed = 1, Retreated = 2, BrokenThrough = 3 }
    public enum TacticalCompletion { None = 0, Decisive = 1, Objective = 2, RoundLimit = 3 }
    public enum TacticalEventKind
    {
        RoundStarted, OrderCommitted, Maneuver, FiringOpportunity, WeaponDischarged,
        InterceptionAttempt, TorpedoIntercepted, AttackHit, AttackMissed, ShieldImpact,
        ArmorMitigation, HullDamage, ShipDestroyed, ObjectiveProgress, ObjectiveCompleted,
        FormationDestroyed, Retreated, BrokenThrough, ShieldRecharged, EncounterCompleted
    }
    public sealed class TacticalRulesV2Dto
    {
        // All numerical choices are provisional fixture tuning, frozen with the state.
        public int SpeedWeight { get; set; } = 4;
        public int AgilityWeight { get; set; } = 3;
        public int SensorsWeight { get; set; } = 1;
        public int CommandWeight { get; set; } = 1;
        public int CohesionWeight { get; set; } = 1;
        public int ManeuverTieMargin { get; set; } = 10;
        public int ArmorNumerator { get; set; } = 6;
        public int ArmorDenominator { get; set; } = 100;
        public int DistantTorpedoHitPenaltyBps { get; set; } = 1500;
        public int DistantInterceptionBonusBps { get; set; } = 1000;
        public int InterceptionBaseBps { get; set; } = 3000;
        public int ScreenDefenseBps { get; set; } = 10000;
        public int WithdrawalDefenseBps { get; set; } = 10000;
        public int HoldDefenseBps { get; set; } = 5000;
        public int ChargeDefenseBps { get; set; } = 0;
        public bool SuppressRechargeOnHitSector { get; set; } = true;
    }
    public sealed class TacticalOrderV2Dto
    {
        public string FormationId { get; set; } = string.Empty;
        public TacticalManeuver Maneuver { get; set; } = TacticalManeuver.MaintainDistance;
        public TacticalTargetPriority TargetPriority { get; set; } = TacticalTargetPriority.LowestId;
        public TacticalRange PreferredRange { get; set; } = TacticalRange.Engagement;
        // Optional lawful enemy formation concentration; no subsystem targeting.
        public string? TargetFormationId { get; set; }
        public string? ProtectedFormationId { get; set; }
    }
    public sealed class TacticalOrderBatchV2Dto
    {
        public string EncounterId { get; set; } = string.Empty;
        public int Round { get; set; }
        // Joint commitment supplied by a trusted host. No partial order reveal API.
        public List<TacticalOrderV2Dto> Replacements { get; set; } = new List<TacticalOrderV2Dto>();
    }
    public sealed class TacticalShieldV2Dto
    {
        public TacticalSector Sector { get; set; }
        public long Maximum { get; set; }
        public long Current { get; set; }
    }
    public sealed class TacticalMountV2Dto
    {
        public string MountId { get; set; } = string.Empty;
        public TacticalWeaponClass WeaponClass { get; set; }
        public TacticalArc Coverage { get; set; } = TacticalArc.All;
        public int Slots { get; set; } = 1;
    }
    public sealed class TacticalWeaponV2Dto
    {
        public string WeaponId { get; set; } = string.Empty;
        public string MountId { get; set; } = string.Empty;
        public TacticalWeaponClass Class { get; set; }
        public TacticalArc Coverage { get; set; } = TacticalArc.All;
        public bool Equipped { get; set; } = true;
        public long RequiredPower { get; set; } = 1;
        public long AllocatedPower { get; set; } = 1;
        public long Damage { get; set; }
        public int AccuracyBps { get; set; } = 8000;
        public int TrackingBps { get; set; } = 3000;
        // One opportunity is either an offensive discharge or one interception attempt.
        public int OpportunitiesPerRound { get; set; } = 1;
        public int CadenceRounds { get; set; } = 1;
        public int NextFireRound { get; set; } = 1;
        public long MagazineCapacity { get; set; }
        public long Ammunition { get; set; }
    }
    public sealed class TacticalShipV2Dto
    {
        public string ShipId { get; set; } = string.Empty;
        public TacticalShipRole Role { get; set; } = TacticalShipRole.Line;
        public long Hull { get; set; }
        public long MaxHull { get; set; }
        public long Armor { get; set; }
        public int EvasionBps { get; set; }
        public long PowerBudget { get; set; } = 100;
        public long ShieldPowerRequired { get; set; } = 1;
        public long ShieldPowerAllocated { get; set; } = 1;
        public long ShieldRechargePerRound { get; set; }
        public List<TacticalShieldV2Dto> Shields { get; set; } = new List<TacticalShieldV2Dto>();
        public List<TacticalMountV2Dto> Mounts { get; set; } = new List<TacticalMountV2Dto>();
        public List<TacticalWeaponV2Dto> Weapons { get; set; } = new List<TacticalWeaponV2Dto>();
    }
    public sealed class TacticalFormationV2Dto
    {
        public string FormationId { get; set; } = string.Empty;
        // Explicit encounter sides: distinct sides are hostile; same side is allied.
        public string SideId { get; set; } = string.Empty;
        public int Speed { get; set; } = 50;
        public int Agility { get; set; } = 50;
        public int Sensors { get; set; } = 50;
        public int Command { get; set; } = 50;
        public int Cohesion { get; set; } = 50;
        public TacticalFormationStatus Status { get; set; }
        public TacticalOrderV2Dto StandingOrder { get; set; } = new TacticalOrderV2Dto();
        public TacticalObjective Objective { get; set; } = TacticalObjective.Defeat;
        public int ObjectiveRequiredRounds { get; set; } = 2;
        public int ObjectiveProgress { get; set; }
        public bool ObjectiveCompleted { get; set; }
        public List<TacticalShipV2Dto> Ships { get; set; } = new List<TacticalShipV2Dto>();
    }
    public sealed class TacticalGeometryV2Dto
    {
        // Exactly one canonical pair for every opposing formation pair (including exited).
        public string FormationA { get; set; } = string.Empty;
        public string FormationB { get; set; } = string.Empty;
        public TacticalRange Range { get; set; } = TacticalRange.Engagement;
        // Which facing on each formation points toward the other formation.
        public TacticalSector FacingA { get; set; } = TacticalSector.Fore;
        public TacticalSector FacingB { get; set; } = TacticalSector.Fore;
    }
    public sealed class TacticalEncounterV2Dto
    {
        public int FormatVersion { get; set; } = TacticalCombatV2Identity.FormatVersion;
        public string ResolverId { get; set; } = TacticalCombatV2Identity.ResolverId;
        public int ResolverVersion { get; set; } = TacticalCombatV2Identity.ResolverVersion;
        public string RulesId { get; set; } = TacticalCombatV2Identity.RulesId;
        public string RandomAlgorithmId { get; set; } = TacticalCombatV2Identity.RandomAlgorithmId;
        public string EncounterId { get; set; } = string.Empty;
        public ulong RandomState { get; set; }
        public int NextRound { get; set; } = 1;
        public TacticalCompletion Completion { get; set; }
        public TacticalRulesV2Dto Rules { get; set; } = new TacticalRulesV2Dto();
        public List<TacticalFormationV2Dto> Formations { get; set; } = new List<TacticalFormationV2Dto>();
        public List<TacticalGeometryV2Dto> Geometry { get; set; } = new List<TacticalGeometryV2Dto>();
    }
    // Immutable presentation data; displaying/replaying these values consumes no RNG.
    public sealed class TacticalEventV2
    {
        public int Sequence { get; }
        public int Round { get; }
        public TacticalEventKind Kind { get; }
        public string? FormationId { get; }
        public string? ShipId { get; }
        public string? TargetFormationId { get; }
        public string? TargetShipId { get; }
        public string? WeaponId { get; }
        public TacticalSector? Sector { get; }
        public TacticalRange? Range { get; }
        public long Amount { get; }
        public int ChanceBps { get; }
        public string Detail { get; }
        public TacticalEventV2(int sequence, int round, TacticalEventKind kind, string? formationId,
            string? shipId, string? targetFormationId, string? targetShipId, string? weaponId,
            TacticalSector? sector, TacticalRange? range, long amount, int chanceBps, string detail)
        {
            Sequence = sequence; Round = round; Kind = kind; FormationId = formationId; ShipId = shipId;
            TargetFormationId = targetFormationId; TargetShipId = targetShipId; WeaponId = weaponId;
            Sector = sector; Range = range; Amount = amount; ChanceBps = chanceBps; Detail = detail;
        }
    }
}
