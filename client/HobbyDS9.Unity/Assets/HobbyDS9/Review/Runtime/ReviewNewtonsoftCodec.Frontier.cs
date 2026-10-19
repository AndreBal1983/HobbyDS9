#nullable enable
using System;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    // Opt-in wrapper5 adapter only. Existing2/3/4 overloads/settings remain independent.
    public sealed partial class ReviewNewtonsoftCodec : IFrontierReviewHostCodec
    {
        public CampaignFrontierPackageDto CopyPackage(CampaignFrontierPackageDto package)
            =>Decode<CampaignFrontierPackageDto>(Encode(package));
        public CampaignFrontierSaveDto CopySave(CampaignFrontierSaveDto root)
            =>DecodeFrontierRoot(EncodeFrontierRoot(root));
        public CampaignFrontierPackageDto DecodeFrontierProfile(byte[] bytes)=>Decode<CampaignFrontierPackageDto>(bytes);
        public byte[] EncodeFrontierRoot(CampaignFrontierSaveDto root)
        {RequireFrontierEnvelope(root);return Encode(root);}
        public CampaignFrontierSaveDto DecodeFrontierRoot(byte[] bytes)
        {
            try {var root=Decode<CampaignFrontierSaveDto>(bytes);RequireFrontierEnvelope(root);return root;}
            catch(Exception){throw new InvalidDataException("Invalid frontier review data.");}
        }
        private static void RequireFrontierEnvelope(CampaignFrontierSaveDto? root)
        {
            bool Hash(string? value)=>value!=null&&value.Length==64&&value.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
            // Envelope/required carriers only: domain rules and full accepted-history
            // replay stay in CampaignFrontierApplication, invoked by the existing host.
            if(root==null||root.FormatVersion!=CampaignFrontierIdentity.Format||root.ProfileId!=CampaignFrontierIdentity.Profile
                ||root.PolicyId!=CampaignFrontierIdentity.Policy||!Hash(root.PackageHash)||root.Transcript==null||root.Discoveries==null
                ||root.Transcript.Any(o=>o==null)||root.Discoveries.Any(r=>r==null||r.ChartedNodeIds==null||r.ChartedLaneIds==null)
                ||root.Integrated==null||root.Integrated.FormatVersion!=CampaignIntegratedPlaytestIdentity.SaveFormat
                ||root.Integrated.ProfileId!=CampaignIntegratedPlaytestIdentity.Profile||!Hash(root.Integrated.PackageHash)
                ||root.Integrated.BuiltShips==null||root.Integrated.Physical==null
                ||root.Integrated.Physical.FormatVersion!=CampaignTacticalTravelIdentity.SaveFormat
                ||root.Integrated.Physical.ProfileId!=CampaignTacticalTravelIdentity.Profile||!Hash(root.Integrated.Physical.PackageHash)
                ||root.Integrated.Physical.Core==null||root.Integrated.Physical.Core.ProfileId!=CampaignFrontierIdentity.EmbeddedCoreProfile
                ||root.Integrated.Physical.Core.FormatVersion!=CampaignTacticalReviewIdentity.SaveFormat||!Hash(root.Integrated.Physical.Core.PackageHash)
                ||root.Integrated.Physical.Core.Pools==null||root.Integrated.Physical.Core.FleetSettings==null
                ||root.Integrated.Physical.Core.Battles==null||root.Integrated.Physical.Core.AcceptedRoundCommands==null
                ||root.Integrated.Physical.Core.Campaign==null||root.Integrated.Physical.Core.Campaign.GameState==null
                ||root.Integrated.Physical.Orders==null||root.Integrated.Physical.Contacts==null||root.Integrated.Physical.LastMovement==null
                ||root.Integrated.Physical.Requests==null)
                throw new InvalidDataException("Invalid frontier review data.");
        }
    }
}
