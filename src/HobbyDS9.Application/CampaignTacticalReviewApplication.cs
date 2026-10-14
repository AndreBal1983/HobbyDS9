using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public sealed class CampaignTacticalReviewApplication : ICampaignTacticalReviewSession
    {
        private readonly CampaignTacticalReviewSession session;
        private CampaignTacticalReviewApplication(CampaignTacticalReviewSession session) { this.session = session; }
        public static ICampaignTacticalReviewSession StartReviewPackage(CampaignTacticalReviewPackageDto package)
            => new CampaignTacticalReviewApplication(CampaignTacticalReviewSession.Start(package));
        public static ICampaignTacticalReviewSession LoadReviewSave(CampaignTacticalReviewPackageDto package, CampaignTacticalReviewSaveDto save)
            => new CampaignTacticalReviewApplication(CampaignTacticalReviewSession.Load(package, save));
        public IReadOnlyList<FleetSnapshot> GetOwnStrategicFleets(string actor) => session.OwnStrategicFleets(actor);
        public CampaignTacticalReviewStatus CaptureStatus() => session.CaptureStatus();
        public IReadOnlyList<CampaignTacticalFleetCloakSetting> GetFleetCloakSettings(string actor) => session.FleetCloakSettings(actor);
        public CampaignTacticalReviewSaveDto ExportSave() => session.ExportSave();
        public IReadOnlyList<string> GetEncounters(string actor) => session.Encounters(actor);
        public IReadOnlyList<CampaignTacticalOwnShip> InspectOwnShips(string actor) => session.InspectOwnShips(actor);
        public CampaignTacticalObservation Observe(string encounter, string actor) => session.Observe(encounter, actor);
        public CampaignTacticalMutationResult SetApproachCloak(string actor, string commandId, string fleet, bool enabled, int turn, long revision)
            => session.SetApproachCloak(actor, commandId, fleet, enabled, turn, revision);
        public CampaignTacticalMutationResult SubmitStrategicCommand(CommandEnvelope? command) => session.SubmitStrategicCommand(command);
        public CampaignTacticalMutationResult AdvanceTurn(int turn, long revision) => session.AdvanceTurn(turn, revision);
        public CampaignTacticalMutationResult CommitRound(CommitCampaignTacticalRoundDto? command) => session.CommitRound(command);
        public CampaignTacticalMutationResult FinalizeBatch(int turn, long revision) => session.FinalizeBatch(turn, revision);
    }
}
