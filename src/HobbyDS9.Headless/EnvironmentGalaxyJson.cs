using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class EnvironmentGalaxyJson
{
    internal const int MaximumBytes = 2 * 1024 * 1024;
    internal static string Write(EnvironmentGalaxyReview review) => JsonSerializer.Serialize(review.Export(), new JsonSerializerOptions { WriteIndented = true });
    internal static EnvironmentGalaxyReview Read(string json, IEnumerable<CivilizationDefinitionDto> admitted)
    {
        if (json == null || json.Length > MaximumBytes || Encoding.UTF8.GetByteCount(json) > MaximumBytes)
            throw new ArgumentException("Environment-map JSON exceeds the review size limit.");
        try
        {
            using var parsed = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            void Object(JsonElement value, params string[] fields)
            {
                if (value.ValueKind != JsonValueKind.Object) throw new ArgumentException("Environment-map object required.");
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in value.EnumerateObject())
                    if (!names.Add(p.Name) || !fields.Contains(p.Name, StringComparer.Ordinal))
                        throw new ArgumentException("Duplicate or unsupported environment-map field: " + p.Name);
                if (names.Count != fields.Length) throw new ArgumentException("Environment-map object is missing required fields.");
            }
            IEnumerable<JsonElement> Rows(JsonElement value, int max)
            {
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > max)
                    throw new ArgumentException("Invalid or oversized environment-map array.");
                return value.EnumerateArray().ToArray();
            }
            var root = parsed.RootElement;
            Object(root, "FormatVersion", "ProfileId", "GeneratorVersion", "TuningVersion", "ContentHash", "Settings", "Tuning", "Features", "Layout", "RegionInfluences");
            var settings = root.GetProperty("Settings");
            Object(settings, "Map", "StellarInfluenceEnabled", "BlackHoleInfluenceEnabled", "NebulaInfluenceEnabled");
            var map = settings.GetProperty("Map");
            Object(map, "Seed", "SystemCount", "HomeCivilizationDefinitionIds");
            _ = Rows(map.GetProperty("HomeCivilizationDefinitionIds"), 2).ToList();
            Object(root.GetProperty("Tuning"), "BaseDensity", "MaximumDensity", "MinimumSiteSeparation", "MinimumStarSeparation",
                "OuterMargin", "BlackHoleCount", "NebulaCount", "CandidateDrawsPerRegion", "PlacementAttempts");
            foreach (var f in Rows(root.GetProperty("Features"), 55))
            {
                Object(f, "FeatureId", "Kind", "DisplayName", "Position", "RadiusX", "RadiusY", "DensityWeight", "SystemId", "NodeId");
                Object(f.GetProperty("Position"), "X", "Y");
            }
            var layout = root.GetProperty("Layout");
            Object(layout, "Width", "Height", "Regions", "Systems", "Connections", "HomeCandidates");
            foreach (var r in Rows(layout.GetProperty("Regions"), 100))
            {
                Object(r, "NodeId", "DisplayName", "SystemId", "Center", "Boundary");
                Object(r.GetProperty("Center"), "X", "Y");
                foreach (var p in Rows(r.GetProperty("Boundary"), 104)) Object(p, "X", "Y");
            }
            foreach (var s in Rows(layout.GetProperty("Systems"), 50)) Object(s, "SystemId", "DisplayName", "NodeId");
            foreach (var c in Rows(layout.GetProperty("Connections"), 300)) Object(c, "ConnectionId", "EndpointA", "EndpointB", "Kind", "BaseTraversalCost");
            foreach (var h in Rows(layout.GetProperty("HomeCandidates"), 2)) Object(h, "CivilizationDefinitionId", "SystemId", "NodeId");
            foreach (var r in Rows(root.GetProperty("RegionInfluences"), 100))
            {
                Object(r, "NodeId", "BaseDensity", "TargetDensity", "Contributions");
                foreach (var c in Rows(r.GetProperty("Contributions"), 55)) Object(c, "FeatureId", "Amount");
            }
            return EnvironmentGalaxyApplication.Open(JsonSerializer.Deserialize<EnvironmentGalaxyDocumentDto>(json)!, admitted);
        }
        catch (JsonException e) { throw new ArgumentException("Invalid environment-map JSON.", e); }
    }
}
