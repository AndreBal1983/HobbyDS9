using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

// Reference host codec. Portable authority owns validation; hosts own bounded JSON and disk I/O.
internal static class GeneratedGalaxyJson
{
    internal const int MaximumBytes = 2 * 1024 * 1024;
    internal static string Write(GeneratedGalaxyReview review) => JsonSerializer.Serialize(review.Export(), new JsonSerializerOptions { WriteIndented = true });
    internal static GeneratedGalaxyReview Read(string json, IEnumerable<CivilizationDefinitionDto> admitted)
    {
        if (json == null || json.Length > MaximumBytes || Encoding.UTF8.GetByteCount(json) > MaximumBytes)
            throw new ArgumentException("Generated-map JSON exceeds the review size limit.");
        try
        {
            using var parsed = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            void Object(JsonElement value, params string[] fields)
            {
                if (value.ValueKind != JsonValueKind.Object) throw new ArgumentException("Generated-map object required.");
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in value.EnumerateObject())
                    if (!names.Add(p.Name) || !fields.Contains(p.Name, StringComparer.Ordinal))
                        throw new ArgumentException("Duplicate or unsupported generated-map field: " + p.Name);
                if (names.Count != fields.Length) throw new ArgumentException("Generated-map object is missing required fields.");
            }
            IEnumerable<JsonElement> Rows(JsonElement value, int max)
            {
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > max)
                    throw new ArgumentException("Invalid or oversized generated-map array.");
                return value.EnumerateArray().ToArray();
            }
            var root = parsed.RootElement;
            Object(root, "FormatVersion", "ProfileId", "GeneratorVersion", "TuningVersion", "ContentHash", "Settings",
                "Width", "Height", "Regions", "Systems", "Connections", "HomeCandidates");
            var settings = root.GetProperty("Settings");
            Object(settings, "Seed", "SystemCount", "HomeCivilizationDefinitionIds");
            _ = Rows(settings.GetProperty("HomeCivilizationDefinitionIds"), 2).ToList();
            foreach (var r in Rows(root.GetProperty("Regions"), 100))
            {
                Object(r, "NodeId", "DisplayName", "SystemId", "Center", "Boundary");
                Object(r.GetProperty("Center"), "X", "Y");
                foreach (var p in Rows(r.GetProperty("Boundary"), 104)) Object(p, "X", "Y");
            }
            foreach (var s in Rows(root.GetProperty("Systems"), 50)) Object(s, "SystemId", "DisplayName", "NodeId");
            foreach (var c in Rows(root.GetProperty("Connections"), 300)) Object(c, "ConnectionId", "EndpointA", "EndpointB", "Kind", "BaseTraversalCost");
            foreach (var h in Rows(root.GetProperty("HomeCandidates"), 2)) Object(h, "CivilizationDefinitionId", "SystemId", "NodeId");
            return GeneratedGalaxyApplication.Open(JsonSerializer.Deserialize<GeneratedGalaxyDocumentDto>(json)!, admitted);
        }
        catch (JsonException e) { throw new ArgumentException("Invalid generated-map JSON.", e); }
    }
}
