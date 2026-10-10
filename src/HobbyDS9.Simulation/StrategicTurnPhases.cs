using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal enum StrategicTurnPhase { AiPlanning, Economy, Research, Production, Movement, EncounterCreation }

    // Fixed execution order, not a pluggable rule system. The probe is internal
    // test instrumentation only; production sessions always supply null.
    internal static class StrategicTurnPhases
    {
        internal static bool Execute(ref GameStateDto staging, DefinitionCatalogDto definitions, Func<StrategicTurnPhase, GameStateDto, bool>? probe, Action aiPlanning, Action encounterCreation)
        {
            if (!ExecutePhase(StrategicTurnPhase.AiPlanning, ref staging, definitions, probe, aiPlanning, encounterCreation)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Economy, ref staging, definitions, probe, aiPlanning, encounterCreation)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Research, ref staging, definitions, probe, aiPlanning, encounterCreation)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Production, ref staging, definitions, probe, aiPlanning, encounterCreation)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Movement, ref staging, definitions, probe, aiPlanning, encounterCreation)) return false;
            return ExecutePhase(StrategicTurnPhase.EncounterCreation, ref staging, definitions, probe, aiPlanning, encounterCreation);
        }

        private static void Validate(GameStateDto staging, DefinitionCatalogDto definitions)
        {
            var report = FoundationValidator.ValidateGameState(staging, definitions);
            if (!report.IsValid) throw new InvalidOperationException("Strategic phase violated state invariants: " + report.Issues[0].Code);
        }

        private static bool ExecutePhase(StrategicTurnPhase phase, ref GameStateDto staging, DefinitionCatalogDto definitions,
            Func<StrategicTurnPhase, GameStateDto, bool>? probe, Action aiPlanning, Action encounterCreation)
        {
            var before = SimulationSession.CopyState(staging);
            if (probe != null && !probe(phase, staging)) return false;
            Validate(staging, definitions);
            // Instrumentation must not supply rule behavior, even at implemented phases.
            SimulationSession.RequirePhaseChanges(before, SimulationSession.CopyState(staging), allowEconomy: false);
            if (phase == StrategicTurnPhase.AiPlanning) aiPlanning();
            if (phase == StrategicTurnPhase.Economy) EconomyPhase.Execute(staging, definitions);
            if (phase == StrategicTurnPhase.Research) ResearchPhase.Execute(staging, definitions);
            if (phase == StrategicTurnPhase.Production) ProductionPhase.Execute(staging, definitions);
            if (phase == StrategicTurnPhase.Movement) MovementPhase.Execute(staging);
            if (phase == StrategicTurnPhase.EncounterCreation) encounterCreation();
            Validate(staging, definitions);
            if (phase == StrategicTurnPhase.EncounterCreation) CampaignCombatState.RequireDetectionOnly(before, SimulationSession.CopyState(staging));
            else if (phase != StrategicTurnPhase.AiPlanning) SimulationSession.RequirePhaseChanges(before, SimulationSession.CopyState(staging), allowEconomy: phase == StrategicTurnPhase.Economy, allowResearch: phase == StrategicTurnPhase.Research, allowProduction: phase == StrategicTurnPhase.Production, allowMovement: phase == StrategicTurnPhase.Movement);
            return true;
        }
    }
}
