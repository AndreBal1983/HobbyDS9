using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignRandomTests
{
    [Fact]
    public void KnownVectorsIncludeZeroScenarioSeedAndUnsignedOverflow()
    {
        // Fixed constants cross-checked with a separate arbitrary-precision
        // implementation of the specified unsigned arithmetic, not this class.
        var fixtures = new[]
        {
            (0UL, new[] { 0xe220a8397b1dcdafUL, 0x6e789e6aa1b965f4UL, 0x06c45d188009454fUL, 0xf88bb8a8724c81ecUL, 0x1b39896a51a8749bUL }),
            (47UL, new[] { 0x7bb3c45c597cdb85UL, 0x4a0fc32026e8cb6bUL, 0x4a3647b64b7378a8UL, 0x327021c076e5cbeeUL, 0x4d1dfc0a4632380eUL }),
            (ulong.MaxValue, new[] { 0xe4d971771b652c20UL, 0xe99ff867dbf682c9UL, 0x382ff84cb27281e9UL, 0x6d1db36ccba982d2UL, 0xb4a0472e578069aeUL })
        };
        foreach (var (seed, values) in fixtures)
        {
            var random = new SplitMix64Random(seed);
            Assert.Equal(seed, random.State); // Seeding consumes no draw.
            foreach (var value in values) Assert.Equal(value, random.NextUInt64());
        }
        var wrapping = new SplitMix64Random(ulong.MaxValue);
        wrapping.NextUInt64();
        Assert.Equal(0x9e3779b97f4a7c14UL, wrapping.State);
    }

    [Fact]
    public void BoundedDrawRejectsLowValuesAndZeroDoesNotAdvanceState()
    {
        var random = new SplitMix64Random(0);
        random.NextUInt64();
        Assert.Equal(0x788bb8a8724c81ebUL, random.NextBelow(0x8000000000000001UL));
        Assert.Equal(0x78dde6e5fd29f054UL, random.State);
        var before = random.State;
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextBelow(0));
        Assert.Equal(before, random.State);
        Assert.Equal(0UL, random.NextBelow(1));
        foreach (var bound in new[] { 2UL, 3UL, 50UL, ulong.MaxValue })
            for (var i = 0; i < 100; i++) Assert.True(random.NextBelow(bound) < bound);
    }

    [Fact]
    public void SerializedReloadMatchesNextRawAndBoundedDrawsExactly()
    {
        var uninterrupted = new SplitMix64Random(47);
        for (var i = 0; i < 17; i++) uninterrupted.NextBelow(100);
        var dto = JsonSerializer.Deserialize<RngStateDto>(JsonSerializer.Serialize(uninterrupted.ToDto()))!;
        var resumed = SplitMix64Random.FromDto(dto);
        dto.State = 0;
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(uninterrupted.NextUInt64(), resumed.NextUInt64());
            Assert.Equal(uninterrupted.NextBelow(0x8000000000000001UL), resumed.NextBelow(0x8000000000000001UL));
            Assert.Equal(uninterrupted.State, resumed.State);
        }
    }

    [Fact]
    public void RngRestoreRejectsUnknownMetadataAndMissingStateButAcceptsZero()
    {
        var dto = new SplitMix64Random(0).ToDto();
        Assert.Equal(0UL, SplitMix64Random.FromDto(dto).State);
        dto.AlgorithmId = "system-random";
        Assert.Throws<ArgumentException>(() => SplitMix64Random.FromDto(dto));
        dto = new SplitMix64Random(0).ToDto();
        dto.AlgorithmVersion++;
        Assert.Throws<ArgumentException>(() => SplitMix64Random.FromDto(dto));
        dto = new SplitMix64Random(0).ToDto();
        dto.State = null;
        Assert.Throws<ArgumentException>(() => SplitMix64Random.FromDto(dto));
        Assert.Throws<ArgumentException>(() => SplitMix64Random.FromDto(null));
    }
}
