using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    public sealed partial class SimulationSession
    {
        internal GameStateDto RunDemoPlanningAi(string actorId,IReadOnlyList<string> admitted)
        {
            var staged=CopyState(state);
            var actor=staged.Civilizations!.Single(c=>c.CivilizationId==actorId);
            if(actor.ControllerKind!=ControllerKind.AI)throw new ArgumentException("Demo planner requires AI ownership.");
            CommandEnvelope Envelope(CommandKind kind,int ordinal)=>new CommandEnvelope {ActorCivilizationId=actorId,Kind=kind,
                CommandId="ai-command.demo.t"+staged.TurnNumber.ToString("D10",CultureInfo.InvariantCulture)+"."+actorId+".k"+(int)kind+".n"+ordinal,
                ExpectedTurn=staged.TurnNumber,ExpectedRevision=staged.StateRevision};
            // Planner reads only owned research/resources/queues and public definitions.
            if(actor.EngineeringTargetDefinitionId==null)
            {
                var target=definitions.EngineeringApplications!.Where(a=>EngineeringRules.Availability(actor,a,staged.ResearchPaceFactor!.Value)==CommandErrorCode.None)
                    .OrderBy(a=>a.DefinitionId,StringComparer.Ordinal).FirstOrDefault();
                if(target!=null){var c=Envelope(CommandKind.SetEngineeringTarget,0);c.SetEngineeringTarget=new SetEngineeringTargetPayload {TargetDefinitionId=target.DefinitionId};
                    ExecuteCommand(ref staged,c,true);}
            }
            // Three ships is this plan's procurement objective, not a fleet-size rule/cap.
            if(staged.Ships!.Count(s=>s.OwnerCivilizationId==actorId)<3)
            {
                var yards=staged.Colonies!.Where(c=>c.OwnerCivilizationId==actorId&&c.ProductionQueue!.Count==0).OrderBy(c=>c.ColonyId,StringComparer.Ordinal).ToList();
                for(int n=0;n<yards.Count;n++)
                {
                    var current=staged.Civilizations!.Single(c=>c.CivilizationId==actorId);
                    var design=definitions.ShipDesigns!.Where(d=>admitted.Contains(d.DefinitionId!)&&d.CreditCost<=current.Credits)
                        .OrderBy(d=>d.CreditCost).ThenBy(d=>d.DefinitionId,StringComparer.Ordinal).FirstOrDefault();
                    if(design==null)continue;
                    var command=Envelope(CommandKind.EnqueueProduction,n);command.EnqueueProduction=new EnqueueProductionPayload {ColonyId=yards[n].ColonyId,DefinitionId=design.DefinitionId};
                    ExecuteCommand(ref staged,command,true); // Same yard/resources/prerequisite validator, no free stocks.
                }
            }
            FoundationValidator.ValidateGameState(staged,definitions).ThrowIfInvalid(nameof(staged));
            return CopyState(staged);
        }
    }
}
