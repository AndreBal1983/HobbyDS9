using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Host-supplied identity of the definitions/rules it actually loaded.
    public sealed class SaveCompatibility
    {
        public string ScenarioId { get; }
        public string RulesVersion { get; }
        public string ContentHash { get; }

        public SaveCompatibility(string scenarioId, string rulesVersion, string contentHash)
        {
            ScenarioId = EntityId.FromAuthored(scenarioId).Value;
            if (string.IsNullOrWhiteSpace(rulesVersion)) throw new ArgumentException("Missing rules version.", nameof(rulesVersion));
            if (!FoundationValidator.IsContentHash(contentHash)) throw new ArgumentException("Invalid SHA256 content hash.", nameof(contentHash));
            RulesVersion = rulesVersion;
            ContentHash = contentHash;
        }
    }
}
