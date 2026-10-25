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
    // Closed wire-carrier admission only. The host must replay the approved
    // package through Application before replacing a session or accepting a save.
    public sealed partial class ReviewNewtonsoftCodec : IBackgroundActivitiesCheckpointCodec
    {
        private const int ActivityCarrierRows = 65536;
        public CampaignActivitiesPackageDto CopyPackage(CampaignActivitiesPackageDto package)
            => Decode<CampaignActivitiesPackageDto>(Encode(package));
        public CampaignActivitiesSaveDto CopySave(CampaignActivitiesSaveDto root) => CopyActivitiesRoot(root);
        public CampaignActivitiesSaveDto CopyActivitiesRoot(CampaignActivitiesSaveDto root)
            => DecodeActivitiesRoot(EncodeActivitiesRoot(root));
        public CampaignActivityContentDto DecodeActivityContent(byte[] bytes) => DecodeActivityResource<CampaignActivityContentDto>(bytes);
        public ShipDesignCatalogueDto DecodeActivityCatalogue(byte[] bytes) => DecodeActivityResource<ShipDesignCatalogueDto>(bytes);
        public ActivityTacticalSupportCatalogueDto DecodeActivitySupport(byte[] bytes) => DecodeActivityResource<ActivityTacticalSupportCatalogueDto>(bytes);
        public List<ShipClassDraftDto> DecodeActivityReadyClasses(byte[] bytes) => DecodeActivityResource<List<ShipClassDraftDto>>(bytes);

        private static T DecodeActivityResource<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > 4 * 1024 * 1024) throw Invalid();
            return Decode<T>(bytes);
        }
        public byte[] EncodeActivitiesRoot(CampaignActivitiesSaveDto root)
        {
            RequireActivitiesCarrier(root);
            var bytes = Encode(root);
            if (bytes.Length == 0 || bytes.Length > ReviewActivitiesSlotStore.MaximumBytes) throw Invalid();
            return bytes;
        }
        public CampaignActivitiesSaveDto DecodeActivitiesRoot(byte[] bytes)
        {
            try
            {
                var wire = GeneratedRoot(bytes, ReviewActivitiesSlotStore.MaximumBytes);
                ActivitiesObject(wire, typeof(CampaignActivitiesSaveDto));
                // New required lists may have DTO initializers: require their actual
                // wire presence so omission never silently becomes an empty history.
                var state = wire["Activities"];
                ActivitiesObject(state, typeof(CampaignActivityStateDto));
                ActivityWireRows(state!, "Production", typeof(ActivityProductionItemDto));
                foreach (var item in GeneratedRows(state!["Production"], ActivityCarrierRows))
                    ActivitiesObject(item["Paid"], typeof(ProductionQueueItemDto));
                ActivityWireRows(state!, "Structures", typeof(ActivityStructureReceiptDto));
                ActivityWireRows(state!, "Sites", typeof(ActivitySiteStateDto));
                ActivityWireRows(state!, "Minors", typeof(ActivityMinorStateDto));
                ActivityWireRows(state!, "Bodies", typeof(ActivityBodyDto));
                ActivityWireRows(state!, "Relations", typeof(ActivityMinorRelationDto));
                ActivityWireRows(state!, "Observations", typeof(ActivitySiteObservationDto));
                foreach (var finding in GeneratedRows(state!["Findings"], ActivityCarrierRows))
                {
                    ActivitiesObject(finding, typeof(ActivityFindingDto));
                    foreach (var receipt in GeneratedRows(finding["ResearchReceipts"], 256))
                        ActivitiesObject(receipt, typeof(ResearchReceiptDto));
                }
                foreach (var operation in GeneratedRows(wire["Journal"], ActivityCarrierRows))
                {
                    ActivitiesObject(operation, typeof(CampaignActivityOperationDto));
                    bool activity = operation["Activity"]?.Type != JTokenType.Null;
                    bool common = operation["Common"]?.Type != JTokenType.Null;
                    if (activity == common) throw Invalid();
                    if (activity) ActivitiesObject(operation["Activity"], typeof(CampaignActivityCommandDto));
                    else
                    {
                        var entry = operation["Common"]!;
                        ActivitiesObject(entry, typeof(CampaignPlayableOperationDto));
                        if ((entry["Common"]!.Type == JTokenType.Null) == (entry["Organization"]!.Type == JTokenType.Null)) throw Invalid();
                    }
                }
                // Preserve the established strict Natural wire shape. Geometry and
                // content identity are still validated by authoritative full replay.
                DecodeNaturalGalaxy(Utf8.GetBytes(wire["Map"]!.ToString(Formatting.None)));
                var root = Decode<CampaignActivitiesSaveDto>(bytes);
                RequireActivitiesCarrier(root);
                return root;
            }
            catch (Exception) { throw new InvalidDataException("Invalid activity campaign carrier."); }
        }
        private static void ActivitiesObject(JToken? token, Type type)
            => GeneratedObject(token, type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite).Select(p => p.Name).ToArray());
        private static void ActivityWireRows(JToken state, string field, Type row)
        {
            foreach (var token in GeneratedRows(state[field], ActivityCarrierRows)) ActivitiesObject(token, row);
        }
        private static bool ActivityRows<T>(List<T>? rows) where T : class
            => rows != null && rows.Count <= ActivityCarrierRows && rows.All(row => row != null);
        private static bool ActivityText(string? value, int maximum = 128)
            => value != null && value.Length > 0 && value.Length <= maximum;
        private static void RequireActivitiesCarrier(CampaignActivitiesSaveDto? root)
        {
            bool Hash(string? value) => value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
            if (root == null || root.FormatVersion != CampaignActivitiesIdentity.Format
                || root.ProfileId != CampaignActivitiesIdentity.Profile || root.PolicyId != CampaignActivitiesIdentity.Policy
                || !Hash(root.TemplatePackageHash) || !Hash(root.PackageHash) || !Hash(root.SupportHash)
                || root.Map == null || root.Map.FormatVersion != NaturalGalaxyIdentity.FormatVersion || !Hash(root.Map.ContentHash)
                || root.Map.Settings == null || root.Map.Tuning == null || root.Map.Layout == null || root.Map.Features == null
                || root.Map.StarGroups == null || root.Map.RegionInfluences == null
                || !ActivityRows(root.Discoveries) || !ActivityRows(root.PublishedBuiltShips) || !ActivityRows(root.Journal)
                || root.Campaign == null || root.Campaign.FormatVersion != CampaignPlayableFleetIdentity.Format
                || root.Campaign.ProfileId != CampaignPlayableFleetIdentity.Profile || !Hash(root.Campaign.PackageHash)
                || !ActivityRows(root.Campaign.Transcript) || !ActivityRows(root.Campaign.Names)
                || root.Campaign.Integrated?.Physical?.Core == null
                || root.Campaign.Integrated.Physical.Core.ProfileId != CampaignActivitiesIdentity.EmbeddedCoreProfile
                || root.Campaign.Integrated.Physical.Core.Campaign?.GameState == null || root.Activities == null)
                throw Invalid();
            var state = root.Activities;
            if (!ActivityRows(state.Production) || !ActivityRows(state.Structures) || !ActivityRows(state.Sites)
                || !ActivityRows(state.Minors) || !ActivityRows(state.Findings) || !ActivityRows(state.Bodies)
                || !ActivityRows(state.Relations) || !ActivityRows(state.Observations)) throw Invalid();
            foreach (var row in state.Production!)
                if (!ActivityText(row.ColonyId) || !ActivityText(row.DefinitionId) || row.Paid == null
                    || !ActivityText(row.Paid.QueueItemId) || !ActivityRows(row.Paid.PaidResources)
                    || !Enum.IsDefined(typeof(ActivityProductionKind), row.Kind)) throw Invalid();
            foreach (var row in state.Structures!)
                if (!ActivityText(row.QueueItemId) || !ActivityText(row.ColonyId) || !ActivityText(row.DefinitionId) || !ActivityText(row.OwnerId)) throw Invalid();
            foreach (var row in state.Sites!)
                if (!ActivityText(row.SiteId) || !ActivityText(row.DefinitionId) || !ActivityText(row.NodeId) || !ActivityText(row.SystemId)
                    || !ActivityText(row.BodyId) || row.MinorActorId != null && !ActivityText(row.MinorActorId)) throw Invalid();
            foreach (var row in state.Minors!)
                if (!ActivityText(row.ActorId) || !ActivityText(row.DefinitionId) || !ActivityText(row.BodyId) || !ActivityText(row.SpeciesId)) throw Invalid();
            foreach (var row in state.Bodies!)
                if (!ActivityText(row.BodyId) || !ActivityText(row.SystemId) || !ActivityText(row.DisplayName, 256)
                    || row.GovernmentActorId != null && !ActivityText(row.GovernmentActorId)) throw Invalid();
            foreach (var row in state.Relations!)
                if (!ActivityText(row.ActorId) || !ActivityText(row.MinorActorId) || !Enum.IsDefined(typeof(ActivityMinorRelation), row.Relation)) throw Invalid();
            foreach (var row in state.Observations!)
                if (!ActivityText(row.ActorId) || !ActivityText(row.SiteId)) throw Invalid();
            foreach (var row in state.Findings!)
                if (!ActivityText(row.ActorId) || !ActivityText(row.SiteId) || !ActivityText(row.ChoiceId)
                    || !ActivityText(row.FindingId) || !ActivityText(row.ShipId) || !ActivityText(row.FleetId)
                    || row.Report == null || row.Report.Length > 8192 || row.ContactOutcome == null || row.ContactOutcome.Length > 4096
                    || !ActivityRows(row.ResearchReceipts) || row.ResearchReceipts!.Count > 256
                    || !Enum.IsDefined(typeof(ActivityProgressStage), row.Stage)) throw Invalid();
            foreach (var finding in state.Findings!)
                foreach (var receipt in finding.ResearchReceipts!)
                    if (!ActivityText(receipt.CivilizationId) || !ActivityText(receipt.Category)
                        || receipt.ApplicationDefinitionId != null && !ActivityText(receipt.ApplicationDefinitionId)
                        || receipt.Discipline != null && !Enum.IsDefined(typeof(ScienceDiscipline), receipt.Discipline.Value)) throw Invalid();
            foreach (var operation in root.Journal!)
            {
                if ((operation.Activity == null) == (operation.Common == null)) throw Invalid();
                if (operation.Common != null && (operation.Common.Common == null) == (operation.Common.Organization == null)) throw Invalid();
                if (operation.Activity != null)
                {
                    var command = operation.Activity;
                    if (!ActivityText(command.ActorId) || !ActivityText(command.CommandId) || !ActivityText(command.TargetId)
                        || command.DefinitionId == null || command.ShipId == null
                        || !Enum.IsDefined(typeof(CampaignActivityCommandKind), command.Kind)) throw Invalid();
                }
            }
        }
    }
}
