#nullable enable
using System;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityClient
{
    // Pure, detached presentation shaping; no commands or mutable DTOs.
    public static class StrategicScreenPresentation
    {
        public static ColonySnapshot? InitialWorld(CampaignSnapshot snapshot) => snapshot.Campaign.Colonies
            .Where(c => string.Equals(c.Environment.DisplayName, "Mars", StringComparison.Ordinal))
            .OrderBy(c => c.PlanetId.Value, StringComparer.Ordinal).FirstOrDefault();

        private static bool IsCode(string value, string code) => value == code
            || value.EndsWith("[" + code + "]", StringComparison.Ordinal)
            || value.EndsWith(": " + code, StringComparison.Ordinal);

        public static string PlayerMessage(string value)
        {
            // Stable codes stay in command results/logs, outside the player strip.
            foreach (CommandErrorCode code in Enum.GetValues(typeof(CommandErrorCode)))
            {
                if (code == CommandErrorCode.None) continue;
                if (IsCode(value, code.ToString()))
                    return code == CommandErrorCode.NavigationUnknown
                        ? "That destination is not yet known. Explore or choose a known region."
                        : CommandFeedback.Explain(code);
            }
            foreach (TurnErrorCode code in Enum.GetValues(typeof(TurnErrorCode)))
            {
                if (code == TurnErrorCode.None || !IsCode(value, code.ToString())) continue;
                switch (code)
                {
                    case TurnErrorCode.UnresolvedCombats: return "Resolve every pending battle before completing the turn.";
                    case TurnErrorCode.NotAwaitingCombat: return "There is no battle checkpoint to complete. Continue with strategic orders.";
                    case TurnErrorCode.InvalidCombatBatch: return "The battle checkpoint could not be completed. Review the pending battles.";
                    default: return "The turn could not be completed. Your campaign has not advanced; review the current state.";
                }
            }
            foreach (CombatSubmissionError code in Enum.GetValues(typeof(CombatSubmissionError)))
            {
                if (code == CombatSubmissionError.None || !IsCode(value, code.ToString())) continue;
                switch (code)
                {
                    case CombatSubmissionError.InvalidRetreat: return "That retreat route is unavailable. Choose an allowed retreat destination.";
                    case CombatSubmissionError.AlreadyResolved: return "This battle already has a result. Review it before continuing.";
                    case CombatSubmissionError.UnknownEncounter: return "This battle is no longer pending. Review the current checkpoint.";
                    case CombatSubmissionError.WrongPhase: return "Battle results can only be submitted during a battle checkpoint.";
                    default: return "The battle result could not be accepted. Review the pending battle before continuing.";
                }
            }
            return value;
        }
    }
}
