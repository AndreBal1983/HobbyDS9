using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class PoliticalEconomy
    {
        // Basis points. These are effectiveness, not population or ownership changes.
        internal static PoliticalEconomySnapshot Project(ColonyStateDto world, DefinitionCatalogDto definitions)
        {
            var policy=PoliticsRules.Policy(world,definitions);
            var resistance=world.Politics!.Resistance;
            var unrest=resistance==ResistanceState.Unrest;
            var organized=resistance==ResistanceState.OrganizedResistance;
            var disruption=world.Politics.DisruptionRemainingTurns>0;
            int Combine(int policyFactor,int resistanceFactor,int disruptionFactor)=>
                (int)PopulationRules.ScaleBy(PopulationRules.ScaleBy(policyFactor,resistanceFactor),disruptionFactor);
            return new PoliticalEconomySnapshot(
                Combine(policy.CreditsFactor,organized?9000:unrest?9500:10000,disruption?9500:10000),
                Combine(policy.IndustryFactor,organized?7500:unrest?9000:10000,disruption?5000:10000),
                Combine(policy.ResearchFactor,organized?9000:unrest?9500:10000,disruption?8000:10000),
                Combine(policy.LogisticsFactor,organized?6500:unrest?8000:10000,disruption?4000:10000),
                Combine(policy.IndustryFactor,organized?7500:unrest?9000:10000,disruption?5000:10000),
                world.AcceptsRefugees&&policy.AcceptsRefugees);
        }
    }
}
