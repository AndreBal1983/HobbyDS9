using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Application;

namespace HobbyDS9.TacticalReviewClient
{
    // Only Application's lawful projections cross the host/presenter boundary.
    public sealed class ReviewFrame
    {
        internal Guid HostId { get; }
        public long Generation { get; }
        public string Actor { get; }
        public CampaignTacticalReviewStatus Status { get; }
        public IReadOnlyList<string> Encounters { get; }
        public IReadOnlyList<CampaignTacticalOwnShip> OwnShips { get; }
        public IReadOnlyList<FleetSnapshot> OwnStrategicFleets { get; }
        public IReadOnlyList<CampaignTacticalFleetCloakSetting> CloakSettings { get; }
        public CampaignTacticalObservation? Battle { get; }
        public PhysicalTravelView? Travel { get; }
        public CampaignIntegratedStrategySnapshot? Strategy { get; }
        public CampaignDemoSnapshot? Demo { get; }
        internal ReviewFrame(Guid host, long generation, string actor, CampaignTacticalReviewStatus status,
            IReadOnlyList<string> encounters, IReadOnlyList<CampaignTacticalOwnShip> own,
            IReadOnlyList<CampaignTacticalFleetCloakSetting> cloak, IReadOnlyList<FleetSnapshot> fleets, CampaignTacticalObservation? battle,
            PhysicalTravelView? travel = null,CampaignIntegratedStrategySnapshot? strategy = null,CampaignDemoSnapshot? demo = null)
        { HostId=host; Generation=generation; Actor=actor; Status=status; Encounters=encounters;
            OwnShips=own; CloakSettings=cloak; OwnStrategicFleets=fleets; Battle=battle; Travel=travel; Strategy=strategy; Demo=demo; }
    }
    public sealed class FleetEdit
    {
        public string Fleet { get; }
        public CampaignFleetManeuver Maneuver { get; }
        public string? Focus { get; }
        public FleetEdit(string fleet, CampaignFleetManeuver maneuver, string? focus)
        { Fleet=fleet; Maneuver=maneuver; Focus=focus; }
    }
    public enum ReviewAction { Round, Finalize, Travel }
    public sealed class ReviewWork
    {
        internal Guid HostId { get; }
        internal Guid ControllerId { get; }
        internal long Request { get; }
        public long Generation { get; }
        public ReviewAction Action { get; }
        public string Actor { get; }
        public string Encounter { get; }
        public string CommandId { get; }
        public int Turn { get; }
        public int Round { get; }
        public long BoundaryRevision { get; }
        public long CampaignRevision { get; }
        public IReadOnlyList<FleetEdit> Replacements { get; }
        public TravelEdit? Travel { get; }
        internal ReviewWork(ReviewFrame frame, Guid controller, long request, ReviewAction action,
            string command, IReadOnlyList<FleetEdit> edits)
        { HostId=frame.HostId; ControllerId=controller; Request=request; Generation=frame.Generation;
            Action=action; Actor=frame.Actor; Encounter=frame.Battle!.EncounterId; CommandId=command;
            Turn=frame.Status.Turn; Round=frame.Battle.Round; BoundaryRevision=frame.Battle.BoundaryRevision;
            CampaignRevision=frame.Status.CampaignRevision; Replacements=edits; }
        internal ReviewWork(ReviewFrame frame,Guid controller,long request,string command,TravelEdit travel)
        {
            HostId=frame.HostId; ControllerId=controller; Request=request; Generation=frame.Generation;
            Action=ReviewAction.Travel; Actor=frame.Actor; Encounter=""; CommandId=command;
            Turn=frame.Status.Turn; CampaignRevision=frame.Status.CampaignRevision;
            Replacements=Array.Empty<FleetEdit>(); Travel=travel;
        }
    }
    public sealed class ReviewReceipt
    {
        internal ReviewWork Work { get; }
        public CampaignTacticalMutationResult Result { get; }
        public ReviewFrame Frame { get; }
        internal ReviewReceipt(ReviewWork work, CampaignTacticalMutationResult result, ReviewFrame frame)
        { Work=work; Result=result; Frame=frame; }
    }
    public enum ReviewMode { Planning, EncounterSelection, Orders, RoundReview, Sending, Waiting, Complete, CompletionReview, Finalizing, TravelReview }
}
