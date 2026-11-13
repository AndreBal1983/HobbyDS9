using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public CampaignPublishedConstructionEligibility CapturePublishedConstructionEligibility(string actor,string blueprint,int revision,string colony)
        {
            lock(gate)
            {
                var game=state.Campaign.GameState;
                var source=game.Blueprints!.SingleOrDefault(b=>b.BlueprintId==blueprint&&b.Revision==revision&&b.OwnerCivilizationId==actor);
                // Reject absent and foreign identities identically before projecting any cost/support.
                Require(published!=null&&Actor(actor)==CampaignTacticalError.None&&source!=null
                    &&game.Colonies!.Any(c=>c.ColonyId==colony&&c.OwnerCivilizationId==actor),"Owned construction observation required.");
                var restored=CampaignBlueprints.Restore(source!)!;
                var ordinary=CampaignBlueprints.Eligibility(game,definitions,actor,colony,source);
                var supported=PublishedFit(game,new BlueprintReferenceDto{BlueprintId=blueprint,Revision=revision},true);
                // The real submission path performs payment, allocation, support and
                // canonical post-command reserve validation on a wholly detached fork.
                var candidate=ForkPublished();candidate.publishedExecuting=false;
                var used=new HashSet<string>(game.AcceptedCommandIds!,StringComparer.Ordinal);used.UnionWith(state.AcceptedRoundCommands);
                int n=0;string command;
                do{command="command.construction-observation."+(n++).ToString(CultureInfo.InvariantCulture);}while(used.Contains(command));
                var result=candidate.SubmitStrategicCommand(new CommandEnvelope{ActorCivilizationId=actor,CommandId=command,
                    ExpectedTurn=game.TurnNumber,ExpectedRevision=game.StateRevision,Kind=CommandKind.EnqueueBlueprintConstruction,
                    EnqueueBlueprintConstruction=new EnqueueBlueprintConstructionPayload{ColonyId=colony,BlueprintId=blueprint,Revision=revision}});
                return new CampaignPublishedConstructionEligibility(actor,game.TurnNumber,game.StateRevision,blueprint,revision,colony,
                    restored.Performance.Cost!,ordinary,supported.Issues,result.Error);
            }
        }
    }
}
