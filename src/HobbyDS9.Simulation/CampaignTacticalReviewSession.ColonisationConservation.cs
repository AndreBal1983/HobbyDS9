using System;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private static BigInteger People(GameStateDto game)=>game.Colonies!.SelectMany(c=>c.Cohorts!).Aggregate(BigInteger.Zero,(n,c)=>n+c.Population);
        private void ExecuteColonisationEconomy(GameStateDto game)
        {
            var before=game.Colonies!.Aggregate(BigInteger.Zero,(n,c)=>n+c.Food);
            var produced=game.Colonies!.Aggregate(BigInteger.Zero,(n,c)=>n+ProjectOwnedWorld(c,game).FoodOutput);
            EconomyPhase.Execute(game,definitions,ColonisationDemand(game),PlanetaryOwner(game));
            var after=game.Colonies!.Aggregate(BigInteger.Zero,(n,c)=>n+c.Food);var consumed=before+produced-after;
            Require(consumed>=0,"Colony economy created unaccounted food.");
            colonisation!.State.FoodProduced=checked(colonisation.State.FoodProduced+(long)produced);
            colonisation.State.FoodConsumed=checked(colonisation.State.FoodConsumed+(long)consumed);
        }
        private void ValidateColonisationConservation(GameStateDto game)
        {
            var c=colonisation!;var a=c.State;var original=published!.Package.Integrated!.Physical!.CorePackage!.Campaign!.InitialState;
            BigInteger passengers=a.ManifestRecords!.Where(m=>m.Stage==SettlerManifestStage.Loaded&&m.Passengers!=null).Aggregate(BigInteger.Zero,(n,m)=>n+m.Passengers!.Population);
            Require(People(original)+(game.PopulationStatistics!.Births-original.PopulationStatistics!.Births)-(game.PopulationStatistics.Deaths-original.PopulationStatistics.Deaths)==People(game)+passengers,
                "Settler population is not conserved outside the separate independent-minor domain.");
            Require(a.FoodProduced>=0&&a.FoodConsumed>=0,"Negative cumulative food facts.");
            BigInteger sourceFood=original.Colonies!.Aggregate(BigInteger.Zero,(n,colony)=>n+colony.Food)+a.FoodProduced;
            BigInteger currentFood=game.Colonies!.Aggregate(BigInteger.Zero,(n,colony)=>n+colony.Food)+a.FoodConsumed
                +a.ManifestRecords.Where(m=>m.Stage==SettlerManifestStage.Loaded).Aggregate(BigInteger.Zero,(n,m)=>n+m.Food)+a.ManifestRecords.Aggregate(BigInteger.Zero,(n,m)=>n+m.FoodLost);
            Require(sourceFood==currentFood,"Physical colony/carried food is not conserved.");
            foreach(var resource in definitions.Resources!)
            {
                BigInteger Stock(System.Collections.Generic.IEnumerable<ResourceAmountDto> rows)=>rows.Where(r=>r.ResourceDefinitionId==resource.DefinitionId).Aggregate(BigInteger.Zero,(n,r)=>n+r.Amount);
                BigInteger source=Stock(original.Colonies.SelectMany(w=>w.Stockpiles!))+Stock(c.FrozenPlanets.InitialPlanets!.SelectMany(p=>p.RemainingDeposits!));
                if(resource.DefinitionId==c.Policy.EquipmentResourceId)source+=c.History.Count(r=>r.Completed&&r.Order!.Kind==ColonisationProductionKind.SettlementKit);
                BigInteger current=Stock(game.Colonies.SelectMany(w=>w.Stockpiles!))+Stock(game.Planets!.SelectMany(p=>p.RemainingDeposits!))
                    +Stock(a.ManifestRecords.Where(m=>m.Stage==SettlerManifestStage.Loaded).SelectMany(m=>m.Cargo!))+Stock(a.ManifestRecords.SelectMany(m=>m.MaterialsLost!))
                    +Stock(c.Production.Concat(c.History.Select(h=>h.Order!)).SelectMany(q=>q.ConsumedResources!));
                if(resource.DefinitionId==c.Policy.EquipmentResourceId)current+=a.ManifestRecords.Sum(m=>(long)m.EquipmentConsumed);
                Require(source==current,"Physical deposits, local/carried materials and exact consumption are not conserved.");
            }
        }
    }
}
