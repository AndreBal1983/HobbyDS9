using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
namespace HobbyDS9.Application
{
    public interface ICampaignSingleplayerDemoSession : ICampaignIntegratedPlaytestSession
    {
        CampaignDemoSnapshot CaptureDemo();
        CampaignSingleplayerDemoSaveDto ExportDemoSave();
    }
    public sealed class CampaignSingleplayerDemoApplication : CampaignIntegratedPlaytestApplication,ICampaignSingleplayerDemoSession
    {
        private CampaignSingleplayerDemoApplication(CampaignTacticalReviewSession session):base(session){}
        public CampaignDemoSnapshot CaptureDemo()=>session.CaptureDemo();
        public CampaignSingleplayerDemoSaveDto ExportDemoSave()=>session.ExportDemoSave();
        public static ICampaignSingleplayerDemoSession StartPackage(CampaignSingleplayerDemoPackageDto p)
            =>new CampaignSingleplayerDemoApplication(CampaignTacticalReviewSession.StartDemo(p));
        public static ICampaignSingleplayerDemoSession LoadSave(CampaignSingleplayerDemoPackageDto p,CampaignSingleplayerDemoSaveDto s)
            =>new CampaignSingleplayerDemoApplication(CampaignTacticalReviewSession.LoadDemo(p,s));
    }
}
