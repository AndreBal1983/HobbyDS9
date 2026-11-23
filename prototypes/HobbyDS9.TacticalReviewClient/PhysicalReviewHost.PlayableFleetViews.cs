using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed partial class PhysicalReviewHost
    {
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(ReviewFrame observed)
        {
            lock(gate)
            {
                RequireCurrent(observed.HostId,observed.Generation);
                return ((ICampaignPlayableFleetSession)session).CaptureFleetOrganization(observed.Actor);
            }
        }
        public CampaignBattlePlayback CaptureBattlePlayback(ReviewFrame observed,string encounter)
        {
            lock(gate)
            {
                RequireCurrent(observed.HostId,observed.Generation);
                return ((ICampaignPlayableFleetSession)session).CaptureBattlePlayback(encounter,observed.Actor);
            }
        }
        public IReadOnlyList<CampaignBattlePlayback> CaptureBattleHistory(ReviewFrame observed)
        {
            lock(gate)
            {
                RequireCurrent(observed.HostId,observed.Generation);
                return ((ICampaignPlayableFleetSession)session).GetBattleHistory(observed.Actor);
            }
        }
        public CampaignFleetOrganizationResult OrganizeFleet(ReviewFrame observed,CampaignFleetOrganizationKind kind,
            IReadOnlyList<string> ships,string? target,string? name,string command)
        {
            lock(gate)
            {
                RequireCurrent(observed.HostId,observed.Generation);
                return ((ICampaignPlayableFleetSession)session).SubmitFleetOrganization(new SubmitCampaignFleetOrganizationDto {
                    ActorId=observed.Actor,CommandId=command,ExpectedTurn=observed.Status.Turn,
                    ExpectedRevision=observed.Status.CampaignRevision,Kind=kind,ShipIds=new List<string>(ships),
                    TargetFleetId=target,DisplayName=name
                });
            }
        }
    }
}
