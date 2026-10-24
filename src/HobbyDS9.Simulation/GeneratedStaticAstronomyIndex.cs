using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Derived from immutable frozen geometry only. Never persisted, exposed or used for routing/contact coverage.
    internal sealed class GeneratedStaticAstronomyIndex
    {
        private readonly Dictionary<string, List<CampaignNebulaVisualProxy>> byRegion;
        internal long CandidateRegionChecks { get; }
        internal long CrossProducts { get; }
        internal int Memberships { get; }
        private GeneratedStaticAstronomyIndex(Dictionary<string, List<CampaignNebulaVisualProxy>> rows, long candidates, long crosses, int memberships)
        { byRegion = rows; CandidateRegionChecks = candidates; CrossProducts = crosses; Memberships = memberships; }

        internal static GeneratedStaticAstronomyIndex Build(IReadOnlyList<GeneratedGalaxyRegionDto> regions,
            IReadOnlyList<EnvironmentGalaxyFeatureDto> features, int width, int height)
        {
#if HOBBYDS9_CPU_PROFILE
            using var cpuProfileScope = CpuProfile.Measure("astronomy.index");
#endif

            void Require([DoesNotReturnIf(false)] bool condition) { if (!condition) throw new ArgumentException("Invalid or over-budget frozen static-astronomy geometry."); }
            Require(regions != null && features != null && width > 0 && height > 0
                && width <= ScaledGalaxyIdentity.MaximumCoordinate && height <= ScaledGalaxyIdentity.MaximumCoordinate
                && regions.Count > 0 && regions.Count <= ScaledGalaxyIdentity.MaximumRegions && features.Count <= ScaledGalaxyIdentity.MaximumFeatures);
            Require(regions!.All(r => r != null && EntityId.TryParse(r.NodeId, out _) && r.Boundary != null
                && r.Boundary.Count >= 3 && r.Boundary.Count <= ScaledGalaxyIdentity.MaximumVerticesPerRegion
                && r.Boundary.All(p => p != null && p.X >= 0 && p.X <= width && p.Y >= 0 && p.Y <= height))
                && regions.Select(r => r.NodeId).Distinct(StringComparer.Ordinal).Count() == regions.Count
                && regions.Sum(r => (long)r.Boundary.Count) <= ScaledGalaxyIdentity.MaximumTotalVertices);
            Require(features!.All(f => f != null));
            var nebulae = features.Where(f => f.Kind == GalaxyEnvironmentFeatureKind.Nebula).OrderBy(f => f.FeatureId, StringComparer.Ordinal).ToList();
            Require(nebulae.All(f => EntityId.TryParse(f.FeatureId, out _) && f.NodeId == null && f.SystemId == null && f.Position != null
                && f.Position.X >= 0 && f.Position.X <= width && f.Position.Y >= 0 && f.Position.Y <= height && f.RadiusX > 0 && f.RadiusY > 0)
                && nebulae.Select(f => f.FeatureId).Distinct(StringComparer.Ordinal).Count() == nebulae.Count);
            var boxes = regions.Select(r => (Region: r, MinX: r.Boundary.Min(p => p.X), MaxX: r.Boundary.Max(p => p.X),
                MinY: r.Boundary.Min(p => p.Y), MaxY: r.Boundary.Max(p => p.Y))).OrderBy(b => b.Region.NodeId, StringComparer.Ordinal).ToList();
            var rows = regions.ToDictionary(r => r.NodeId, _ => new List<CampaignNebulaVisualProxy>(), StringComparer.Ordinal);
            long candidates = 0, crosses = 0; int memberships = 0;
            foreach (var feature in nebulae)
            {
                bool found = false;
                var proxy = new CampaignNebulaVisualProxy(feature.FeatureId, feature.Position.X, feature.Position.Y, feature.RadiusX, feature.RadiusY);
                foreach (var box in boxes)
                {
                    candidates++;
                    var point = feature.Position;
                    if (point.X < box.MinX || point.X > box.MaxX || point.Y < box.MinY || point.Y > box.MaxY) continue;
                    bool contains = true; var boundary = box.Region.Boundary;
                    for (int i = 0; i < boundary.Count; i++)
                    {
                        crosses++;
                        if (GeneratedGalaxyGeometry.Cross(boundary[i], boundary[(i + 1) % boundary.Count], point) < 0) { contains = false; break; }
                    }
                    if (!contains) continue;
                    rows[box.Region.NodeId].Add(proxy); memberships++; found = true;
                }
                Require(found); // Valid frozen tilings contain every in-bounds centre, including all boundary ties.
            }
            Require(candidates <= (long)ScaledGalaxyIdentity.MaximumFeatures * ScaledGalaxyIdentity.MaximumRegions
                && crosses <= (long)ScaledGalaxyIdentity.MaximumFeatures * ScaledGalaxyIdentity.MaximumTotalVertices);
            return new GeneratedStaticAstronomyIndex(rows, candidates, crosses, memberships);
        }

        internal IReadOnlyList<CampaignNebulaVisualProxy> Visible(IEnumerable<string> knownNodes)
        {
            var visible = new Dictionary<string, CampaignNebulaVisualProxy>(StringComparer.Ordinal);
            foreach (string node in knownNodes.Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal))
                if (byRegion.TryGetValue(node, out var rows)) foreach (var row in rows) visible[row.FeatureId] = row;
            return visible.Values.OrderBy(n => n.FeatureId, StringComparer.Ordinal).ToList().AsReadOnly();
        }
        internal IReadOnlyList<CampaignNebulaVisualProxy> VisibleKnown(IEnumerable<NavigationKnowledgeEntryDto> knowledge)
            => Visible(knowledge.Where(n => n.Level == NavigationKnowledgeLevel.Charted || n.Level == NavigationKnowledgeLevel.Surveyed).Select(n => n.Id));
    }
}
