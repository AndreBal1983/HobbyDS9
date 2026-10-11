using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class CombatPlanValidation
    {
        internal static void Validate(CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto> plans)
        {
            if (plans == null) throw new ArgumentNullException(nameof(plans));
            var fleets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var participant in request.Participants) fleets.Add(participant.FleetId);
            if (plans.Count != fleets.Count) throw new ArgumentException("Exactly one plan per fleet is required.", nameof(plans));
            foreach (var plan in plans)
                if (plan == null || !EntityId.TryParse(plan.FleetId, out _) || !fleets.Remove(plan.FleetId)
                    || !Enum.IsDefined(typeof(CombatTactic), plan.LongRangeTactic) || !Enum.IsDefined(typeof(CombatTactic), plan.MediumRangeTactic) || !Enum.IsDefined(typeof(CombatTactic), plan.CloseRangeTactic)
                    || !Enum.IsDefined(typeof(CombatTargetPriority), plan.TargetPriority) || plan.RetreatHullPercent < 0 || plan.RetreatHullPercent > 100
                    || (plan.RetreatHullPercent == 0 ? plan.RetreatNodeId != null : !EntityId.TryParse(plan.RetreatNodeId, out _)))
                    throw new ArgumentException("Invalid, duplicate or unknown fleet plan.", nameof(plans));
        }
    }
}
