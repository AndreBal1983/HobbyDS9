using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Contracts
{
    // Opt-in NEW campaign observation profile. Never reinterpret a generated-campaign-v1 save.
    public static class CampaignStaticAstronomyIdentity
    {
        public const int Format = 7;
        public const string Profile = "generated-campaign-v2";
        public const string Policy = "generated-neighbor-exploration-static-centres-v2";
        public const string EmbeddedCoreProfile = "generated-campaign-owned-core-v2";
        public const string ObservationPolicy = "charted-static-centres-v1";
        public const string VisualReferencePolicy = "cosmetic-influence-scale-v1";
    }

    // Coarse charted information only. These radii are cosmetic generation-influence references,
    // NOT physical extent, sensor range, a navigation hazard or permission to disclose other features.
    public sealed class CampaignNebulaVisualProxy
    {
        public string FeatureId { get; }
        public GalaxyEnvironmentFeatureKind Kind => GalaxyEnvironmentFeatureKind.Nebula;
        public int CentreX { get; }
        public int CentreY { get; }
        public int VisualReferenceRadiusX { get; }
        public int VisualReferenceRadiusY { get; }
        public string VisualReferencePolicy => CampaignStaticAstronomyIdentity.VisualReferencePolicy;
        public CampaignNebulaVisualProxy(string featureId, int centreX, int centreY, int visualReferenceRadiusX, int visualReferenceRadiusY)
        {
            FeatureId = featureId ?? throw new ArgumentNullException(nameof(featureId));
            CentreX = centreX; CentreY = centreY;
            VisualReferenceRadiusX = visualReferenceRadiusX; VisualReferenceRadiusY = visualReferenceRadiusY;
        }
    }

    public sealed class CampaignStaticAstronomySnapshot
    {
        public string ActorId { get; }
        public string MapContentHash { get; }
        public int ObservationTurn { get; }
        public long CampaignRevision { get; }
        public string ObservationPolicy => CampaignStaticAstronomyIdentity.ObservationPolicy;
        public IReadOnlyList<CampaignNebulaVisualProxy> Nebulae { get; }
        public CampaignStaticAstronomySnapshot(string actorId, string mapContentHash, int turn, long revision, IEnumerable<CampaignNebulaVisualProxy> nebulae)
        {
            ActorId = actorId; MapContentHash = mapContentHash; ObservationTurn = turn; CampaignRevision = revision;
            Nebulae = nebulae.OrderBy(n => n.FeatureId, StringComparer.Ordinal).Select(n => new CampaignNebulaVisualProxy(
                n.FeatureId, n.CentreX, n.CentreY, n.VisualReferenceRadiusX, n.VisualReferenceRadiusY)).ToList().AsReadOnly();
        }
    }
}
