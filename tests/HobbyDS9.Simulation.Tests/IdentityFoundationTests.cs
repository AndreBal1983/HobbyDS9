using System.Globalization;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class IdentityFoundationTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("entity.alpha")]
    [InlineData("definition.alpha-1_b")]
    public void CanonicalIdsRoundTripWithoutNormalization(string value)
    {
        Assert.True(EntityId.TryParse(value, out var entity));
        Assert.True(DefinitionId.TryParse(value, out var definition));
        Assert.Equal(value, entity.Value);
        Assert.Equal(value, definition.Value);
        Assert.Equal(new EntityId(value), entity);
        Assert.Equal(new DefinitionId(value), definition);
        Assert.False(entity.Equals((object)definition));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" Alpha")]
    [InlineData("alpha ")]
    [InlineData("Alpha")]
    [InlineData("1alpha")]
    [InlineData(".alpha")]
    [InlineData("alpha/beta")]
    [InlineData("alpha\nbeta")]
    [InlineData("álpha")]
    [InlineData("alpha\u200b")]
    public void NoncanonicalIdsAreRejected(string? value)
    {
        Assert.False(EntityId.TryParse(value, out var entity));
        Assert.False(DefinitionId.TryParse(value, out var definition));
        Assert.False(entity.IsValid);
        Assert.False(definition.IsValid);
        Assert.Throws<ArgumentException>(() => new EntityId(value!));
        Assert.Throws<ArgumentException>(() => new DefinitionId(value!));
    }

    [Fact]
    public void IdLengthLimitsAndDefaultValuesAreExplicit()
    {
        Assert.True(new EntityId(new string('a', 128)).IsValid);
        Assert.True(new DefinitionId(new string('a', 128)).IsValid);
        Assert.False(EntityId.TryParse(new string('a', 129), out _));
        Assert.False(DefinitionId.TryParse(new string('a', 129), out _));
        Assert.False(default(EntityId).IsValid);
        Assert.False(default(DefinitionId).IsValid);
        Assert.Throws<InvalidOperationException>(() => default(EntityId).Value);
        Assert.Throws<InvalidOperationException>(() => default(DefinitionId).Value);
        Assert.Throws<InvalidOperationException>(() => default(EntityId).CompareTo(new EntityId("a")));
        Assert.Throws<InvalidOperationException>(() => new DefinitionId("a").CompareTo(default));
    }

    [Fact]
    public void EqualityOrderingAndHashingAreOrdinalAcrossCultures()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "tr-TR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var entities = new[] { "entity.z", "entity.i", "entity._", "entity.a" }.Select(v => new EntityId(v)).ToArray();
                Array.Sort(entities);
                Assert.Equal(new[] { "entity._", "entity.a", "entity.i", "entity.z" }, entities.Select(e => e.Value));
                var definitions = new[] { new DefinitionId("definition.z"), new DefinitionId("definition.a") };
                Array.Sort(definitions);
                Assert.Equal("definition.a", definitions[0].Value);
                Assert.Equal(121836416, new EntityId("entity.alpha").GetHashCode());
                Assert.Equal(-1908775373, new DefinitionId("design.scout").GetHashCode());
                Assert.True(new EntityId("a") == new EntityId("a"));
                Assert.True(new DefinitionId("a") != new DefinitionId("b"));
                Assert.Contains(new EntityId("a"), new HashSet<EntityId> { new EntityId("a") });
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void AllocationNamespaceIsReservedFromAuthoredAndDefinitionIds()
    {
        var runtime = new EntityId("entity.spawn.0000000000000001");
        Assert.True(runtime.UsesAllocationNamespace);
        Assert.True(runtime.TryGetAllocationSequence(out var sequence));
        Assert.Equal(1UL, sequence);
        Assert.Throws<ArgumentException>(() => EntityId.FromAuthored(runtime.Value));
        Assert.False(DefinitionId.TryParse(runtime.Value, out _));
        Assert.Throws<ArgumentException>(() => new DefinitionId(runtime.Value));
        Assert.False(new EntityId("entity.authored").TryGetAllocationSequence(out _));
        Assert.False(new EntityId("entity.spawn.0000000000000000").TryGetAllocationSequence(out _));
        Assert.False(new EntityId("entity.spawn.000000000000000g").TryGetAllocationSequence(out _));
        Assert.False(new EntityId("entity.spawn.1").TryGetAllocationSequence(out _));
    }

    [Fact]
    public void OnePersistedSequenceIsSharedAcrossCategoriesAndNeverReused()
    {
        var allocator = new EntityIdAllocator();
        Assert.Equal("entity.spawn.0000000000000001", allocator.Allocate("entity").Value);
        Assert.Equal("token.spawn.0000000000000002", allocator.Allocate("token").Value);
        var loadedDto = JsonSerializer.Deserialize<EntityAllocationStateDto>(JsonSerializer.Serialize(allocator.ToDto()))!;
        var restored = EntityIdAllocator.FromDto(loadedDto);
        loadedDto.NextEntitySequence = 1; // Restoration owns its counter, not the DTO.
        Assert.Equal("entity.spawn.0000000000000003", restored.Allocate("entity").Value);
        Assert.Equal(4UL, restored.NextEntitySequence);
        Assert.Equal(3UL, allocator.NextEntitySequence);
    }

    [Fact]
    public void InvalidCategoriesDoNotConsumeIdsAndMaximumIdLengthWorks()
    {
        var allocator = new EntityIdAllocator();
        foreach (var category in new[] { "", "Bad", "entity.spawn.invalid", "entity.spawn", new string('a', 106) })
        {
            Assert.Throws<ArgumentException>(() => allocator.Allocate(category));
            Assert.Equal(1UL, allocator.NextEntitySequence);
        }
        Assert.Equal(128, allocator.Allocate(new string('a', 105)).Value.Length);
        Assert.Equal(2UL, allocator.NextEntitySequence);
    }

    [Fact]
    public void ExhaustionIsPersistedWithoutWrappingOrRecyclingIds()
    {
        var allocator = new EntityIdAllocator(ulong.MaxValue - 1);
        Assert.Equal("entity.spawn.fffffffffffffffe", allocator.Allocate("entity").Value);
        Assert.Equal(ulong.MaxValue, allocator.NextEntitySequence);
        var restored = EntityIdAllocator.FromDto(JsonSerializer.Deserialize<EntityAllocationStateDto>(JsonSerializer.Serialize(allocator.ToDto()))!);
        Assert.Throws<OverflowException>(() => restored.Allocate("entity"));
        Assert.Throws<OverflowException>(() => restored.Allocate("token"));
        Assert.Equal(ulong.MaxValue, restored.NextEntitySequence);
        Assert.Throws<ArgumentOutOfRangeException>(() => new EntityIdAllocator(0));
        Assert.Throws<ArgumentException>(() => EntityIdAllocator.FromDto(new EntityAllocationStateDto()));
        Assert.Throws<ArgumentException>(() => EntityIdAllocator.FromDto(null));
    }
}
