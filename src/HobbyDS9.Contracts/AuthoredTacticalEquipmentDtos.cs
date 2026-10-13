using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class AuthoredTacticalEquipmentIdentity
    {
        // Content 10 remains the legacy package. Content 11 adds this required catalogue.
        public const int ContentFormatVersion = 11;
        public const int FormatVersion = 1;
        public const string SchemaId = "authored-tactical-v2-equipment-v1";
        public const string ProjectionId = "authored-tactical-v2-equipment-projection-v1";
    }

    public enum TacticalEssentialSystem { LifeSupport = 1, Command = 2, Propulsion = 3, Targeting = 4 }
    public enum CampaignTacticalSourceKind { LegacyStatic = 1, FrozenBlueprintV1 = 2, AuthoredEquipmentV2 = 3 }

    // No defaults: omission differs from explicit zero/empty in every required field.
    // These are content inputs, not mutable campaign stocks or a battle fixture.
    public sealed class AuthoredTacticalEquipmentCatalogueDto
    {
        public int? FormatVersion { get; set; }
        public string? SchemaId { get; set; }
        public string? ProvenanceId { get; set; }
        public string? BalanceStatus { get; set; }
        public List<AuthoredTacticalAmmoDefinitionDto>? AmmunitionDefinitions { get; set; }
        public List<AuthoredTacticalFitDto>? Fits { get; set; }
        public List<AuthoredTacticalShipProvisionDto>? InitialShipProvisions { get; set; }
    }
    public sealed class AuthoredTacticalAmmoDefinitionDto
    {
        public string? DefinitionId { get; set; }
        public string? DisplayName { get; set; }
    }
    public sealed class AuthoredTacticalFitDto
    {
        public string? DefinitionId { get; set; }
        public int? Revision { get; set; }
        public string? ProvenanceId { get; set; }
        public long? MaxHull { get; set; }
        public long? Armor { get; set; }
        public int? EvasionBps { get; set; }
        public TacticalShipRole? Role { get; set; }
        public AuthoredTacticalCapabilitiesDto? Capabilities { get; set; }
        public long? ReactorOutput { get; set; }
        public long? DistributionLimit { get; set; }
        public List<AuthoredTacticalEssentialPowerDto>? EssentialReservations { get; set; }
        public long? ShieldRechargeRequiredPower { get; set; }
        public long? ShieldRechargeAllocatedPower { get; set; }
        public long? ShieldRechargePerRound { get; set; }
        public List<AuthoredTacticalShieldLimitDto>? ShieldLimits { get; set; }
        public List<AuthoredTacticalMountDto>? Mounts { get; set; }
        public List<AuthoredTacticalWeaponDto>? Weapons { get; set; }
    }
    public sealed class AuthoredTacticalCapabilitiesDto
    {
        public int? Speed { get; set; }
        public int? Agility { get; set; }
        public int? Sensors { get; set; }
        public int? Command { get; set; }
        public int? Cohesion { get; set; }
    }
    public sealed class AuthoredTacticalEssentialPowerDto
    {
        public TacticalEssentialSystem? System { get; set; }
        public long? RequiredPower { get; set; }
        public long? AllocatedPower { get; set; }
    }
    public sealed class AuthoredTacticalShieldLimitDto
    {
        public TacticalSector? Sector { get; set; }
        public long? Maximum { get; set; }
    }
    public sealed class AuthoredTacticalMountDto
    {
        public string? MountId { get; set; }
        public List<TacticalWeaponClass>? AllowedWeaponClasses { get; set; }
        public TacticalArc? Coverage { get; set; }
        public int? Slots { get; set; }
    }
    public sealed class AuthoredTacticalWeaponDto
    {
        public string? WeaponId { get; set; }
        public string? PackageVersionId { get; set; }
        public string? MountId { get; set; }
        public int? SlotsRequired { get; set; }
        public TacticalWeaponClass? Class { get; set; }
        public TacticalArc? Coverage { get; set; }
        public long? Damage { get; set; }
        public int? AccuracyBps { get; set; }
        public int? TrackingBps { get; set; }
        public int? OpportunitiesPerRound { get; set; }
        public int? CadenceRounds { get; set; }
        public int? InitialNextFireRound { get; set; }
        public long? RequiredPower { get; set; }
        public long? AllocatedPower { get; set; }
        // Energy explicitly uses empty string and zero capacity, never omission.
        public string? AmmunitionDefinitionId { get; set; }
        public long? MagazineCapacity { get; set; }
    }
    public sealed class AuthoredTacticalShipProvisionDto
    {
        public string? ShipId { get; set; }
        public string? OwnerCivilizationId { get; set; }
        public string? ShipDesignDefinitionId { get; set; }
        public string? FitDefinitionId { get; set; }
        public int? FitRevision { get; set; }
        public List<AuthoredTacticalShieldStockDto>? InitialShields { get; set; }
        public List<AuthoredTacticalAmmoStockDto>? InitialAmmunition { get; set; }
    }
    public sealed class AuthoredTacticalShieldStockDto
    {
        public TacticalSector? Sector { get; set; }
        public long? Current { get; set; }
    }
    public sealed class AuthoredTacticalAmmoStockDto
    {
        public string? WeaponId { get; set; }
        public string? AmmunitionDefinitionId { get; set; }
        public long? Stock { get; set; }
    }
}
