using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal enum StrategicTurnPhase { AiPlanning, Economy, Research, Production, Movement, EncounterCreation }

    // Fixed execution order, not a pluggable rule system. The probe is internal
    // test instrumentation only; production sessions always supply null.
    internal static class StrategicTurnPhases
    {
        internal static bool Execute(ref GameStateDto staging, DefinitionCatalogDto definitions, Func<StrategicTurnPhase, GameStateDto, bool>? probe, Action aiPlanning, Action encounterCreation,PlanetaryOwnershipContext? ownership=null)
        {
            if (!ExecutePhase(StrategicTurnPhase.AiPlanning, ref staging, definitions, probe, aiPlanning, encounterCreation,ownership)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Economy, ref staging, definitions, probe, aiPlanning, encounterCreation,ownership)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Research, ref staging, definitions, probe, aiPlanning, encounterCreation,ownership)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Production, ref staging, definitions, probe, aiPlanning, encounterCreation,ownership)) return false;
            if (!ExecutePhase(StrategicTurnPhase.Movement, ref staging, definitions, probe, aiPlanning, encounterCreation,ownership)) return false;
            return ExecutePhase(StrategicTurnPhase.EncounterCreation, ref staging, definitions, probe, aiPlanning, encounterCreation,ownership);
        }

        private static void Validate(GameStateDto staging, DefinitionCatalogDto definitions,PlanetaryOwnershipContext? ownership)
        {
            var report=ownership==null?FoundationValidator.ValidateGameState(staging,definitions):FoundationValidator.ValidatePlanetaryGameState(staging,definitions,ownership);
            if (!report.IsValid) throw new InvalidOperationException("Strategic phase violated state invariants: " + report.Issues[0].Code);
        }

        private static bool ExecutePhase(StrategicTurnPhase phase, ref GameStateDto staging, DefinitionCatalogDto definitions,
            Func<StrategicTurnPhase, GameStateDto, bool>? probe, Action aiPlanning, Action encounterCreation,PlanetaryOwnershipContext? ownership)
        {
            if(ownership!=null)ownership=ownership.CopyForState(staging);
            var before = SimulationSession.CopyState(staging);var beforeOwnership=ownership?.CopyForState(before);
            if (probe != null && !probe(phase, staging)) return false;
            Validate(staging,definitions,ownership?.CopyForState(staging));
            // Instrumentation must not supply rule behavior, even at implemented phases.
            Require(before,SimulationSession.CopyState(staging),definitions,beforeOwnership,ownership,false);
            if (phase == StrategicTurnPhase.AiPlanning) aiPlanning();
            if (phase == StrategicTurnPhase.Economy) EconomyPhase.Execute(staging, definitions,null,ownership);
            if (phase == StrategicTurnPhase.Research) ResearchPhase.Execute(staging, definitions,ownership);
            if (phase == StrategicTurnPhase.Production) ProductionPhase.Execute(staging, definitions);
            if (phase == StrategicTurnPhase.Movement) MovementPhase.Execute(staging,definitions);
            if (phase == StrategicTurnPhase.EncounterCreation) encounterCreation();
            Validate(staging,definitions,ownership?.CopyForState(staging));
            if (phase == StrategicTurnPhase.EncounterCreation) CampaignCombatState.RequireDetectionOnly(before, SimulationSession.CopyState(staging));
            else if (phase != StrategicTurnPhase.AiPlanning) Require(before,SimulationSession.CopyState(staging),definitions,beforeOwnership,ownership,phase==StrategicTurnPhase.Economy,phase==StrategicTurnPhase.Research,phase==StrategicTurnPhase.Production,phase==StrategicTurnPhase.Movement);
            return true;
        }
        private static void Require(GameStateDto before,GameStateDto after,DefinitionCatalogDto definitions,PlanetaryOwnershipContext? beforeOwnership,
            PlanetaryOwnershipContext? afterOwnership,bool economy,bool research=false,bool production=false,bool movement=false)
        {
            if(beforeOwnership==null&&afterOwnership==null)SimulationSession.RequirePhaseChanges(before,after,economy,research,production,movement);
            else if(beforeOwnership==null||afterOwnership==null)throw new InvalidOperationException("Missing planetary phase era.");
            else SimulationSession.RequirePlanetaryPhaseChanges(before,after,definitions,beforeOwnership,afterOwnership.CopyForState(after),economy,research,production,movement);
        }

    }
}
