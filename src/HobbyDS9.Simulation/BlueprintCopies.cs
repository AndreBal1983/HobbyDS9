using System;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    internal static class BlueprintCopies
    {
        internal static ShipDesignCostDto Copy(ShipDesignCostDto x) => new ShipDesignCostDto
        {
            Credits = x.Credits,
            Industry = x.Industry,
            Maintenance = x.Maintenance,
            Resources = x.Resources?.OrderBy(v => v?.ResourceDefinitionId, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList()
        };
        internal static bool Same(ShipDesignCostDto? a, ShipDesignCostDto? b) => a == null ? b == null : b != null &&
            a.Credits == b.Credits &&
            a.Industry == b.Industry &&
            a.Maintenance == b.Maintenance &&
            (a.Resources == null ? b.Resources == null : b.Resources != null && a.Resources.Count == b.Resources.Count && a.Resources.Zip(b.Resources, (x,y) => Same(x,y)).All(v => v));
        internal static ShipOutputDto Copy(ShipOutputDto x) => new ShipOutputDto
        {
            Kind = x.Kind,
            Amount = x.Amount
        };
        internal static bool Same(ShipOutputDto? a, ShipOutputDto? b) => a == null ? b == null : b != null &&
            a.Kind == b.Kind &&
            a.Amount == b.Amount;
        internal static MountLimitDto Copy(MountLimitDto x) => new MountLimitDto
        {
            Id = x.Id,
            Kind = x.Kind,
            SizeRating = x.SizeRating,
            Count = x.Count,
            CoverageTag = x.CoverageTag
        };
        internal static bool Same(MountLimitDto? a, MountLimitDto? b) => a == null ? b == null : b != null &&
            a.Id == b.Id &&
            a.Kind == b.Kind &&
            a.SizeRating == b.SizeRating &&
            a.Count == b.Count &&
            a.CoverageTag == b.CoverageTag;
        internal static MountRequirementDto Copy(MountRequirementDto x) => new MountRequirementDto
        {
            Kind = x.Kind,
            SizeRating = x.SizeRating,
            Count = x.Count,
            CoverageTag = x.CoverageTag
        };
        internal static bool Same(MountRequirementDto? a, MountRequirementDto? b) => a == null ? b == null : b != null &&
            a.Kind == b.Kind &&
            a.SizeRating == b.SizeRating &&
            a.Count == b.Count &&
            a.CoverageTag == b.CoverageTag;
        internal static ComponentOperatingModeDto Copy(ComponentOperatingModeDto x) => new ComponentOperatingModeDto
        {
            Profile = x.Profile,
            FullPower = x.FullPower,
            MinimumPower = x.MinimumPower,
            Scaling = x.Scaling,
            Essential = x.Essential,
            Outputs = x.Outputs?.OrderBy(v => v?.Kind).Select(v => v == null ? null! : Copy(v)).ToList()
        };
        internal static bool Same(ComponentOperatingModeDto? a, ComponentOperatingModeDto? b) => a == null ? b == null : b != null &&
            a.Profile == b.Profile &&
            a.FullPower == b.FullPower &&
            a.MinimumPower == b.MinimumPower &&
            a.Scaling == b.Scaling &&
            a.Essential == b.Essential &&
            (a.Outputs == null ? b.Outputs == null : b.Outputs != null && a.Outputs.Count == b.Outputs.Count && a.Outputs.Zip(b.Outputs, (x,y) => Same(x,y)).All(v => v));
        internal static HullFamilyDto Copy(HullFamilyDto x) => new HullFamilyDto
        {
            FamilyId = x.FamilyId,
            DisplayName = x.DisplayName
        };
        internal static bool Same(HullFamilyDto? a, HullFamilyDto? b) => a == null ? b == null : b != null &&
            a.FamilyId == b.FamilyId &&
            a.DisplayName == b.DisplayName;
        internal static HullVersionDto Copy(HullVersionDto x) => new HullVersionDto
        {
            VersionId = x.VersionId,
            FamilyId = x.FamilyId,
            VersionNumber = x.VersionNumber,
            ParentVersionId = x.ParentVersionId,
            DisplayName = x.DisplayName,
            Capacity = x.Capacity,
            MaxHull = x.MaxHull,
            DesignLoad = x.DesignLoad,
            MinimumCrew = x.MinimumCrew,
            PowerDistributionLimit = x.PowerDistributionLimit,
            MaxReactors = x.MaxReactors,
            MaxPropulsionPackages = x.MaxPropulsionPackages,
            ReactorSizeLimit = x.ReactorSizeLimit,
            PropulsionSizeLimit = x.PropulsionSizeLimit,
            Tags = x.Tags?.OrderBy(v => v, StringComparer.Ordinal).ToList(),
            Mounts = x.Mounts?.OrderBy(v => v?.Id, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList(),
            Cost = x.Cost == null ? null : Copy(x.Cost)
        };
        internal static bool Same(HullVersionDto? a, HullVersionDto? b) => a == null ? b == null : b != null &&
            a.VersionId == b.VersionId &&
            a.FamilyId == b.FamilyId &&
            a.VersionNumber == b.VersionNumber &&
            a.ParentVersionId == b.ParentVersionId &&
            a.DisplayName == b.DisplayName &&
            a.Capacity == b.Capacity &&
            a.MaxHull == b.MaxHull &&
            a.DesignLoad == b.DesignLoad &&
            a.MinimumCrew == b.MinimumCrew &&
            a.PowerDistributionLimit == b.PowerDistributionLimit &&
            a.MaxReactors == b.MaxReactors &&
            a.MaxPropulsionPackages == b.MaxPropulsionPackages &&
            a.ReactorSizeLimit == b.ReactorSizeLimit &&
            a.PropulsionSizeLimit == b.PropulsionSizeLimit &&
            (a.Tags == null ? b.Tags == null : b.Tags != null && a.Tags.SequenceEqual(b.Tags)) &&
            (a.Mounts == null ? b.Mounts == null : b.Mounts != null && a.Mounts.Count == b.Mounts.Count && a.Mounts.Zip(b.Mounts, (x,y) => Same(x,y)).All(v => v)) &&
            Same(a.Cost, b.Cost);
        internal static ComponentVersionDto Copy(ComponentVersionDto x) => new ComponentVersionDto
        {
            VersionId = x.VersionId,
            FamilyId = x.FamilyId,
            VersionNumber = x.VersionNumber,
            ParentVersionId = x.ParentVersionId,
            DisplayName = x.DisplayName,
            Kind = x.Kind,
            Capacity = x.Capacity,
            CrewRequired = x.CrewRequired,
            ComputerDemand = x.ComputerDemand,
            PowerSupply = x.PowerSupply,
            EnvelopeSize = x.EnvelopeSize,
            CompatibleHullFamilyIds = x.CompatibleHullFamilyIds?.OrderBy(v => v, StringComparer.Ordinal).ToList(),
            RequiredHullTags = x.RequiredHullTags?.OrderBy(v => v, StringComparer.Ordinal).ToList(),
            Tags = x.Tags?.OrderBy(v => v, StringComparer.Ordinal).ToList(),
            RequiredComponentTags = x.RequiredComponentTags?.OrderBy(v => v, StringComparer.Ordinal).ToList(),
            Mounts = x.Mounts?.OrderBy(v => v?.Kind).ThenBy(v => v?.SizeRating).ThenBy(v => v?.CoverageTag, StringComparer.Ordinal).ThenBy(v => v?.Count).Select(v => v == null ? null! : Copy(v)).ToList(),
            Dependencies = x.Dependencies?.OrderBy(v => v?.Kind).Select(v => v == null ? null! : Copy(v)).ToList(),
            Modes = x.Modes?.OrderBy(v => v?.Profile).Select(v => v == null ? null! : Copy(v)).ToList(),
            Cost = x.Cost == null ? null : Copy(x.Cost)
        };
        internal static bool Same(ComponentVersionDto? a, ComponentVersionDto? b) => a == null ? b == null : b != null &&
            a.VersionId == b.VersionId &&
            a.FamilyId == b.FamilyId &&
            a.VersionNumber == b.VersionNumber &&
            a.ParentVersionId == b.ParentVersionId &&
            a.DisplayName == b.DisplayName &&
            a.Kind == b.Kind &&
            a.Capacity == b.Capacity &&
            a.CrewRequired == b.CrewRequired &&
            a.ComputerDemand == b.ComputerDemand &&
            a.PowerSupply == b.PowerSupply &&
            a.EnvelopeSize == b.EnvelopeSize &&
            (a.CompatibleHullFamilyIds == null ? b.CompatibleHullFamilyIds == null : b.CompatibleHullFamilyIds != null && a.CompatibleHullFamilyIds.SequenceEqual(b.CompatibleHullFamilyIds)) &&
            (a.RequiredHullTags == null ? b.RequiredHullTags == null : b.RequiredHullTags != null && a.RequiredHullTags.SequenceEqual(b.RequiredHullTags)) &&
            (a.Tags == null ? b.Tags == null : b.Tags != null && a.Tags.SequenceEqual(b.Tags)) &&
            (a.RequiredComponentTags == null ? b.RequiredComponentTags == null : b.RequiredComponentTags != null && a.RequiredComponentTags.SequenceEqual(b.RequiredComponentTags)) &&
            (a.Mounts == null ? b.Mounts == null : b.Mounts != null && a.Mounts.Count == b.Mounts.Count && a.Mounts.Zip(b.Mounts, (x,y) => Same(x,y)).All(v => v)) &&
            (a.Dependencies == null ? b.Dependencies == null : b.Dependencies != null && a.Dependencies.Count == b.Dependencies.Count && a.Dependencies.Zip(b.Dependencies, (x,y) => Same(x,y)).All(v => v)) &&
            (a.Modes == null ? b.Modes == null : b.Modes != null && a.Modes.Count == b.Modes.Count && a.Modes.Zip(b.Modes, (x,y) => Same(x,y)).All(v => v)) &&
            Same(a.Cost, b.Cost);
        internal static RoleRecommendationDto Copy(RoleRecommendationDto x) => new RoleRecommendationDto
        {
            Profile = x.Profile,
            Output = x.Output,
            Minimum = x.Minimum
        };
        internal static bool Same(RoleRecommendationDto? a, RoleRecommendationDto? b) => a == null ? b == null : b != null &&
            a.Profile == b.Profile &&
            a.Output == b.Output &&
            a.Minimum == b.Minimum;
        internal static ShipRoleDto Copy(ShipRoleDto x) => new ShipRoleDto
        {
            RoleId = x.RoleId,
            DisplayName = x.DisplayName,
            Recommendations = x.Recommendations?.OrderBy(v => v?.Profile).ThenBy(v => v?.Output).ThenBy(v => v?.Minimum).Select(v => v == null ? null! : Copy(v)).ToList()
        };
        internal static bool Same(ShipRoleDto? a, ShipRoleDto? b) => a == null ? b == null : b != null &&
            a.RoleId == b.RoleId &&
            a.DisplayName == b.DisplayName &&
            (a.Recommendations == null ? b.Recommendations == null : b.Recommendations != null && a.Recommendations.Count == b.Recommendations.Count && a.Recommendations.Zip(b.Recommendations, (x,y) => Same(x,y)).All(v => v));
        internal static ShipInstallationDto Copy(ShipInstallationDto x) => new ShipInstallationDto
        {
            InstallationId = x.InstallationId,
            ComponentVersionId = x.ComponentVersionId,
            Quantity = x.Quantity
        };
        internal static bool Same(ShipInstallationDto? a, ShipInstallationDto? b) => a == null ? b == null : b != null &&
            a.InstallationId == b.InstallationId &&
            a.ComponentVersionId == b.ComponentVersionId &&
            a.Quantity == b.Quantity;
        internal static ProfilePowerAllocationDto Copy(ProfilePowerAllocationDto x) => new ProfilePowerAllocationDto
        {
            InstallationId = x.InstallationId,
            RequestedPower = x.RequestedPower,
            AllocatedPower = x.AllocatedPower
        };
        internal static bool Same(ProfilePowerAllocationDto? a, ProfilePowerAllocationDto? b) => a == null ? b == null : b != null &&
            a.InstallationId == b.InstallationId &&
            a.RequestedPower == b.RequestedPower &&
            a.AllocatedPower == b.AllocatedPower;
        internal static ShipOperatingProfileDto Copy(ShipOperatingProfileDto x) => new ShipOperatingProfileDto
        {
            Kind = x.Kind,
            Allocations = x.Allocations?.OrderBy(v => v?.InstallationId, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList()
        };
        internal static bool Same(ShipOperatingProfileDto? a, ShipOperatingProfileDto? b) => a == null ? b == null : b != null &&
            a.Kind == b.Kind &&
            (a.Allocations == null ? b.Allocations == null : b.Allocations != null && a.Allocations.Count == b.Allocations.Count && a.Allocations.Zip(b.Allocations, (x,y) => Same(x,y)).All(v => v));
        internal static ShipClassDraftDto Copy(ShipClassDraftDto x) => new ShipClassDraftDto
        {
            ClassId = x.ClassId,
            DisplayName = x.DisplayName,
            RoleId = x.RoleId,
            HullVersionId = x.HullVersionId,
            EmbarkedCrew = x.EmbarkedCrew,
            Installations = x.Installations?.OrderBy(v => v?.InstallationId, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList(),
            Profiles = x.Profiles?.OrderBy(v => v?.Kind).Select(v => v == null ? null! : Copy(v)).ToList()
        };
        internal static bool Same(ShipClassDraftDto? a, ShipClassDraftDto? b) => a == null ? b == null : b != null &&
            a.ClassId == b.ClassId &&
            a.DisplayName == b.DisplayName &&
            a.RoleId == b.RoleId &&
            a.HullVersionId == b.HullVersionId &&
            a.EmbarkedCrew == b.EmbarkedCrew &&
            (a.Installations == null ? b.Installations == null : b.Installations != null && a.Installations.Count == b.Installations.Count && a.Installations.Zip(b.Installations, (x,y) => Same(x,y)).All(v => v)) &&
            (a.Profiles == null ? b.Profiles == null : b.Profiles != null && a.Profiles.Count == b.Profiles.Count && a.Profiles.Zip(b.Profiles, (x,y) => Same(x,y)).All(v => v));
        internal static ShipDesignAccessDto Copy(ShipDesignAccessDto x) => new ShipDesignAccessDto
        {
            AvailableVersionIds = x.AvailableVersionIds?.OrderBy(v => v, StringComparer.Ordinal).ToList()
        };
        internal static bool Same(ShipDesignAccessDto? a, ShipDesignAccessDto? b) => a == null ? b == null : b != null &&
            (a.AvailableVersionIds == null ? b.AvailableVersionIds == null : b.AvailableVersionIds != null && a.AvailableVersionIds.SequenceEqual(b.AvailableVersionIds));
        internal static ShipDesignCatalogueDto Copy(ShipDesignCatalogueDto x) => new ShipDesignCatalogueDto
        {
            FormatVersion = x.FormatVersion,
            HullFamilies = x.HullFamilies?.OrderBy(v => v?.FamilyId, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList(),
            Hulls = x.Hulls?.OrderBy(v => v?.VersionId, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList(),
            Components = x.Components?.OrderBy(v => v?.VersionId, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList(),
            Roles = x.Roles?.OrderBy(v => v?.RoleId, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList()
        };
        internal static bool Same(ShipDesignCatalogueDto? a, ShipDesignCatalogueDto? b) => a == null ? b == null : b != null &&
            a.FormatVersion == b.FormatVersion &&
            (a.HullFamilies == null ? b.HullFamilies == null : b.HullFamilies != null && a.HullFamilies.Count == b.HullFamilies.Count && a.HullFamilies.Zip(b.HullFamilies, (x,y) => Same(x,y)).All(v => v)) &&
            (a.Hulls == null ? b.Hulls == null : b.Hulls != null && a.Hulls.Count == b.Hulls.Count && a.Hulls.Zip(b.Hulls, (x,y) => Same(x,y)).All(v => v)) &&
            (a.Components == null ? b.Components == null : b.Components != null && a.Components.Count == b.Components.Count && a.Components.Zip(b.Components, (x,y) => Same(x,y)).All(v => v)) &&
            (a.Roles == null ? b.Roles == null : b.Roles != null && a.Roles.Count == b.Roles.Count && a.Roles.Zip(b.Roles, (x,y) => Same(x,y)).All(v => v));
        internal static ShipDesignVersionAccessDto Copy(ShipDesignVersionAccessDto x) => new ShipDesignVersionAccessDto
        {
            VersionId = x.VersionId,
            RequiredEngineeringApplicationDefinitionIds = x.RequiredEngineeringApplicationDefinitionIds?.OrderBy(v => v, StringComparer.Ordinal).ToList()
        };
        internal static bool Same(ShipDesignVersionAccessDto? a, ShipDesignVersionAccessDto? b) => a == null ? b == null : b != null &&
            a.VersionId == b.VersionId &&
            (a.RequiredEngineeringApplicationDefinitionIds == null ? b.RequiredEngineeringApplicationDefinitionIds == null : b.RequiredEngineeringApplicationDefinitionIds != null && a.RequiredEngineeringApplicationDefinitionIds.SequenceEqual(b.RequiredEngineeringApplicationDefinitionIds));
        internal static BlueprintReferenceDto Copy(BlueprintReferenceDto x) => new BlueprintReferenceDto
        {
            BlueprintId = x.BlueprintId,
            Revision = x.Revision
        };
        internal static bool Same(BlueprintReferenceDto? a, BlueprintReferenceDto? b) => a == null ? b == null : b != null &&
            a.BlueprintId == b.BlueprintId &&
            a.Revision == b.Revision;
        internal static BlueprintRevisionDto Copy(BlueprintRevisionDto x) => new BlueprintRevisionDto
        {
            OwnerCivilizationId = x.OwnerCivilizationId,
            BlueprintId = x.BlueprintId,
            Revision = x.Revision,
            CompilerVersion = x.CompilerVersion,
            RulesVersion = x.RulesVersion,
            SourceContentHash = x.SourceContentHash,
            FrozenCatalogue = x.FrozenCatalogue == null ? null : Copy(x.FrozenCatalogue),
            Fit = x.Fit == null ? null : Copy(x.Fit),
            CompiledDesign = x.CompiledDesign == null ? null : Copy(x.CompiledDesign),
            Cost = x.Cost == null ? null : Copy(x.Cost)
        };
        internal static bool Same(BlueprintRevisionDto? a, BlueprintRevisionDto? b) => a == null ? b == null : b != null &&
            a.OwnerCivilizationId == b.OwnerCivilizationId &&
            a.BlueprintId == b.BlueprintId &&
            a.Revision == b.Revision &&
            a.CompilerVersion == b.CompilerVersion &&
            a.RulesVersion == b.RulesVersion &&
            a.SourceContentHash == b.SourceContentHash &&
            Same(a.FrozenCatalogue, b.FrozenCatalogue) &&
            Same(a.Fit, b.Fit) &&
            Same(a.CompiledDesign, b.CompiledDesign) &&
            Same(a.Cost, b.Cost);
        internal static ResourceAmountDto Copy(ResourceAmountDto x) => new ResourceAmountDto
        {
            ResourceDefinitionId = x.ResourceDefinitionId,
            Amount = x.Amount
        };
        internal static bool Same(ResourceAmountDto? a, ResourceAmountDto? b) => a == null ? b == null : b != null &&
            a.ResourceDefinitionId == b.ResourceDefinitionId &&
            a.Amount == b.Amount;
        internal static ShipDesignDefinitionDto Copy(ShipDesignDefinitionDto x) => new ShipDesignDefinitionDto
        {
            DefinitionId = x.DefinitionId,
            DisplayName = x.DisplayName,
            MovementBudget = x.MovementBudget,
            IndustryCost = x.IndustryCost,
            ResourceCosts = x.ResourceCosts?.OrderBy(v => v?.ResourceDefinitionId, StringComparer.Ordinal).Select(v => v == null ? null! : Copy(v)).ToList(),
            CreditCost = x.CreditCost,
            MaxHull = x.MaxHull,
            CombatProfile = x.CombatProfile == null ? null : Copy(x.CombatProfile),
            RequiredEngineeringApplicationDefinitionIds = x.RequiredEngineeringApplicationDefinitionIds?.OrderBy(v => v, StringComparer.Ordinal).ToList()
        };
        internal static bool Same(ShipDesignDefinitionDto? a, ShipDesignDefinitionDto? b) => a == null ? b == null : b != null &&
            a.DefinitionId == b.DefinitionId &&
            a.DisplayName == b.DisplayName &&
            a.MovementBudget == b.MovementBudget &&
            a.IndustryCost == b.IndustryCost &&
            (a.ResourceCosts == null ? b.ResourceCosts == null : b.ResourceCosts != null && a.ResourceCosts.Count == b.ResourceCosts.Count && a.ResourceCosts.Zip(b.ResourceCosts, (x,y) => Same(x,y)).All(v => v)) &&
            a.CreditCost == b.CreditCost &&
            a.MaxHull == b.MaxHull &&
            Same(a.CombatProfile, b.CombatProfile) &&
            (a.RequiredEngineeringApplicationDefinitionIds == null ? b.RequiredEngineeringApplicationDefinitionIds == null : b.RequiredEngineeringApplicationDefinitionIds != null && a.RequiredEngineeringApplicationDefinitionIds.SequenceEqual(b.RequiredEngineeringApplicationDefinitionIds));
        internal static ShipCombatProfileDto Copy(ShipCombatProfileDto x) => new ShipCombatProfileDto
        {
            ShieldCapacity = x.ShieldCapacity,
            LongRangeAttack = x.LongRangeAttack,
            MediumRangeAttack = x.MediumRangeAttack,
            CloseRangeAttack = x.CloseRangeAttack,
            AccuracyBps = x.AccuracyBps,
            EvasionBps = x.EvasionBps
        };
        internal static bool Same(ShipCombatProfileDto? a, ShipCombatProfileDto? b) => a == null ? b == null : b != null &&
            a.ShieldCapacity == b.ShieldCapacity &&
            a.LongRangeAttack == b.LongRangeAttack &&
            a.MediumRangeAttack == b.MediumRangeAttack &&
            a.CloseRangeAttack == b.CloseRangeAttack &&
            a.AccuracyBps == b.AccuracyBps &&
            a.EvasionBps == b.EvasionBps;
    }
}
