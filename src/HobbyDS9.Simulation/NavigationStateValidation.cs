using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidateNavigationDefinitions(DefinitionCatalogDto definitions,HashSet<DefinitionId> ids,HashSet<string> typed,string path,List<ValidationIssue> issues)
        {
            foreach(var issue in WeightedNavigationGraph.Validate(definitions).Issues)Add(issues,issue.Code,path+issue.Path);
            if(definitions.Phenomena!=null)foreach(var phenomenon in definitions.Phenomena.Where(p=>p!=null))
                if(!DefinitionId.TryParse(phenomenon.DefinitionId,out var id)||!ids.Contains(id)||!typed.Add(phenomenon.DefinitionId))Add(issues,ValidationCode.MissingDefinition,path+".Phenomena");
        }
        private static void ValidateNavigationState(GameStateDto state,DefinitionCatalogDto? d,string path,List<ValidationIssue> issues)
        {
            void Check(bool valid){if(!valid)Add(issues,ValidationCode.InvalidFleetRoute,path+".Navigation");}
            if(state.NavigationKnowledge==null||state.TraversalEffects==null||state.NavigationStatistics==null){Add(issues,ValidationCode.MissingField,path+".Navigation");return;}
            if(d==null||!WeightedNavigationGraph.Validate(d).IsValid)return;
            var graph=new WeightedNavigationGraph(d);var nodes=d.NavigationRegions!.Select(n=>n.NodeId).ToHashSet(StringComparer.Ordinal);var connections=d.NavigationConnections!.Select(e=>e.ConnectionId).ToHashSet(StringComparer.Ordinal);
            Check(d.NavigationRegions!.All(n=>n.AssociatedSystemId==null||state.StarSystems?.Any(s=>s!=null&&s.SystemId==n.AssociatedSystemId)==true));
            Check((state.Colonies??new List<ColonyStateDto>()).Where(c=>c!=null).All(c=>graph.NodeForSystem(c.SystemId)!=null));
            var civilizations=(state.Civilizations??new List<CivilizationStateDto>()).Where(c=>c!=null).Select(c=>c.CivilizationId).ToHashSet(StringComparer.Ordinal);
            Check(state.NavigationKnowledge.Count==civilizations.Count&&state.NavigationKnowledge.All(k=>k!=null)&&state.NavigationKnowledge.Where(k=>k!=null).Select(k=>k.CivilizationId).Distinct(StringComparer.Ordinal).Count()==civilizations.Count);
            foreach(var knowledge in state.NavigationKnowledge.Where(k=>k!=null))
            {
                Check(civilizations.Contains(knowledge.CivilizationId));
                foreach(var pair in new[]{(knowledge.Nodes,nodes),(knowledge.Connections,connections)})
                    Check(pair.Item1!=null&&pair.Item1.All(k=>k!=null&&pair.Item2.Contains(k.Id)&&Enum.IsDefined(typeof(NavigationKnowledgeLevel),k.Level))&&pair.Item1.Where(k=>k!=null).Select(k=>k.Id).Distinct(StringComparer.Ordinal).Count()==pair.Item1.Count);
            }
            Check(state.TraversalEffects.Where(e=>e!=null).Select(e=>e.EffectId).Distinct(StringComparer.Ordinal).Count()==state.TraversalEffects.Count);
            foreach(var effect in state.TraversalEffects.Where(e=>e!=null))
                Check(EntityId.TryParse(effect.EffectId,out _)&&(effect.NodeId==null)!=(effect.ConnectionId==null)
                    &&(effect.NodeId==null||nodes.Contains(effect.NodeId))&&(effect.ConnectionId==null||connections.Contains(effect.ConnectionId))&&effect.TraversalCostIncrease>=0&&effect.MovementBudgetPenalty>=0
                    &&(effect.ConnectionId==null||effect.MovementBudgetPenalty==0)&&effect.StartTurn>0&&effect.EndTurn>=effect.StartTurn);
            foreach(var connection in d.NavigationConnections!)
            {
                var regions=d.NavigationRegions!.Where(n=>n.NodeId==connection.EndpointA||n.NodeId==connection.EndpointB);
                var natural=regions.SelectMany(n=>n.PhenomenonDefinitionIds!).Distinct(StringComparer.Ordinal).Select(id=>d.Phenomena!.Single(p=>p.DefinitionId==id).TraversalCostIncrease);
                var artificial=state.TraversalEffects.Where(e=>e!=null&&(e.ConnectionId==connection.ConnectionId||e.NodeId==connection.EndpointA||e.NodeId==connection.EndpointB)).Select(e=>e.TraversalCostIncrease);
                Check(natural.Concat(artificial).Aggregate((BigInteger)connection.BaseTraversalCost,(sum,cost)=>sum+cost)<=long.MaxValue);
            }
            foreach(var region in d.NavigationRegions!)
                Check(region.PhenomenonDefinitionIds!.Select(id=>d.Phenomena!.Single(p=>p.DefinitionId==id).MovementBudgetPenalty)
                    .Concat(state.TraversalEffects.Where(e=>e!=null&&e.NodeId==region.NodeId).Select(e=>e.MovementBudgetPenalty))
                    .Aggregate(BigInteger.Zero,(sum,value)=>sum+value)<=long.MaxValue);
            var stats=state.NavigationStatistics;Check(stats.TraversalCostPaid>=0&&stats.EdgesCompleted>=0&&stats.PartialTraversalTurns>=0);
            foreach(var fleet in (state.Fleets??new List<FleetStateDto>()).Where(f=>f!=null))
            {
                Check(nodes.Contains(fleet.LocationNodeId)&&fleet.TransitTotalCost>=0&&fleet.TransitProgress>=0
                    &&(fleet.TransitTotalCost==0?fleet.TransitProgress==0:fleet.TransitProgress>0&&fleet.TransitProgress<fleet.TransitTotalCost&&fleet.RouteNodeIds?.Count>0));
                if(fleet.RouteNodeIds!=null&&state.NavigationKnowledge.Count(k=>k!=null&&k.CivilizationId==fleet.OwnerCivilizationId&&k.Nodes!=null&&k.Connections!=null)==1
                    &&fleet.RouteNodeIds.All(n=>n!=null))Check(graph.ValidateRoute(state,fleet.OwnerCivilizationId,fleet.LocationNodeId,fleet.RouteNodeIds,Math.Max(0,fleet.TransitTotalCost-fleet.TransitProgress)));
            }
        }
    }
}
