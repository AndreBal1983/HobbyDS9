using System;
using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts.ShipDesign
{
    public static class ShipDesignVersions
    {
        public const int CatalogueFormat = 1; public const string Compiler = "ship-design-compiler-v1";
    }
    public sealed class DesignIssue
    {
        public DesignIssueCode Code
        { get; }
        public DesignIssueSeverity Severity
        { get; }
        public string Path
        { get; }
        public string Message
        { get; }
        public DesignIssue(DesignIssueCode code, DesignIssueSeverity severity, string path, string message)
        {
            Code = code;
            Severity = severity;
            Path = path;
            Message = message;
        }
    }
    public sealed class EnvelopeUsage
    {
        public ComponentKind Kind
        { get; }
        public long PackagesUsed
        { get; }
        public long PackageLimit
        { get; }
        public int LargestSizeUsed
        { get; }
        public int SizeLimit
        { get; }
        public EnvelopeUsage(ComponentKind kind, long used, long limit, int size, int sizeLimit)
        {
            Kind = kind;
            PackagesUsed = used;
            PackageLimit = limit;
            LargestSizeUsed = size;
            SizeLimit = sizeLimit;
        }
    }
    public sealed class MountUsage
    {
        public string MountId
        { get; }
        public MountKind Kind
        { get; }
        public int SizeRating
        { get; }
        public string CoverageTag
        { get; }
        public long Used
        { get; }
        public long Limit
        { get; }
        public MountUsage(MountLimitSnapshot mount, long used)
        {
            MountId = mount.Id;
            Kind = mount.Kind;
            SizeRating = mount.SizeRating;
            CoverageTag = mount.CoverageTag;
            Used = used;
            Limit = mount.Count;
        }
    }
    public sealed class InstallationPowerPreview
    {
        public string InstallationId
        { get; }
        public long FullPower
        { get; }
        public long MinimumPower
        { get; }
        public long RequestedPower
        { get; }
        public long AllocatedPower
        { get; }
        public bool Essential
        { get; }
        public IReadOnlyList<ShipOutputSnapshot> Outputs
        { get; }
        public InstallationPowerPreview(string id, long full, long minimum, long requested, long allocated, bool essential, IEnumerable<ShipOutputSnapshot> outputs)
        {
            InstallationId = id;
            FullPower = full;
            MinimumPower = minimum;
            RequestedPower = requested;
            AllocatedPower = allocated;
            Essential = essential;
            Outputs = outputs.OrderBy(o => o.Kind).ToList().AsReadOnly();
        }
    }
    public sealed class ProfilePerformance
    {
        public OperatingProfileKind Kind
        { get; }
        public long AvailablePower
        { get; }
        public long InstalledPeakPower
        { get; }
        public long RequestedPower
        { get; }
        public long AllocatedPower
        { get; }
        public IReadOnlyList<InstallationPowerPreview> Installations
        { get; }
        public IReadOnlyList<ShipOutputSnapshot> Outputs
        { get; }
        public long Output(ShipOutputKind kind) => Outputs.SingleOrDefault(o => o.Kind == kind)?.Amount ?? 0;
        public ProfilePerformance(OperatingProfileKind kind, long available, long peak, long requested, long allocated, IEnumerable<InstallationPowerPreview> installations, IEnumerable<ShipOutputSnapshot> outputs)
        {
            Kind = kind;
            AvailablePower = available;
            InstalledPeakPower = peak;
            RequestedPower = requested;
            AllocatedPower = allocated;
            Installations = installations.OrderBy(i => i.InstallationId, StringComparer.Ordinal).ToList().AsReadOnly();
            Outputs = outputs.OrderBy(o => o.Kind).ToList().AsReadOnly();
        }
    }
    public sealed class CompiledShipPerformance
    {
        public long MaxHull
        { get; }
        public long MovementBudget
        { get; }
        public ShipCombatProfileSnapshot Combat
        { get; }
        public CompiledShipPerformance(long hull, long movement, ShipCombatProfileDto combat)
        {
            MaxHull = hull;
            MovementBudget = movement;
            Combat = new ShipCombatProfileSnapshot(combat);
        }
    }
    public sealed class ShipDesignPreview
    {
        public bool IsValid => Issues.All(i => i.Severity != DesignIssueSeverity.Error);
        public IReadOnlyList<DesignIssue> Issues
        { get; }
        public long? CapacityUsed
        { get; }
        public long? CapacityLimit
        { get; }
        public long? RequiredCrew
        { get; }
        public long? ReactorPower
        { get; }
        public long? DistributionLimit
        { get; }
        public IReadOnlyList<MountUsage> Mounts
        { get; }
        public IReadOnlyList<EnvelopeUsage> Envelopes
        { get; }
        public ShipDesignCostSnapshot? Cost
        { get; }
        public IReadOnlyList<ProfilePerformance> Profiles
        { get; }
        public CompiledShipPerformance? Compiled
        { get; }
        public ShipDesignPreview(IEnumerable<DesignIssue> issues, long? used, long? limit, long? crew, long? reactor, long? distribution, IEnumerable<MountUsage> mounts, ShipDesignCostSnapshot? cost, IEnumerable<ProfilePerformance> profiles, CompiledShipPerformance? compiled, IEnumerable<EnvelopeUsage>? envelopes = null)
        {
            Issues = issues.OrderBy(i => i.Path, StringComparer.Ordinal).ThenBy(i => i.Severity).ThenBy(i => i.Code).ThenBy(i => i.Message, StringComparer.Ordinal).ToList().AsReadOnly();
            CapacityUsed = used;
            CapacityLimit = limit;
            RequiredCrew = crew;
            ReactorPower = reactor;
            DistributionLimit = distribution;
            Mounts = mounts.OrderBy(m => m.MountId, StringComparer.Ordinal).ToList().AsReadOnly();
            Cost = cost;
            Envelopes = (envelopes ?? Array.Empty<EnvelopeUsage>()).OrderBy(e => e.Kind).ToList().AsReadOnly();
            Profiles = profiles.OrderBy(p => p.Kind).ToList().AsReadOnly();
            Compiled = compiled;
        }
    }
    public sealed class PublishedShipBlueprint
    {
        public const string CompilerVersion = ShipDesignVersions.Compiler;
        public string BlueprintId
        { get; }
        public int Revision
        { get; }
        public string RulesIdentity => CompilerVersion;
        public ShipClassDraftSnapshot Fit
        { get; }
        public HullVersionSnapshot Hull
        { get; }
        public IReadOnlyList<ComponentVersionSnapshot> Components
        { get; }
        public ShipDesignPreview Performance
        { get; }
        public PublishedShipBlueprint(string id, int revision, ShipClassDraftSnapshot fit, HullVersionSnapshot hull, IEnumerable<ComponentVersionSnapshot> components, ShipDesignPreview performance)
        {
            _ = new DefinitionId(id);
            if (revision <= 0 || !performance.IsValid || performance.Compiled == null)
                throw new ArgumentException("Publication requires a valid compiled design and positive revision.");
            BlueprintId = id;
            Revision = revision;
            Fit = fit;
            Hull = hull;
            Components = components.OrderBy(c => c.VersionId, StringComparer.Ordinal).ToList().AsReadOnly();
            Performance = performance;
        }
    }
    public sealed class ShipBlueprintPublicationResult
    {
        public ShipDesignPreview Preview
        { get; }
        public PublishedShipBlueprint? Blueprint
        { get; }
        public bool Published => Blueprint != null;
        public ShipBlueprintPublicationResult(ShipDesignPreview preview, PublishedShipBlueprint? blueprint)
        {
            Preview = preview;
            Blueprint = blueprint;
        }
    }
}
