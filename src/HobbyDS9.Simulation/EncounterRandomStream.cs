using System;

namespace HobbyDS9.Simulation
{
    // A resolver owns its stream. No static/global mutable state or campaign access.
    public sealed class EncounterRandomStream
    {
        public const string AlgorithmId = "splitmix64-rejection-v1";
        public ulong State { get; private set; }

        public EncounterRandomStream(ulong initialState) { State = initialState; }

        public ulong NextUInt64()
        {
            unchecked
            {
                State += 0x9e3779b97f4a7c15UL;
                var z = State;
                z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
                z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
                return z ^ (z >> 31);
            }
        }

        public ulong NextBelow(ulong exclusiveBound)
        {
            if (exclusiveBound == 0) throw new ArgumentOutOfRangeException(nameof(exclusiveBound));
            var threshold = unchecked(0UL - exclusiveBound) % exclusiveBound;
            ulong draw;
            do { draw = NextUInt64(); } while (draw < threshold);
            return draw % exclusiveBound;
        }
    }
}
