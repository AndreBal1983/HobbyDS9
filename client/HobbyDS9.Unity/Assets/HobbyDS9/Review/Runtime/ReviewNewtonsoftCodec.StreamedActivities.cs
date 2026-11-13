#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec : IBackgroundStreamedActivitiesCheckpointCodec
    {
        public CampaignStreamedActivitiesPackageDto CopyPackage(CampaignStreamedActivitiesPackageDto package)
            => Decode<CampaignStreamedActivitiesPackageDto>(Encode(package));
        public CampaignStreamedActivitiesSaveDto CopySave(CampaignStreamedActivitiesSaveDto root)
            => DecodeStreamedActivitiesRoot(EncodeStreamedActivitiesRoot(root));
        public byte[] EncodeStreamedActivitiesRoot(CampaignStreamedActivitiesSaveDto root)
        {
            RequireStreamedCarrier(root);
            var bytes = Encode(root);
            if (bytes.Length == 0 || bytes.Length > ReviewStreamedActivitiesSlotStore.MaximumBytes) throw Invalid();
            return bytes;
        }
        public CampaignStreamedActivitiesSaveDto DecodeStreamedActivitiesRoot(byte[] bytes)
        {
            try
            {
                var wire = GeneratedRoot(bytes, ReviewStreamedActivitiesSlotStore.MaximumBytes);
                ActivitiesObject(wire, typeof(CampaignStreamedActivitiesSaveDto));
                ActivityFactsWire(wire["Activities"], wire["Journal"]);
                foreach (var order in GeneratedRows(wire["Production"], ActivityCarrierRows)) StreamedOrderWire(order);
                foreach (var receipt in GeneratedRows(wire["ProductionHistory"], ActivityCarrierRows))
                {
                    ActivitiesObject(receipt, typeof(StreamedProductionReceiptDto));
                    StreamedOrderWire(receipt["Order"]);
                }
                DecodeNaturalGalaxy(Utf8.GetBytes(wire["Map"]!.ToString(Formatting.None)));
                var root = Decode<CampaignStreamedActivitiesSaveDto>(bytes);
                RequireStreamedCarrier(root);
                return root;
            }
            catch (Exception) { throw new InvalidDataException("Invalid streamed activity campaign carrier."); }
        }
        private static void StreamedOrderWire(JToken? order)
        {
            ActivitiesObject(order, typeof(StreamedProductionItemDto));
            foreach (var resource in GeneratedRows(order!["RequiredResources"], ActivityCarrierRows)) ActivitiesObject(resource, typeof(ResourceAmountDto));
            foreach (var resource in GeneratedRows(order["ConsumedResources"], ActivityCarrierRows)) ActivitiesObject(resource, typeof(ResourceAmountDto));
            if (order["Blueprint"]!.Type != JTokenType.Null) ActivitiesObject(order["Blueprint"], typeof(BlueprintReferenceDto));
        }
        private static void RequireStreamedCarrier(CampaignStreamedActivitiesSaveDto? root)
        {
            bool Hash(string? value) => value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
            if (root == null || root.FormatVersion != CampaignStreamedActivitiesIdentity.Format
                || root.ProfileId != CampaignStreamedActivitiesIdentity.Profile || root.PolicyId != CampaignStreamedActivitiesIdentity.Policy
                || !Hash(root.TemplatePackageHash) || !Hash(root.PackageHash) || !Hash(root.SupportHash)
                || root.Map == null || root.Map.FormatVersion != NaturalGalaxyIdentity.FormatVersion || !Hash(root.Map.ContentHash)
                || root.Map.Settings == null || root.Map.Tuning == null || root.Map.Layout == null || root.Map.Features == null
                || root.Map.StarGroups == null || root.Map.RegionInfluences == null
                || !ActivityRows(root.Discoveries) || !ActivityRows(root.PublishedBuiltShips) || !ActivityRows(root.Journal)
                || !ActivityRows(root.Production) || !ActivityRows(root.ProductionHistory) || root.Activities == null
                || root.Activities.Production == null || root.Activities.Production.Count != 0
                || root.Campaign == null || root.Campaign.FormatVersion != CampaignPlayableFleetIdentity.Format
                || root.Campaign.ProfileId != CampaignPlayableFleetIdentity.Profile || !Hash(root.Campaign.PackageHash)
                || !ActivityRows(root.Campaign.Transcript) || !ActivityRows(root.Campaign.Names)
                || root.Campaign.Integrated?.Physical?.Core == null
                || root.Campaign.Integrated.Physical.Core.ProfileId != CampaignStreamedActivitiesIdentity.EmbeddedCoreProfile
                || root.Campaign.Integrated.Physical.Core.Campaign?.GameState == null) throw Invalid();
            RequireActivityFacts(root.Activities, root.Journal!);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var order in root.Production!)
            {
                RequireStreamedOrder(order, ids);
                if (order.CompletedIndustry >= order.RequiredIndustry) throw Invalid();
            }
            foreach (var receipt in root.ProductionHistory!)
            {
                if (receipt.Order == null || receipt.ResolvedTurn < 0) throw Invalid();
                var order = receipt.Order;
                RequireStreamedOrder(order, ids);
                if (receipt.Completed != (order.CompletedIndustry == order.RequiredIndustry)) throw Invalid();
                bool built = receipt.Completed && order.Kind == ActivityProductionKind.PublishedShip;
                if (built ? !ActivityText(receipt.ShipId) : receipt.ShipId != null) throw Invalid();
            }
        }
        private static void RequireStreamedOrder(StreamedProductionItemDto order, HashSet<string> ids)
        {
            if (!ActivityText(order.QueueItemId) || !ids.Add(order.QueueItemId) || !ActivityText(order.OwnerId)
                || !ActivityText(order.ColonyId) || !ActivityText(order.DefinitionId) || !Enum.IsDefined(typeof(ActivityProductionKind), order.Kind)
                || order.RequiredIndustry <= 0 || order.CompletedIndustry < 0 || order.CompletedIndustry > order.RequiredIndustry
                || !ActivityRows(order.RequiredResources) || !ActivityRows(order.ConsumedResources)
                || order.RequiredResources!.Count != order.ConsumedResources!.Count) throw Invalid();
            if (order.Kind == ActivityProductionKind.PublishedShip)
            {
                if (order.Blueprint == null || !ActivityText(order.Blueprint.BlueprintId) || order.Blueprint.Revision <= 0) throw Invalid();
            }
            else if (order.Blueprint != null) throw Invalid();
            string? previous = null;
            for (int i = 0; i < order.RequiredResources.Count; i++)
            {
                var required = order.RequiredResources[i]; var consumed = order.ConsumedResources[i];
                if (!ActivityText(required.ResourceDefinitionId) || previous != null && StringComparer.Ordinal.Compare(previous, required.ResourceDefinitionId) >= 0
                    || required.Amount <= 0 || consumed.ResourceDefinitionId != required.ResourceDefinitionId
                    || consumed.Amount < 0 || consumed.Amount > required.Amount) throw Invalid();
                previous = required.ResourceDefinitionId;
            }
            // Exact cumulative consumption, source costs, ownership and spawn proof
            // remain authoritative Application replay duties, not carrier authority.
        }
    }
}
