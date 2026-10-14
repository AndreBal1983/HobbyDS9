using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Contracts
{
    // Explicit review package/save namespace. No v1 migration or default activation.
    public static class CampaignTacticalReviewIdentity
    {
        public const int PackageFormat = 1, SaveFormat = 1;
        public const string Profile = "campaign-tactical-v2-lifecycle-review-1";
        public const string Resolver = "combat-resolver.campaign-tactical-review";
        public const int ResolverVersion = 1;
    }
    public sealed class CampaignTacticalReviewPackageDto
    {
        public int? FormatVersion { get; set; }
        public string? PackageHash { get; set; }
        public string? ProfileId { get; set; }
        public string? PolicyStatus { get; set; }
        public bool? DefaultActivationAllowed { get; set; }
        public ScenarioDto? Campaign { get; set; }
        public TacticalRulesV2Dto? Rules { get; set; }
        public List<CampaignTacticalCloakSourceDto>? CloakSources { get; set; }
    }
    public sealed class CampaignTacticalCloakSourceDto
    {
        public string? FitId { get; set; }
        public int? FitRevision { get; set; }
        public bool? Capable { get; set; }
        public long? ConcurrentPowerRequired { get; set; }
    }
    public sealed class CampaignTacticalPoolDto
    {
        public string ShipId { get; set; } = "";
        public string FitId { get; set; } = "";
        public int FitRevision { get; set; }
        public string ProvenanceId { get; set; } = "";
        public List<AuthoredTacticalShieldStockDto> Shields { get; set; } = new List<AuthoredTacticalShieldStockDto>();
        public List<AuthoredTacticalAmmoStockDto> Ammunition { get; set; } = new List<AuthoredTacticalAmmoStockDto>();
    }
    public sealed class CampaignTacticalFleetSettingDto
    {
        public string FleetId { get; set; } = "";
        public bool ApproachCloak { get; set; }
    }
    public sealed class CampaignTacticalReviewSaveDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string PackageHash { get; set; } = "";
        public string ResolverId { get; set; } = "";
        public int ResolverVersion { get; set; }
        public SaveEnvelopeDto Campaign { get; set; } = null!;
        public List<CampaignTacticalPoolDto> Pools { get; set; } = null!;
        public List<CampaignTacticalFleetSettingDto> FleetSettings { get; set; } = null!;
        public List<CampaignTacticalBattleCheckpointDto> Battles { get; set; } = null!;
        public List<string> AcceptedRoundCommands { get; set; } = null!;
    }
    public sealed class CampaignTacticalBattleCheckpointDto
    {
        public TacticalEncounterV2Dto State { get; set; } = null!;
        public long BoundaryRevision { get; set; }
        public List<string> HiddenShipIds { get; set; } = null!;
        public List<CampaignTacticalCommitmentDto> Commitments { get; set; } = null!;
        public List<CampaignTacticalActorReportDto> Reports { get; set; } = null!;
        public List<CampaignTacticalResolvedRoundDto> ResolvedRounds { get; set; } = null!;
    }
    public sealed class CampaignTacticalResolvedRoundDto
    {
        public int Round { get; set; }
        public List<CampaignTacticalCommitmentDto> Commitments { get; set; } = null!;
    }
    public enum CampaignFleetManeuver { Hold = 1, Charge = 2, Flank = 3, Screen = 4, FightingWithdrawal = 5 }
    public sealed class CampaignTacticalFleetOrderDto
    {
        public string FleetId { get; set; } = "";
        public CampaignFleetManeuver Maneuver { get; set; }
        public string? FocusFleetId { get; set; }
    }
    public sealed class CampaignTacticalCommitmentDto
    {
        public string ActorId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public List<CampaignTacticalFleetOrderDto> Orders { get; set; } = null!;
    }
    public sealed class CommitCampaignTacticalRoundDto
    {
        public string ActorId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public string EncounterId { get; set; } = "";
        public int? ExpectedTurn { get; set; }
        public int? ExpectedRound { get; set; }
        public long? ExpectedBoundaryRevision { get; set; }
        // Changed orders only. Omitted owned fleets explicitly retain validated standing intent.
        public List<CampaignTacticalFleetOrderDto>? Replacements { get; set; }
    }
    public enum CampaignTacticalError
    {
        None, WrongPhase, UnknownActor, ActorNotControlled, StaleTurn, StaleRevision,
        UnknownEncounter, StaleBoundary, DuplicateCommand, InvalidOrder, AlreadyCommitted,
        UnsupportedSource, IncompleteBatch, InvalidBatch, Overflow
    }
    public sealed class CampaignTacticalMutationResult
    {
        public CampaignTacticalError Error { get; }
        public bool Accepted => Error == CampaignTacticalError.None;
        public long CampaignRevision { get; }
        public CampaignTacticalMutationResult(CampaignTacticalError error, long revision)
        { Error = error; CampaignRevision = revision; }
    }
    public sealed class CampaignTacticalReviewStatus
    {
        public int Turn { get; }
        public long CampaignRevision { get; }
        public SessionPhase Phase { get; }
        public bool CanFinalizeBatch { get; }
        public bool ProductionActivationAllowed => false;
        public string TravelModel { get; }
        public string ContactAdmission { get; }
        public CampaignTacticalReviewStatus(int turn, long revision, SessionPhase phase, bool canFinalize,bool physical=false)
        { Turn = turn; CampaignRevision = revision; Phase = phase; CanFinalizeBatch = canFinalize;
            TravelModel=physical?"exact shared time; proposed review profile":"legacy-cost-step; physical-contact integration pending";
            ContactAdmission=physical?"declared hostility, lawful tracking and explicit intent; proposed":"legacy-review-allocation; explicit hostility/intent pending"; }
    }
    public sealed class CampaignTacticalFleetCloakSetting
    {
        public string FleetId { get; }
        public bool Enabled { get; }
        public bool CanChange { get; }
        public IReadOnlyList<string> CapableShipIds { get; }
        public CampaignTacticalFleetCloakSetting(string fleet, bool enabled, bool canChange, IEnumerable<string> capable)
        { FleetId = fleet; Enabled = enabled; CanChange = canChange; CapableShipIds = capable.OrderBy(s => s,StringComparer.Ordinal).ToList().AsReadOnly(); }
    }
    public sealed class CampaignTacticalActorReportDto
    {
        public string ActorId { get; set; } = "";
        public int Sequence { get; set; }
        public int Round { get; set; }
        public string Kind { get; set; } = "";
        public string? ShipId { get; set; }
        public string? TargetShipId { get; set; }
        public string? OwnWeaponId { get; set; }
        public long Amount { get; set; }
        public TacticalSector? Sector { get; set; }
        public TacticalRange? Range { get; set; }
        public string Detail { get; set; } = "";
    }
    public sealed class CampaignTacticalCondition
    {
        public string ShipId { get; }
        public string FleetId { get; }
        public string Type { get; }
        public long Hull { get; }
        public long MaxHull { get; }
        public IReadOnlyList<long> Shields { get; }
        public CampaignTacticalCondition(string ship, string fleet, string type, long hull, long max, IEnumerable<long> shields)
        { ShipId = ship; FleetId = fleet; Type = type; Hull = hull; MaxHull = max; Shields = shields.ToList().AsReadOnly(); }
    }
    [Flags]
    public enum CampaignWeaponOwnBlockers
    {
        None=0, NotEquipped=1, PowerIncomplete=2, NoDamage=4, NoAmmunition=8, CadenceWait=16,
        ShipDestroyed=32, FormationInactive=64, EncounterComplete=128
    }
    public enum CampaignWeaponAvailabilityStatus { OutsideEncounter=0, OwnStateBlocked=1, TargetDependent=2 }
    public sealed class CampaignWeaponAvailability
    {
        public CampaignWeaponAvailabilityStatus Status { get; }
        public CampaignWeaponOwnBlockers OwnBlockers { get; }
        public int? Round { get; }
        // No shot count, target readiness or outcome promise: this is a boundary projection.
        public CampaignWeaponAvailability(CampaignWeaponAvailabilityStatus status,CampaignWeaponOwnBlockers blockers,int? round)
        {Status=status;OwnBlockers=blockers;Round=round;}
    }
    public enum CampaignFocusFallbackCause { PreferredFocusUnavailableForOpportunity=0 }
    public sealed class CampaignTacticalOwnWeapon
    {
        public string WeaponId { get; }
        public TacticalWeaponClass Class { get; }
        public long Ammunition { get; }
        public long MagazineCapacity { get; }
        public int NextFireRound { get; }
        public bool Powered { get; }
        public string PackageVersionId { get; }
        public string MountId { get; }
        public int SlotsRequired { get; }
        public TacticalArc Coverage { get; }
        public long RequiredPower { get; }
        public long AllocatedPower { get; }
        public long Damage { get; }
        public int AccuracyBps { get; }
        public int TrackingBps { get; }
        public int OpportunitiesPerRound { get; }
        public int CadenceRounds { get; }
        public string AmmunitionDefinitionId { get; }
        public CampaignWeaponAvailability Availability { get; }
        public CampaignTacticalOwnWeapon(TacticalWeaponV2Dto w, AuthoredTacticalWeaponDto source):this(w,source,null) { }
        public CampaignTacticalOwnWeapon(TacticalWeaponV2Dto w, AuthoredTacticalWeaponDto source,CampaignWeaponAvailability? availability)
        { WeaponId = w.WeaponId; Class = w.Class; Ammunition = w.Ammunition; MagazineCapacity = w.MagazineCapacity;
            NextFireRound = w.NextFireRound; Powered = w.Equipped && w.RequiredPower == w.AllocatedPower;
            PackageVersionId = source.PackageVersionId!; MountId = source.MountId!; SlotsRequired = source.SlotsRequired!.Value;
            Coverage = w.Coverage; RequiredPower = w.RequiredPower; AllocatedPower = w.AllocatedPower; Damage = w.Damage;
            AccuracyBps = w.AccuracyBps; TrackingBps = w.TrackingBps; OpportunitiesPerRound = w.OpportunitiesPerRound;
            CadenceRounds = w.CadenceRounds; AmmunitionDefinitionId = source.AmmunitionDefinitionId!;
            Availability=availability??new CampaignWeaponAvailability(CampaignWeaponAvailabilityStatus.OutsideEncounter,CampaignWeaponOwnBlockers.None,null); }
    }
    public sealed class CampaignTacticalOwnShip
    {
        public CampaignTacticalCondition Condition { get; }
        public string FitId { get; }
        public int FitRevision { get; }
        public string ProvenanceId { get; }
        public bool Cloaked { get; }
        public IReadOnlyList<CampaignTacticalOwnWeapon> Weapons { get; }
        public bool Unarmed => Weapons.Count==0;
        public long Armor { get; }
        public int EvasionBps { get; }
        public AuthoredTacticalCapabilitiesSnapshot Capabilities { get; }
        public IReadOnlyList<long> ShieldMaximum { get; }
        public IReadOnlyList<AuthoredTacticalMountSnapshot> Mounts { get; }
        public IReadOnlyList<AuthoredTacticalEssentialPowerSnapshot> EssentialReservations { get; }
        public long EffectivePower { get; }
        public long EquipmentPowerBudget { get; }
        public long CloakPowerReserved { get; }
        public bool CloakCapable { get; }
        public long ShieldPowerRequired { get; }
        public long ShieldPowerAllocated { get; }
        public long ShieldRechargePerRound { get; }
        public CampaignTacticalOwnShip(CampaignTacticalCondition condition, CampaignTacticalPoolDto pool, bool cloaked,
            TacticalShipV2Dto ship, AuthoredTacticalFitDto source, bool cloakCapable, long cloakPower)
            :this(condition,pool,cloaked,ship,source,cloakCapable,cloakPower,null) { }
        public CampaignTacticalOwnShip(CampaignTacticalCondition condition, CampaignTacticalPoolDto pool, bool cloaked,
            TacticalShipV2Dto ship, AuthoredTacticalFitDto source, bool cloakCapable, long cloakPower,
            IReadOnlyDictionary<string,CampaignWeaponAvailability>? availability)
        { Condition = condition; FitId = pool.FitId; FitRevision = pool.FitRevision; ProvenanceId = pool.ProvenanceId; Cloaked = cloaked;
            Weapons = ship.Weapons.OrderBy(w=>w.WeaponId,StringComparer.Ordinal).Select(w => new CampaignTacticalOwnWeapon(w,source.Weapons!.Single(s => s.WeaponId == w.WeaponId),
                availability!=null&&availability.TryGetValue(w.WeaponId,out var value)?value:null)).ToList().AsReadOnly();
            Armor = ship.Armor; EvasionBps = ship.EvasionBps; Capabilities = new AuthoredTacticalCapabilitiesSnapshot(source.Capabilities!);
            ShieldMaximum = ship.Shields.OrderBy(q => q.Sector).Select(q => q.Maximum).ToList().AsReadOnly();
            Mounts = source.Mounts!.Select(m => new AuthoredTacticalMountSnapshot(m)).ToList().AsReadOnly();
            EssentialReservations = source.EssentialReservations!.Select(e => new AuthoredTacticalEssentialPowerSnapshot(e)).ToList().AsReadOnly();
            EffectivePower = Math.Min(source.ReactorOutput!.Value,source.DistributionLimit!.Value); CloakPowerReserved = cloaked ? cloakPower : 0;
            EquipmentPowerBudget = ship.PowerBudget - CloakPowerReserved; CloakCapable = cloakCapable;
            ShieldPowerRequired = ship.ShieldPowerRequired; ShieldPowerAllocated = ship.ShieldPowerAllocated; ShieldRechargePerRound = ship.ShieldRechargePerRound; }
    }
    public sealed class CampaignTacticalStandingOrder
    {
        public string FleetId { get; }
        public CampaignFleetManeuver Maneuver { get; }
        public string? FocusFleetId { get; }
        public bool CanOrder { get; }
        public CampaignTacticalStandingOrder(string fleet, CampaignFleetManeuver maneuver, string? focus, bool canOrder = true)
        { FleetId = fleet; Maneuver = maneuver; FocusFleetId = focus; CanOrder = canOrder; }
    }
    public sealed class CampaignTacticalRelation
    {
        public string OwnFleetId { get; }
        public string VisibleEnemyFleetId { get; }
        public TacticalRange Range { get; }
        public TacticalSector OwnFacing { get; }
        public TacticalSector EnemyFacing { get; }
        public CampaignTacticalRelation(string own, string enemy, TacticalRange range, TacticalSector ownFacing, TacticalSector enemyFacing)
        { OwnFleetId=own;VisibleEnemyFleetId=enemy;Range=range;OwnFacing=ownFacing;EnemyFacing=enemyFacing; }
    }
    public sealed class CampaignTacticalReportEntry
    {
        public int Sequence { get; }
        public int Round { get; }
        public string Kind { get; }
        public string? ShipId { get; }
        public string? TargetShipId { get; }
        public string? OwnWeaponId { get; }
        public long Amount { get; }
        public TacticalSector? Sector { get; }
        public TacticalRange? Range { get; }
        public string Detail { get; }
        public CampaignFocusFallbackCause? FocusFallbackCause { get; }
        public CampaignTacticalReportEntry(CampaignTacticalActorReportDto e)
        { Sequence = e.Sequence; Round = e.Round; Kind = e.Kind; ShipId = e.ShipId; TargetShipId = e.TargetShipId;
            OwnWeaponId = e.OwnWeaponId; Amount = e.Amount; Sector = e.Sector; Range = e.Range; Detail = e.Detail;
            FocusFallbackCause=e.Kind=="FocusFallback"?CampaignFocusFallbackCause.PreferredFocusUnavailableForOpportunity:(CampaignFocusFallbackCause?)null; }
    }
    public sealed class CampaignTacticalObservation
    {
        public string ActorId { get; }
        public string LocationNodeId { get; }
        public string ObservationPolicy => "exact-visible-hull-condition-and-sector-stocks-review-1";
        public string EncounterId { get; }
        public int Turn { get; }
        public int Round { get; }
        public long BoundaryRevision { get; }
        public bool OwnCommitmentAccepted { get; }
        public string? OwnAcceptedCommandId { get; }
        public bool Complete { get; }
        public bool ProductionActivationAllowed => false;
        public IReadOnlyList<CampaignTacticalOwnShip> OwnShips { get; }
        public IReadOnlyList<CampaignTacticalCondition> VisibleEnemies { get; }
        public IReadOnlyList<CampaignTacticalStandingOrder> StandingOrders { get; }
        public IReadOnlyList<CampaignTacticalStandingOrder> OwnCommittedOrders { get; }
        public IReadOnlyList<CampaignFleetManeuver> LegalManeuvers { get; }
        public IReadOnlyList<string> LegalFocusFleetIds { get; }
        public IReadOnlyList<CampaignTacticalRelation> Relations { get; }
        public IReadOnlyList<CampaignTacticalReportEntry> Reports { get; }
        public CampaignTacticalObservation(string encounter, int turn, int round, long revision, bool accepted, bool complete,
            IEnumerable<CampaignTacticalOwnShip> own, IEnumerable<CampaignTacticalCondition> enemies,
            IEnumerable<CampaignTacticalStandingOrder> orders, IEnumerable<CampaignTacticalReportEntry> reports,
            string? acceptedCommand, IEnumerable<CampaignTacticalStandingOrder> committedOrders, IEnumerable<CampaignTacticalRelation> relations,
            string actor, string location)
        { EncounterId = encounter; Turn = turn; Round = round; BoundaryRevision = revision; OwnCommitmentAccepted = accepted; Complete = complete;
            OwnShips = own.ToList().AsReadOnly(); VisibleEnemies = enemies.ToList().AsReadOnly();
            StandingOrders = orders.ToList().AsReadOnly(); Reports = reports.ToList().AsReadOnly();
            OwnAcceptedCommandId = acceptedCommand; OwnCommittedOrders = committedOrders.ToList().AsReadOnly();
            Relations = relations.ToList().AsReadOnly(); ActorId = actor; LocationNodeId = location;
            LegalManeuvers = Enum.GetValues(typeof(CampaignFleetManeuver)).Cast<CampaignFleetManeuver>().ToList().AsReadOnly();
            LegalFocusFleetIds = VisibleEnemies.Select(e => e.FleetId).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToList().AsReadOnly(); }
    }
}
