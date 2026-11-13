using System.Text;
using HobbyDS9.Application;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ContentByteCompatibilityTests
{
    [Fact]
    public void HistoricalCompositionBytesKeepAuthoringPackageIdentityAndRejectReencoding()
    {
        byte[] Read(string folder, string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, folder, name + ".json"));
        var codec = new ReviewNewtonsoftCodec();
        var template = PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory, "scenarios/playable-fleet-v1"));
        var activities = ActivitiesPlaytestOptions.CreatePackage(template,
            new[] { "catalogue", "ready-classes", "support", "activities" }.Select(n => Read("scenarios/activities-v1", n)).ToArray(), codec);
        var names = new[] { "catalogue", "ready-classes", "support", "settlement" };
        var assets = names.Select(n => Read("scenarios/colonisation-v1", n)).ToArray();
        var authoring = names.Select(n => Read("authoring/colonisation-v1", n)).ToArray();
        for (var i = 0; i < names.Length; i++)
        {
            var source = Encoding.UTF8.GetString(authoring[i]);
            Assert.DoesNotContain("\r", source);
            Assert.Contains("\n", source);
            Assert.Equal(source.Replace("\n", "\r\n"), Encoding.UTF8.GetString(assets[i]));
        }
        var historical = ColonisationPlaytestOptions.CreatePackage(activities, assets, codec);
        var authored = CampaignColonisationApplication.CreatePackage(activities,
            codec.DecodeColonisationCatalogue(authoring[0]), codec.DecodeColonisationReadyClasses(authoring[1]),
            codec.DecodeColonisationSupport(authoring[2]), codec.DecodeColonisationPolicy(authoring[3]));
        Assert.Equal(authored.PackageHash, historical.PackageHash);
        for (var i = 0; i < names.Length; i++)
        {
            var changed = assets.Select(b => b.ToArray()).ToArray();
            changed[i] = authoring[i];
            Assert.Throws<ArgumentException>(() => ColonisationPlaytestOptions.CreatePackage(activities, changed, codec));
            changed[i] = assets[i].Concat(new byte[] { 32 }).ToArray();
            Assert.Throws<ArgumentException>(() => ColonisationPlaytestOptions.CreatePackage(activities, changed, codec));
        }
    }
}
