#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    // Closed wire admission, never evidence of source authority or successful replay.
    // The typed host must fully load/replay before publishing a replacement session.
    public sealed partial class ReviewNewtonsoftCodec : IBackgroundColonisationCheckpointCodec
    {
        public CampaignColonisationPackageDto CopyPackage(CampaignColonisationPackageDto package)
            => Decode<CampaignColonisationPackageDto>(Encode(package));
        public CampaignColonisationSaveDto CopySave(CampaignColonisationSaveDto root)
            => DecodeColonisationRoot(EncodeColonisationRoot(root));
        public ShipDesignCatalogueDto DecodeColonisationCatalogue(byte[] bytes) => DecodeActivityResource<ShipDesignCatalogueDto>(bytes);
        public List<ShipClassDraftDto> DecodeColonisationReadyClasses(byte[] bytes) => DecodeActivityResource<List<ShipClassDraftDto>>(bytes);
        public ColonisationSupportCatalogueDto DecodeColonisationSupport(byte[] bytes) => DecodeActivityResource<ColonisationSupportCatalogueDto>(bytes);
        public ColonisationPolicyDto DecodeColonisationPolicy(byte[] bytes) => DecodeActivityResource<ColonisationPolicyDto>(bytes);

        public byte[] EncodeColonisationRoot(CampaignColonisationSaveDto root)
        {
            RequireColonisationCarrier(root);
            var bytes = Encode(root);
            if (bytes.Length == 0 || bytes.Length > ReviewColonisationSlotStore.MaximumBytes) throw Invalid();
            return bytes;
        }
        public CampaignColonisationSaveDto DecodeColonisationRoot(byte[] bytes)
        {
            var phase=ReviewLoadPhase.DecodeParse;
            try
            {
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                if (bytes == null || bytes.Length == 0 || bytes.Length > ReviewColonisationSlotStore.MaximumBytes) throw Invalid();
                JToken wire;
                using (var text = new StringReader(Utf8.GetString(bytes)))
                using (var reader = new JsonTextReader(text) { MaxDepth = 128, DateParseHandling = DateParseHandling.None })
                {
                    wire = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (!(wire is JObject) || reader.Read()) throw Invalid();
                }
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeShape;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                // Unlike DTO default initializers, omitted fields are not retained history.
                // This adapter-only reflection closes the fixed contract graph; portable
                // simulation uses its independently versioned non-reflective preflight.
                int visits = 2000000;
                ColonisationWire(wire, typeof(CampaignColonisationSaveDto), ref visits, new Dictionary<Type, PropertyInfo[]>());
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeNatural;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                DecodeNaturalGalaxy(Utf8.GetBytes(wire["Map"]!.ToString(Formatting.None)));
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeTyped;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                var root = Decode<CampaignColonisationSaveDto>(bytes);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeCarrier;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                RequireColonisationCarrier(root);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                return root;
            }
            catch (Exception e) { loadDiagnostic?.Record(phase,ReviewLoadEvent.Error,e);throw new InvalidDataException("Invalid colonisation campaign carrier."); }
        }
        private enum ColonisationCarrierVocabulary { Original,Growth }
        private static void ColonisationWire(JToken token, Type declared, ref int visits, Dictionary<Type, PropertyInfo[]> shapes,ColonisationCarrierVocabulary vocabulary=ColonisationCarrierVocabulary.Original)
        {
            if (--visits < 0) throw Invalid();
            if (token.Type == JTokenType.Null) return; // Typed decoding and required-fact checks follow.
            var type = Nullable.GetUnderlyingType(declared) ?? declared;
            if (vocabulary==ColonisationCarrierVocabulary.Original&&token.Type==JTokenType.Integer&&((type==typeof(SettlerManifestKind)&&token.Value<long>()==4)
                ||(type==typeof(ColonisationAction)&&token.Value<long>()==11)
                ||(type==typeof(ColonisationAiStage)&&token.Value<long>()>8)
                ||(type==typeof(ColonisationAiAction)&&token.Value<long>()>14)))throw Invalid();
            if (type == typeof(string) || type.IsValueType) return;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var rows = GeneratedRows(token, ActivityCarrierRows);
                var itemType = type.GetGenericArguments()[0];
                foreach (var row in rows)
                {
                    if (row.Type == JTokenType.Null) throw Invalid();
                    ColonisationWire(row, itemType, ref visits, shapes,vocabulary);
                }
                return;
            }
            if (type.Assembly != typeof(GameStateDto).Assembly) throw Invalid();
            if (!shapes.TryGetValue(type, out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite).ToArray();
                shapes.Add(type, properties);
            }
            GeneratedObject(token, properties.Select(p => p.Name).ToArray());
            foreach (var property in properties) ColonisationWire(token[property.Name]!, property.PropertyType, ref visits, shapes,vocabulary);
        }
        private static void RequireColonisationCarrier(CampaignColonisationSaveDto? root)
            =>RequireColonisationCarrier(root,CampaignColonisationIdentity.InventoryPolicy);
        private static void RequireColonisationCarrier(CampaignColonisationSaveDto? root,string expectedInventoryPolicy,ColonisationCarrierVocabulary vocabulary=ColonisationCarrierVocabulary.Original)
        {
            bool Hash(string? value) => value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
            if (root == null || root.FormatVersion != CampaignColonisationIdentity.Format
                || root.ProfileId != CampaignColonisationIdentity.Profile || root.PolicyId != CampaignColonisationIdentity.Policy
                || !Hash(root.TemplatePackageHash) || !Hash(root.PackageHash) || !Hash(root.SupportHash)
                || root.Map == null || root.Map.FormatVersion != NaturalGalaxyIdentity.FormatVersion || !Hash(root.Map.ContentHash)
                || root.Map.Settings == null || root.Map.Tuning == null || root.Map.Layout == null || root.Map.Features == null
                || root.Map.StarGroups == null || root.Map.RegionInfluences == null
                || !ActivityRows(root.Discoveries) || !ActivityRows(root.PublishedBuiltShips) || !ActivityRows(root.Journal)
                || !ActivityRows(root.Production) || !ActivityRows(root.ProductionHistory)
                || root.Activities?.Production == null || root.Activities.Production.Count != 0
                || root.FrozenPlanets == null || root.FrozenPlanets.PolicyId != expectedInventoryPolicy || !Hash(root.FrozenPlanets.ContentHash)
                || !ActivityRows(root.FrozenPlanets.Definitions) || !ActivityRows(root.FrozenPlanets.InitialPlanets)
                || !ActivityRows(root.FrozenPlanets.Bodies) || !ActivityRows(root.FrozenPlanets.GenesisControls)
                || root.Colonisation == null || root.Campaign == null || root.Campaign.FormatVersion != CampaignPlayableFleetIdentity.Format
                || root.Campaign.ProfileId != CampaignPlayableFleetIdentity.Profile || !Hash(root.Campaign.PackageHash)
                || !ActivityRows(root.Campaign.Transcript) || !ActivityRows(root.Campaign.Names)
                || root.Campaign.Integrated?.Physical?.Core?.ProfileId != CampaignColonisationIdentity.EmbeddedCoreProfile
                || root.Campaign.Integrated.Physical.Core.Campaign?.GameState == null) throw Invalid();
            RequireColonisationStateCarrier(root.Colonisation,vocabulary);
            if (root.MovementOwnership != null && (!ActivityRows(root.MovementOwnership.PlanetControls) || !ActivityRows(root.MovementOwnership.ColonyStages))) throw Invalid();
            if (root.LastSettlementProof != null)
            {
                var proof = root.LastSettlementProof;
                if (proof.BeforeGameState == null || proof.AfterGameState == null || proof.BeforeColonisation == null || proof.AfterColonisation == null || !ActivityRows(proof.Receipts)) throw Invalid();
                RequireColonisationStateCarrier(proof.BeforeColonisation,vocabulary); RequireColonisationStateCarrier(proof.AfterColonisation,vocabulary);
                foreach (var receipt in proof.Receipts!)
                {
                    if (receipt.Intent == null || receipt.ManifestBefore == null || !ActivityRows(receipt.MaterialsTransferred)) throw Invalid();
                    RequireColonisationManifestCarrier(receipt.ManifestBefore,vocabulary);
                }
            }
            var inherited = new List<CampaignActivityOperationDto>();
            foreach (var operation in root.Journal!)
            {
                if ((operation.Colonisation == null) == (operation.Existing == null)) throw Invalid();
                if (operation.Existing != null) inherited.Add(operation.Existing);
                else RequireColonisationCommand(operation.Colonisation!,vocabulary);
            }
            RequireActivityFacts(root.Activities, inherited);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var order in root.Production!)
            {
                RequireColonisationOrder(order, ids);
                if (order.CompletedIndustry >= order.RequiredIndustry) throw Invalid();
            }
            foreach (var receipt in root.ProductionHistory!)
            {
                if (receipt.Order == null || receipt.ResolvedTurn < 0) throw Invalid();
                RequireColonisationOrder(receipt.Order, ids);
                if (receipt.Completed != (receipt.Order.CompletedIndustry == receipt.Order.RequiredIndustry)) throw Invalid();
                bool built = receipt.Completed && receipt.Order.Kind == ColonisationProductionKind.PublishedShip;
                if (built ? !ActivityText(receipt.ShipId) : receipt.ShipId != null) throw Invalid();
            }
        }
        private static void RequireColonisationStateCarrier(CampaignColonisationStateDto state,ColonisationCarrierVocabulary vocabulary=ColonisationCarrierVocabulary.Original)
        {
            if (!ActivityRows(state.PlanetControls) || !ActivityRows(state.Bodies) || !ActivityRows(state.Surveys)
                || !ActivityRows(state.ManifestRecords) || !ActivityRows(state.ManifestOrigins) || !ActivityRows(state.ManifestResolutions)
                || !ActivityRows(state.ColonyOrigins) || !ActivityRows(state.LandedCohortLineages) || !ActivityRows(state.CampSources)
                || !ActivityRows(state.CampTerminals) || !ActivityRows(state.DeploymentIntents)) throw Invalid();
            foreach (var manifest in state.ManifestRecords!) RequireColonisationManifestCarrier(manifest,vocabulary);
            foreach (var origin in state.ManifestOrigins!) if (!ActivityRows(origin.LoadedMaterials)) throw Invalid();
            foreach (var resolution in state.ManifestResolutions!)
            {
                if (!ActivityRows(resolution.CargoTransferred) || !ActivityRows(resolution.CargoLost)) throw Invalid();
            }
        }
        private static void RequireColonisationManifestCarrier(SettlerManifestDto manifest,ColonisationCarrierVocabulary vocabulary=ColonisationCarrierVocabulary.Original)
        {
            if (manifest.Blueprint == null || !ActivityRows(manifest.Cargo) || !ActivityRows(manifest.MaterialsDelivered) || !ActivityRows(manifest.MaterialsLost)
                || (!Enum.IsDefined(typeof(SettlerManifestKind), manifest.Kind)||vocabulary==ColonisationCarrierVocabulary.Original&&(int)manifest.Kind>3) || !Enum.IsDefined(typeof(SettlerManifestStage), manifest.Stage)) throw Invalid();
        }
        private static void RequireColonisationCommand(ColonisationCommandDto command,ColonisationCarrierVocabulary vocabulary=ColonisationCarrierVocabulary.Original)
        {
            object?[] payloads = { command.StartSurvey, command.CancelSurvey, command.Embark, command.Unload, command.Deploy,
                command.Withdraw, command.Evacuate, command.Deliver, command.EnqueueKit, command.LoadSupplies };
            int action = (int)command.Action;
            int payload=action==11&&vocabulary==ColonisationCarrierVocabulary.Growth?2:action-1;
            if (!ActivityText(command.ActorId) || !ActivityText(command.CommandId) || command.Turn < 0 || command.Revision < 0
                || payload < 0 || payload >= payloads.Length || payloads.Count(p => p != null) != 1 || payloads[payload] == null) throw Invalid();
        }
        private static void RequireColonisationOrder(ColonisationProductionItemDto order, HashSet<string> ids)
        {
            if (!ActivityText(order.QueueItemId) || !ids.Add(order.QueueItemId) || !ActivityText(order.OwnerId)
                || !ActivityText(order.ColonyId) || !ActivityText(order.DefinitionId) || !Enum.IsDefined(typeof(ColonisationProductionKind), order.Kind)
                || order.RequiredIndustry <= 0 || order.CompletedIndustry < 0 || order.CompletedIndustry > order.RequiredIndustry
                || !ActivityRows(order.RequiredResources) || !ActivityRows(order.ConsumedResources)
                || order.RequiredResources!.Count != order.ConsumedResources!.Count) throw Invalid();
            if (order.Kind == ColonisationProductionKind.PublishedShip)
            { if (order.Blueprint == null || !ActivityText(order.Blueprint.BlueprintId) || order.Blueprint.Revision <= 0) throw Invalid(); }
            else if (order.Blueprint != null) throw Invalid();
            string? previous = null;
            for (int i = 0; i < order.RequiredResources.Count; i++)
            {
                var required = order.RequiredResources[i]; var consumed = order.ConsumedResources[i];
                if (!ActivityText(required.ResourceDefinitionId) || previous != null && StringComparer.Ordinal.Compare(previous, required.ResourceDefinitionId) >= 0
                    || required.Amount <= 0 || consumed.ResourceDefinitionId != required.ResourceDefinitionId || consumed.Amount < 0 || consumed.Amount > required.Amount) throw Invalid();
                previous = required.ResourceDefinitionId;
            }
        }
    }
}
