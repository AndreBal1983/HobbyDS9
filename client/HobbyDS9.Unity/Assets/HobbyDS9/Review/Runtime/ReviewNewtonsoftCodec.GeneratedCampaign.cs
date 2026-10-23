#nullable enable
using System;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec
    {
        // Shape admission only. Full map identity, scenario and operation replay
        // validation belongs to CampaignGeneratedApplication before publication.
        public CampaignGeneratedSaveDto CopySave(CampaignGeneratedSaveDto root)
            =>DecodeGeneratedCampaignRoot(EncodeGeneratedCampaignRoot(root));
        public byte[] EncodeGeneratedCampaignRoot(CampaignGeneratedSaveDto root)
        {
            RequireGeneratedCarrier(root);var bytes=Encode(root);
            if(bytes.Length>ReviewGeneratedCampaignSlotStore.MaximumBytes)throw Invalid();
            return bytes;
        }
        public CampaignGeneratedSaveDto DecodeGeneratedCampaignRoot(byte[] bytes)
        {
            if(bytes==null||bytes.Length<1||bytes.Length>ReviewGeneratedCampaignSlotStore.MaximumBytes)throw Invalid();
            var root=Decode<CampaignGeneratedSaveDto>(bytes);RequireGeneratedCarrier(root);return root;
        }
        private static void RequireGeneratedCarrier(CampaignGeneratedSaveDto? root)
        {
            bool Hash(string? s)=>s!=null&&s.Length==64&&s.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
            if(root==null||root.FormatVersion!=CampaignGeneratedIdentity.Format||root.ProfileId!=CampaignGeneratedIdentity.Profile
                ||root.PolicyId!=CampaignGeneratedIdentity.Policy||!Hash(root.TemplatePackageHash)||!Hash(root.PackageHash)
                ||root.Map==null||root.Map.FormatVersion!=4||!Hash(root.Map.ContentHash)||root.Map.Settings==null||root.Map.Layout==null
                ||root.Map.Features==null||root.Map.RegionInfluences==null||root.Discoveries==null||root.Discoveries.Any(x=>x==null)
                ||root.Campaign==null||root.Campaign.FormatVersion!=CampaignPlayableFleetIdentity.Format
                ||root.Campaign.ProfileId!=CampaignPlayableFleetIdentity.Profile||!Hash(root.Campaign.PackageHash)
                ||root.Campaign.Transcript==null||root.Campaign.Names==null||root.Campaign.Integrated?.Physical?.Core==null
                ||root.Campaign.Integrated.Physical.Core.ProfileId!=CampaignGeneratedIdentity.EmbeddedCoreProfile)
                throw new InvalidDataException("Invalid generated campaign data.");
        }
    }
}
