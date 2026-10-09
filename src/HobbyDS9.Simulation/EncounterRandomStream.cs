namespace HobbyDS9.Simulation
{
    // A resolver owns its stream. No static/global mutable state or campaign access.
    public sealed class EncounterRandomStream
    {
        private readonly SplitMix64Random random;
        public const string AlgorithmId = SplitMix64Random.AlgorithmId;
        public ulong State => random.State;
        public EncounterRandomStream(ulong initialState) { random = new SplitMix64Random(initialState); }
        public ulong NextUInt64() => random.NextUInt64();
        public ulong NextBelow(ulong exclusiveBound) => random.NextBelow(exclusiveBound);
    }
}
