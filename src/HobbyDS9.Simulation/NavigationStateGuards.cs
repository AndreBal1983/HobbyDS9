using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class NavigationStateGuards
    {
        internal static bool Same(GameStateDto before,GameStateDto after,bool movement) =>
            before.NavigationKnowledge!.Count==after.NavigationKnowledge!.Count&&before.NavigationKnowledge.Zip(after.NavigationKnowledge,(a,b)=>PopulationData.Same(a,b)).All(v=>v)
            &&before.TraversalEffects!.Count==after.TraversalEffects!.Count&&before.TraversalEffects.Zip(after.TraversalEffects,(a,b)=>PopulationData.Same(a,b)).All(v=>v)
            &&(movement||PopulationData.Same(before.NavigationStatistics,after.NavigationStatistics));
    }
}
