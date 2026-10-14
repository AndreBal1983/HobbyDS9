using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Pure source validation. No encounter, geometry, commands, RNG or provisioning writes.
    public static class AuthoredTacticalEquipment
    {
        public static ValidationReport Validate(DefinitionCatalogDto? definitions)
        {
            var issues = new List<ValidationIssue>();
            const string root = "$.Definitions.AuthoredTacticalEquipment";
            void Error(ValidationCode code, string path) => issues.Add(new ValidationIssue(code, path));
            bool Number(long? value, long minimum, long maximum, string path)
            {
                if (!value.HasValue) { Error(ValidationCode.MissingField, path); return false; }
                if (value < minimum || value > maximum) { Error(ValidationCode.InvalidAuthoredTacticalEquipment, path); return false; }
                return true;
            }
            bool Id(string? value, string path, bool registered = false)
            {
                if (!DefinitionId.TryParse(value, out _)) { Error(ValidationCode.InvalidId, path); return false; }
                if (registered && !(definitions?.DefinitionIds?.Contains(value!) ?? false))
                { Error(ValidationCode.MissingDefinition, path); return false; }
                return true;
            }
            bool Known<T>(T? value, string path) where T : struct
            {
                if (!value.HasValue) { Error(ValidationCode.MissingField, path); return false; }
                if (!Enum.IsDefined(typeof(T), value.Value)) { Error(ValidationCode.InvalidAuthoredTacticalEquipment, path); return false; }
                return true;
            }
            bool Arc(TacticalArc? value, string path)
            {
                if (!value.HasValue) { Error(ValidationCode.MissingField, path); return false; }
                if (value == TacticalArc.None || (value & ~TacticalArc.All) != 0)
                { Error(ValidationCode.InvalidAuthoredTacticalEquipment, path); return false; }
                return true;
            }
            string Index(string path, int i) => path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]";
            if (definitions == null) { Error(ValidationCode.MissingField, "$.Definitions"); return new ValidationReport(issues); }
            var catalogue = definitions.AuthoredTacticalEquipment;
            if (definitions.ContentFormatVersion == FoundationVersions.ContentFormatVersion)
            {
                if (catalogue != null) Error(ValidationCode.UnsupportedContentFormat, root);
                return new ValidationReport(issues);
            }
            if (definitions.ContentFormatVersion != AuthoredTacticalEquipmentIdentity.ContentFormatVersion)
            { Error(ValidationCode.UnsupportedContentFormat, "$.Definitions.ContentFormatVersion"); return new ValidationReport(issues); }
            if (catalogue == null) { Error(ValidationCode.MissingField, root); return new ValidationReport(issues); }
            if (catalogue.FormatVersion != AuthoredTacticalEquipmentIdentity.FormatVersion
                || catalogue.SchemaId != AuthoredTacticalEquipmentIdentity.SchemaId)
                Error(ValidationCode.UnsupportedContentFormat, root + ".SchemaId");
            Id(catalogue.ProvenanceId, root + ".ProvenanceId");
            if (catalogue.BalanceStatus != "provisional-test-scenario")
                Error(ValidationCode.InvalidAuthoredTacticalEquipment, root + ".BalanceStatus");
            var ammoIds = new HashSet<string>(StringComparer.Ordinal);
            if (catalogue.AmmunitionDefinitions == null) Error(ValidationCode.MissingField, root + ".AmmunitionDefinitions");
            else for (var i = 0; i < catalogue.AmmunitionDefinitions.Count; i++)
            {
                var value = catalogue.AmmunitionDefinitions[i]; var p = Index(root + ".AmmunitionDefinitions", i);
                if (value == null) { Error(ValidationCode.MissingField, p); continue; }
                if (Id(value.DefinitionId, p + ".DefinitionId", true) && !ammoIds.Add(value.DefinitionId!)) Error(ValidationCode.DuplicateId, p);
                if (string.IsNullOrWhiteSpace(value.DisplayName)) Error(ValidationCode.MissingField, p + ".DisplayName");
            }
            var fits = new Dictionary<string, AuthoredTacticalFitDto>(StringComparer.Ordinal);
            if (catalogue.Fits == null) Error(ValidationCode.MissingField, root + ".Fits");
            else for (var i = 0; i < catalogue.Fits.Count; i++)
            {
                var fit = catalogue.Fits[i]; var p = Index(root + ".Fits", i);
                if (fit == null) { Error(ValidationCode.MissingField, p); continue; }
                if (Id(fit.DefinitionId, p + ".DefinitionId", true) && !fits.TryAdd(fit.DefinitionId!, fit)) Error(ValidationCode.DuplicateId, p);
                if (fit.DefinitionId != null && ammoIds.Contains(fit.DefinitionId)) Error(ValidationCode.DuplicateId, p);
                Number(fit.Revision, 1, int.MaxValue, p + ".Revision"); Id(fit.ProvenanceId, p + ".ProvenanceId");
                Number(fit.MaxHull, 1, long.MaxValue, p + ".MaxHull"); Number(fit.Armor, 0, long.MaxValue, p + ".Armor");
                Number(fit.EvasionBps, 0, 10000, p + ".EvasionBps"); Known(fit.Role, p + ".Role");
                if (fit.Capabilities == null) Error(ValidationCode.MissingField, p + ".Capabilities");
                else
                {
                    Number(fit.Capabilities.Speed, 0, 100, p + ".Capabilities.Speed");
                    Number(fit.Capabilities.Agility, 0, 100, p + ".Capabilities.Agility");
                    Number(fit.Capabilities.Sensors, 0, 100, p + ".Capabilities.Sensors");
                    Number(fit.Capabilities.Command, 0, 100, p + ".Capabilities.Command");
                    Number(fit.Capabilities.Cohesion, 0, 100, p + ".Capabilities.Cohesion");
                }
                Number(fit.ReactorOutput, 0, long.MaxValue, p + ".ReactorOutput");
                Number(fit.DistributionLimit, 0, long.MaxValue, p + ".DistributionLimit");
                BigInteger essential = 0;
                var systems = new HashSet<TacticalEssentialSystem>();
                if (fit.EssentialReservations == null) Error(ValidationCode.MissingField, p + ".EssentialReservations");
                else for (var j = 0; j < fit.EssentialReservations.Count; j++)
                {
                    var reservation = fit.EssentialReservations[j]; var q = Index(p + ".EssentialReservations", j);
                    if (reservation == null) { Error(ValidationCode.MissingField, q); continue; }
                    if (Known(reservation.System, q + ".System") && !systems.Add(reservation.System!.Value)) Error(ValidationCode.DuplicateId, q);
                    var minimum = reservation.System == TacticalEssentialSystem.Targeting ? 0 : 1;
                    Number(reservation.RequiredPower, minimum, long.MaxValue, q + ".RequiredPower");
                    Number(reservation.AllocatedPower, 0, long.MaxValue, q + ".AllocatedPower");
                    if (reservation.AllocatedPower != reservation.RequiredPower) Error(ValidationCode.InvalidTacticalEquipmentPower, q);
                    if (reservation.AllocatedPower.HasValue) essential += reservation.AllocatedPower.Value;
                }
                if (systems.Count != 4) Error(ValidationCode.MissingField, p + ".EssentialReservations");
                Number(fit.ShieldRechargeRequiredPower, 0, long.MaxValue, p + ".ShieldRechargeRequiredPower");
                Number(fit.ShieldRechargeAllocatedPower, 0, long.MaxValue, p + ".ShieldRechargeAllocatedPower");
                Number(fit.ShieldRechargePerRound, 0, long.MaxValue, p + ".ShieldRechargePerRound");
                if ((fit.ShieldRechargeAllocatedPower != 0 && fit.ShieldRechargeAllocatedPower != fit.ShieldRechargeRequiredPower)
                    || (fit.ShieldRechargePerRound > 0 && !(fit.ShieldRechargeRequiredPower > 0)))
                    Error(ValidationCode.InvalidTacticalEquipmentPower, p + ".ShieldRechargeAllocatedPower");
                var sectors = new HashSet<TacticalSector>();
                if (fit.ShieldLimits == null) Error(ValidationCode.MissingField, p + ".ShieldLimits");
                else for (var j = 0; j < fit.ShieldLimits.Count; j++)
                {
                    var sector = fit.ShieldLimits[j]; var q = Index(p + ".ShieldLimits", j);
                    if (sector == null) { Error(ValidationCode.MissingField, q); continue; }
                    if (Known(sector.Sector, q + ".Sector") && !sectors.Add(sector.Sector!.Value)) Error(ValidationCode.DuplicateId, q);
                    Number(sector.Maximum, 0, long.MaxValue, q + ".Maximum");
                }
                if (sectors.Count != 4) Error(ValidationCode.MissingField, p + ".ShieldLimits");
                var mounts = new Dictionary<string, AuthoredTacticalMountDto>(StringComparer.Ordinal);
                if (fit.Mounts == null) Error(ValidationCode.MissingField, p + ".Mounts");
                else for (var j = 0; j < fit.Mounts.Count; j++)
                {
                    var mount = fit.Mounts[j]; var q = Index(p + ".Mounts", j);
                    if (mount == null) { Error(ValidationCode.MissingField, q); continue; }
                    if (Id(mount.MountId, q + ".MountId") && !mounts.TryAdd(mount.MountId!, mount)) Error(ValidationCode.DuplicateId, q);
                    Arc(mount.Coverage, q + ".Coverage"); Number(mount.Slots, 1, int.MaxValue, q + ".Slots");
                    if (mount.AllowedWeaponClasses == null) Error(ValidationCode.MissingField, q + ".AllowedWeaponClasses");
                    else if (mount.AllowedWeaponClasses.Count == 0 || mount.AllowedWeaponClasses.Distinct().Count() != mount.AllowedWeaponClasses.Count
                        || mount.AllowedWeaponClasses.Any(c => !Enum.IsDefined(typeof(TacticalWeaponClass), c)))
                        Error(ValidationCode.InvalidAuthoredTacticalEquipment, q + ".AllowedWeaponClasses");
                }
                var weaponIds = new HashSet<string>(StringComparer.Ordinal);
                BigInteger equipment = fit.ShieldRechargeAllocatedPower ?? 0;
                if (fit.Weapons == null) Error(ValidationCode.MissingField, p + ".Weapons");
                else for (var j = 0; j < fit.Weapons.Count; j++)
                {
                    var weapon = fit.Weapons[j]; var q = Index(p + ".Weapons", j);
                    if (weapon == null) { Error(ValidationCode.MissingField, q); continue; }
                    if (Id(weapon.WeaponId, q + ".WeaponId") && !weaponIds.Add(weapon.WeaponId!)) Error(ValidationCode.DuplicateId, q);
                    Id(weapon.PackageVersionId, q + ".PackageVersionId"); Id(weapon.MountId, q + ".MountId");
                    Known(weapon.Class, q + ".Class"); Arc(weapon.Coverage, q + ".Coverage");
                    Number(weapon.SlotsRequired, 1, int.MaxValue, q + ".SlotsRequired");
                    if (weapon.MountId == null || !mounts.TryGetValue(weapon.MountId, out var mount)
                        || !(mount.AllowedWeaponClasses?.Contains(weapon.Class ?? 0) ?? false)
                        || !weapon.Coverage.HasValue || !mount.Coverage.HasValue || (weapon.Coverage & mount.Coverage) != weapon.Coverage)
                        Error(ValidationCode.InvalidAuthoredTacticalEquipment, q + ".MountId");
                    Number(weapon.Damage, 0, long.MaxValue, q + ".Damage"); Number(weapon.AccuracyBps, 0, 10000, q + ".AccuracyBps");
                    Number(weapon.TrackingBps, 0, 10000, q + ".TrackingBps");
                    Number(weapon.OpportunitiesPerRound, 1, 64, q + ".OpportunitiesPerRound");
                    Number(weapon.CadenceRounds, 1, 3, q + ".CadenceRounds");
                    Number(weapon.InitialNextFireRound, 1, weapon.CadenceRounds ?? 0, q + ".InitialNextFireRound");
                    Number(weapon.RequiredPower, 1, long.MaxValue, q + ".RequiredPower");
                    Number(weapon.AllocatedPower, 0, long.MaxValue, q + ".AllocatedPower");
                    if (weapon.AllocatedPower != 0 && weapon.AllocatedPower != weapon.RequiredPower)
                        Error(ValidationCode.InvalidTacticalEquipmentPower, q + ".AllocatedPower");
                    if (weapon.AllocatedPower.HasValue) equipment += weapon.AllocatedPower.Value;
                    Number(weapon.MagazineCapacity, 0, long.MaxValue, q + ".MagazineCapacity");
                    if (weapon.Class == TacticalWeaponClass.Torpedo)
                    {
                        if (Id(weapon.AmmunitionDefinitionId, q + ".AmmunitionDefinitionId", true)
                            && !ammoIds.Contains(weapon.AmmunitionDefinitionId!)) Error(ValidationCode.MissingDefinition, q + ".AmmunitionDefinitionId");
                    }
                    else if (weapon.AmmunitionDefinitionId == null) Error(ValidationCode.MissingField, q + ".AmmunitionDefinitionId");
                    else if (weapon.AmmunitionDefinitionId != string.Empty || weapon.MagazineCapacity != 0)
                        Error(ValidationCode.InvalidAuthoredTacticalEquipment, q + ".AmmunitionDefinitionId");
                }
                if ((fit.Weapons?.Count ?? 0) > 0 && !(fit.EssentialReservations?.Any(r => r?.System == TacticalEssentialSystem.Targeting && r.RequiredPower > 0) ?? false))
                    Error(ValidationCode.InvalidTacticalEquipmentPower, p + ".EssentialReservations");
                foreach (var mount in mounts.Values)
                    if ((fit.Weapons ?? new List<AuthoredTacticalWeaponDto>()).Where(w => w != null && w.MountId == mount.MountId)
                        .Aggregate(BigInteger.Zero, (sum, w) => sum + (w.SlotsRequired ?? 0)) > (mount.Slots ?? 0))
                        Error(ValidationCode.InvalidAuthoredTacticalEquipment, p + ".Mounts");
                var available = Math.Min(fit.ReactorOutput ?? 0, fit.DistributionLimit ?? 0);
                if (essential + equipment > available) Error(ValidationCode.InvalidTacticalEquipmentPower, p + ".EssentialReservations");
            }
            var provisionIds = new HashSet<string>(StringComparer.Ordinal);
            if (catalogue.InitialShipProvisions == null) Error(ValidationCode.MissingField, root + ".InitialShipProvisions");
            else for (var i = 0; i < catalogue.InitialShipProvisions.Count; i++)
            {
                var provision = catalogue.InitialShipProvisions[i]; var p = Index(root + ".InitialShipProvisions", i);
                if (provision == null) { Error(ValidationCode.MissingField, p); continue; }
                if (Id(provision.ShipId, p + ".ShipId") && !provisionIds.Add(provision.ShipId!)) Error(ValidationCode.DuplicateId, p);
                Id(provision.OwnerCivilizationId, p + ".OwnerCivilizationId");
                Id(provision.ShipDesignDefinitionId, p + ".ShipDesignDefinitionId", true);
                Id(provision.FitDefinitionId, p + ".FitDefinitionId", true); Number(provision.FitRevision, 1, int.MaxValue, p + ".FitRevision");
                fits.TryGetValue(provision.FitDefinitionId ?? string.Empty, out var fit);
                if (fit == null || fit.Revision != provision.FitRevision) Error(ValidationCode.InvalidTacticalEquipmentBinding, p + ".FitDefinitionId");
                var sectors = new HashSet<TacticalSector>();
                if (provision.InitialShields == null) Error(ValidationCode.MissingField, p + ".InitialShields");
                else for (var j = 0; j < provision.InitialShields.Count; j++)
                {
                    var stock = provision.InitialShields[j]; var q = Index(p + ".InitialShields", j);
                    if (stock == null) { Error(ValidationCode.MissingField, q); continue; }
                    if (Known(stock.Sector, q + ".Sector") && !sectors.Add(stock.Sector!.Value)) Error(ValidationCode.DuplicateId, q);
                    var limit = fit?.ShieldLimits?.FirstOrDefault(s => s != null && s.Sector == stock.Sector);
                    Number(stock.Current, 0, limit?.Maximum ?? 0, q + ".Current");
                    if (limit == null) Error(ValidationCode.InvalidTacticalEquipmentBinding, q);
                }
                if (sectors.Count != 4) Error(ValidationCode.MissingField, p + ".InitialShields");
                var stockedWeapons = new HashSet<string>(StringComparer.Ordinal);
                if (provision.InitialAmmunition == null) Error(ValidationCode.MissingField, p + ".InitialAmmunition");
                else for (var j = 0; j < provision.InitialAmmunition.Count; j++)
                {
                    var stock = provision.InitialAmmunition[j]; var q = Index(p + ".InitialAmmunition", j);
                    if (stock == null) { Error(ValidationCode.MissingField, q); continue; }
                    if (Id(stock.WeaponId, q + ".WeaponId") && !stockedWeapons.Add(stock.WeaponId!)) Error(ValidationCode.DuplicateId, q);
                    Id(stock.AmmunitionDefinitionId, q + ".AmmunitionDefinitionId", true);
                    var weapon = fit?.Weapons?.FirstOrDefault(w => w != null && w.WeaponId == stock.WeaponId);
                    if (weapon == null || weapon.Class != TacticalWeaponClass.Torpedo || weapon.AmmunitionDefinitionId != stock.AmmunitionDefinitionId)
                        Error(ValidationCode.InvalidTacticalEquipmentBinding, q);
                    Number(stock.Stock, 0, weapon?.MagazineCapacity ?? 0, q + ".Stock");
                }
                foreach (var weapon in fit?.Weapons ?? new List<AuthoredTacticalWeaponDto>())
                    if (weapon?.Class == TacticalWeaponClass.Torpedo && !stockedWeapons.Contains(weapon.WeaponId ?? string.Empty))
                        Error(ValidationCode.MissingField, p + ".InitialAmmunition");
            }
            return new ValidationReport(issues);
        }
    }
}
