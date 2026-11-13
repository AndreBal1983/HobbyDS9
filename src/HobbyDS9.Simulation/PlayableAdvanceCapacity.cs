using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class PlayableAdvanceCapacity
    {
        // Exact existing staged-batch journal cost, after AI-only resolution.
        internal static long RequiredSlots(CampaignTacticalTravelSaveDto next, IReadOnlyDictionary<string, ControllerKind> controllers)
        {
            long required = 1;
            if (next.Phase == SessionPhase.AwaitingCombat)
            {
                required++;
                foreach (var battle in next.Core.Battles.Where(b => b.State.Completion == TacticalCompletion.None))
                {
                    if (battle.State.NextRound < 1 || battle.State.NextRound > 3)
                        throw new ArgumentException("Invalid playable continuation boundary.");
                    if (!battle.State.Formations.Any(f => f.Status == TacticalFormationStatus.Active && controllers[f.SideId] == ControllerKind.Human))
                        throw new ArgumentException("Unresolved AI-only playable encounter.");
                    required = checked(required + 4 - battle.State.NextRound);
                }
            }
            return required;
        }
    }
}
