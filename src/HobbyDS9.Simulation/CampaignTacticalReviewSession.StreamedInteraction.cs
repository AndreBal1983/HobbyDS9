using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        // A privately owned branch, not a root ingress or a relaxed transaction.
        // The normal public mutation pipeline must run on the resulting session.
        public CampaignTacticalReviewSession ForkStreamedInteraction(int turn,long revision)
        {
            lock(gate)
            {
                Require(Streams!=null&&state.Campaign.GameState.TurnNumber==turn&&state.Campaign.GameState.StateRevision==revision,"Obsolete streamed interaction.");
                var candidate=ForkPublished();candidate.publishedExecuting=false;return candidate;
            }
        }
    }
}
