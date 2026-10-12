using System;
using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts.ShipDesign
{
    public sealed class ShipDesignCostSnapshot
    {
        public long Credits
        { get; }
        public long Industry
        { get; }
        public long Maintenance
        { get; }
        public IReadOnlyList<ResourceAmountSnapshot> Resources
        { get; }
        public ShipDesignCostSnapshot(ShipDesignCostDto dto)
        {
            Credits = dto.Credits;
            Industry = dto.Industry;
            Maintenance = dto.Maintenance;
            Resources = ResourceAmountSnapshot.Copy(dto.Resources!);
        }
    }
    public sealed class ShipOutputSnapshot
    {
        public ShipOutputKind Kind
        { get; }
        public long Amount
        { get; }
        public ShipOutputSnapshot(ShipOutputDto dto)
        {
            Kind = dto.Kind;
            Amount = dto.Amount;
        }
    }
    public sealed class MountLimitSnapshot
    {
        public string Id
        { get; }
        public MountKind Kind
        { get; }
        public int SizeRating
        { get; }
        public long Count
        { get; }
        public string CoverageTag
        { get; }
        public MountLimitSnapshot(MountLimitDto dto)
        {
            Id = dto.Id;
            Kind = dto.Kind;
            SizeRating = dto.SizeRating;
            Count = dto.Count;
            CoverageTag = dto.CoverageTag;
        }
    }
    public sealed class MountRequirementSnapshot
    {
        public MountKind Kind
        { get; }
        public int SizeRating
        { get; }
        public long Count
        { get; }
        public string CoverageTag
        { get; }
        public MountRequirementSnapshot(MountRequirementDto dto)
        {
            Kind = dto.Kind;
            SizeRating = dto.SizeRating;
            Count = dto.Count;
            CoverageTag = dto.CoverageTag;
        }
    }
    public sealed class ComponentOperatingModeSnapshot
    {
        public OperatingProfileKind Profile
        { get; }
        public long FullPower
        { get; }
        public long MinimumPower
        { get; }
        public PowerScaling Scaling
        { get; }
        public bool Essential
        { get; }
        public IReadOnlyList<ShipOutputSnapshot> Outputs
        { get; }
        public ComponentOperatingModeSnapshot(ComponentOperatingModeDto dto)
        {
            Profile = dto.Profile;
            FullPower = dto.FullPower;
            MinimumPower = dto.MinimumPower;
            Scaling = dto.Scaling;
            Essential = dto.Essential;
            Outputs = dto.Outputs!.OrderBy(v => v.Kind).Select(v => new ShipOutputSnapshot(v)).ToList().AsReadOnly();
        }
    }
    public sealed class HullFamilySnapshot
    {
        public string FamilyId
        { get; }
        public string DisplayName
        { get; }
        public HullFamilySnapshot(HullFamilyDto dto)
        {
            FamilyId = dto.FamilyId;
            DisplayName = dto.DisplayName;
        }
    }
    public sealed class HullVersionSnapshot
    {
        public string VersionId
        { get; }
        public string FamilyId
        { get; }
        public int VersionNumber
        { get; }
        public string? ParentVersionId
        { get; }
        public string DisplayName
        { get; }
        public long Capacity
        { get; }
        public long MaxHull
        { get; }
        public long DesignLoad
        { get; }
        public long MinimumCrew
        { get; }
        public long PowerDistributionLimit
        { get; }
        public long MaxReactors
        { get; }
        public long MaxPropulsionPackages
        { get; }
        public int ReactorSizeLimit
        { get; }
        public int PropulsionSizeLimit
        { get; }
        public IReadOnlyList<string> Tags
        { get; }
        public IReadOnlyList<MountLimitSnapshot> Mounts
        { get; }
        public ShipDesignCostSnapshot Cost
        { get; }
        public HullVersionSnapshot(HullVersionDto dto)
        {
            VersionId = dto.VersionId;
            FamilyId = dto.FamilyId;
            VersionNumber = dto.VersionNumber;
            ParentVersionId = dto.ParentVersionId;
            DisplayName = dto.DisplayName;
            Capacity = dto.Capacity;
            MaxHull = dto.MaxHull;
            DesignLoad = dto.DesignLoad;
            MinimumCrew = dto.MinimumCrew;
            PowerDistributionLimit = dto.PowerDistributionLimit;
            MaxReactors = dto.MaxReactors;
            MaxPropulsionPackages = dto.MaxPropulsionPackages;
            ReactorSizeLimit = dto.ReactorSizeLimit;
            PropulsionSizeLimit = dto.PropulsionSizeLimit;
            Tags = dto.Tags!.OrderBy(v => v, StringComparer.Ordinal).ToList().AsReadOnly();
            Mounts = dto.Mounts!.OrderBy(v => v.Id, StringComparer.Ordinal).Select(v => new MountLimitSnapshot(v)).ToList().AsReadOnly();
            Cost = new ShipDesignCostSnapshot(dto.Cost!);
        }
    }
    public sealed class ComponentVersionSnapshot
    {
        public string VersionId
        { get; }
        public string FamilyId
        { get; }
        public int VersionNumber
        { get; }
        public string? ParentVersionId
        { get; }
        public string DisplayName
        { get; }
        public ComponentKind Kind
        { get; }
        public long Capacity
        { get; }
        public long CrewRequired
        { get; }
        public long ComputerDemand
        { get; }
        public long PowerSupply
        { get; }
        public int EnvelopeSize
        { get; }
        public IReadOnlyList<string> CompatibleHullFamilyIds
        { get; }
        public IReadOnlyList<string> RequiredHullTags
        { get; }
        public IReadOnlyList<string> Tags
        { get; }
        public IReadOnlyList<string> RequiredComponentTags
        { get; }
        public IReadOnlyList<MountRequirementSnapshot> Mounts
        { get; }
        public IReadOnlyList<ShipOutputSnapshot> Dependencies
        { get; }
        public IReadOnlyList<ComponentOperatingModeSnapshot> Modes
        { get; }
        public ShipDesignCostSnapshot Cost
        { get; }
        public ComponentVersionSnapshot(ComponentVersionDto dto)
        {
            VersionId = dto.VersionId;
            FamilyId = dto.FamilyId;
            VersionNumber = dto.VersionNumber;
            ParentVersionId = dto.ParentVersionId;
            DisplayName = dto.DisplayName;
            Kind = dto.Kind;
            Capacity = dto.Capacity;
            CrewRequired = dto.CrewRequired;
            ComputerDemand = dto.ComputerDemand;
            PowerSupply = dto.PowerSupply;
            EnvelopeSize = dto.EnvelopeSize;
            CompatibleHullFamilyIds = dto.CompatibleHullFamilyIds!.OrderBy(v => v, StringComparer.Ordinal).ToList().AsReadOnly();
            RequiredHullTags = dto.RequiredHullTags!.OrderBy(v => v, StringComparer.Ordinal).ToList().AsReadOnly();
            Tags = dto.Tags!.OrderBy(v => v, StringComparer.Ordinal).ToList().AsReadOnly();
            RequiredComponentTags = dto.RequiredComponentTags!.OrderBy(v => v, StringComparer.Ordinal).ToList().AsReadOnly();
            Mounts = dto.Mounts!.OrderBy(v => v.Kind).ThenBy(v => v.SizeRating).ThenBy(v => v.CoverageTag, StringComparer.Ordinal).Select(v => new MountRequirementSnapshot(v)).ToList().AsReadOnly();
            Dependencies = dto.Dependencies!.OrderBy(v => v.Kind).Select(v => new ShipOutputSnapshot(v)).ToList().AsReadOnly();
            Modes = dto.Modes!.OrderBy(v => v.Profile).Select(v => new ComponentOperatingModeSnapshot(v)).ToList().AsReadOnly();
            Cost = new ShipDesignCostSnapshot(dto.Cost!);
        }
    }
    public sealed class RoleRecommendationSnapshot
    {
        public OperatingProfileKind Profile
        { get; }
        public ShipOutputKind Output
        { get; }
        public long Minimum
        { get; }
        public RoleRecommendationSnapshot(RoleRecommendationDto dto)
        {
            Profile = dto.Profile;
            Output = dto.Output;
            Minimum = dto.Minimum;
        }
    }
    public sealed class ShipRoleSnapshot
    {
        public string RoleId
        { get; }
        public string DisplayName
        { get; }
        public IReadOnlyList<RoleRecommendationSnapshot> Recommendations
        { get; }
        public ShipRoleSnapshot(ShipRoleDto dto)
        {
            RoleId = dto.RoleId;
            DisplayName = dto.DisplayName;
            Recommendations = dto.Recommendations!.OrderBy(v => v.Profile).ThenBy(v => v.Output).Select(v => new RoleRecommendationSnapshot(v)).ToList().AsReadOnly();
        }
    }
    public sealed class ShipInstallationSnapshot
    {
        public string InstallationId
        { get; }
        public string ComponentVersionId
        { get; }
        public long Quantity
        { get; }
        public ShipInstallationSnapshot(ShipInstallationDto dto)
        {
            InstallationId = dto.InstallationId;
            ComponentVersionId = dto.ComponentVersionId;
            Quantity = dto.Quantity;
        }
    }
    public sealed class ProfilePowerAllocationSnapshot
    {
        public string InstallationId
        { get; }
        public long RequestedPower
        { get; }
        public long AllocatedPower
        { get; }
        public ProfilePowerAllocationSnapshot(ProfilePowerAllocationDto dto)
        {
            InstallationId = dto.InstallationId;
            RequestedPower = dto.RequestedPower;
            AllocatedPower = dto.AllocatedPower;
        }
    }
    public sealed class ShipOperatingProfileSnapshot
    {
        public OperatingProfileKind Kind
        { get; }
        public IReadOnlyList<ProfilePowerAllocationSnapshot> Allocations
        { get; }
        public ShipOperatingProfileSnapshot(ShipOperatingProfileDto dto)
        {
            Kind = dto.Kind;
            Allocations = dto.Allocations!.OrderBy(v => v.InstallationId, StringComparer.Ordinal).Select(v => new ProfilePowerAllocationSnapshot(v)).ToList().AsReadOnly();
        }
    }
    public sealed class ShipClassDraftSnapshot
    {
        public string ClassId
        { get; }
        public string DisplayName
        { get; }
        public string RoleId
        { get; }
        public string HullVersionId
        { get; }
        public long EmbarkedCrew
        { get; }
        public IReadOnlyList<ShipInstallationSnapshot> Installations
        { get; }
        public IReadOnlyList<ShipOperatingProfileSnapshot> Profiles
        { get; }
        public ShipClassDraftSnapshot(ShipClassDraftDto dto)
        {
            ClassId = dto.ClassId;
            DisplayName = dto.DisplayName;
            RoleId = dto.RoleId;
            HullVersionId = dto.HullVersionId;
            EmbarkedCrew = dto.EmbarkedCrew;
            Installations = dto.Installations!.OrderBy(v => v.InstallationId, StringComparer.Ordinal).Select(v => new ShipInstallationSnapshot(v)).ToList().AsReadOnly();
            Profiles = dto.Profiles!.OrderBy(v => v.Kind).Select(v => new ShipOperatingProfileSnapshot(v)).ToList().AsReadOnly();
        }
    }
    public sealed class ShipDesignCatalogueSnapshot
    {
        public int FormatVersion
        { get; }
        public IReadOnlyList<HullFamilySnapshot> HullFamilies
        { get; }
        public IReadOnlyList<HullVersionSnapshot> Hulls
        { get; }
        public IReadOnlyList<ComponentVersionSnapshot> Components
        { get; }
        public IReadOnlyList<ShipRoleSnapshot> Roles
        { get; }
        public ShipDesignCatalogueSnapshot(ShipDesignCatalogueDto dto)
        {
            FormatVersion = dto.FormatVersion!.Value;
            HullFamilies = dto.HullFamilies!.OrderBy(v => v.FamilyId, StringComparer.Ordinal).Select(v => new HullFamilySnapshot(v)).ToList().AsReadOnly();
            Hulls = dto.Hulls!.OrderBy(v => v.VersionId, StringComparer.Ordinal).Select(v => new HullVersionSnapshot(v)).ToList().AsReadOnly();
            Components = dto.Components!.OrderBy(v => v.VersionId, StringComparer.Ordinal).Select(v => new ComponentVersionSnapshot(v)).ToList().AsReadOnly();
            Roles = dto.Roles!.OrderBy(v => v.RoleId, StringComparer.Ordinal).Select(v => new ShipRoleSnapshot(v)).ToList().AsReadOnly();
        }
    }
}
