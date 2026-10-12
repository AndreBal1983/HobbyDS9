using System.Collections.Generic;
namespace HobbyDS9.Contracts.ShipDesign
{
    public enum ComponentKind
    {
        Reactor = 1, LifeSupport = 2, Command = 3, Propulsion = 4, Sensors = 5, Shields = 6, EnergyWeapon = 7, TorpedoWeapon = 8, Science = 9, Support = 10
    }
    public enum OperatingProfileKind
    {
        Cruise = 1, Combat = 2, Survey = 3
    }
    public enum PowerScaling
    {
        Fixed = 1, Linear = 2
    }
    public enum MountKind
    {
        EnergyBank = 1, TorpedoLauncher = 2
    }
    public enum ShipOutputKind
    {
        CrewSupport = 1, ComputerSupport = 2, PropulsionLoadSupport = 3, ShieldCapacity = 4, LongRangeAttack = 5, MediumRangeAttack = 6, CloseRangeAttack = 7, AccuracyBps = 8, EvasionBps = 9, MovementBudget = 10, Sensors = 11, Science = 12, OperationalRange = 13
    }
    public enum DesignIssueSeverity
    {
        Error = 1, Warning = 2
    }
    public enum DesignIssueCode
    {
        MissingField = 1, InvalidId = 2, DuplicateId = 3, InvalidValue = 4, UnknownReference = 5, IncompatibleHull = 6, CapacityExceeded = 7, MountExceeded = 8, EnvelopeExceeded = 9, MissingRequiredSystem = 10, DependencyUnsatisfied = 11, EssentialPowerInsufficient = 12, InvalidPowerAllocation = 13, AvailablePowerExceeded = 14, VersionUnavailable = 15, ArithmeticOverflow = 16, Unarmed = 101, RoleUnderperformance = 102, PartialPower = 103, PeakDemandExceedsSupply = 104, IntegrationGap = 105
    }

