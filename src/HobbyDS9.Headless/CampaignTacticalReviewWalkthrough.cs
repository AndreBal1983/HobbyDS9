using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class CampaignTacticalReviewWalkthrough
{
    internal static readonly JsonSerializerOptions Options = CreateOptions();
    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(t => {
            if (t.Type.Assembly != typeof(GameStateDto).Assembly) return;
            foreach (var property in t.Properties)
                if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null) property.IsRequired = true;
        });
        return new JsonSerializerOptions { TypeInfoResolver = resolver, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    internal static CampaignTacticalReviewPackageDto ReadPackage(string profilePath)
    {
        var profile = File.ReadAllBytes(profilePath);
        var source = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(profilePath))!, "scenario.json"));
        var p = JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(profile, Options) ?? throw new InvalidDataException("Missing review profile.");
        p.Campaign = JsonSerializer.Deserialize<ScenarioDto>(source, Options) ?? throw new InvalidDataException("Missing authored source.");
        p.Campaign.ContentHash = Hash(source);
        p.PackageHash = Hash(Encoding.ASCII.GetBytes("campaign-tactical-review-package-v1\n" + Hash(profile) + "\n" + Hash(source) + "\n"));
        return p;
    }
    private static ICampaignTacticalReviewSession Reload(CampaignTacticalReviewPackageDto p, ICampaignTacticalReviewSession app) =>
        CampaignTacticalReviewApplication.LoadReviewSave(p, JsonSerializer.Deserialize<CampaignTacticalReviewSaveDto>(JsonSerializer.Serialize(app.ExportSave()), Options)!);
    private static CommitCampaignTacticalRoundDto Command(ICampaignTacticalReviewSession app, string id, string actor)
    {
        var view = app.Observe(id, actor);
        var maneuver = view.Round == 2 ? CampaignFleetManeuver.Flank : view.Round == 3 ? CampaignFleetManeuver.Screen : CampaignFleetManeuver.Hold;
        return new CommitCampaignTacticalRoundDto { ActorId = actor, EncounterId = id, CommandId = "command.walkthrough." + actor + "." + id + "." + view.Round,
            ExpectedTurn = view.Turn, ExpectedRound = view.Round, ExpectedBoundaryRevision = view.BoundaryRevision,
            Replacements = view.StandingOrders.Where(o => view.OwnShips.Any(s => s.Condition.FleetId == o.FleetId && s.Condition.Hull > 0)).Select(o =>
                new CampaignTacticalFleetOrderDto { FleetId = o.FleetId, Maneuver = maneuver, FocusFleetId = o.FocusFleetId }).ToList() };
    }
    internal static void Run(string profilePath)
    {
        var p = ReadPackage(profilePath);
        Console.WriteLine("AUTHORITATIVE REVIEW LAYER: real Scenario/Simulation/Application; production activation=False; physical travel integration pending.");
        Console.WriteLine("review-package-sha256=" + p.PackageHash);
        var vectors = new List<string>();
        foreach (var variant in new[] { "canonical", "reverse", "repeated", "reload" })
        {
            ICampaignTacticalReviewSession app = CampaignTacticalReviewApplication.StartReviewPackage(p);
            var initial = app.CaptureStatus();
            Require(app.SetApproachCloak("civ.federation", "command.walkthrough.cloak", "fleet.federation-scout", true, initial.Turn, initial.CampaignRevision).Accepted);
            for (var battle = 1; battle <= 2; battle++)
            {
                var before = app.CaptureStatus(); Require(app.AdvanceTurn(before.Turn, before.CampaignRevision).Accepted);
                var id = app.GetEncounters("civ.federation").Single();
                var view = app.Observe(id,"civ.federation");
                if (variant == "canonical") Console.WriteLine("campaign encounter=" + id + " turn=" + view.Turn + " round=" + view.Round + " own-pools=" + Pools(view));
                while (!app.Observe(id,"civ.federation").Complete)
                {
                    var a = Command(app,id,"civ.federation");var b = Command(app,id,"civ.cardassian");
                    bool reverse = variant == "reverse";
                    Require(app.CommitRound(reverse ? b : a).Accepted);
                    if (variant == "reload") app = Reload(p,app); // genuine waiting commitments/resources/ledger.
                    Require(app.CommitRound(reverse ? a : b).Accepted);
                    if (variant == "reload") app = Reload(p,app);
                    if (variant == "canonical") Console.WriteLine("campaign resolved-round=" + a.ExpectedRound + " next=" + app.Observe(id,"civ.federation").Round
                        + " own-pools=" + Pools(app.Observe(id,"civ.federation")));
                }
                var ready=app.CaptureStatus();Require(ready.CanFinalizeBatch);Require(app.FinalizeBatch(ready.Turn,ready.CampaignRevision).Accepted);
                app=Reload(p,app);
                if (variant == "canonical") Console.WriteLine("campaign finalized turn=" + app.CaptureStatus().Turn + " phase=" + app.CaptureStatus().Phase
                    + " own-retained=" + string.Join(";",app.InspectOwnShips("civ.federation").Select(s=>s.Condition.ShipId + ":hull=" + s.Condition.Hull
                        + ":ammo=" + string.Join(",",s.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo).Select(w=>w.Ammunition)))));
            }
            var vector=Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(app.ExportSave())));
            vectors.Add(vector);Console.WriteLine("authoritative-review " + variant + " save-sha256=" + vector);
        }
        Require(vectors.Distinct(StringComparer.Ordinal).Count()==1);
        Console.WriteLine("Actual campaign two-battle/current-pool/commitment/round/save/finalization equality passed; no prototype checkpoint imported.");
        Console.WriteLine("Gate remains closed: equipment/policy acceptance and physical shared travel/contact/intent integration.");
    }
    private static string Pools(CampaignTacticalObservation view) => string.Join(";",view.OwnShips.Select(s=>s.Condition.ShipId+":hull="+s.Condition.Hull
        +":shields="+string.Join(",",s.Condition.Shields)+":ammo="+string.Join(",",s.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo).Select(w=>w.Ammunition))));
    private static void Require(bool value) { if(!value) throw new InvalidOperationException("Authoritative review walkthrough failed."); }
}
