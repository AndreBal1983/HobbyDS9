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
            ReconcileExpansion(game);
        }
        private List<ColonisationDeploymentReceiptDto> ApplyColonisationWeek(GameStateDto game,CampaignColonisationStateDto a)
        {
            var census=a.DeploymentIntents!.OrderBy(i=>i.PlanetId,StringComparer.Ordinal).ThenBy(i=>i.ActorId,StringComparer.Ordinal).ThenBy(i=>i.IntentId,StringComparer.Ordinal)
                .Select(i=>(Intent:ColonisationData.Copy(i),Manifest:a.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==i.ManifestId) is SettlerManifestDto m?ColonisationData.Copy(m):null)).ToList();
            TickCarriedSettlers(game,a);
            TickPlanetSurveys(game,a);
            TickSettlementCamps(game,a);
            var receipts=new List<ColonisationDeploymentReceiptDto>();
            foreach(var group in census.GroupBy(r=>r.Intent.PlanetId,StringComparer.Ordinal))
            {
                var viable=group.Where(r=>a.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==r.Intent.ManifestId&&m.OwnerId==r.Intent.ActorId) is SettlerManifestDto m&&MayDeploy(game,a,m,r.Intent.PlanetId)).ToList();
                Require(viable.Select(r=>r.Intent.ActorId).Distinct(StringComparer.Ordinal).Count()==viable.Count,"Duplicate same-actor deployment candidate.");
                var disputed=viable.Select(r=>r.Intent.ActorId).Distinct(StringComparer.Ordinal).Count()>1;
                foreach(var row in group)
                {
                    var intent=a.DeploymentIntents!.Single(i=>i.IntentId==row.Intent.IntentId);
                    var receipt=new ColonisationDeploymentReceiptDto{Intent=ColonisationData.Copy(row.Intent),ManifestBefore=row.Manifest,Outcome=DeploymentBoundaryOutcome.Interrupted};
                    if(viable.Any(r=>r.Intent.IntentId==intent.IntentId))
                    {
                        if(disputed){intent.Disputed=true;receipt.Outcome=DeploymentBoundaryOutcome.Disputed;}
                        else
                        {
                            var manifest=a.ManifestRecords!.Single(m=>m.ManifestId==intent.ManifestId);
                            LandSettlement(game,a,manifest,intent.PlanetId,receipt);receipt.Outcome=DeploymentBoundaryOutcome.Deployed;
                        }
                    }
                    if(receipt.Outcome!=DeploymentBoundaryOutcome.Disputed){a.DeploymentIntents!.Remove(intent);game.EntityIdentities.RemoveAll(e=>e.EntityId==intent.IntentId);}
                    receipts.Add(receipt);
                }
            }
            ObserveSurveyControls(game,a);
            a.LastSettlementTurn=game.TurnNumber;
            return receipts;
        }
        private void TickCarriedSettlers(GameStateDto game,CampaignColonisationStateDto a)
        {
            foreach(var manifest in a.ManifestRecords!.Where(m=>m.Stage==SettlerManifestStage.Loaded).OrderBy(m=>m.ManifestId,StringComparer.Ordinal))
            {
                Require(manifest.LastSurvivalTurn<game.TurnNumber,"Carried survival already processed this week.");
                var alive=game.Ships!.Any(s=>s.ShipId==manifest.ShipId&&s.OwnerCivilizationId==manifest.OwnerId&&s.CurrentHull>0);
                if(alive&&manifest.Kind!=SettlerManifestKind.SupplyOnly)
                {
                    var people=manifest.Passengers!;var demand=people.Population/100+(people.Population%100==0?0:1);var eaten=Math.Min(demand,manifest.Food);
                    manifest.Food-=eaten;manifest.FoodConsumed=checked(manifest.FoodConsumed+eaten);manifest.FoodShortageWeeks=eaten==demand?0:checked(manifest.FoodShortageWeeks+1);
                    a.FoodConsumed=checked(a.FoodConsumed+eaten);
                    if(manifest.FoodShortageWeeks>=PopulationRules.ShortageGrace)
                    {
                        var lost=Math.Min(people.Population,Math.Max(1,people.Population/100));people.Population-=lost;
                        manifest.PassengerDeaths=checked(manifest.PassengerDeaths+lost);game.PopulationStatistics!.Deaths=checked(game.PopulationStatistics.Deaths+lost);
                    }
                    if(people.Population==0)alive=false;
                }
                manifest.LastSurvivalTurn=game.TurnNumber;
                if(alive)continue;
                if(manifest.Passengers!=null)
                {
                    var lost=manifest.Passengers.Population;manifest.PassengerDeaths=checked(manifest.PassengerDeaths+lost);game.PopulationStatistics!.Deaths=checked(game.PopulationStatistics.Deaths+lost);
                    game.EntityIdentities.RemoveAll(e=>e.EntityId==manifest.Passengers.CohortId);manifest.Passengers=null;
                }
                manifest.FoodLost=checked(manifest.FoodLost+manifest.Food);manifest.Food=0;
                foreach(var r in manifest.Cargo!){AddVector(manifest.MaterialsLost!,r.ResourceDefinitionId,r.Amount);r.Amount=0;}
                manifest.LatestReport="Transport or passenger group lost; remaining physical cargo lost.";
                FinishManifest(game,a,manifest,SettlerManifestStage.Destroyed,null);
            }
        }
        private void TickSettlementCamps(GameStateDto game,CampaignColonisationStateDto a)
        {
            foreach(var camp in a.CampSources!.GroupBy(c=>c.ColonyId,StringComparer.Ordinal).Select(g=>g.OrderByDescending(c=>c.DeployedTurn).First()).OrderBy(c=>c.ColonyId,StringComparer.Ordinal))
            {
                var colony=game.Colonies!.Single(c=>c.ColonyId==camp.ColonyId);var population=PopulationRules.Population(colony);
                if(camp.Stage==ColonyEstablishmentStage.Founding)
                {
                    var landed=a.LandedCohortLineages!.Single(l=>l.ManifestId==camp.ManifestId&&l.ColonyId==camp.ColonyId).Population;
                    camp.Deaths=landed-population;Require(camp.Deaths>=0&&population<=colonisation!.Policy.Settlers,"Founding population changed outside its single conserved group.");
                }
                if(population==0&&camp.Stage!=ColonyEstablishmentStage.EmptyOutpost)
                {
                    foreach(var cohort in colony.Cohorts!)game.EntityIdentities.RemoveAll(e=>e.EntityId==cohort.CohortId);colony.Cohorts!.Clear();
                    if(camp.Stage==ColonyEstablishmentStage.Founding)
                    {a.CampTerminals!.Add(new SettlementCampTerminalDto{CampId=camp.CampId,Kind=SettlementTerminalKind.Extinct,Turn=game.TurnNumber});game.EntityIdentities.RemoveAll(e=>e.EntityId==camp.CampId);}
                    camp.Stage=ColonyEstablishmentStage.EmptyOutpost;camp.LatestReport="No surviving population; empty outpost retains control, infrastructure and local stocks.";continue;
                }
                if(camp.Stage!=ColonyEstablishmentStage.Founding)continue;
                if(colony.FoodShortageTurns>0){camp.LatestReport="Establishment paused: delivered food is insufficient.";continue;}
                camp.FedWeeks++;camp.FoodConsumed=checked(camp.FoodConsumed+1);
                var output=WorldEconomy.Project(colony,game,definitions,PlanetaryOwnershipContext.CreateForColonisation(definitions,game,a,activities!.State.Minors!)).Industry;
                camp.CompletedIndustry=checked(camp.CompletedIndustry+Math.Min(colonisation!.Policy.EstablishmentIndustry-camp.CompletedIndustry,output));
                if(camp.FedWeeks<colonisation.Policy.MinimumFedWeeks||camp.CompletedIndustry<colonisation.Policy.EstablishmentIndustry){camp.LatestReport="Founding camp supplied; establishment progressing.";continue;}
                colony.Infrastructure!.RemoveAll(i=>i.DefinitionId==colonisation.Policy.Camp!.DefinitionId||i.DefinitionId=="infrastructure.settlement-hub.v1");
                colony.Infrastructure.Add(new InfrastructureStateDto{DefinitionId="infrastructure.settlement-hub.v1",Count=1});camp.Stage=ColonyEstablishmentStage.Established;
                camp.LatestReport="Colony established; normal production and migration available next week.";
                a.CampTerminals!.Add(new SettlementCampTerminalDto{CampId=camp.CampId,Kind=SettlementTerminalKind.Established,Turn=game.TurnNumber});game.EntityIdentities.RemoveAll(e=>e.EntityId==camp.CampId);
            }
        }
        private void LandSettlement(GameStateDto game,CampaignColonisationStateDto a,SettlerManifestDto manifest,string planetId,ColonisationDeploymentReceiptDto receipt)
        {
            var planet=game.Planets!.Single(p=>p.PlanetId==planetId);var people=manifest.Passengers!;var policy=colonisation!.Policy;
            var origin=a.ManifestOrigins!.Single(o=>o.ManifestId==manifest.ManifestId);var home=game.Colonies!.Single(c=>c.ColonyId==origin.SourceColonyId);
            var colony=game.Colonies.SingleOrDefault(c=>c.PlanetId==planetId);
            var created=colony==null;
            if(colony==null)
            {
                colony=new ColonyStateDto{ColonyId=CampaignAllocation.Allocate(game,"colony",null),PlanetId=planet.PlanetId,SystemId=planet.SystemId,OwnerCivilizationId=manifest.OwnerId,
                    Cohorts=new List<PopulationCohortDto>(),Infrastructure=new List<InfrastructureStateDto>(),Stockpiles=new List<ResourceAmountDto>(),DesiredStockpiles=new List<ResourceAmountDto>(),
                    Stability=home.Stability,Prosperity=home.Prosperity,Automation=0,AcceptsRefugees=false,ProductionQueue=new List<ProductionQueueItemDto>(),
                    Politics=new WorldPoliticalStateDto{GovernancePolicyDefinitionId=home.Politics!.GovernancePolicyDefinitionId,Resistance=ResistanceState.Quiet,LastUpdateTurn=game.TurnNumber,
                        CohortReports=new List<CohortPoliticalReportDto>(),Events=new List<string>()}};
                game.Colonies!.Add(colony);
            }
            Require(colony.Cohorts!.Count==0&&colony.ProductionQueue!.Count==0,"Recommissioning requires an empty retained outpost.");
            colony.Cohorts.Add(PopulationData.Copy(people));colony.FoodShortageTurns=0;
            colony.Infrastructure!.RemoveAll(i=>i.DefinitionId==policy.Camp!.DefinitionId||i.DefinitionId=="infrastructure.settlement-hub.v1");colony.Infrastructure.Add(new InfrastructureStateDto{DefinitionId=policy.Camp!.DefinitionId,Count=1});
            PopulationRules.AddStock(manifest.Cargo!,policy.EquipmentResourceId,-1);manifest.EquipmentConsumed=checked(manifest.EquipmentConsumed+1);game.PopulationStatistics!.ResourceConsumed=checked(game.PopulationStatistics.ResourceConsumed+1);
            receipt.ColonyId=colony.ColonyId;receipt.PopulationTransferred=people.Population;receipt.LandedCohortId=people.CohortId;receipt.FoodTransferred=manifest.Food;receipt.EquipmentConsumed=1;
            var cargo=manifest.Cargo!.Where(r=>r.Amount>0).Select(PopulationData.Copy).ToList();receipt.MaterialsTransferred=cargo.Select(PopulationData.Copy).ToList();
            MoveCarriedSupply(game,manifest,colony,manifest.Food,cargo);manifest.PopulationTransferred=checked(manifest.PopulationTransferred+people.Population);
            a.LandedCohortLineages!.Add(new LandedCohortLineageDto{ManifestId=manifest.ManifestId,ColonyId=colony.ColonyId,CohortId=people.CohortId,Population=people.Population,Turn=game.TurnNumber});manifest.Passengers=null;
            var campId=CampaignAllocation.Allocate(game,"settlement-camp",null);receipt.CampId=campId;
            a.CampSources!.Add(new SettlementCampDto{CampId=campId,ColonyId=colony.ColonyId,ManifestId=manifest.ManifestId,OwnerId=manifest.OwnerId,Stage=ColonyEstablishmentStage.Founding,DeployedTurn=game.TurnNumber,LatestReport="Founding camp deployed; first economy week begins next turn."});
            if(created)a.ColonyOrigins!.Add(new ColonyOriginDto{ColonyId=colony.ColonyId,PlanetId=planetId,ManifestId=manifest.ManifestId,Turn=game.TurnNumber});
            else Require(a.ColonyOrigins!.Count(o=>o.ColonyId==colony.ColonyId&&o.PlanetId==planetId)==1,"Recommissioning preserves the original colony origin.");
            var controller=a.PlanetControls!.Single(c=>c.PlanetId==planetId);controller.Kind=PlanetControllerKind.Empire;controller.ActorId=manifest.OwnerId;
            manifest.LatestReport="Settlers deployed; transport is empty and reusable.";FinishManifest(game,a,manifest,SettlerManifestStage.Deployed,planetId);
        }
        private void ValidateColonisationState(GameStateDto game)
        {
            var c=colonisation!;var a=c.State;PlanetaryOwner(game).ValidateOwnership();
            ValidateColonisationLineages(game);
            ValidateColonisationConservation(game);
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
            // Both retained snapshots use the canonical state-copy ordering, including newly allocated identities.
            expected=SimulationSession.CopyState(expected);
            Require(FrontierSame(expected,proof.AfterGameState),"Settlement game result differs from its complete historical inputs.");
            Require(FrontierSame(inputs,proof.AfterColonisation),"Settlement context result differs from its complete historical inputs.");
            Require(FrontierSame(outcomes,proof.Receipts),"Settlement outcome batch differs from its complete historical inputs.");
            FoundationValidator.ValidatePlanetaryGameState(proof.AfterGameState!,definitions,PlanetaryOwnershipContext.CreateForColonisation(definitions,proof.AfterGameState!,proof.AfterColonisation!,activities.State.Minors!)).ThrowIfInvalid(nameof(proof));
        }
        private void ValidateColonisationWorlds(GameStateDto game,GameStateDto original)
        {
            var a=colonisation!.State;var structures=activities!.State.Structures!;
            foreach(var colony in game.Colonies!)
            {
                var initial=original.Colonies!.SingleOrDefault(c=>c.ColonyId==colony.ColonyId);var camp=CurrentCamp(a,colony.ColonyId);
                var expected=initial==null?new List<InfrastructureStateDto>():initial.Infrastructure!.Select(PopulationData.Copy).ToList();
                if(initial!=null)Require(colony.OwnerCivilizationId==initial.OwnerCivilizationId&&colony.SystemId==initial.SystemId&&colony.PlanetId==initial.PlanetId&&colony.Automation==initial.Automation,"Initial world identity changed.");
                else Require(camp!=null&&a.ColonyOrigins!.Any(o=>o.ColonyId==colony.ColonyId&&o.PlanetId==colony.PlanetId)&&colony.Automation==0,"Unreceipted settlement world.");
                if(camp!=null)
                {
                    Require(camp.OwnerId==colony.OwnerCivilizationId,"Settlement owner changed.");
                    var established=camp.Stage==ColonyEstablishmentStage.Established||a.CampTerminals!.Any(t=>t.CampId==camp.CampId&&t.Kind==SettlementTerminalKind.Established);
                    expected.Add(new InfrastructureStateDto{DefinitionId=established?"infrastructure.settlement-hub.v1":colonisation.Policy.Camp!.DefinitionId,Count=1});
                    if(camp.Stage==ColonyEstablishmentStage.Founding)Require(colony.Cohorts!.Count==1&&PopulationRules.Population(colony)>0&&PopulationRules.Population(colony)<=colonisation.Policy.Settlers,"Founding is one bounded conserved cohort.");
                    if(camp.Stage==ColonyEstablishmentStage.EmptyOutpost)Require(PopulationRules.Population(colony)==0,"Empty outpost contains passengers.");
                }
                expected.AddRange(structures.Where(s=>s.ColonyId==colony.ColonyId).Select(s=>new InfrastructureStateDto{DefinitionId=s.DefinitionId,Count=1}));
                Require(FrontierSame(expected.OrderBy(i=>i.DefinitionId,StringComparer.Ordinal).ToList(),colony.Infrastructure!.OrderBy(i=>i.DefinitionId,StringComparer.Ordinal).ToList()),"Only exactly receipted settlement/streamed infrastructure is retained.");
            }
        }
    }
}
