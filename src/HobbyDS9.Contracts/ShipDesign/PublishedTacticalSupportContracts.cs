using System;
using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts.ShipDesign
{
    // Standalone derivation inputs only. No catalogue here enables a campaign profile.
    public static class PublishedTacticalSupportIdentity
    {
        public const int Format=1;
        public const string Schema="published-tactical-support-v1", Compiler="published-tactical-support-compiler-v1";
        public const string NoCloak="unsupported-no-cloak-v1";
    }
    public enum TacticalComponentSupportKind { Reactor=1, LifeSupport=2, Command=3, Propulsion=4, Beam=5, TargetingSensors=6 }
    public sealed class TacticalSupportCatalogueDto
    {
        public int? FormatVersion {get;set;}
        public string? SchemaId {get;set;}
        public string? CatalogueId {get;set;}
        public int? Revision {get;set;}
        public string? SourceCompilerId {get;set;}
        public string? BalanceStatus {get;set;}
        public string? CloakPolicyId {get;set;}
        // Explicit split of an existing command allocation; never additional free power.
        public long? TargetingPowerFromCommandWhenArmed {get;set;}
        public List<TacticalHullSupportDto>? Hulls {get;set;}
        public List<TacticalComponentSupportDto>? Components {get;set;}
    }
    public sealed class TacticalHullSupportDto
    {
        public string? HullVersionId {get;set;}
        public List<string>? SupportedRoleIds {get;set;}
        public long? Armor {get;set;}
        public TacticalShipRole? TacticalRole {get;set;}
        public AuthoredTacticalCapabilitiesDto? Capabilities {get;set;}
        public List<TacticalMountSupportDto>? Mounts {get;set;}
    }
    public sealed class TacticalMountSupportDto
    {
        public string? SourceMountId {get;set;}
        public string? SourceCoverageTag {get;set;}
        public TacticalArc? Coverage {get;set;}
        public int? Slots {get;set;}
    }
    public sealed class TacticalComponentSupportDto
    {
        public string? ComponentVersionId {get;set;}
        public TacticalComponentSupportKind? Kind {get;set;}
        public int? TacticalSensorRating {get;set;}
        public TacticalBeamSupportDto? Beam {get;set;}
    }
    public sealed class TacticalBeamSupportDto
    {
        public string? SourceMountId {get;set;}
        public int? SlotsRequired {get;set;}
        public TacticalArc? Coverage {get;set;}
        public long? Damage {get;set;}
        public int? AccuracyBps {get;set;}
        public int? TrackingBps {get;set;}
        public int? OpportunitiesPerRound {get;set;}
        public int? CadenceRounds {get;set;}
        public int? InitialNextFireRound {get;set;}
    }
    public enum TacticalSupportIssueCode
    {
        InvalidMetadata=1, InvalidPublication=2, OrdinaryDesignRejected=3, MissingMapping=4,
        UnsupportedComponent=5, UnsupportedQuantity=6, UnsupportedRole=7, CombatPower=8,
        MountBinding=9, TacticalValidation=10, IdentityMismatch=11
    }
    public sealed class TacticalSupportIssue
    {
        public TacticalSupportIssueCode Code {get;}
        public string Path {get;}
        public TacticalSupportIssue(TacticalSupportIssueCode code,string path){Code=code;Path=path;}
    }
    // Detached projection, deliberately not a ship provision, queue or command payload.
    public sealed class SupportedPublishedTacticalFit
    {
        public string BlueprintId {get;}
        public int BlueprintRevision {get;}
        public string ClassId {get;}
        public string CompilerId=>PublishedTacticalSupportIdentity.Compiler;
        public string SupportCatalogueId {get;}
        public int SupportCatalogueRevision {get;}
        public string SupportHash {get;}
        public string SourceHash {get;}
        public string FitId {get;}
        public int FitRevision {get;}
        public string ProvenanceId {get;}
        public long MaxHull {get;}
        public long Armor {get;}
        public int EvasionBps {get;}
        public TacticalShipRole Role {get;}
        public AuthoredTacticalCapabilitiesSnapshot Capabilities {get;}
        public long ReactorOutput {get;}
        public long DistributionLimit {get;}
        public long MovementBudget {get;}
        public ShipDesignCostSnapshot Cost {get;}
        public string CloakPolicyId=>PublishedTacticalSupportIdentity.NoCloak;
        public bool CloakCapable=>false;
        public long CloakConcurrentPowerRequired=>0;
        public long ShieldRechargePerRound=>0;
        public long ShieldRechargeRequiredPower=>0;
        public long ShieldRechargeAllocatedPower=>0;
        public IReadOnlyList<AuthoredTacticalEssentialPowerSnapshot> EssentialReservations {get;}
        public IReadOnlyList<AuthoredTacticalMountSnapshot> Mounts {get;}
        public IReadOnlyList<AuthoredTacticalWeaponSnapshot> Weapons {get;}
        // This compiler supports no shields, magazines or provisioning transaction.
        public IReadOnlyList<TacticalSector> ZeroShieldSectors {get;}
        public SupportedPublishedTacticalFit(PublishedShipBlueprint source,TacticalSupportCatalogueDto support,
            string supportHash,string sourceHash,AuthoredTacticalFitDto fit)
        {
            BlueprintId=source.BlueprintId;BlueprintRevision=source.Revision;ClassId=source.Fit.ClassId;
            SupportCatalogueId=support.CatalogueId!;SupportCatalogueRevision=support.Revision!.Value;
            SupportHash=supportHash;SourceHash=sourceHash;FitId=fit.DefinitionId!;FitRevision=fit.Revision!.Value;ProvenanceId=fit.ProvenanceId!;
            MaxHull=fit.MaxHull!.Value;Armor=fit.Armor!.Value;EvasionBps=fit.EvasionBps!.Value;Role=fit.Role!.Value;
            Capabilities=new AuthoredTacticalCapabilitiesSnapshot(fit.Capabilities!);ReactorOutput=fit.ReactorOutput!.Value;DistributionLimit=fit.DistributionLimit!.Value;
            MovementBudget=source.Performance.Compiled!.MovementBudget;Cost=source.Performance.Cost!;
            EssentialReservations=fit.EssentialReservations!.OrderBy(x=>x.System).Select(x=>new AuthoredTacticalEssentialPowerSnapshot(x)).ToList().AsReadOnly();
            Mounts=fit.Mounts!.OrderBy(x=>x.MountId,StringComparer.Ordinal).Select(x=>new AuthoredTacticalMountSnapshot(x)).ToList().AsReadOnly();
            Weapons=fit.Weapons!.OrderBy(x=>x.WeaponId,StringComparer.Ordinal).Select(x=>new AuthoredTacticalWeaponSnapshot(x,0)).ToList().AsReadOnly();
            ZeroShieldSectors=Enum.GetValues(typeof(TacticalSector)).Cast<TacticalSector>().OrderBy(x=>x).ToList().AsReadOnly();
        }
    }
    public sealed class PublishedTacticalSupportResult
    {
        public bool IsSupportedByCompiler=>Fit!=null && Issues.Count==0;
        public IReadOnlyList<TacticalSupportIssue> Issues {get;}
        public IReadOnlyList<DesignIssue> OrdinaryIssues {get;}
        public SupportedPublishedTacticalFit? Fit {get;}
        public PublishedTacticalSupportResult(IEnumerable<TacticalSupportIssue> issues,SupportedPublishedTacticalFit? fit=null,IEnumerable<DesignIssue>? ordinaryIssues=null)
        {Issues=issues.OrderBy(x=>x.Path,StringComparer.Ordinal).ThenBy(x=>x.Code).ToList().AsReadOnly();OrdinaryIssues=(ordinaryIssues??Array.Empty<DesignIssue>()).ToList().AsReadOnly();Fit=Issues.Count==0?fit:null;}
    }
}
