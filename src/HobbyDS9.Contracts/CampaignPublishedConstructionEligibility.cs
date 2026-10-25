using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Contracts
{
    // Detached actor-owned observation, never a permission token for queue admission.
    public sealed class CampaignPublishedConstructionEligibility
    {
        public string ActorId { get; }
        public int Turn { get; }
        public long CampaignRevision { get; }
        public string BlueprintId { get; }
        public int BlueprintRevision { get; }
        public string ColonyId { get; }
        public ShipDesignCostSnapshot Cost { get; }
        public CommandErrorCode OrdinaryReason { get; }
        public IReadOnlyList<TacticalSupportIssue> SupportIssues { get; }
        public CampaignTacticalError CommandResult { get; }
        public bool Supported => SupportIssues.Count==0;
        public bool CanQueue => Supported && OrdinaryReason==CommandErrorCode.None && CommandResult==CampaignTacticalError.None;
        public CampaignPublishedConstructionEligibility(string actor,int turn,long revision,string blueprint,int blueprintRevision,string colony,
            ShipDesignCostSnapshot cost,CommandErrorCode ordinary,IEnumerable<TacticalSupportIssue> issues,CampaignTacticalError commandResult)
        {ActorId=actor;Turn=turn;CampaignRevision=revision;BlueprintId=blueprint;BlueprintRevision=blueprintRevision;ColonyId=colony;
         Cost=cost;OrdinaryReason=ordinary;SupportIssues=issues.ToList().AsReadOnly();CommandResult=commandResult;}
    }
}
