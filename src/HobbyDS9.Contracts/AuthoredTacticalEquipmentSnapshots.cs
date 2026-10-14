using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Contracts
{
    // Initial provisioning remains content, not a current-resource or resolver DTO.
    // A projection cannot be submitted to a campaign or imported as a trusted source.
    public sealed class AuthoredTacticalEquipmentSnapshot
    {
        public string ProjectionId => AuthoredTacticalEquipmentIdentity.ProjectionId;
        public int FormatVersion => AuthoredTacticalEquipmentIdentity.FormatVersion;
        public string CatalogueProvenanceId { get; }
        public string BalanceStatus { get; }
        public string ShipId { get; }
        public string OwnerCivilizationId { get; }
        public string ShipDesignDefinitionId { get; }
        public string FitDefinitionId { get; }
        public int FitRevision { get; }
        public string FitProvenanceId { get; }
        public long MaxHull { get; }
        public long Armor { get; }
        public int EvasionBps { get; }
        public TacticalShipRole Role { get; }
        public AuthoredTacticalCapabilitiesSnapshot Capabilities { get; }
        public long ReactorOutput { get; }
        public long DistributionLimit { get; }
        public long EffectivePower => Math.Min(ReactorOutput, DistributionLimit);
        public long EssentialPowerReserved { get; }
        public long EquipmentPowerBudget => EffectivePower - EssentialPowerReserved;
        public long EquipmentPowerAllocated { get; }
        public long ShieldRechargeRequiredPower { get; }
        public long ShieldRechargeAllocatedPower { get; }
        public long ShieldRechargePerRound { get; }
        public IReadOnlyList<AuthoredTacticalEssentialPowerSnapshot> EssentialReservations { get; }
        public IReadOnlyList<AuthoredTacticalShieldSnapshot> InitialShields { get; }
        public IReadOnlyList<AuthoredTacticalMountSnapshot> Mounts { get; }
        public IReadOnlyList<AuthoredTacticalWeaponSnapshot> Weapons { get; }

        public AuthoredTacticalEquipmentSnapshot(AuthoredTacticalEquipmentCatalogueDto catalogue,
            AuthoredTacticalFitDto fit, AuthoredTacticalShipProvisionDto provision)
        {
            CatalogueProvenanceId = catalogue.ProvenanceId!; BalanceStatus = catalogue.BalanceStatus!;
            ShipId = provision.ShipId!; OwnerCivilizationId = provision.OwnerCivilizationId!;
            ShipDesignDefinitionId = provision.ShipDesignDefinitionId!; FitDefinitionId = fit.DefinitionId!;
            FitRevision = fit.Revision!.Value; FitProvenanceId = fit.ProvenanceId!;
            MaxHull = fit.MaxHull!.Value; Armor = fit.Armor!.Value; EvasionBps = fit.EvasionBps!.Value;
            Role = fit.Role!.Value; Capabilities = new AuthoredTacticalCapabilitiesSnapshot(fit.Capabilities!);
            ReactorOutput = fit.ReactorOutput!.Value; DistributionLimit = fit.DistributionLimit!.Value;
            ShieldRechargeRequiredPower = fit.ShieldRechargeRequiredPower!.Value;
            ShieldRechargeAllocatedPower = fit.ShieldRechargeAllocatedPower!.Value;
            ShieldRechargePerRound = fit.ShieldRechargePerRound!.Value;
            EssentialReservations = fit.EssentialReservations!.OrderBy(p => p.System)
                .Select(p => new AuthoredTacticalEssentialPowerSnapshot(p)).ToList().AsReadOnly();
            // Validated feasible totals fit Int64; reject caller-created overflow too.
            EssentialPowerReserved = EssentialReservations.Aggregate(0L, (sum, p) => checked(sum + p.AllocatedPower));
            InitialShields = fit.ShieldLimits!.OrderBy(q => q.Sector).Select(q => new AuthoredTacticalShieldSnapshot(
                q, provision.InitialShields!.Single(s => s.Sector == q.Sector))).ToList().AsReadOnly();
            Mounts = fit.Mounts!.OrderBy(m => m.MountId, StringComparer.Ordinal)
                .Select(m => new AuthoredTacticalMountSnapshot(m)).ToList().AsReadOnly();
            Weapons = fit.Weapons!.OrderBy(w => w.WeaponId, StringComparer.Ordinal).Select(w =>
                new AuthoredTacticalWeaponSnapshot(w, w.Class == TacticalWeaponClass.Torpedo
                    ? provision.InitialAmmunition!.Single(a => a.WeaponId == w.WeaponId).Stock!.Value : 0)).ToList().AsReadOnly();
            EquipmentPowerAllocated = Weapons.Aggregate(ShieldRechargeAllocatedPower, (sum, w) => checked(sum + w.AllocatedPower));
        }
    }
    public sealed class AuthoredTacticalCapabilitiesSnapshot
    {
        public int Speed { get; }
        public int Agility { get; }
        public int Sensors { get; }
        public int Command { get; }
        public int Cohesion { get; }
        public AuthoredTacticalCapabilitiesSnapshot(AuthoredTacticalCapabilitiesDto value)
        { Speed = value.Speed!.Value; Agility = value.Agility!.Value; Sensors = value.Sensors!.Value; Command = value.Command!.Value; Cohesion = value.Cohesion!.Value; }
    }
    public sealed class AuthoredTacticalEssentialPowerSnapshot
    {
        public TacticalEssentialSystem System { get; }
        public long RequiredPower { get; }
        public long AllocatedPower { get; }
        public AuthoredTacticalEssentialPowerSnapshot(AuthoredTacticalEssentialPowerDto value)
        { System = value.System!.Value; RequiredPower = value.RequiredPower!.Value; AllocatedPower = value.AllocatedPower!.Value; }
    }
    public sealed class AuthoredTacticalShieldSnapshot
    {
        public TacticalSector Sector { get; }
        public long Maximum { get; }
        public long InitialCurrent { get; }
        public AuthoredTacticalShieldSnapshot(AuthoredTacticalShieldLimitDto limit, AuthoredTacticalShieldStockDto stock)
        { Sector = limit.Sector!.Value; Maximum = limit.Maximum!.Value; InitialCurrent = stock.Current!.Value; }
    }
    public sealed class AuthoredTacticalMountSnapshot
    {
        public string MountId { get; }
        public IReadOnlyList<TacticalWeaponClass> AllowedWeaponClasses { get; }
        public TacticalArc Coverage { get; }
        public int Slots { get; }
        public AuthoredTacticalMountSnapshot(AuthoredTacticalMountDto value)
        { MountId = value.MountId!; AllowedWeaponClasses = value.AllowedWeaponClasses!.OrderBy(c => c).ToList().AsReadOnly(); Coverage = value.Coverage!.Value; Slots = value.Slots!.Value; }
    }
    public sealed class AuthoredTacticalWeaponSnapshot
    {
        public string WeaponId { get; }
        public string PackageVersionId { get; }
        public string MountId { get; }
        public int SlotsRequired { get; }
        public TacticalWeaponClass Class { get; }
        public TacticalArc Coverage { get; }
        public long Damage { get; }
        public int AccuracyBps { get; }
        public int TrackingBps { get; }
        public int OpportunitiesPerRound { get; }
        public int CadenceRounds { get; }
        public int InitialNextFireRound { get; }
        public long RequiredPower { get; }
        public long AllocatedPower { get; }
        public bool Powered => AllocatedPower == RequiredPower;
        public string AmmunitionDefinitionId { get; }
        public long MagazineCapacity { get; }
        public long InitialAmmunition { get; }
        public AuthoredTacticalWeaponSnapshot(AuthoredTacticalWeaponDto value, long stock)
        {
            WeaponId = value.WeaponId!; PackageVersionId = value.PackageVersionId!; MountId = value.MountId!;
            SlotsRequired = value.SlotsRequired!.Value; Class = value.Class!.Value; Coverage = value.Coverage!.Value;
            Damage = value.Damage!.Value; AccuracyBps = value.AccuracyBps!.Value; TrackingBps = value.TrackingBps!.Value;
            OpportunitiesPerRound = value.OpportunitiesPerRound!.Value; CadenceRounds = value.CadenceRounds!.Value;
            InitialNextFireRound = value.InitialNextFireRound!.Value;
            RequiredPower = value.RequiredPower!.Value; AllocatedPower = value.AllocatedPower!.Value;
            AmmunitionDefinitionId = value.AmmunitionDefinitionId!; MagazineCapacity = value.MagazineCapacity!.Value; InitialAmmunition = stock;
        }
    }
}
