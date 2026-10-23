using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class GalaxyReviewRoutePolicy
    {
        public const string MinimumCrossingsDirectV1 = "minimum-crossings-centre-span-v1";
        public const string SecondaryMetric = "sum-floor-centre-hop-length-v1";
        public const int MaximumTargets = 64;
    }

    public enum GalaxyReviewTargetIntent { RegionTransit = 1, SystemVisit = 2 }
    public enum GalaxyReviewPlanError { None = 0, InvalidRequest = 1, UnsupportedPolicy = 2, StaleMap = 3,
        InvalidFilter = 4, UnavailableOrigin = 5, UnavailableTarget = 6, InvalidVisitTarget = 7, UnreachableLeg = 8 }

    public sealed class GalaxyReviewTargetDto
    {
        public string NodeId { get; set; } = "";
        public GalaxyReviewTargetIntent Intent { get; set; } = GalaxyReviewTargetIntent.RegionTransit;
        public string? SystemId { get; set; }
    }

    // Explicit full-map review subset, not a source of actor authorization or sensor facts.
    // Missing lists reject; empty lists grant no access. Never persist as trusted authorization.
    public sealed class GalaxyReviewRouteFilterDto
    {
        public List<string> PermittedNodeIds { get; set; } = null!;
        public List<string> PermittedConnectionIds { get; set; } = null!;
    }

    public sealed class GalaxyReviewRouteQueryDto
    {
        public string PolicyVersion { get; set; } = GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1;
        public string MapContentHash { get; set; } = "";
        public string OriginNodeId { get; set; } = "";
        // Includes the final destination as the last target; order and repeated nodes matter.
        public List<GalaxyReviewTargetDto> Targets { get; set; } = null!;
        public GalaxyReviewRouteFilterDto Filter { get; set; } = null!;
    }

    public sealed class GalaxyReviewTargetSnapshot
    {
        public string NodeId { get; }
        public GalaxyReviewTargetIntent Intent { get; }
        public string? SystemId { get; }
        public GalaxyReviewTargetSnapshot(string node, GalaxyReviewTargetIntent intent, string? system)
        { NodeId = node; Intent = intent; SystemId = system; }
    }

    public sealed class GalaxyReviewLegSnapshot
    {
        public int Index { get; }
        public string OriginNodeId { get; }
        public GalaxyReviewTargetSnapshot Target { get; }
        public StrategicRouteSnapshot Route { get; }
        public long GeographicLengthUnits { get; }
        public GalaxyReviewLegSnapshot(int index, string origin, GalaxyReviewTargetSnapshot target, StrategicRouteSnapshot route, long geographicLength)
        { Index = index; OriginNodeId = origin; Target = target; Route = route; GeographicLengthUnits = geographicLength; }
    }

    public sealed class GalaxyReviewRoutePlanSnapshot
    {
        public string PolicyVersion { get; }
        public string SecondaryMetric => GalaxyReviewRoutePolicy.SecondaryMetric;
        public string MapContentHash { get; }
        public string OriginNodeId { get; }
        public GalaxyReviewPlanError ErrorCode { get; }
        public bool Success => ErrorCode == GalaxyReviewPlanError.None;
        public int? FailedLegIndex { get; }
        public IReadOnlyList<GalaxyReviewLegSnapshot> Legs { get; }
        public IReadOnlyList<string> Steps { get; }
        public long? TotalCost { get; }
        public long? GeographicLengthUnits { get; }
        public GalaxyReviewRoutePlanSnapshot(string policy, string mapHash, string origin, GalaxyReviewPlanError error, int? failedLeg,
            IEnumerable<GalaxyReviewLegSnapshot> legs, IEnumerable<string> steps, long? total, long? geographicLength)
        { PolicyVersion = policy; MapContentHash = mapHash; OriginNodeId = origin; ErrorCode = error; FailedLegIndex = failedLeg;
            Legs = new List<GalaxyReviewLegSnapshot>(legs).AsReadOnly(); Steps = new List<string>(steps).AsReadOnly();
            TotalCost = total; GeographicLengthUnits = geographicLength; }
    }
}
