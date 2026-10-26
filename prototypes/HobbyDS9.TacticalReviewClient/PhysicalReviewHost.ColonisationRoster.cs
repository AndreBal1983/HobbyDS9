using System;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.TacticalReviewClient
{
    public interface IColonisationRosterReviewHostCodec:IColonisationReviewHostCodec
    {CampaignColonisationRosterPackageDto CopyRosterPackage(CampaignColonisationRosterPackageDto package);}
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignColonisationRosterPackageDto? colonisationRosterPackage;
        private PhysicalReviewHost(CampaignColonisationRosterPackageDto package,IColonisationRosterReviewHostCodec codec,
            IColonisationCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignColonisationSaveDto? saved)
        {
            this.codec=codec;colonisationCodec=codec;colonisationStore=store;colonisationRosterPackage=codec.CopyRosterPackage(package);
            colonisationPackage=colonisationRosterPackage.BaseV1;
            session=saved==null?CampaignColonisationApplication.StartRosterGame(colonisationRosterPackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignColonisationApplication.LoadRosterSave(colonisationRosterPackage,codec.CopySave(saved));
        }
        public static PhysicalReviewHost StartColonisationRoster(CampaignColonisationRosterPackageDto package,NaturalGalaxySettingsDto settings,
            IColonisationRosterReviewHostCodec codec,IColonisationCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenColonisationRosterFromSlot(CampaignColonisationRosterPackageDto package,
            IColonisationRosterReviewHostCodec codec,IColonisationCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CaptureColonisationCandidate((ICampaignColonisationSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private ICampaignColonisationSession LoadColonisationSource(CampaignColonisationSaveDto root)
            =>colonisationRosterPackage==null?CampaignColonisationApplication.LoadSave(colonisationPackage!,root)
                :CampaignColonisationApplication.LoadRosterSave(colonisationRosterPackage,root);
        private PhysicalHostOpenResult OpenColonisationSource(string slot,string actor,string? encounter=null)
            =>colonisationRosterPackage==null?OpenColonisationFromSlot(colonisationPackage!,colonisationCodec!,colonisationStore!,slot,actor,encounter)
                :OpenColonisationRosterFromSlot(colonisationRosterPackage,(IColonisationRosterReviewHostCodec)colonisationCodec!,colonisationStore!,slot,actor,encounter);
    }
}
