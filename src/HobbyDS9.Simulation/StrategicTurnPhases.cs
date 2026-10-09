using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal enum StrategicTurnPhase { AiPlanning, Economy, Research, Production, Movement, EncounterCreation }

    // Fixed execution order, not a pluggable rule system. The probe is internal
    // test instrumentation only; production sessions always supply null.
    internal static class StrategicTurnPhases
    {
        internal static bool Execute(GameStateDto staging, Func<StrategicTurnPhase, GameStateDto, bool>? probe)
        {
            if (!ExecutePhase(StrategicTurnPhase.AiPlanning, staging, probe)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Economy, staging, probe)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Research, staging, probe)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Production, staging, probe)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Movement, staging, probe)) return false;
            return ExecutePhase(StrategicTurnPhase.EncounterCreation, staging, probe);
        }

        private static bool ExecutePhase(StrategicTurnPhase phase, GameStateDto staging,
            Func<StrategicTurnPhase, GameStateDto, bool>? probe)
        {
            if (probe != null && !probe(phase, staging)) return false;
            // Future built-in systems execute here in the fixed order above.
            // Phase 3B has no phase behavior or encounter creation.
            return true;
        }
    }
}
