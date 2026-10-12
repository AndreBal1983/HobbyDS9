using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Simulation
{
    public static class ShipDesignCatalogueValidation
    {
        internal static void Error(List<DesignIssue> issues, DesignIssueCode code, string path, string message) => issues.Add(new DesignIssue(code, DesignIssueSeverity.Error, path, message));
        internal static bool Known<T>(T value) where T : struct => Enum.IsDefined(typeof(T), value);
        internal static bool Id(string? id) => DefinitionId.TryParse(id, out _);
        internal static bool List<T>(IEnumerable<T>? list, string path, List<DesignIssue> issues)
        {
            if (list == null)
            {
                Error(issues, DesignIssueCode.MissingField, path, "Required list is missing.");
                return false;
            }
            if (list.Any(v => v == null))
            {
                Error(issues, DesignIssueCode.MissingField, path, "Null list entries are not allowed.");
                return false;
            }
            return true;
        }
        internal static void Unique(IEnumerable<string?> ids, string path, List<DesignIssue> issues)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids.OrderBy(v => v, StringComparer.Ordinal))
                if (!Id(id))
                    Error(issues, DesignIssueCode.InvalidId, path, "Canonical nonreserved definition IDs are required.");
                else if (!seen.Add(id!))
                    Error(issues, DesignIssueCode.DuplicateId, path, "Duplicate ID: " + id);
        }
        private static void Tags(List<string>? tags, string path, List<DesignIssue> issues)
        {
            if (List(tags, path, issues))
                Unique(tags!, path, issues);
        }
        private static void Outputs(List<ShipOutputDto>? outputs, string path, List<DesignIssue> issues)
        {
            if (!List(outputs, path, issues))
                return;
            if (outputs!.GroupBy(o => o.Kind).Any(g => g.Count() > 1))
                Error(issues, DesignIssueCode.DuplicateId, path, "Each output appears once.");
            if (outputs.Any(o => !Known(o.Kind) || o.Amount < 0 || ((o.Kind == ShipOutputKind.AccuracyBps || o.Kind == ShipOutputKind.EvasionBps) && o.Amount > 10000)))
                Error(issues, DesignIssueCode.InvalidValue, path, "Unknown output or invalid amount/basis points.");
        }
        private static void Cost(ShipDesignCostDto? cost, string path, List<DesignIssue> issues)
        {
            if (cost == null)
            {
                Error(issues, DesignIssueCode.MissingField, path, "Cost is required.");
                return;
            }
            if (cost.Credits < 0 || cost.Industry < 0 || cost.Maintenance < 0)
                Error(issues, DesignIssueCode.InvalidValue, path, "Costs cannot be negative.");
            if (List(cost.Resources, path + ".Resources", issues))
            {
                Unique(cost.Resources!.Select(r => r.ResourceDefinitionId), path + ".Resources", issues);
                if (cost.Resources.Any(r => r.Amount < 0))
                    Error(issues, DesignIssueCode.InvalidValue, path + ".Resources", "Resource costs cannot be negative.");
            }
        }
        public static IReadOnlyList<DesignIssue> Validate(ShipDesignCatalogueDto? dto)
        {
            var issues = new List<DesignIssue>();
            if (dto == null)
            {
                Error(issues, DesignIssueCode.MissingField, "$", "Catalogue is required.");
                return issues.AsReadOnly();
            }
            if (dto.FormatVersion != ShipDesignVersions.CatalogueFormat)
                Error(issues, DesignIssueCode.InvalidValue, "$.FormatVersion", "Unsupported or missing standalone catalogue format (expected 1).");
            bool families = List(dto.HullFamilies, "$.HullFamilies", issues), hulls = List(dto.Hulls, "$.Hulls", issues), components = List(dto.Components, "$.Components", issues), roles = List(dto.Roles, "$.Roles", issues);
            if (!families || !hulls || !components || !roles)
                return Ordered(issues);
            Unique(dto.HullFamilies!.Select(v => v.FamilyId).Concat(dto.Hulls!.Select(v => v.VersionId)).Concat(dto.Components!.Select(v => v.VersionId)).Concat(dto.Roles!.Select(v => v.RoleId)), "$.Ids", issues);
            var familyIds = new HashSet<string>(dto.HullFamilies.Select(f => f.FamilyId), StringComparer.Ordinal);
            foreach (var f in dto.HullFamilies!)
            if (string.IsNullOrWhiteSpace(f.DisplayName))
                Error(issues, DesignIssueCode.MissingField, "$.HullFamilies[" + f.FamilyId + "].DisplayName", "Display name required.");
            foreach (var h in dto.Hulls.OrderBy(v => v.VersionId, StringComparer.Ordinal))
            {
                string p = "$.Hulls[" + h.VersionId + "]";
                if (!familyIds.Contains(h.FamilyId))
                    Error(issues, DesignIssueCode.UnknownReference, p + ".FamilyId", "Unknown hull family.");
                if (h.VersionNumber <= 0 || string.IsNullOrWhiteSpace(h.DisplayName) || h.Capacity <= 0 || h.MaxHull <= 0 || h.DesignLoad <= 0 || h.MinimumCrew <= 0 || h.PowerDistributionLimit <= 0 || h.MaxReactors <= 0 || h.MaxPropulsionPackages <= 0 || h.ReactorSizeLimit <= 0 || h.PropulsionSizeLimit <= 0)
                    Error(issues, DesignIssueCode.InvalidValue, p, "Hull operating limits must be positive.");
                if (dto.Hulls.Count(v => v.FamilyId == h.FamilyId && v.VersionNumber == h.VersionNumber) > 1)
                    Error(issues, DesignIssueCode.DuplicateId, p + ".VersionNumber", "Duplicate hull family revision.");
                if (h.ParentVersionId != null && !dto.Hulls.Any(v => v.VersionId == h.ParentVersionId && v.FamilyId == h.FamilyId && v.VersionNumber < h.VersionNumber))
                    Error(issues, DesignIssueCode.UnknownReference, p + ".ParentVersionId", "Parent must be an earlier version of this family.");
                Tags(h.Tags, p + ".Tags", issues);
                Cost(h.Cost, p + ".Cost", issues);
                if (h.Cost != null && h.Cost.Industry <= 0)
                    Error(issues, DesignIssueCode.InvalidValue, p + ".Cost.Industry", "Hull construction workload must be positive.");
                if (List(h.Mounts, p + ".Mounts", issues))
                {
                    Unique(h.Mounts!.Select(m => m.Id), p + ".Mounts", issues);
                    if (h.Mounts.Any(m => !Known(m.Kind) || m.SizeRating <= 0 || m.Count <= 0 || m.CoverageTag == null || (m.CoverageTag != "" && !Id(m.CoverageTag))))
                        Error(issues, DesignIssueCode.InvalidValue, p + ".Mounts", "Invalid mount limit.");
                }
            }
            foreach (var c in dto.Components.OrderBy(v => v.VersionId, StringComparer.Ordinal))
            {
                string p = "$.Components[" + c.VersionId + "]";
                if (!Id(c.FamilyId) || c.VersionNumber <= 0 || string.IsNullOrWhiteSpace(c.DisplayName) || !Known(c.Kind) || c.Capacity <= 0 || c.CrewRequired < 0 || c.ComputerDemand < 0 || c.PowerSupply < 0 || c.EnvelopeSize < 0 || (c.Kind == ComponentKind.Reactor ? (c.PowerSupply <= 0 || c.EnvelopeSize <= 0) : c.PowerSupply != 0) || (c.Kind == ComponentKind.Propulsion && c.EnvelopeSize <= 0))
                    Error(issues, DesignIssueCode.InvalidValue, p, "Invalid component parameters.");
                if (dto.Components.Count(v => v.FamilyId == c.FamilyId && v.VersionNumber == c.VersionNumber) > 1)
                    Error(issues, DesignIssueCode.DuplicateId, p + ".VersionNumber", "Duplicate component family revision.");
                if (c.ParentVersionId != null && !dto.Components.Any(v => v.VersionId == c.ParentVersionId && v.FamilyId == c.FamilyId && v.Kind == c.Kind && v.VersionNumber < c.VersionNumber))
                    Error(issues, DesignIssueCode.UnknownReference, p + ".ParentVersionId", "Parent must be an earlier same-family/kind version.");
                Tags(c.CompatibleHullFamilyIds, p + ".CompatibleHullFamilyIds", issues);
                if (c.CompatibleHullFamilyIds != null && (c.CompatibleHullFamilyIds.Count == 0 || c.CompatibleHullFamilyIds.Any(id => !familyIds.Contains(id))))
                    Error(issues, DesignIssueCode.UnknownReference, p + ".CompatibleHullFamilyIds", "At least one known compatible hull family required.");
                Tags(c.RequiredHullTags, p + ".RequiredHullTags", issues);
                Tags(c.Tags, p + ".Tags", issues);
                Tags(c.RequiredComponentTags, p + ".RequiredComponentTags", issues);
                Cost(c.Cost, p + ".Cost", issues);
                Outputs(c.Dependencies, p + ".Dependencies", issues);
                if (List(c.Mounts, p + ".Mounts", issues))
                {
                    if ((c.Kind == ComponentKind.EnergyWeapon || c.Kind == ComponentKind.TorpedoWeapon) && c.Mounts!.Count == 0)
                        Error(issues, DesignIssueCode.MissingField, p + ".Mounts", "Weapon packages must declare their physical mounts.");
                    if (c.Mounts!.Any(m => !Known(m.Kind) || m.SizeRating <= 0 || m.Count <= 0 || m.CoverageTag == null || (m.CoverageTag != "" && !Id(m.CoverageTag))))
                        Error(issues, DesignIssueCode.InvalidValue, p + ".Mounts", "Invalid mount requirement.");
                    if (c.Mounts.GroupBy(m => (m.Kind, m.SizeRating, m.CoverageTag)).Any(g => g.Count() > 1))
                        Error(issues, DesignIssueCode.DuplicateId, p + ".Mounts", "Duplicate package mount requirement.");
                }
                if (!List(c.Modes, p + ".Modes", issues))
                    continue;
                if (c.Modes!.Count != 3 || c.Modes.Select(m => m.Profile).Distinct().Count() != 3)
                    Error(issues, DesignIssueCode.MissingField, p + ".Modes", "Cruise, Combat and Survey modes required exactly once.");
                foreach (var m in c.Modes)
                {
                    string mp = p + ".Modes[" + m.Profile + "]";
                    bool core = c.Kind == ComponentKind.Reactor || c.Kind == ComponentKind.LifeSupport || c.Kind == ComponentKind.Command || c.Kind == ComponentKind.Propulsion;
                    if (!Known(m.Profile) || !Known(m.Scaling) || m.FullPower < 0 || m.MinimumPower < 0 || m.MinimumPower > m.FullPower || (m.Scaling == PowerScaling.Fixed && m.MinimumPower != m.FullPower) || (core && !m.Essential) || (m.Essential && c.Kind != ComponentKind.Reactor && m.MinimumPower <= 0) || (c.Kind == ComponentKind.Reactor && (m.FullPower != 0 || m.MinimumPower != 0)))
                        Error(issues, DesignIssueCode.InvalidValue, mp, "Invalid demand/scaling/essential minimum.");
                    Outputs(m.Outputs, mp + ".Outputs", issues);
                    if (m.Outputs != null && m.Outputs.Any(o => o != null && ((o.Kind == ShipOutputKind.CrewSupport && c.Kind != ComponentKind.LifeSupport) || (o.Kind == ShipOutputKind.ComputerSupport && c.Kind != ComponentKind.Command) || (o.Kind == ShipOutputKind.ShieldCapacity && c.Kind != ComponentKind.Shields) || ((o.Kind == ShipOutputKind.LongRangeAttack || o.Kind == ShipOutputKind.MediumRangeAttack || o.Kind == ShipOutputKind.CloseRangeAttack) && c.Kind != ComponentKind.EnergyWeapon && c.Kind != ComponentKind.TorpedoWeapon))))
                        Error(issues, DesignIssueCode.InvalidValue, mp + ".Outputs", "Output is not supported by this component kind.");
                    ShipOutputKind? required = c.Kind == ComponentKind.LifeSupport ? ShipOutputKind.CrewSupport : c.Kind == ComponentKind.Command ? ShipOutputKind.ComputerSupport : c.Kind == ComponentKind.Propulsion ? ShipOutputKind.PropulsionLoadSupport : (ShipOutputKind?)null;
                    if (required.HasValue && m.Outputs != null && !m.Outputs.Any(o => o != null && o.Kind == required.Value && o.Amount > 0))
                        Error(issues, DesignIssueCode.InvalidValue, mp + ".Outputs", "Required-system mode lacks its operating output.");
                    if (c.Kind == ComponentKind.Reactor && m.Outputs != null && m.Outputs.Count > 0)
                        Error(issues, DesignIssueCode.InvalidValue, mp + ".Outputs", "Reactors supply installed power only, not free powered performance.");
                    if (m.Outputs != null && c.Kind != ComponentKind.Propulsion && m.Outputs.Any(o => o != null && (o.Kind == ShipOutputKind.MovementBudget || o.Kind == ShipOutputKind.PropulsionLoadSupport)))
                        Error(issues, DesignIssueCode.InvalidValue, mp + ".Outputs", "Only propulsion provides movement/load support.");
                    if (c.Kind != ComponentKind.Reactor && m.FullPower == 0 && m.Outputs != null && m.Outputs.Any(o => o != null && o.Amount > 0))
                        Error(issues, DesignIssueCode.InvalidValue, mp + ".Outputs", "Active output requires a positive power demand.");
                }
            }
            foreach (var role in dto.Roles.OrderBy(r => r.RoleId, StringComparer.Ordinal))
            {
                var p = "$.Roles[" + role.RoleId + "]";
                if (string.IsNullOrWhiteSpace(role.DisplayName))
                    Error(issues, DesignIssueCode.MissingField, p + ".DisplayName", "Display name required.");
                if (List(role.Recommendations, p + ".Recommendations", issues))
                {
                    if (role.Recommendations!.Any(r => !Known(r.Profile) || !Known(r.Output) || r.Minimum < 0))
                        Error(issues, DesignIssueCode.InvalidValue, p + ".Recommendations", "Invalid recommendation.");
                    if (role.Recommendations.GroupBy(r => (r.Profile, r.Output)).Any(g => g.Count() > 1))
                        Error(issues, DesignIssueCode.DuplicateId, p + ".Recommendations", "Duplicate recommendation.");
                }
            }
            return Ordered(issues);
        }
        internal static IReadOnlyList<DesignIssue> Ordered(IEnumerable<DesignIssue> issues) => issues.OrderBy(i => i.Path, StringComparer.Ordinal).ThenBy(i => i.Severity).ThenBy(i => i.Code).ThenBy(i => i.Message, StringComparer.Ordinal).ToList().AsReadOnly();
    }
}