    public sealed class ShipDesignCostDto
    {
        public long Credits
        {
            get; set;
        }
        public long Industry
        {
            get; set;
        }
        public long Maintenance
        {
            get; set;
        }
        public List<ResourceAmountDto>? Resources
        {
            get; set;
        }
    }
    public sealed class ShipOutputDto
    {
        public ShipOutputKind Kind
        {
            get; set;
        }
        public long Amount
        {
            get; set;
        }
    }
    public sealed class MountLimitDto
    {
        public string Id { get; set; } = string.Empty;
        public MountKind Kind
        {
            get; set;
        }
        public int SizeRating
        {
            get; set;
        }
        public long Count
        {
            get; set;
        }
        public string CoverageTag { get; set; } = string.Empty;
    }
    public sealed class MountRequirementDto
    {
        public MountKind Kind
        {
            get; set;
        }
        public int SizeRating
        {
            get; set;
        }
        public long Count
        {
            get; set;
        }
        public string CoverageTag { get; set; } = string.Empty;
    }
    public sealed class ComponentOperatingModeDto
    {
        public OperatingProfileKind Profile
        {
            get; set;
        }
        public long FullPower
        {
            get; set;
        }
        public long MinimumPower
        {
            get; set;
        }
        public PowerScaling Scaling
        {
            get; set;
        }
        public bool Essential
        {
            get; set;
        }
        public List<ShipOutputDto>? Outputs
        {
            get; set;
        }
    }
    public sealed class HullFamilyDto
    {
        public string FamilyId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
    public sealed class HullVersionDto
    {
        public string VersionId { get; set; } = string.Empty;
        public string FamilyId { get; set; } = string.Empty;
        public int VersionNumber
        {
            get; set;
        }
        public string? ParentVersionId
        {
            get; set;
        }
        public string DisplayName { get; set; } = string.Empty;
        public long Capacity
        {
            get; set;
        }
        public long MaxHull
        {
            get; set;
        }
        public long DesignLoad
        {
            get; set;
        }
        public long MinimumCrew
        {
            get; set;
        }
        public long PowerDistributionLimit
        {
            get; set;
        }
        public long MaxReactors
        {
            get; set;
        }
        public long MaxPropulsionPackages
        {
            get; set;
        }
        public int ReactorSizeLimit
        {
            get; set;
        }
        public int PropulsionSizeLimit
        {
            get; set;
        }
        public List<string>? Tags
        {
            get; set;
        }
        public List<MountLimitDto>? Mounts
        {
            get; set;
        }
        public ShipDesignCostDto? Cost
        {
            get; set;
        }
    }
    public sealed class ComponentVersionDto
    {
        public string VersionId { get; set; } = string.Empty;
        public string FamilyId { get; set; } = string.Empty;
        public int VersionNumber
        {
            get; set;
        }
        public string? ParentVersionId
        {
            get; set;
        }
        public string DisplayName { get; set; } = string.Empty;
        public ComponentKind Kind
        {
            get; set;
        }
        public long Capacity
        {
            get; set;
        }
        public long CrewRequired
        {
            get; set;
        }
        public long ComputerDemand
        {
            get; set;
        }
        public long PowerSupply
        {
            get; set;
        }
        public int EnvelopeSize
        {
            get; set;
        }
        public List<string>? CompatibleHullFamilyIds
        {
            get; set;
        }
        public List<string>? RequiredHullTags
        {
            get; set;
        }
        public List<string>? Tags
        {
            get; set;
        }
        public List<string>? RequiredComponentTags
        {
            get; set;
        }
        public List<MountRequirementDto>? Mounts
        {
            get; set;
        }
        public List<ShipOutputDto>? Dependencies
        {
            get; set;
        }
        public List<ComponentOperatingModeDto>? Modes
        {
            get; set;
        }
        public ShipDesignCostDto? Cost
        {
            get; set;
        }
    }
    public sealed class RoleRecommendationDto
    {
        public OperatingProfileKind Profile
        {
            get; set;
        }
        public ShipOutputKind Output
        {
            get; set;
        }
        public long Minimum
        {
            get; set;
        }
    }
    public sealed class ShipRoleDto
    {
        public string RoleId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<RoleRecommendationDto>? Recommendations
        {
            get; set;
        }
    }
    public sealed class ShipInstallationDto
    {
        public string InstallationId { get; set; } = string.Empty;
        public string ComponentVersionId { get; set; } = string.Empty;
        public long Quantity
        {
            get; set;
        }
    }
    public sealed class ProfilePowerAllocationDto
    {
        public string InstallationId { get; set; } = string.Empty;
        public long RequestedPower
        {
            get; set;
        }
        public long AllocatedPower
        {
            get; set;
        }
    }
    public sealed class ShipOperatingProfileDto
    {
        public OperatingProfileKind Kind
        {
            get; set;
        }
        public List<ProfilePowerAllocationDto>? Allocations
        {
            get; set;
        }
    }
    public sealed class ShipClassDraftDto
    {
        public string ClassId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string HullVersionId { get; set; } = string.Empty;
        public long EmbarkedCrew
        {
            get; set;
        }
        public List<ShipInstallationDto>? Installations
        {
            get; set;
        }
        public List<ShipOperatingProfileDto>? Profiles
        {
            get; set;
        }
    }
    public sealed class ShipDesignAccessDto
    {
        public List<string>? AvailableVersionIds
        {
            get; set;
        }
    }
    public sealed class ShipDesignCatalogueDto
    {
        public int? FormatVersion
        {
            get; set;
        }
        public List<HullFamilyDto>? HullFamilies
        {
            get; set;
        }
        public List<HullVersionDto>? Hulls
        {
            get; set;
        }
        public List<ComponentVersionDto>? Components
        {
            get; set;
        }
        public List<ShipRoleDto>? Roles
        {
            get; set;
        }
    }
}
