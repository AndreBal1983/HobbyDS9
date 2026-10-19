using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignFrontierSession : ICampaignIntegratedPlaytestSession
    {
        CampaignDiscoveryOverview CaptureDiscovery(string actor);
        CampaignFrontierSaveDto ExportFrontierSave();
    }
    public sealed class CampaignFrontierApplication : CampaignIntegratedPlaytestApplication,ICampaignFrontierSession
    {
        private CampaignFrontierApplication(CampaignTacticalReviewSession backend):base(backend) { }
        public static ICampaignFrontierSession StartPackage(CampaignFrontierPackageDto package)
            =>new CampaignFrontierApplication(CampaignTacticalReviewSession.StartFrontier(package));
        public static ICampaignFrontierSession LoadSave(CampaignFrontierPackageDto package,CampaignFrontierSaveDto save)
            =>new CampaignFrontierApplication(CampaignTacticalReviewSession.LoadFrontier(package,save));
        public CampaignDiscoveryOverview CaptureDiscovery(string actor)=>session.CaptureDiscovery(actor);
        public CampaignFrontierSaveDto ExportFrontierSave()=>session.ExportFrontierSave();
    }
}
