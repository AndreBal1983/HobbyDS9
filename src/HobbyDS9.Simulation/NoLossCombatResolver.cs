using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed class NoLossCombatResolver : ICombatResolver
    {
        public const string ResolverId = "combat-resolver.no-loss";
        public const int ResolverVersion = 1;
        public CombatResultDto Resolve(CombatRequestDto request)
        {
            CombatEncounterScaffold.ValidateRequest(request, allocated: true);
            if (request.ResolverId != ResolverId || request.ResolverVersion != ResolverVersion) throw new ArgumentException("No-loss resolver identity mismatch.", nameof(request));
            return new CombatResultDto
            {
                EncounterId = request.EncounterId, ResolverId = ResolverId, ResolverVersion = ResolverVersion,
                Status = CombatResolutionStatus.Resolved, FinalRandomState = request.RandomState,
                Events = new List<CombatEventDto> { new CombatEventDto { Sequence = 0, Kind = CombatEventKind.EncounterResolved } }
            };
        }
    }
}
