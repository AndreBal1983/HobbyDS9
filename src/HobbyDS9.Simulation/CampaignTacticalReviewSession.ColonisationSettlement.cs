using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private void CompleteColonisationWeek(GameStateDto game)
        {
            if(colonisation==null)return;
            Require(colonisation.State.LastSettlementTurn<game.TurnNumber,"Settlement boundary already completed.");
            var before=SimulationSession.CopyState(game);var input=ColonisationData.Copy(colonisation.State);
            var receipts=ApplyColonisationWeek(game,colonisation.State);
            colonisation.SettlementProof=new ColonisationSettlementProofDto{Turn=game.TurnNumber,BeforeGameState=before,BeforeColonisation=input,
                AfterGameState=SimulationSession.CopyState(game),AfterColonisation=ColonisationData.Copy(colonisation.State),Receipts=receipts};
        }
        private List<ColonisationDeploymentReceiptDto> ApplyColonisationWeek(GameStateDto game,CampaignColonisationStateDto a)
        {
            TickPlanetSurveys(game,a);
            a.LastSettlementTurn=game.TurnNumber;
            return new List<ColonisationDeploymentReceiptDto>();
        }
        private void ValidateColonisationState(GameStateDto game)
        {
            var c=colonisation!;var a=c.State;PlanetaryOwner(game).ValidateOwnership();
            Require(a.Bodies!=null&&FrontierSame(a.Bodies,c.FrozenPlanets.Bodies)&&a.Surveys!=null&&a.Surveys.Count<=c.Policy.RecordBudget,"Frozen body bindings and bounded surveys required.");
            Require(a.Surveys!.All(s=>s!=null&&EntityId.TryParse(s.SurveyId,out var id)&&id.TryGetAllocationSequence(out var n)&&n<game.NextEntitySequence
                &&controllers.ContainsKey(s.ActorId)&&a.Bodies.Any(b=>b.PlanetId==s.PlanetId)&&Enum.IsDefined(typeof(PlanetSurveyStage),s.Stage)
                &&s.CompletedWeeks>=0&&s.CompletedWeeks<=2&&s.StartedTurn>=initialTurn&&s.StartedTurn<=game.TurnNumber
                &&(s.Stage==PlanetSurveyStage.Active?s.ResolvedTurn==null&&game.EntityIdentities.Any(e=>e.EntityId==s.SurveyId):s.ResolvedTurn>=s.StartedTurn&&s.ResolvedTurn<=game.TurnNumber&&!game.EntityIdentities.Any(e=>e.EntityId==s.SurveyId))
                &&(s.Stage==PlanetSurveyStage.Completed?s.CompletedWeeks==2:s.Stage==PlanetSurveyStage.Active?s.CompletedWeeks<2:s.CompletedWeeks==0)),"Invalid actual survey source/progress.");
            Require(a.Surveys.Select(s=>(s.ActorId,s.PlanetId)).Distinct().Count()==a.Surveys.Count&&a.Surveys.Select(s=>s.SurveyId).Distinct(StringComparer.Ordinal).Count()==a.Surveys.Count,"Duplicate survey identity.");
            foreach(var minor in c.FrozenPlanets.Bodies!.Where(b=>b.MinorActorId!=null))
                Require(a.PlanetControls!.Count(p=>p.PlanetId==minor.PlanetId&&p.Kind==PlanetControllerKind.IndependentGovernment&&p.ActorId==minor.MinorActorId)==1,"Independent inhabited planet control changed.");
            var proof=c.SettlementProof;
            if(proof==null){Require(a.LastSettlementTurn==0,"Missing latest settlement boundary proof.");return;}
            Require(proof.BeforeGameState!=null&&proof.AfterGameState!=null&&proof.BeforeColonisation!=null&&proof.AfterColonisation!=null&&proof.Receipts!=null
                &&proof.Turn==a.LastSettlementTurn&&proof.Turn==proof.BeforeGameState.TurnNumber&&proof.Turn==proof.AfterGameState.TurnNumber
                &&proof.BeforeColonisation.LastSettlementTurn<proof.Turn&&proof.AfterColonisation.LastSettlementTurn==proof.Turn,"Exact settlement input/result eras required.");
            FoundationValidator.ValidatePlanetaryGameState(proof.BeforeGameState!,definitions,PlanetaryOwnershipContext.CreateForColonisation(definitions,proof.BeforeGameState!,proof.BeforeColonisation!,activities!.State.Minors!)).ThrowIfInvalid(nameof(proof));
            var expected=SimulationSession.CopyState(proof.BeforeGameState!);var inputs=ColonisationData.Copy(proof.BeforeColonisation!);
            var outcomes=ApplyColonisationWeek(expected,inputs);
            Require(FrontierSame(expected,proof.AfterGameState)&&FrontierSame(inputs,proof.AfterColonisation)&&FrontierSame(outcomes,proof.Receipts),"Settlement proof differs from its complete historical inputs.");
            FoundationValidator.ValidatePlanetaryGameState(proof.AfterGameState!,definitions,PlanetaryOwnershipContext.CreateForColonisation(definitions,proof.AfterGameState!,proof.AfterColonisation!,activities.State.Minors!)).ThrowIfInvalid(nameof(proof));
        }
    }
}
