#nullable enable
using System;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec : IBackgroundPublishedCheckpointCodec
    {
        public CampaignPublishedConstructionPackageDto CopyPackage(CampaignPublishedConstructionPackageDto package)=>Decode<CampaignPublishedConstructionPackageDto>(Encode(package));
        public CampaignPublishedConstructionSaveDto CopySave(CampaignPublishedConstructionSaveDto root)=>DecodePublishedConstructionRoot(EncodePublishedConstructionRoot(root));
        public TacticalSupportCatalogueDto DecodePublishedSupport(byte[] bytes)=>Decode<TacticalSupportCatalogueDto>(bytes);
        public byte[] EncodePublishedConstructionRoot(CampaignPublishedConstructionSaveDto root)
        {RequirePublishedCarrier(root);var bytes=Encode(root);if(bytes.Length>ReviewPublishedConstructionSlotStore.MaximumBytes)throw Invalid();return bytes;}
        public CampaignPublishedConstructionSaveDto DecodePublishedConstructionRoot(byte[] bytes)
        {if(bytes==null||bytes.Length==0||bytes.Length>ReviewPublishedConstructionSlotStore.MaximumBytes)throw Invalid();var root=Decode<CampaignPublishedConstructionSaveDto>(bytes);RequirePublishedCarrier(root);return root;}
        private static void RequirePublishedCarrier(CampaignPublishedConstructionSaveDto? root)
        {
            bool Hash(string? s)=>s!=null&&s.Length==64&&s.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
            if(root==null||root.FormatVersion!=CampaignPublishedConstructionIdentity.Format||root.ProfileId!=CampaignPublishedConstructionIdentity.Profile
                ||root.PolicyId!=CampaignPublishedConstructionIdentity.Policy||!Hash(root.TemplatePackageHash)||!Hash(root.PackageHash)
                ||root.SupportIdentity==null||!Hash(root.SupportIdentity.SupportHash)||root.Map==null||root.Map.FormatVersion!=4||!Hash(root.Map.ContentHash)
                ||root.Map.Settings==null||root.Map.Layout==null||root.Map.Features==null||root.Map.RegionInfluences==null
                ||root.Discoveries==null||root.Discoveries.Any(r=>r==null)||root.PublishedBuiltShips==null||root.PublishedBuiltShips.Any(r=>r==null)
                ||root.Campaign==null||root.Campaign.FormatVersion!=CampaignPlayableFleetIdentity.Format||root.Campaign.ProfileId!=CampaignPlayableFleetIdentity.Profile
                ||!Hash(root.Campaign.PackageHash)||root.Campaign.Transcript==null||root.Campaign.Names==null
                ||root.Campaign.Integrated?.Physical?.Core==null||root.Campaign.Integrated.Physical.Core.ProfileId!=CampaignPublishedConstructionIdentity.EmbeddedCoreProfile)
                throw new InvalidDataException("Invalid published-construction carrier.");
            // Complete external-package identity and authoritative replay are host duties.
        }
    }
}
