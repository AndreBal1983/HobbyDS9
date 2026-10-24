#nullable enable
using System;
using System.IO;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec : IStaticAstronomyReviewHostCodec, IStaticAstronomyOwnedExportValidator
    {
        // Used only after the host's first closed-codec copy, while its gate is held.
        // Load still validates frozen geometry, identity, replay and exact equality;
        // its detached candidate is deliberately discarded before any slot write.
        public void ValidateOwnedStaticAstronomyExport(CampaignPlayableFleetPackageDto template, CampaignGeneratedSaveDto ownedRoot)
            =>CampaignStaticAstronomyApplication.LoadSave(template,ownedRoot);
        public CampaignGeneratedMapDto CopyKnownGeneratedMap(CampaignGeneratedMapDto map)=>Decode<CampaignGeneratedMapDto>(Encode(map));
        // Shape admission only. Full map identity, scenario and operation replay
        // validation belongs to CampaignStaticAstronomyApplication before publication.
        public CampaignGeneratedSaveDto CopyStaticAstronomySave(CampaignGeneratedSaveDto root)
            =>DecodeStaticAstronomyCampaignRoot(EncodeStaticAstronomyCampaignRoot(root));
        public byte[] EncodeStaticAstronomyCampaignRoot(CampaignGeneratedSaveDto root)
        {
            RequireStaticAstronomyCarrier(root);var bytes=Encode(root);
            if(bytes.Length>ReviewGeneratedCampaignSlotStore.MaximumBytes)throw Invalid();
            return bytes;
        }
        public CampaignGeneratedSaveDto DecodeStaticAstronomyCampaignRoot(byte[] bytes)
        {
            if(bytes==null||bytes.Length<1||bytes.Length>ReviewGeneratedCampaignSlotStore.MaximumBytes)throw Invalid();
            var root=Decode<CampaignGeneratedSaveDto>(bytes);RequireStaticAstronomyCarrier(root);return root;
        }
        private static void RequireStaticAstronomyCarrier(CampaignGeneratedSaveDto? root)
        {
            bool Hash(string? s)=>s!=null&&s.Length==64&&s.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
            if(root==null||root.FormatVersion!=CampaignStaticAstronomyIdentity.Format||root.ProfileId!=CampaignStaticAstronomyIdentity.Profile
                ||root.PolicyId!=CampaignStaticAstronomyIdentity.Policy||!Hash(root.TemplatePackageHash)||!Hash(root.PackageHash)
                ||root.Map==null||root.Map.FormatVersion!=4||!Hash(root.Map.ContentHash)||root.Map.Settings==null||root.Map.Layout==null
                ||root.Map.Features==null||root.Map.RegionInfluences==null||root.Discoveries==null||root.Discoveries.Any(x=>x==null)
                ||root.Campaign==null||root.Campaign.FormatVersion!=CampaignPlayableFleetIdentity.Format
                ||root.Campaign.ProfileId!=CampaignPlayableFleetIdentity.Profile||!Hash(root.Campaign.PackageHash)
                ||root.Campaign.Transcript==null||root.Campaign.Names==null||root.Campaign.Integrated?.Physical?.Core==null
                ||root.Campaign.Integrated.Physical.Core.ProfileId!=CampaignStaticAstronomyIdentity.EmbeddedCoreProfile)
                throw new InvalidDataException("Invalid generated campaign data.");
        }
    }
}

