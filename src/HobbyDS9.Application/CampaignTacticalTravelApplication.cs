using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignTacticalTravelSession
    {
        CampaignTacticalTravelSaveDto ExportSave();
        CampaignTacticalReviewStatus CaptureStatus();
        IReadOnlyList<FleetSnapshot> GetOwnStrategicFleets(string actor);
        IReadOnlyList<CampaignTacticalFleetCloakSetting> GetFleetCloakSettings(string actor);
        IReadOnlyList<CampaignTacticalOwnShip> InspectOwnShips(string actor);
        IReadOnlyList<string> GetEncounters(string actor);
        CampaignTacticalObservation Observe(string encounter,string actor);
        IReadOnlyList<CampaignKnownContact> GetKnownContacts(string actor);
        IReadOnlyList<CampaignOwnTravelReceipt> GetOwnTravelReceipts(string actor);
        IReadOnlyList<CampaignOwnTravelOrder> GetOwnTravelOrders(string actor);
        StrategicRouteSnapshot FindKnownRoute(string actor,string origin,string destination);
        CampaignTacticalMutationResult SubmitTravelOrder(SubmitCampaignTravelOrderDto? command);
        CampaignTacticalMutationResult SetApproachCloak(string actor,string command,string fleet,bool enabled,int turn,long revision);
        CampaignTacticalMutationResult SubmitStrategicCommand(CommandEnvelope? command);
        CampaignTacticalMutationResult AdvanceTurn(int turn,long revision);
        CampaignTacticalMutationResult CommitRound(CommitCampaignTacticalRoundDto? command);
        CampaignTacticalMutationResult FinalizeBatch(int turn,long revision);
    }
    public sealed class CampaignTacticalTravelApplication : ICampaignTacticalTravelSession
    {
        private readonly CampaignTacticalReviewSession session;
        private CampaignTacticalTravelApplication(CampaignTacticalReviewSession session){this.session=session;}
        public static ICampaignTacticalTravelSession StartReviewPackage(CampaignTacticalTravelPackageDto p)
            =>new CampaignTacticalTravelApplication(CampaignTacticalReviewSession.StartTravel(p));
        public static ICampaignTacticalTravelSession LoadReviewSave(CampaignTacticalTravelPackageDto p,CampaignTacticalTravelSaveDto s)
            =>new CampaignTacticalTravelApplication(CampaignTacticalReviewSession.LoadTravel(p,s));
        public CampaignTacticalTravelSaveDto ExportSave()=>session.ExportTravelSave();
        public CampaignTacticalReviewStatus CaptureStatus()=>session.CaptureStatus();
        public IReadOnlyList<FleetSnapshot> GetOwnStrategicFleets(string actor)=>session.OwnStrategicFleets(actor);
        public IReadOnlyList<CampaignTacticalFleetCloakSetting> GetFleetCloakSettings(string actor)=>session.FleetCloakSettings(actor);
        public IReadOnlyList<CampaignTacticalOwnShip> InspectOwnShips(string actor)=>session.InspectOwnShips(actor);
        public IReadOnlyList<string> GetEncounters(string actor)=>session.Encounters(actor);
        public CampaignTacticalObservation Observe(string encounter,string actor)=>session.Observe(encounter,actor);
        public IReadOnlyList<CampaignKnownContact> GetKnownContacts(string actor)=>session.KnownContacts(actor);
        public IReadOnlyList<CampaignOwnTravelReceipt> GetOwnTravelReceipts(string actor)=>session.OwnTravelReceipts(actor);
        public IReadOnlyList<CampaignOwnTravelOrder> GetOwnTravelOrders(string actor)=>session.OwnTravelOrders(actor);
        public StrategicRouteSnapshot FindKnownRoute(string actor,string origin,string destination)=>session.FindKnownTravelRoute(actor,origin,destination);
        public CampaignTacticalMutationResult SubmitTravelOrder(SubmitCampaignTravelOrderDto? command)=>session.SubmitTravelOrder(command);
        public CampaignTacticalMutationResult SetApproachCloak(string actor,string command,string fleet,bool enabled,int turn,long revision)=>session.SetApproachCloak(actor,command,fleet,enabled,turn,revision);
        public CampaignTacticalMutationResult SubmitStrategicCommand(CommandEnvelope? command)=>session.SubmitStrategicCommand(command);
        public CampaignTacticalMutationResult AdvanceTurn(int turn,long revision)=>session.AdvanceTurn(turn,revision);
        public CampaignTacticalMutationResult CommitRound(CommitCampaignTacticalRoundDto? command)=>session.CommitRound(command);
        public CampaignTacticalMutationResult FinalizeBatch(int turn,long revision)=>session.FinalizeBatch(turn,revision);
    }
}
