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
            IColonisationCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignColonisationSaveDto? saved,ReviewLoadDiagnostic? diagnostic=null)
        {
            this.codec=codec;colonisationCodec=codec;colonisationStore=store;
            diagnostic?.Record(ReviewLoadPhase.RosterCopy,ReviewLoadEvent.Begin);
            colonisationRosterPackage=codec.CopyRosterPackage(package);
            diagnostic?.Record(ReviewLoadPhase.RosterCopy,ReviewLoadEvent.NormalReturn);
            colonisationPackage=colonisationRosterPackage.BaseV1;
            if(saved==null)session=CampaignColonisationApplication.StartRosterGame(colonisationRosterPackage,settings??throw new ArgumentNullException(nameof(settings)));
            else
            {
                diagnostic?.Record(ReviewLoadPhase.SaveCopy,ReviewLoadEvent.Begin);
                var copied=codec.CopySave(saved);
                diagnostic?.Record(ReviewLoadPhase.SaveCopy,ReviewLoadEvent.NormalReturn);
                diagnostic?.Record(ReviewLoadPhase.FullReplay,ReviewLoadEvent.Begin);
                session=CampaignColonisationApplication.LoadRosterSave(colonisationRosterPackage,copied);
                diagnostic?.Record(ReviewLoadPhase.FullReplay,ReviewLoadEvent.NormalReturn);
            }
        }
        public static PhysicalReviewHost StartColonisationRoster(CampaignColonisationRosterPackageDto package,NaturalGalaxySettingsDto settings,
            IColonisationRosterReviewHostCodec codec,IColonisationCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenColonisationRosterFromSlot(CampaignColonisationRosterPackageDto package,
            IColonisationRosterReviewHostCodec codec,IColonisationCheckpointStore store,string slot,string actor,string? encounter=null,ReviewLoadDiagnostic? diagnostic=null)
        {
            try{
                diagnostic?.Record(ReviewLoadPhase.StoreRead,ReviewLoadEvent.Begin);
                var saved=store.Read(slot);
                diagnostic?.Record(ReviewLoadPhase.StoreRead,ReviewLoadEvent.NormalReturn);
                var candidate=new PhysicalReviewHost(package,codec,store,null,saved,diagnostic);
                diagnostic?.Record(ReviewLoadPhase.ReadFrame,ReviewLoadEvent.Begin);
                var frame=candidate.Read(actor,encounter);
                diagnostic?.Record(ReviewLoadPhase.ReadFrame,ReviewLoadEvent.NormalReturn);
                diagnostic?.Record(ReviewLoadPhase.Presentation,ReviewLoadEvent.Begin);
                candidate.CaptureColonisationCandidate((ICampaignColonisationSession)candidate.session,frame);
                diagnostic?.Record(ReviewLoadPhase.Presentation,ReviewLoadEvent.NormalReturn);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){diagnostic?.Record(ReviewLoadPhase.HostOpen,ReviewLoadEvent.Error,e,e.Error);return new PhysicalHostOpenResult(e.Error);}
            catch(Exception e){diagnostic?.Record(ReviewLoadPhase.HostOpen,ReviewLoadEvent.Error,e,ReviewCheckpointError.InvalidCheckpoint);return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private ICampaignColonisationSession LoadColonisationSource(CampaignColonisationSaveDto root)
            =>colonisationRosterPackage==null?CampaignColonisationApplication.LoadSave(colonisationPackage!,root)
                :CampaignColonisationApplication.LoadRosterSave(colonisationRosterPackage,root);
        private PhysicalHostOpenResult OpenColonisationSource(string slot,string actor,string? encounter=null)
            =>colonisationRosterPackage==null?OpenColonisationFromSlot(colonisationPackage!,colonisationCodec!,colonisationStore!,slot,actor,encounter)
                :OpenColonisationRosterFromSlot(colonisationRosterPackage,(IColonisationRosterReviewHostCodec)colonisationCodec!,colonisationStore!,slot,actor,encounter);
    }
}
