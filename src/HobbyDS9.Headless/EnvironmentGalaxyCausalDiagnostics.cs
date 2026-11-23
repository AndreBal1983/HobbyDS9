using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class EnvironmentGalaxyCausalDiagnostics
{
    internal static string Run(IEnumerable<CivilizationDefinitionDto> catalog)
    {
        var rows = new List<object>();
        var totals = new Dictionary<GalaxyEnvironmentFeatureKind, (int On, int Off)>();
        foreach (var seed in Enumerable.Range(0, 16).Append(42))
        {
            EnvironmentGalaxySettingsDto Settings() => new() { Map = new() { Seed = seed.ToString(CultureInfo.InvariantCulture), SystemCount = 40,
                HomeCivilizationDefinitionIds = new() { "civilization.federation", "civilization.cardassian" } },
                StellarInfluenceEnabled = true, BlackHoleInfluenceEnabled = true, NebulaInfluenceEnabled = true };
            var on = EnvironmentGalaxyApplication.Generate(Settings(), catalog).Export();
            foreach (var kind in new[] { GalaxyEnvironmentFeatureKind.Star, GalaxyEnvironmentFeatureKind.BlackHole, GalaxyEnvironmentFeatureKind.Nebula })
            {
                var settings = Settings();
                if (kind == GalaxyEnvironmentFeatureKind.Star) settings.StellarInfluenceEnabled = false;
                else if (kind == GalaxyEnvironmentFeatureKind.BlackHole) settings.BlackHoleInfluenceEnabled = false;
                else settings.NebulaInfluenceEnabled = false;
                var off = EnvironmentGalaxyApplication.Generate(settings, catalog).Export();
                if (JsonSerializer.Serialize(on.Features) != JsonSerializer.Serialize(off.Features)
                    || JsonSerializer.Serialize(on.Tuning) != JsonSerializer.Serialize(off.Tuning))
                    throw new InvalidOperationException("Controlled comparison changed unrelated feature inputs.");
                var family = on.Features.Where(f => f.Kind == kind).ToList();
                var anchors = on.Features.Where(f => f.NodeId != null).Select(f => f.NodeId).ToHashSet();
                bool Core(EnvironmentGalaxyFeatureDto f, GeneratedGalaxyPointDto p)
                {
                    long dx = p.X - f.Position.X, dy = p.Y - f.Position.Y;
                    if (Math.Abs(dx) >= f.RadiusX || Math.Abs(dy) >= f.RadiusY) return false;
                    long q = dx * dx * 1000000 / ((long)f.RadiusX * f.RadiusX) + dy * dy * 1000000 / ((long)f.RadiusY * f.RadiusY);
                    return q < 1000000 && f.DensityWeight * (1000000 - q) / 1000000 * 2 >= f.DensityWeight;
                }
                int Local(EnvironmentGalaxyDocumentDto d) => d.Layout.Regions.Count(r => !anchors.Contains(r.NodeId) && family.Any(f => Core(f, r.Center)));
                int enabled = Local(on), disabled = Local(off);
                totals.TryGetValue(kind, out var total); totals[kind] = (total.On + enabled, total.Off + disabled);
                rows.Add(new { Seed = seed, Family = kind.ToString(), EnabledLocalExtraSites = enabled, DisabledLocalExtraSites = disabled,
                    AllFeaturesAndTuningIdentical = true, EnabledHash = on.ContentHash, DisabledHash = off.ContentHash });
            }
        }
        if (totals.Values.Any(t => t.On <= t.Off)) throw new InvalidOperationException("Declared local density direction failed over controlled seed set.");
        return JsonSerializer.Serialize(new {
            GeneratorVersion = EnvironmentGalaxyIdentity.GeneratorVersion, TuningVersion = EnvironmentGalaxyIdentity.TuningVersion,
            Measurement = "Extra sites in the union of a family's >= half normalized-weight elliptical cores, excluding mandatory star/black-hole anchors.",
            Caveat = "Exact same feature positions/parameters and other inputs in each pair; fixed region budget redistributes sites. This is an aggregate directional check, not a guaranteed per-seed increase, physical law, inverse-area formula or unique polygon attribution.",
            SystemCount = 40, Seeds = Enumerable.Range(0, 16).Append(42),
            Totals = totals.OrderBy(p => p.Key).Select(p => new { Family = p.Key.ToString(), EnabledLocalExtraSites = p.Value.On, DisabledLocalExtraSites = p.Value.Off }),
            Comparisons = rows }, new JsonSerializerOptions { WriteIndented = true });
    }
}
