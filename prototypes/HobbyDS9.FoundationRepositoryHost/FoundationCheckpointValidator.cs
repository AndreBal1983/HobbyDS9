using System;
using System.Globalization;
using System.Linq;
using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.FoundationRepositoryHost
{
    // Opt-in host adapter for the foundational envelope only, never an outer Cycle root.
    // Repository metadata and CheckpointImage construction are not game-validity tokens.
    public sealed class FoundationCheckpointValidator : ICheckpointValidator
    {
        private readonly byte[] scenarioBytes;
        private readonly ScenarioDto scenario;
        public string EditionIdentity { get; }

        public FoundationCheckpointValidator(byte[] scenarioBytes)
        {
            this.scenarioBytes = Own(scenarioBytes);
            scenario = StrictFoundationJson.Decode<ScenarioDto>(this.scenarioBytes);
            scenario.ContentHash = CheckpointImage.Hash(this.scenarioBytes);
            // Use the same exact-byte hash and actual scenario factory as CampaignHost.
            CampaignApplication.StartScenario(scenario);
            EditionIdentity = Describe(scenario);
        }

        public byte[] ReadScenarioBytes() => (byte[])scenarioBytes.Clone();
        public CheckpointImage Validate(byte[] ownedPayload)
        {
            var bytes = Own(ownedPayload);
            var session = Load(bytes);
            var state = session.ExportSave().GameState;
            return new CheckpointImage(bytes, EditionIdentity, state.TurnNumber,
                state.StateRevision, state.Phase!.Value.ToString());
        }

        // Always reconstructs and validates. Never trusts supplied image metadata.
        // Each successful call returns a fresh, independently owned session.
        public ICampaignSession Restore(byte[] payload) => Load(Own(payload));

        private ICampaignSession Load(byte[] bytes)
        {
            var save = StrictFoundationJson.Decode<SaveEnvelopeDto>(bytes);
            var session = CampaignApplication.LoadSave(scenario, save);
            // Prove the complete deserialized root survives the actual loader, including
            // ledgers, pending requests, buffered results, RNG and allocation state.
            if (!JToken.DeepEquals(Canonical(save), Canonical(session.ExportSave())))
                throw new ArgumentException("The complete foundation root changed during load/export.", nameof(bytes));
            return session;
        }

        private static JToken Canonical(object value) => Order(JToken.FromObject(value, StrictFoundationJson.Serializer()));
        private static JToken Order(JToken token)
        {
            if (token is JObject obj)
                return new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => new JProperty(p.Name, Order(p.Value))));
            if (token is JArray array) return new JArray(array.Select(Order));
            return token.DeepClone();
        }

        private static byte[] Own(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > CheckpointImage.MaximumPayloadBytes)
                throw new ArgumentException("Foundation JSON must contain 1 through 64 MiB of bytes.", nameof(bytes));
            return (byte[])bytes.Clone();
        }

        private static string Describe(ScenarioDto scenario)
        {
            // Descriptor schema v1: fixed order; UTF-8 byte-length prefixed values.
            // No scalar format number is treated as a universal edition identifier.
            var fields = new[] { "foundation-repository-descriptor-v1", "foundationSaveEnvelope",
                FoundationVersions.SaveFormatVersion.ToString(CultureInfo.InvariantCulture),
                scenario.Definitions.ContentFormatVersion.ToString(CultureInfo.InvariantCulture),
                scenario.ScenarioId, scenario.ContentHash, scenario.RulesVersion,
                FoundationVersions.RandomAlgorithmId, FoundationVersions.RandomAlgorithmVersion.ToString(CultureInfo.InvariantCulture),
                "strict-foundation-newtonsoft-json-13.0.3-v1", CampaignCombatRules.ResolverId,
                CampaignCombatRules.ResolverVersion.ToString(CultureInfo.InvariantCulture) };
            var result = new StringBuilder();
            var utf8 = new UTF8Encoding(false, true);
            foreach (var field in fields)
            {
                if (field.IndexOf('\0') >= 0 || utf8.GetByteCount(field) > 4096)
                    throw new ArgumentException("Unbounded edition descriptor field.");
                result.Append(utf8.GetByteCount(field).ToString(CultureInfo.InvariantCulture)).Append(':').Append(field);
            }
            if (result.Length > 16384) throw new ArgumentException("Unbounded edition descriptor.");
            return result.ToString();
        }
    }
}
