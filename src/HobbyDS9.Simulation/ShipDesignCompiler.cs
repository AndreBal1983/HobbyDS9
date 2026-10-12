using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Simulation
{
    // Stateless, no campaign state, RNG, modifier registry or I/O.
    public sealed class ShipDesignCompiler
    {
        public ShipDesignCatalogueSnapshot Catalogue
        { get; }
        public ShipDesignCompiler(ShipDesignCatalogueSnapshot catalogue)
        {
            Catalogue = catalogue;
        }
        private static void Error(List<DesignIssue> issues, DesignIssueCode code, string path, string text) => ShipDesignCatalogueValidation.Error(issues, code, path, text);
        private static void Warn(List<DesignIssue> issues, DesignIssueCode code, string path, string text) => issues.Add(new DesignIssue(code, DesignIssueSeverity.Warning, path, text));
        private static long Sum(IEnumerable<long> values) => checked((long)values.Aggregate(BigInteger.Zero, (sum, v) => sum + v));
        private static long Product(long a, long b) => checked((long)((BigInteger)a * b));
        private static bool Intensive(ShipOutputKind kind) => kind == ShipOutputKind.AccuracyBps || kind == ShipOutputKind.EvasionBps || kind == ShipOutputKind.MovementBudget || kind == ShipOutputKind.OperationalRange;
        private static ShipOutputSnapshot Output(ShipOutputKind kind, long amount) => new ShipOutputSnapshot(new ShipOutputDto { Kind = kind, Amount = amount });
        private static ShipDesignPreview Empty(IEnumerable<DesignIssue> issues) => new ShipDesignPreview(issues, null, null, null, null, null, Array.Empty<MountUsage>(), null, Array.Empty<ProfilePerformance>(), null);
        private static bool ValidList<T>(IEnumerable<T>? values, string path, List<DesignIssue> issues) => ShipDesignCatalogueValidation.List(values, path, issues);
        public ShipDesignPreview Preview(ShipClassDraftDto? draft, ShipDesignAccessDto? access)
        {
            var issues = new List<DesignIssue>();
            if (draft == null)
            {
                Error(issues, DesignIssueCode.MissingField, "$", "Draft required.");
                return Empty(issues);
            }
            if (!ShipDesignCatalogueValidation.Id(draft.ClassId))
                Error(issues, DesignIssueCode.InvalidId, "$.ClassId", "Canonical class ID required.");
            if (string.IsNullOrWhiteSpace(draft.DisplayName))
                Error(issues, DesignIssueCode.MissingField, "$.DisplayName", "Display name required.");
            var hull = Catalogue.Hulls.SingleOrDefault(h => h.VersionId == draft.HullVersionId);
            var role = Catalogue.Roles.SingleOrDefault(r => r.RoleId == draft.RoleId);
            if (hull == null)
                Error(issues, DesignIssueCode.UnknownReference, "$.HullVersionId", "Unknown hull version.");
            if (role == null)
                Error(issues, DesignIssueCode.UnknownReference, "$.RoleId", "Unknown role.");
            if (draft.EmbarkedCrew <= 0)
                Error(issues, DesignIssueCode.InvalidValue, "$.EmbarkedCrew", "Positive embarked crew required.");
            bool fits = ValidList(draft.Installations, "$.Installations", issues), profiles = ValidList(draft.Profiles, "$.Profiles", issues);
            if (fits)
            {
                ShipDesignCatalogueValidation.Unique(draft.Installations!.Select(i => i.InstallationId), "$.Installations", issues);
                foreach (var i in draft.Installations!)
                {
                    if (i.Quantity <= 0)
                        Error(issues, DesignIssueCode.InvalidValue, "$.Installations[" + i.InstallationId + "].Quantity", "Quantity must be positive.");
                    if (!Catalogue.Components.Any(c => c.VersionId == i.ComponentVersionId))
                        Error(issues, DesignIssueCode.UnknownReference, "$.Installations[" + i.InstallationId + "].ComponentVersionId", "Unknown component version.");
                }
            }
            if (profiles)
            {
                if (draft.Profiles!.Count != 3 || draft.Profiles.Select(p => p.Kind).Distinct().Count() != 3)
                    Error(issues, DesignIssueCode.MissingField, "$.Profiles", "Cruise, Combat and Survey required exactly once.");
                foreach (var p in draft.Profiles)
                {
                    string path = "$.Profiles[" + p.Kind + "]";
                    if (!ShipDesignCatalogueValidation.Known(p.Kind))
                        Error(issues, DesignIssueCode.InvalidValue, path, "Unknown profile.");
                    if (!ValidList(p.Allocations, path + ".Allocations", issues))
                        continue;
                    ShipDesignCatalogueValidation.Unique(p.Allocations!.Select(a => a.InstallationId), path + ".Allocations", issues);
                    if (fits && !new HashSet<string>(draft.Installations!.Select(i => i.InstallationId), StringComparer.Ordinal).SetEquals(p.Allocations.Select(a => a.InstallationId)))
                        Error(issues, DesignIssueCode.UnknownReference, path + ".Allocations", "Every installation must have exactly one allocation (zero explicitly disables optional systems).");
                    if (p.Allocations.Any(a => a.RequestedPower < 0 || a.AllocatedPower < 0))
                        Error(issues, DesignIssueCode.InvalidPowerAllocation, path, "Power cannot be negative.");
                }
            }
            if (access == null || !ValidList(access.AvailableVersionIds, "$.Access.AvailableVersionIds", issues))
            {
                if (access == null)
                    Error(issues, DesignIssueCode.MissingField, "$.Access", "Explicit available versions required.");
            }
            else
            {
                ShipDesignCatalogueValidation.Unique(access.AvailableVersionIds!, "$.Access.AvailableVersionIds", issues);
                var all = Catalogue.Hulls.Select(h => h.VersionId).Concat(Catalogue.Components.Select(c => c.VersionId)).ToHashSet(StringComparer.Ordinal);
                if (access.AvailableVersionIds!.Any(id => !all.Contains(id)))
                    Error(issues, DesignIssueCode.UnknownReference, "$.Access.AvailableVersionIds", "Unknown available version.");
            }
            if (issues.Count > 0)
                return Empty(issues);
            try
            {
                return Compile(new ShipClassDraftSnapshot(draft), hull!, role!, new HashSet<string>(access!.AvailableVersionIds!, StringComparer.Ordinal), issues);
            }
            catch (OverflowException) { Error(issues, DesignIssueCode.ArithmeticOverflow, "$.Compilation", "An aggregate exceeds Int64; no performance is published."); return Empty(issues); }
        }
        private ShipDesignPreview Compile(ShipClassDraftSnapshot draft, HullVersionSnapshot hull, ShipRoleSnapshot role, HashSet<string> available, List<DesignIssue> issues)
        {
            var fit = draft.Installations.Select(i => (Installation: i, Component: Catalogue.Components.Single(c => c.VersionId == i.ComponentVersionId))).ToList();
            if (!available.Contains(hull.VersionId))
                Error(issues, DesignIssueCode.VersionUnavailable, "$.HullVersionId", "Hull version is not deployable in the supplied access context.");
            long used = Sum(fit.Select(f => Product(f.Component.Capacity, f.Installation.Quantity)));
            if (used > hull.Capacity)
                Error(issues, DesignIssueCode.CapacityExceeded, "$.Capacity", "Used capacity exceeds hull capacity; mounts grant no extra space.");
            long requiredCrew = checked(hull.MinimumCrew + Sum(fit.Select(f => Product(f.Component.CrewRequired, f.Installation.Quantity))));
            if (draft.EmbarkedCrew < requiredCrew)
                Error(issues, DesignIssueCode.DependencyUnsatisfied, "$.EmbarkedCrew", "Crew is below hull plus installed component requirements.");
            foreach (var kind in new[] { ComponentKind.Reactor, ComponentKind.LifeSupport, ComponentKind.Command, ComponentKind.Propulsion })
                if (!fit.Any(f => f.Component.Kind == kind))
                    Error(issues, DesignIssueCode.MissingRequiredSystem, "$.Installations", "Missing required " + kind + " system.");
            foreach (var f in fit)
            {
                string p = "$.Installations[" + f.Installation.InstallationId + "]";
                if (!available.Contains(f.Component.VersionId))
                    Error(issues, DesignIssueCode.VersionUnavailable, p, "Component version is not deployable in the supplied access context.");
                if (!f.Component.CompatibleHullFamilyIds.Contains(hull.FamilyId) || f.Component.RequiredHullTags.Any(t => !hull.Tags.Contains(t)))
                    Error(issues, DesignIssueCode.IncompatibleHull, p, "Hull family/tags are incompatible.");
                int limit = f.Component.Kind == ComponentKind.Reactor ? hull.ReactorSizeLimit : f.Component.Kind == ComponentKind.Propulsion ? hull.PropulsionSizeLimit : int.MaxValue;
                if (f.Component.EnvelopeSize > limit)
                    Error(issues, DesignIssueCode.EnvelopeExceeded, p, "Component exceeds its hull size envelope.");
            }
            if (Sum(fit.Where(f => f.Component.Kind == ComponentKind.Reactor).Select(f => f.Installation.Quantity)) > hull.MaxReactors)
                Error(issues, DesignIssueCode.EnvelopeExceeded, "$.Reactors", "Too many reactor packages.");
            if (Sum(fit.Where(f => f.Component.Kind == ComponentKind.Propulsion).Select(f => f.Installation.Quantity)) > hull.MaxPropulsionPackages)
                Error(issues, DesignIssueCode.EnvelopeExceeded, "$.Propulsion", "Too many propulsion packages.");
            var mountUsed = hull.Mounts.ToDictionary(m => m.Id, m => 0L, StringComparer.Ordinal);
            var requirements = fit.SelectMany(f => f.Component.Mounts.Select(m => (Mount: m, Count: Product(m.Count, f.Installation.Quantity), Id: f.Installation.InstallationId)))
                .OrderBy(r => r.Mount.Kind).ThenBy(r => r.Mount.CoverageTag == "" ? 1 : 0).ThenBy(r => r.Mount.CoverageTag, StringComparer.Ordinal).ThenByDescending(r => r.Mount.SizeRating).ThenBy(r => r.Id, StringComparer.Ordinal);
            foreach (var r in requirements)
            {
                long remaining = r.Count;
                foreach (var mount in hull.Mounts.Where(m => m.Kind == r.Mount.Kind && m.SizeRating >= r.Mount.SizeRating && (r.Mount.CoverageTag == "" || m.CoverageTag == r.Mount.CoverageTag)).OrderBy(m => m.SizeRating).ThenBy(m => m.Id, StringComparer.Ordinal))
                {
                    long take = Math.Min(remaining, mount.Count - mountUsed[mount.Id]);
                    mountUsed[mount.Id] = checked(mountUsed[mount.Id] + take);
                    remaining -= take;
                }
                if (remaining > 0)
                    Error(issues, DesignIssueCode.MountExceeded, "$.Installations[" + r.Id + "].Mounts", "Insufficient compatible " + r.Mount.Kind + " mounts/coverage/size.");
            }
            var costItems = fit.Select(f => (Cost: f.Component.Cost, Quantity: f.Installation.Quantity)).Append((Cost: hull.Cost, Quantity: 1L)).ToList();
            var costs = new ShipDesignCostDto { Credits = Sum(costItems.Select(c => Product(c.Cost.Credits, c.Quantity))), Industry = Sum(costItems.Select(c => Product(c.Cost.Industry, c.Quantity))), Maintenance = Sum(costItems.Select(c => Product(c.Cost.Maintenance, c.Quantity))), Resources = costItems.SelectMany(c => c.Cost.Resources.Select(r => (Id: r.ResourceDefinitionId, Amount: Product(r.Amount, c.Quantity)))).GroupBy(r => r.Id, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new ResourceAmountDto { ResourceDefinitionId = g.Key, Amount = Sum(g.Select(r => r.Amount)) }).ToList() };
            long power = Sum(fit.Select(f => Product(f.Component.PowerSupply, f.Installation.Quantity))), availablePower = Math.Min(power, hull.PowerDistributionLimit);
            var profileResults = new List<ProfilePerformance>();
            foreach (var p in draft.Profiles)
            {
                string path = "$.Profiles[" + p.Kind + "]";
                int errorsBefore = issues.Count(i => i.Severity == DesignIssueSeverity.Error);
                var installations = new List<InstallationPowerPreview>();
                foreach (var f in fit)
                {
                    var mode = f.Component.Modes.Single(m => m.Profile == p.Kind);
                    var a = p.Allocations.Single(v => v.InstallationId == f.Installation.InstallationId);
                    long full = Product(mode.FullPower, f.Installation.Quantity), minimum = Product(mode.MinimumPower, f.Installation.Quantity);
                    string ap = path + ".Allocations[" + a.InstallationId + "]";
                    bool valid = a.AllocatedPower <= a.RequestedPower && a.RequestedPower <= full && (a.AllocatedPower == 0 || a.AllocatedPower >= minimum) && (mode.Scaling != PowerScaling.Fixed || a.AllocatedPower == 0 || a.AllocatedPower == full);
                    if (!valid)
                        Error(issues, DesignIssueCode.InvalidPowerAllocation, ap, "Allocation must fit request/full demand and a supported operating curve/minimum.");
                    if (mode.Essential && a.AllocatedPower < minimum)
                        Error(issues, DesignIssueCode.EssentialPowerInsufficient, ap, "Essential minimum power is not supplied.");
                    var outputs = valid && (full == 0 || a.AllocatedPower > 0) ? mode.Outputs.Select(o => Output(o.Kind, full == 0 ? Product(o.Amount, f.Installation.Quantity) : checked((long)((BigInteger)o.Amount * (Intensive(o.Kind) ? 1 : f.Installation.Quantity) * a.AllocatedPower / full)))).ToList() : new List<ShipOutputSnapshot>();
                    if (a.AllocatedPower < a.RequestedPower)
                        Warn(issues, DesignIssueCode.PartialPower, ap, "Requested performance is reduced by the declared power curve.");
                    installations.Add(new InstallationPowerPreview(a.InstallationId, full, minimum, a.RequestedPower, a.AllocatedPower, mode.Essential, outputs));
                }
                long peak = Sum(installations.Select(i => i.FullPower)), requested = Sum(installations.Select(i => i.RequestedPower)), allocated = Sum(installations.Select(i => i.AllocatedPower));
                if (allocated > availablePower)
                    Error(issues, DesignIssueCode.AvailablePowerExceeded, path, "Allocated power exceeds min(reactor output, hull distribution).");
                if (peak > availablePower)
                    Warn(issues, DesignIssueCode.PeakDemandExceedsSupply, path, "Installed peak demand exceeds supply; simultaneous maximum performance is unavailable.");
                bool powered = errorsBefore == issues.Count(i => i.Severity == DesignIssueSeverity.Error);
                if (!powered)
                    installations = installations.Select(i => new InstallationPowerPreview(i.InstallationId, i.FullPower, i.MinimumPower, i.RequestedPower, i.AllocatedPower, i.Essential, Array.Empty<ShipOutputSnapshot>())).ToList();
                var aggregate = installations.SelectMany(i => i.Outputs).GroupBy(o => o.Kind).OrderBy(g => g.Key).Select(g => Output(g.Key, Intensive(g.Key) ? g.Max(o => o.Amount) : Sum(g.Select(o => o.Amount)))).ToList();
                var result = new ProfilePerformance(p.Kind, availablePower, peak, requested, allocated, installations, aggregate);
                profileResults.Add(result);
                if (!powered)
                    continue;
                if (result.Output(ShipOutputKind.CrewSupport) < draft.EmbarkedCrew)
                    Error(issues, DesignIssueCode.DependencyUnsatisfied, path + ".CrewSupport", "Powered life support must support all embarked crew.");
                long computingRequired = Sum(fit.Where(f => f.Component.Kind == ComponentKind.Reactor || installations.Single(i => i.InstallationId == f.Installation.InstallationId).AllocatedPower > 0).Select(f => Product(f.Component.ComputerDemand, f.Installation.Quantity)));
                if (result.Output(ShipOutputKind.ComputerSupport) < computingRequired)
                    Error(issues, DesignIssueCode.DependencyUnsatisfied, path + ".ComputerSupport", "Powered computing must support every installed component's demand.");
                if (result.Output(ShipOutputKind.PropulsionLoadSupport) < hull.DesignLoad)
                    Error(issues, DesignIssueCode.DependencyUnsatisfied, path + ".PropulsionLoadSupport", "Powered propulsion must support this hull load.");
                foreach (var f in fit)
                {
                    var active = installations.Single(i => i.InstallationId == f.Installation.InstallationId);
                    if (f.Component.Kind != ComponentKind.Reactor && active.AllocatedPower == 0)
                        continue;
                    foreach (var dependency in f.Component.Dependencies)
                        if (result.Output(dependency.Kind) < Product(dependency.Amount, f.Installation.Quantity))
                            Error(issues, DesignIssueCode.DependencyUnsatisfied, path + ".Installations[" + f.Installation.InstallationId + "].Dependencies", "Insufficient powered " + dependency.Kind + " support.");
                    foreach (var tag in f.Component.RequiredComponentTags)
                        if (!fit.Any(other => other.Installation.InstallationId != f.Installation.InstallationId && other.Component.Tags.Contains(tag) && installations.Any(i => i.InstallationId == other.Installation.InstallationId && (i.FullPower == 0 || i.AllocatedPower > 0))))
                            Error(issues, DesignIssueCode.DependencyUnsatisfied, path + ".Installations[" + f.Installation.InstallationId + "].Tags", "Missing powered other-component tag: " + tag);
                }
                if (errorsBefore != issues.Count(i => i.Severity == DesignIssueSeverity.Error))
                {
                    var unavailable = installations.Select(i => new InstallationPowerPreview(i.InstallationId, i.FullPower, i.MinimumPower, i.RequestedPower, i.AllocatedPower, i.Essential, Array.Empty<ShipOutputSnapshot>()));
                    profileResults[profileResults.Count - 1] = new ProfilePerformance(p.Kind, availablePower, peak, requested, allocated, unavailable, Array.Empty<ShipOutputSnapshot>());
                    continue;
                }
                foreach (var recommendation in role.Recommendations.Where(r => r.Profile == p.Kind))
                if (result.Output(recommendation.Output) < recommendation.Minimum)
                    Warn(issues, DesignIssueCode.RoleUnderperformance, path + ".Role[" + recommendation.Output + "]", "Below the role's recommended " + recommendation.Output + " performance.");
                foreach (var kind in new[] { ShipOutputKind.Sensors, ShipOutputKind.Science, ShipOutputKind.OperationalRange })
                if (result.Output(kind) > 0)
                    Warn(issues, DesignIssueCode.IntegrationGap, path + ".Outputs[" + kind + "]", kind + " is a preview-only rating; no existing campaign consumer is defined.");
            }
            var cruise = profileResults.Single(p => p.Kind == OperatingProfileKind.Cruise);
            var combat = profileResults.Single(p => p.Kind == OperatingProfileKind.Combat);
            if (cruise.Output(ShipOutputKind.MovementBudget) <= 0)
                Error(issues, DesignIssueCode.DependencyUnsatisfied, "$.Profiles[Cruise].MovementBudget", "A mobile class needs a positive powered cruise movement budget.");
            if (combat.Output(ShipOutputKind.LongRangeAttack) == 0 && combat.Output(ShipOutputKind.MediumRangeAttack) == 0 && combat.Output(ShipOutputKind.CloseRangeAttack) == 0)
                Warn(issues, DesignIssueCode.Unarmed, "$.Role", "Unarmed classes are legal; review mission suitability.");
            CompiledShipPerformance? compiled = null;
            if (!issues.Any(i => i.Severity == DesignIssueSeverity.Error))
                compiled = new CompiledShipPerformance(hull.MaxHull, cruise.Output(ShipOutputKind.MovementBudget), new ShipCombatProfileDto { ShieldCapacity = combat.Output(ShipOutputKind.ShieldCapacity), LongRangeAttack = combat.Output(ShipOutputKind.LongRangeAttack), MediumRangeAttack = combat.Output(ShipOutputKind.MediumRangeAttack), CloseRangeAttack = combat.Output(ShipOutputKind.CloseRangeAttack), AccuracyBps = (int)combat.Output(ShipOutputKind.AccuracyBps), EvasionBps = (int)combat.Output(ShipOutputKind.EvasionBps) });
            return new ShipDesignPreview(issues, used, hull.Capacity, requiredCrew, power, hull.PowerDistributionLimit, hull.Mounts.Select(m => new MountUsage(m, mountUsed[m.Id])), new ShipDesignCostSnapshot(costs), profileResults, compiled, new[] { new EnvelopeUsage(ComponentKind.Reactor, Sum(fit.Where(f => f.Component.Kind == ComponentKind.Reactor).Select(f => f.Installation.Quantity)), hull.MaxReactors, fit.Where(f => f.Component.Kind == ComponentKind.Reactor).Select(f => f.Component.EnvelopeSize).DefaultIfEmpty().Max(), hull.ReactorSizeLimit), new EnvelopeUsage(ComponentKind.Propulsion, Sum(fit.Where(f => f.Component.Kind == ComponentKind.Propulsion).Select(f => f.Installation.Quantity)), hull.MaxPropulsionPackages, fit.Where(f => f.Component.Kind == ComponentKind.Propulsion).Select(f => f.Component.EnvelopeSize).DefaultIfEmpty().Max(), hull.PropulsionSizeLimit) });
        }
        public ShipBlueprintPublicationResult Publish(ShipClassDraftDto? draft, ShipDesignAccessDto? access, string blueprintId, int revision)
        {
            var preview = Preview(draft, access);
            var issues = preview.Issues.ToList();
            if (!ShipDesignCatalogueValidation.Id(blueprintId))
                Error(issues, DesignIssueCode.InvalidId, "$.BlueprintId", "Canonical blueprint ID required.");
            if (revision <= 0)
                Error(issues, DesignIssueCode.InvalidValue, "$.Revision", "Positive blueprint revision required.");
            if (issues.Count != preview.Issues.Count)
                preview = new ShipDesignPreview(issues, preview.CapacityUsed, preview.CapacityLimit, preview.RequiredCrew, preview.ReactorPower, preview.DistributionLimit, preview.Mounts, preview.Cost, preview.Profiles, null, preview.Envelopes);
            if (!preview.IsValid)
                return new ShipBlueprintPublicationResult(preview, null);
            var snapshot = new ShipClassDraftSnapshot(draft!);
            var hull = Catalogue.Hulls.Single(h => h.VersionId == snapshot.HullVersionId);
            var components = Catalogue.Components.Where(c => snapshot.Installations.Any(i => i.ComponentVersionId == c.VersionId));
            return new ShipBlueprintPublicationResult(preview, new PublishedShipBlueprint(blueprintId, revision, snapshot, hull, components, preview));
        }
    }
}
