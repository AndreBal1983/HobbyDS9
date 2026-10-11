#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace HobbyDS9.UnityClient
{
    // Host adapter: JSON/content hashing stay out of the portable libraries.
    public sealed class CampaignHost
    {
        private readonly ScenarioDto scenario;
        public StrategicClient Client { get; }
        public CampaignSnapshot Snapshot => Client.Snapshot;
        public CampaignHost(byte[] scenarioBytes)
        {
            scenario = JsonConvert.DeserializeObject<ScenarioDto>(System.Text.Encoding.UTF8.GetString(scenarioBytes), JsonSettings())
                ?? throw new ArgumentException("Scenario is empty.");
            using (var hash = SHA256.Create())
                scenario.ContentHash = string.Concat(hash.ComputeHash(scenarioBytes).Select(b => b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
            Client = new StrategicClient(CampaignApplication.StartScenario(scenario), save => CampaignApplication.LoadSave(scenario, save));
        }
        public string SaveJson() => JsonConvert.SerializeObject(Client.ExportSave(), Formatting.Indented);
        public void LoadJson(string json) => Client.Load(ParseSave(json));
        public string SaveSummary(string json)
        {
            var save = ParseSave(json);
            var issues = CampaignApplication.ValidateSave(scenario, save);
            if (issues.Count != 0) throw new ArgumentException("Incompatible or invalid save: " + issues[0].Code);
            return "Turn " + save.GameState.TurnNumber + " • " + save.GameState.Phase;
        }
        private static SaveEnvelopeDto ParseSave(string json) => JsonConvert.DeserializeObject<SaveEnvelopeDto>(json,
            JsonSettings()) ?? throw new ArgumentException("Save is empty.");
        private static JsonSerializerSettings JsonSettings() => new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error, TypeNameHandling = TypeNameHandling.None,
            MaxDepth = 128, ContractResolver = new AuthoritativeContractResolver()
        };
        // Missing scalar facts must not silently become zero/false/default enum values.
        private sealed class AuthoritativeContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
            {
                var property = base.CreateProperty(member, serialization);
                if (member.DeclaringType?.Assembly == typeof(GameStateDto).Assembly && property.PropertyType != null
                    && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                    property.Required = Required.Always;
                return property;
            }
        }
        public TurnResult EndTurn() => Client.EndTurn(Snapshot);
        public IReadOnlyList<CombatRequestDto> Pending() => Client.Pending();
        public CombatSubmissionResult Resolve(CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto>? plans = null) => Client.Resolve(Snapshot, request, plans);
        public TurnResult FinalizeCombats() => Client.FinalizeCombats(Snapshot);
        public void Refresh() => Client.Refresh();
    }
}
