using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class AuthoredTacticalEquipmentCopies
    {
        // Called only after full validation. Own and canonically order every content list.
        internal static AuthoredTacticalEquipmentCatalogueDto Copy(AuthoredTacticalEquipmentCatalogueDto source)
            => new AuthoredTacticalEquipmentCatalogueDto
            {
                FormatVersion = source.FormatVersion, SchemaId = source.SchemaId,
                ProvenanceId = source.ProvenanceId, BalanceStatus = source.BalanceStatus,
                AmmunitionDefinitions = source.AmmunitionDefinitions!.OrderBy(a => a.DefinitionId, StringComparer.Ordinal)
                    .Select(a => new AuthoredTacticalAmmoDefinitionDto { DefinitionId = a.DefinitionId, DisplayName = a.DisplayName }).ToList(),
                Fits = source.Fits!.OrderBy(f => f.DefinitionId, StringComparer.Ordinal).Select(f => new AuthoredTacticalFitDto
                {
                    DefinitionId = f.DefinitionId, Revision = f.Revision, ProvenanceId = f.ProvenanceId,
                    MaxHull = f.MaxHull, Armor = f.Armor, EvasionBps = f.EvasionBps, Role = f.Role,
                    Capabilities = new AuthoredTacticalCapabilitiesDto { Speed = f.Capabilities!.Speed, Agility = f.Capabilities.Agility,
                        Sensors = f.Capabilities.Sensors, Command = f.Capabilities.Command, Cohesion = f.Capabilities.Cohesion },
                    ReactorOutput = f.ReactorOutput, DistributionLimit = f.DistributionLimit,
                    EssentialReservations = f.EssentialReservations!.OrderBy(p => p.System).Select(p => new AuthoredTacticalEssentialPowerDto
                        { System = p.System, RequiredPower = p.RequiredPower, AllocatedPower = p.AllocatedPower }).ToList(),
                    ShieldRechargeRequiredPower = f.ShieldRechargeRequiredPower, ShieldRechargeAllocatedPower = f.ShieldRechargeAllocatedPower,
                    ShieldRechargePerRound = f.ShieldRechargePerRound,
                    ShieldLimits = f.ShieldLimits!.OrderBy(s => s.Sector).Select(s => new AuthoredTacticalShieldLimitDto { Sector = s.Sector, Maximum = s.Maximum }).ToList(),
                    Mounts = f.Mounts!.OrderBy(m => m.MountId, StringComparer.Ordinal).Select(m => new AuthoredTacticalMountDto
                        { MountId = m.MountId, Coverage = m.Coverage, Slots = m.Slots,
                            AllowedWeaponClasses = m.AllowedWeaponClasses!.OrderBy(c => c).ToList() }).ToList(),
                    Weapons = f.Weapons!.OrderBy(w => w.WeaponId, StringComparer.Ordinal).Select(w => new AuthoredTacticalWeaponDto
                    {
                        WeaponId = w.WeaponId, PackageVersionId = w.PackageVersionId, MountId = w.MountId, SlotsRequired = w.SlotsRequired,
                        Class = w.Class, Coverage = w.Coverage, Damage = w.Damage, AccuracyBps = w.AccuracyBps, TrackingBps = w.TrackingBps,
                        OpportunitiesPerRound = w.OpportunitiesPerRound, CadenceRounds = w.CadenceRounds, InitialNextFireRound = w.InitialNextFireRound,
                        RequiredPower = w.RequiredPower, AllocatedPower = w.AllocatedPower,
                        AmmunitionDefinitionId = w.AmmunitionDefinitionId, MagazineCapacity = w.MagazineCapacity
                    }).ToList()
                }).ToList(),
                InitialShipProvisions = source.InitialShipProvisions!.OrderBy(p => p.ShipId, StringComparer.Ordinal).Select(p => new AuthoredTacticalShipProvisionDto
                {
                    ShipId = p.ShipId, OwnerCivilizationId = p.OwnerCivilizationId, ShipDesignDefinitionId = p.ShipDesignDefinitionId,
                    FitDefinitionId = p.FitDefinitionId, FitRevision = p.FitRevision,
                    InitialShields = p.InitialShields!.OrderBy(s => s.Sector).Select(s => new AuthoredTacticalShieldStockDto { Sector = s.Sector, Current = s.Current }).ToList(),
                    InitialAmmunition = p.InitialAmmunition!.OrderBy(a => a.WeaponId, StringComparer.Ordinal).Select(a => new AuthoredTacticalAmmoStockDto
                        { WeaponId = a.WeaponId, AmmunitionDefinitionId = a.AmmunitionDefinitionId, Stock = a.Stock }).ToList()
                }).ToList()
            };
    }
}
