using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class NaturalGalaxyJson
{
    internal const int MaximumBytes = ScaledGalaxyIdentity.MaximumJsonBytes;
    internal static string Write(NaturalGalaxyReview review)
    {
        string json = JsonSerializer.Serialize(review.Export(), new JsonSerializerOptions { WriteIndented = true });
        if (Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new ArgumentException("Natural-map JSON exceeds the byte budget.");
        return json;
    }
    internal static NaturalGalaxyReview Read(string json, IEnumerable<CivilizationDefinitionDto> admitted)
    {
        if (json == null || json.Length > MaximumBytes || Encoding.UTF8.GetByteCount(json) > MaximumBytes)
            throw new ArgumentException("Natural-map JSON exceeds the review size limit.");
        try
        {
            using var parsed = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            void Object(JsonElement value, params string[] fields)
            {
                if (value.ValueKind != JsonValueKind.Object) throw new ArgumentException("Natural-map object required.");
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in value.EnumerateObject())
                    if (!names.Add(p.Name) || !fields.Contains(p.Name, StringComparer.Ordinal))
                        throw new ArgumentException("Duplicate or unsupported natural-map field: " + p.Name);
                if (names.Count != fields.Length) throw new ArgumentException("Natural-map object is missing required fields.");
            }
            IEnumerable<JsonElement> Rows(JsonElement value, int max)
            {
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > max)
                    throw new ArgumentException("Invalid or oversized natural-map array.");
                return value.EnumerateArray().ToArray();
            }
            int totalVertices = 0, totalContributions = 0;
            var root = parsed.RootElement;
            Object(root, "FormatVersion", "ProfileId", "GeneratorVersion", "TuningVersion", "ContentHash", "Settings", "Tuning", "Features", "StarGroups", "Layout", "RegionInfluences");
            var settings = root.GetProperty("Settings");
            Object(settings, "Map", "IsolatedStarSharePermille", "AssociationSpreadPermille");
            var map = settings.GetProperty("Map");
            Object(map, "Seed", "SystemCount", "EmptyRegionCount", "Width", "Height", "QuietEmptyAreaTargetPermille", "BlackHolesPerBaselineArea", "NebulaePerBaselineArea", "HomeCivilizationDefinitionIds", "StellarInfluenceEnabled", "BlackHoleInfluenceEnabled", "NebulaInfluenceEnabled");

            _ = Rows(map.GetProperty("HomeCivilizationDefinitionIds"), 2).ToList();
            Object(root.GetProperty("Tuning"), "Environment", "IsolatedStarSeparation", "MinimumAssociationSize", "MaximumAssociationSize", "MinimumAssociationRadius", "MaximumAssociationRadius", "MinimumAssociationSeparation", "MaximumAssociationSeparation", "ExtraSiteSeparation", "StellarPlacementPressurePermille", "QuietDisturbanceThreshold");
            Object(root.GetProperty("Tuning").GetProperty("Environment"), "BaseDensity", "MaximumDensity", "MinimumSiteSeparation", "MinimumStarSeparation",
                "OuterMargin", "BlackHoleCount", "NebulaCount", "CandidateDrawsPerRegion", "PlacementAttempts");
            foreach (var f in Rows(root.GetProperty("Features"), ScaledGalaxyIdentity.MaximumFeatures))
            {
                Object(f, "FeatureId", "Kind", "DisplayName", "Position", "RadiusX", "RadiusY", "DensityWeight", "SystemId", "NodeId");
                Object(f.GetProperty("Position"), "X", "Y");
            }
            int totalMembers = 0;
            foreach (var g in Rows(root.GetProperty("StarGroups"), ScaledGalaxyIdentity.MaximumSystems)) {
                Object(g, "GroupId", "Kind", "Center", "RadiusX", "RadiusY", "StarFeatureIds");
                Object(g.GetProperty("Center"), "X", "Y");
                foreach (var member in Rows(g.GetProperty("StarFeatureIds"), 9)) {
                    if (++totalMembers > ScaledGalaxyIdentity.MaximumSystems || member.ValueKind != JsonValueKind.String)
                        throw new ArgumentException("Natural membership budget/type invalid.");
                }
            }
            var layout = root.GetProperty("Layout");
            Object(layout, "Width", "Height", "Regions", "Systems", "Connections", "HomeCandidates");
            foreach (var r in Rows(layout.GetProperty("Regions"), ScaledGalaxyIdentity.MaximumRegions))
            {
                Object(r, "NodeId", "DisplayName", "SystemId", "Center", "Boundary");
                Object(r.GetProperty("Center"), "X", "Y");
                foreach (var p in Rows(r.GetProperty("Boundary"), ScaledGalaxyIdentity.MaximumVerticesPerRegion)) {
                    if (++totalVertices > ScaledGalaxyIdentity.MaximumTotalVertices) throw new ArgumentException("Scaled vertex budget exceeded.");
                    Object(p, "X", "Y");
                }
            }
            foreach (var s in Rows(layout.GetProperty("Systems"), ScaledGalaxyIdentity.MaximumSystems)) Object(s, "SystemId", "DisplayName", "NodeId");
            foreach (var c in Rows(layout.GetProperty("Connections"), ScaledGalaxyIdentity.MaximumConnections)) Object(c, "ConnectionId", "EndpointA", "EndpointB", "Kind", "BaseTraversalCost");
            foreach (var h in Rows(layout.GetProperty("HomeCandidates"), 2)) Object(h, "CivilizationDefinitionId", "SystemId", "NodeId");
            foreach (var r in Rows(root.GetProperty("RegionInfluences"), ScaledGalaxyIdentity.MaximumRegions))
            {
                Object(r, "NodeId", "BaseDensity", "TargetDensity", "Contributions");
                foreach (var c in Rows(r.GetProperty("Contributions"), ScaledGalaxyIdentity.MaximumFeatures)) {
                    if (++totalContributions > ScaledGalaxyIdentity.MaximumTotalContributions) throw new ArgumentException("Scaled contribution budget exceeded.");
                    Object(c, "FeatureId", "Amount");
                }
            }
            return NaturalGalaxyApplication.Open(JsonSerializer.Deserialize<NaturalGalaxyDocumentDto>(json)!, admitted);
        }
        catch (JsonException e) { throw new ArgumentException("Invalid natural-map JSON.", e); }
    }
}
