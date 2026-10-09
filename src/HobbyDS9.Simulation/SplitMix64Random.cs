using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed class SplitMix64Random
    {
        public const string AlgorithmId = FoundationVersions.RandomAlgorithmId;
        public const int AlgorithmVersion = FoundationVersions.RandomAlgorithmVersion;
        public ulong State { get; private set; }

        public SplitMix64Random(ulong seed) { State = seed; }

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

        public RngStateDto ToDto() => new RngStateDto { AlgorithmId = AlgorithmId, AlgorithmVersion = AlgorithmVersion, State = State };

        public static SplitMix64Random FromDto(RngStateDto? dto)
        {
            FoundationValidator.ValidateRngState(dto).ThrowIfInvalid(nameof(dto));
            return new SplitMix64Random(dto!.State!.Value);
        }
    }
}
