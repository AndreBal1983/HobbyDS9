using System;
using System.Globalization;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed class EntityIdAllocator
    {
        public ulong NextEntitySequence { get; private set; }

        public EntityIdAllocator(ulong nextEntitySequence = 1)
        {
            if (nextEntitySequence == 0) throw new ArgumentOutOfRangeException(nameof(nextEntitySequence));
            NextEntitySequence = nextEntitySequence;
        }

        public EntityId Allocate(string category)
        {
            var prefix = EntityId.FromAuthored(category);
            if (prefix.Value.EndsWith(".spawn", StringComparison.Ordinal))
                throw new ArgumentException("Reserved allocation category.", nameof(category));
            // Validate full ID length before changing allocation state.
            var id = new EntityId(prefix.Value + ".spawn." + NextEntitySequence.ToString("x16", CultureInfo.InvariantCulture));
            if (NextEntitySequence == ulong.MaxValue) throw new OverflowException("Entity ID sequence exhausted.");
            NextEntitySequence++;
            return id;
        }

        public EntityAllocationStateDto ToDto() => new EntityAllocationStateDto { NextEntitySequence = NextEntitySequence };

        public static EntityIdAllocator FromDto(EntityAllocationStateDto? dto)
        {
            FoundationValidator.ValidateAllocationState(dto).ThrowIfInvalid(nameof(dto));
            return new EntityIdAllocator(dto!.NextEntitySequence);
        }
    }
}
