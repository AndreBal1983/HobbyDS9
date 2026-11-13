#nullable enable
using System;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    // Opt-in wrapper6 adapter only. Existing2/3/4/5 overloads/settings remain independent.
    public sealed partial class ReviewNewtonsoftCodec : IPlayableFleetReviewHostCodec
    {
        public CampaignPlayableFleetPackageDto CopyPackage(CampaignPlayableFleetPackageDto package)
            =>Decode<CampaignPlayableFleetPackageDto>(Encode(package));
        public CampaignPlayableFleetSaveDto CopySave(CampaignPlayableFleetSaveDto root)
            =>DecodePlayableFleetRoot(EncodePlayableFleetRoot(root));
        public CampaignPlayableFleetPackageDto DecodePlayableFleetProfile(byte[] bytes)=>Decode<CampaignPlayableFleetPackageDto>(bytes);
        public byte[] EncodePlayableFleetRoot(CampaignPlayableFleetSaveDto root)
        {RequirePlayableFleetEnvelope(root);return Encode(root);}
        public CampaignPlayableFleetSaveDto DecodePlayableFleetRoot(byte[] bytes)
        {
            try {var root=Decode<CampaignPlayableFleetSaveDto>(bytes);RequirePlayableFleetEnvelope(root);return root;}
            catch(Exception){throw new InvalidDataException("Invalid playable fleet review data.");}
        }
        private static void RequirePlayableFleetEnvelope(CampaignPlayableFleetSaveDto? root)
        {
            bool Hash(string? value)=>value!=null&&value.Length==64&&value.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
            // Envelope/required carriers only: domain rules and full accepted-history
            // replay stay in CampaignPlayableFleetApplication, invoked by the existing host.
            if(root==null||root.FormatVersion!=CampaignPlayableFleetIdentity.Format||root.ProfileId!=CampaignPlayableFleetIdentity.Profile
                ||!Hash(root.PackageHash)||root.Transcript==null||root.Names==null
                ||root.Transcript.Any(o=>o==null)||root.Names.Any(n=>n==null)
                ||root.Integrated==null||root.Integrated.FormatVersion!=CampaignIntegratedPlaytestIdentity.SaveFormat
                ||root.Integrated.ProfileId!=CampaignIntegratedPlaytestIdentity.Profile||!Hash(root.Integrated.PackageHash)
                ||root.Integrated.BuiltShips==null||root.Integrated.Physical==null
                ||root.Integrated.Physical.FormatVersion!=CampaignTacticalTravelIdentity.SaveFormat
                ||root.Integrated.Physical.ProfileId!=CampaignTacticalTravelIdentity.Profile||!Hash(root.Integrated.Physical.PackageHash)
                ||root.Integrated.Physical.Core==null||root.Integrated.Physical.Core.ProfileId!=CampaignPlayableFleetIdentity.EmbeddedCoreProfile
                ||root.Integrated.Physical.Core.FormatVersion!=CampaignTacticalReviewIdentity.SaveFormat||!Hash(root.Integrated.Physical.Core.PackageHash)
                ||root.Integrated.Physical.Core.Pools==null||root.Integrated.Physical.Core.FleetSettings==null
                ||root.Integrated.Physical.Core.Battles==null||root.Integrated.Physical.Core.AcceptedRoundCommands==null
                ||root.Integrated.Physical.Core.Campaign==null||root.Integrated.Physical.Core.Campaign.GameState==null
                ||root.Integrated.Physical.Orders==null||root.Integrated.Physical.Contacts==null||root.Integrated.Physical.LastMovement==null
                ||root.Integrated.Physical.Requests==null)
                throw new InvalidDataException("Invalid playable fleet review data.");
        }
    }
}
