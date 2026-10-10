using System.Collections.Generic;
using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    // DTOs crossing this boundary are caller-owned copies. Snapshots are immutable.
    // Stamps always come from the client's observed state; never silently refreshed.
    public interface ICampaignSession
    {
        CampaignSnapshot CaptureSnapshot();
        CommandResult SubmitCommand(CommandEnvelope? command);
        CommandResult SubmitFleetRoute(EntityId commandId, EntityId actorCivilizationId, int expectedTurn,
            long expectedRevision, EntityId fleetId, IReadOnlyList<NavigationNodeId>? steps);
        NavigationRoute FindRoute(NavigationNodeId origin, NavigationNodeId destination);
        TurnResult AdvanceTurn(int expectedTurn, long expectedRevision);
        IReadOnlyList<CombatRequestDto> GetPendingCombatRequests();
        IReadOnlyList<ResolvedCombatDto> GetResolvedCombats();
        CombatResultDto ResolveCombat(CombatRequestDto request);
        CombatResultDto ResolveCombat(CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto> plans);
        CombatSubmissionResult SubmitCombatResult(CombatResultDto? result, int expectedTurn, long expectedRevision);
        TurnResult FinalizeCombat(int expectedTurn, long expectedRevision);
        SaveEnvelopeDto ExportSave();
    }
}
