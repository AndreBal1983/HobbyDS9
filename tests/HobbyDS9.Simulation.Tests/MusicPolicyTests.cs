using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class MusicPolicyTests
{
    private static MusicTrack Track(string id, int order = 0, MusicCategory category = MusicCategory.Strategic, double tail = 0) => new(id, id, category, MusicLoopPolicy.FullCue, order, 6, tail);
    private static MusicPlaylist Playlist() => new(new[] { Track("deep-station", 0, tail: 1.65), Track("deep-station-1", 1, tail: .45) });
    private sealed class Store : IMusicSettingsStore
    {
        public Dictionary<string, float> Values { get; } = new();
        public int Flushes { get; private set; }
        public float Read(string key, float fallback) => Values.TryGetValue(key, out var value) ? value : fallback;
        public void Write(string key, float value) => Values[key] = value;
        public void Flush() => Flushes++;
    }
    [Fact]
    public void SequentialRotationAvoidsImmediateRepeatAndUsesStableOrder()
    {
        var playlist = Playlist();
        Assert.Equal(new[] { "deep-station", "deep-station-1", "deep-station", "deep-station-1" }, Enumerable.Range(0, 4).Select(_ => playlist.Next(_ => true)!.Id));
        var reversed = new MusicPlaylist(new[] { Track("b", 1), Track("a", 0) }.Reverse());
        Assert.Equal("a", reversed.Next(_ => true)!.Id); Assert.Equal("b", reversed.Next(_ => true)!.Id);
    }
    [Fact]
    public void MissingBrokenTracksFallBackToAvailableCueOrSilence()
    {
        var playlist = Playlist();
        Assert.Equal("deep-station-1", playlist.Next(t => t.Id != "deep-station")!.Id);
        Assert.Equal("deep-station-1", playlist.Next(t => t.Id != "deep-station")!.Id); // only one playable cue
        Assert.Null(playlist.Next(_ => false));
        Assert.Equal("deep-station", playlist.Next(_ => true)!.Id);
        Assert.Null(new MusicPlaylist(Array.Empty<MusicTrack>()).Next(_ => true));
    }
    [Fact]
    public void SameOrEmptyContextDoesNotRestartStrategicPlaylist()
    {
        var playlist = Playlist(); playlist.Next(_ => true);
        Assert.False(playlist.SetContext(MusicCategory.Strategic, _ => true));
        Assert.False(playlist.SetContext(MusicCategory.Combat, _ => true));
        Assert.False(playlist.SetContext(MusicCategory.Menu, _ => true));
        Assert.Equal("deep-station", playlist.SelectedTrackId);
        Assert.Equal("deep-station-1", playlist.Next(_ => true)!.Id);
    }
    [Fact]
    public void FutureNonemptyCategoryChangesOnlyPresentationAndFallsBackIfAssetsFail()
    {
        var playlist = new MusicPlaylist(new[] { Track("strategic"), Track("combat", category: MusicCategory.Combat) });
        Assert.True(playlist.SetContext(MusicCategory.Combat, _ => true));
        Assert.Equal("combat", playlist.Next(_ => true)!.Id);
        Assert.False(playlist.SetContext(MusicCategory.Combat, _ => true));
        Assert.Equal("strategic", playlist.Next(t => t.Id != "combat")!.Id);
        Assert.Equal(MusicCategory.Strategic, playlist.Category);
    }
    [Fact]
    public void CatalogValidationRejectsDuplicatesBadIdsEnumsAndTiming()
    {
        Assert.Empty(MusicCatalogPolicy.Validate(new[] { Track("a"), Track("b") }));
        Assert.Single(MusicCatalogPolicy.Validate(new[] { Track("a"), Track("a") }));
        Assert.False(MusicCatalogPolicy.Valid(Track(" ")));
        Assert.False(MusicCatalogPolicy.Valid(Track("a", category: (MusicCategory)99)));
        Assert.False(MusicCatalogPolicy.Valid(Track("a", tail: -1)));
        Assert.False(MusicCatalogPolicy.Valid(Track("a", tail: double.NaN)));
        Assert.False(MusicCatalogPolicy.Valid(new("a", "a", MusicCategory.Strategic, MusicLoopPolicy.FullCue, crossfadeSeconds: double.PositiveInfinity)));
        Assert.False(MusicCatalogPolicy.Valid(new("a", "a", MusicCategory.Strategic, MusicLoopPolicy.FullCue, crossfadeSeconds: 0)));
        var filtered = new MusicPlaylist(new[] { Track(" "), Track("a"), Track("a") });
        Assert.Equal("a", filtered.Next(_ => true)!.Id);
    }
    [Theory]
    [InlineData(189.648, 1.65, 281.998)]
    [InlineData(201.888, .45, 295.438)]
    public void FullCuesCrossfadeNearNaturalEndBeforeQuietTail(double duration, double tail, double nextStart)
    {
        var timing = new MusicCueTiming(100, duration, Track("cue", tail: tail));
        Assert.Equal(nextStart, timing.NextStartDsp, 6);
        Assert.Equal(6, timing.CrossfadeSeconds);
        Assert.True(timing.NextStartDsp > 100 + duration * .95);
        Assert.Equal(100 + duration - tail, timing.NextStartDsp + timing.CrossfadeSeconds, 6);
    }
    [Fact]
    public void ShortCuesClampOverlapAndOnlyExplicitSeamlessAssetsCanLoop()
    {
        var timing = new MusicCueTiming(0, 2, Track("short", tail: 3));
        Assert.Equal(1, timing.NextStartDsp); Assert.Equal(.5, timing.CrossfadeSeconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MusicCueTiming(0, 0, Track("bad")));
        var loop = new MusicCueTiming(0, 2, new("loop", "Loop", MusicCategory.Strategic, MusicLoopPolicy.SeamlessLoop));
        Assert.True(double.IsPositiveInfinity(loop.NextStartDsp));
    }
    [Theory]
    [InlineData(0)] [InlineData(.25)] [InlineData(.5)] [InlineData(.75)] [InlineData(1)]
    public void SmoothEqualPowerCrossfadeHasBoundedComplementaryGains(double progress)
    {
        var incoming = MusicEnvelope.Incoming(progress); var outgoing = MusicEnvelope.Outgoing(progress);
        Assert.InRange(incoming, 0, 1); Assert.InRange(outgoing, 0, 1);
        Assert.Equal(1, incoming * incoming + outgoing * outgoing, 6);
        Assert.Equal(0, MusicEnvelope.Progress(5, 10, 6)); Assert.Equal(1, MusicEnvelope.Progress(20, 10, 6));
        Assert.Equal(1, MusicEnvelope.Progress(10, 10, 0));
    }
    [Fact]
    public void PreferencesPersistClampAndMuteWithoutChangingSelectedTrack()
    {
        var store = new Store(); var preferences = new MusicPreferences(store); var playlist = Playlist();
        playlist.Next(_ => true); var selected = playlist.SelectedTrackId;
        preferences.SetMaster(.8f); preferences.SetMusic(.25f); preferences.SetMuted(true);
        var reloaded = new MusicPreferences(store);
        Assert.Equal(.8f, reloaded.MasterVolume); Assert.Equal(.25f, reloaded.MusicVolume); Assert.True(reloaded.Muted); Assert.Equal(0, reloaded.Gain);
        reloaded.SetMuted(false); Assert.Equal(.2f, reloaded.Gain, 6);
        reloaded.SetMaster(2); reloaded.SetMusic(-1); Assert.Equal(1, reloaded.MasterVolume); Assert.Equal(0, reloaded.MusicVolume);
        reloaded.SetMusic(float.NaN); reloaded.SetMaster(float.PositiveInfinity); Assert.Equal(0, reloaded.Gain);
        Assert.Equal(selected, playlist.SelectedTrackId); Assert.True(store.Flushes >= 8);
        Assert.All(store.Values.Keys, k => Assert.StartsWith("hobbyds9.audio.v1.", k));
    }
    [Fact]
    public void MusicSettingsSelectionAndContextLeaveEntireCampaignSaveHashUnchanged()
    {
        var scenario = VerticalSliceFixture.Scenario(false, true); var app = CampaignApplication.StartScenario(scenario);
        var before = JsonSerializer.SerializeToUtf8Bytes(app.ExportSave()); var snapshot = app.CaptureSnapshot();
        var playlist = Playlist(); var preferences = new MusicPreferences(new Store());
        for (var i = 0; i < 100; i++)
        {
            playlist.SetContext(i % 2 == 0 ? MusicCategory.Strategic : MusicCategory.Combat, _ => true);
            playlist.Next(_ => true); preferences.SetMusic(i / 100f); preferences.SetMuted(i % 3 == 0);
        }
        var after = JsonSerializer.SerializeToUtf8Bytes(app.ExportSave());
        Assert.Equal(before, after);
        Assert.Equal(System.Security.Cryptography.SHA256.HashData(before), System.Security.Cryptography.SHA256.HashData(after));
        Assert.Equal(snapshot.Campaign.RngState.State, app.CaptureSnapshot().Campaign.RngState.State);
        Assert.Equal(snapshot.Campaign.NextEntitySequence, app.CaptureSnapshot().Campaign.NextEntitySequence);
    }
}
