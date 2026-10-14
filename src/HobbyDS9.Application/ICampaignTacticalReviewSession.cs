using System.Collections.Generic;
using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    // Deliberately separate opt-in review contract; no default/client activation.
    public interface ICampaignTacticalReviewSession
    {
        IReadOnlyList<FleetSnapshot> GetOwnStrategicFleets(string actor);
        CampaignTacticalReviewStatus CaptureStatus();
        IReadOnlyList<CampaignTacticalFleetCloakSetting> GetFleetCloakSettings(string actor);
        CampaignTacticalReviewSaveDto ExportSave();
        IReadOnlyList<string> GetEncounters(string actor);
        IReadOnlyList<CampaignTacticalOwnShip> InspectOwnShips(string actor);
        CampaignTacticalObservation Observe(string encounter, string actor);
        CampaignTacticalMutationResult SetApproachCloak(string actor, string commandId, string fleet, bool enabled, int turn, long revision);
        CampaignTacticalMutationResult SubmitStrategicCommand(CommandEnvelope? command);
        CampaignTacticalMutationResult AdvanceTurn(int turn, long revision);
        CampaignTacticalMutationResult CommitRound(CommitCampaignTacticalRoundDto? command);
        CampaignTacticalMutationResult FinalizeBatch(int turn, long revision);
    }
}
