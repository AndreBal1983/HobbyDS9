using System.Text.Json;
using System.Security.Cryptography;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation.Tests;

// Input mutations shared by focused tests and the evidence-only old-assembly differential runner.
// This contains no validator logic and is not included in any production assembly.
internal static class NavigationValidationIndexCases
{
    internal static readonly string[] Names={"valid","reordered","reversed-endpoints","regions-null","connections-null","phenomena-null",
        "region-null","connection-null","phenomenon-null","region-duplicate","connection-duplicate","phenomenon-duplicate",
        "region-id-null","region-phenomena-null","unknown-phenomenon","unknown-endpoint","self-edge","negative-phenomenon",
        "duplicate-association","unknown-association","systems-null","system-null","system-id-null","duplicate-system","colonies-null","colony-null","colony-system-null",
        "knowledge-null","knowledge-row-null","knowledge-nodes-null","knowledge-node-null","knowledge-unknown","knowledge-duplicate","knowledge-level",
        "effects-null","effect-null","effect-duplicate","effect-negative","effect-unknown","effect-both-targets","effect-dates","effect-edge-penalty","effect-maxcost","effect-maxpenalty",
        "shared-natural-exact-max","natural-overflow","stats-null","stats-negative","route-unknown","route-unknown-knowledge","fleet-transit"};
    internal static ScenarioDto Scenario()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios","ds9-slice","scenario.json"));
        var scenario=JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
        scenario.ContentHash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();return scenario;
    }
    internal static void Hazard(ScenarioDto s,long cost,long penalty=0,bool shared=false)
    {
        var d=s.Definitions;foreach(var region in d.NavigationRegions!)region.PhenomenonDefinitionIds!.Clear();
        d.Phenomena!.Clear();foreach(var edge in d.NavigationConnections!)edge.BaseTraversalCost=1;
        const string id="phenomenon.navigation-index-test";if(!d.DefinitionIds.Contains(id))d.DefinitionIds.Add(id);
        d.Phenomena.Add(new(){DefinitionId=id,DisplayName="Test hazard",Description="Test-only navigation input",TraversalCostIncrease=cost,MovementBudgetPenalty=penalty,LogisticsFactor=10000});
        var edgeFirst=d.NavigationConnections[0];d.NavigationRegions.Single(n=>n.NodeId==edgeFirst.EndpointA).PhenomenonDefinitionIds!.Add(id);
        if(shared)d.NavigationRegions.Single(n=>n.NodeId==edgeFirst.EndpointB).PhenomenonDefinitionIds!.Add(id);
    }
    internal static void Mutate(ScenarioDto s,string name)
    {
        var d=s.Definitions;var g=s.InitialState;
        TraversalEffectDto Effect()=>new(){EffectId="effect.navigation-index-test",NodeId=d.NavigationRegions![0].NodeId,StartTurn=1,EndTurn=1};
        switch(name)
        {
            case "valid":break;
            case "reordered":d.NavigationRegions!.Reverse();d.NavigationConnections!.Reverse();d.Phenomena!.Reverse();break;
            case "reversed-endpoints":foreach(var e in d.NavigationConnections!)(e.EndpointA,e.EndpointB)=(e.EndpointB,e.EndpointA);break;
            case "regions-null":d.NavigationRegions=null;break;
            case "connections-null":d.NavigationConnections=null;break;
            case "phenomena-null":d.Phenomena=null;break;
            case "region-null":d.NavigationRegions!.Add(null!);break;
            case "connection-null":d.NavigationConnections!.Add(null!);break;
            case "phenomenon-null":d.Phenomena!.Add(null!);break;
            case "region-duplicate":d.NavigationRegions!.Add(d.NavigationRegions[0]);break;
            case "connection-duplicate":d.NavigationConnections!.Add(d.NavigationConnections[0]);break;
            case "phenomenon-duplicate":Hazard(s,1);d.Phenomena!.Add(d.Phenomena[0]);break;
            case "region-id-null":d.NavigationRegions![0].NodeId=null!;break;
            case "region-phenomena-null":d.NavigationRegions![0].PhenomenonDefinitionIds=null;break;
            case "unknown-phenomenon":d.NavigationRegions![0].PhenomenonDefinitionIds!.Add("phenomenon.absent");break;
            case "unknown-endpoint":d.NavigationConnections![0].EndpointA="nav.absent";break;
            case "self-edge":d.NavigationConnections![0].EndpointA=d.NavigationConnections[0].EndpointB;break;
            case "negative-phenomenon":Hazard(s,-1);break;
            case "duplicate-association":{var rows=d.NavigationRegions!.Where(n=>n.AssociatedSystemId!=null).ToList();rows[1].AssociatedSystemId=rows[0].AssociatedSystemId;break;}
            case "unknown-association":d.NavigationRegions![0].AssociatedSystemId="system.absent";break;
            case "systems-null":g.StarSystems=null;break;
            case "system-null":g.StarSystems!.Add(null!);break;
            case "system-id-null":g.StarSystems![0].SystemId=null!;break;
            case "duplicate-system":g.StarSystems!.Add(g.StarSystems[0]);break;
            case "colonies-null":g.Colonies=null;break;
            case "colony-null":g.Colonies!.Add(null!);break;
            case "colony-system-null":g.Colonies![0].SystemId=null!;break;
            case "knowledge-null":g.NavigationKnowledge=null;break;
            case "knowledge-row-null":g.NavigationKnowledge!.Add(null!);break;
            case "knowledge-nodes-null":g.NavigationKnowledge![0].Nodes=null;break;
            case "knowledge-node-null":g.NavigationKnowledge![0].Nodes!.Add(null!);break;
            case "knowledge-unknown":g.NavigationKnowledge![0].Nodes![0].Id="nav.absent";break;
            case "knowledge-duplicate":g.NavigationKnowledge![0].Nodes!.Add(g.NavigationKnowledge[0].Nodes![0]);break;
            case "knowledge-level":g.NavigationKnowledge![0].Nodes![0].Level=(NavigationKnowledgeLevel)999;break;
            case "effects-null":g.TraversalEffects=null;break;
            case "effect-null":g.TraversalEffects!.Add(null!);break;
            case "effect-duplicate":{var e=Effect();g.TraversalEffects!.Add(e);g.TraversalEffects.Add(e);break;}
            case "effect-negative":{var e=Effect();e.TraversalCostIncrease=-1;g.TraversalEffects!.Add(e);break;}
            case "effect-unknown":{var e=Effect();e.NodeId="nav.absent";g.TraversalEffects!.Add(e);break;}
            case "effect-both-targets":{var e=Effect();e.ConnectionId=d.NavigationConnections![0].ConnectionId;g.TraversalEffects!.Add(e);break;}
            case "effect-dates":{var e=Effect();e.EndTurn=0;g.TraversalEffects!.Add(e);break;}
            case "effect-edge-penalty":{var e=Effect();e.NodeId=null;e.ConnectionId=d.NavigationConnections![0].ConnectionId;e.MovementBudgetPenalty=1;g.TraversalEffects!.Add(e);break;}
            case "effect-maxcost":{var e=Effect();e.TraversalCostIncrease=long.MaxValue;g.TraversalEffects!.Add(e);break;}
            case "effect-maxpenalty":{var a=Effect();a.MovementBudgetPenalty=long.MaxValue;var b=Effect();b.EffectId+="-two";b.MovementBudgetPenalty=long.MaxValue;g.TraversalEffects!.Add(a);g.TraversalEffects.Add(b);break;}
            case "shared-natural-exact-max":Hazard(s,long.MaxValue-1,shared:true);break;
            case "natural-overflow":Hazard(s,long.MaxValue,shared:true);break;
            case "stats-null":g.NavigationStatistics=null;break;
            case "stats-negative":g.NavigationStatistics!.EdgesCompleted=-1;break;
            case "route-unknown":g.Fleets![0].RouteNodeIds=new(){"nav.absent"};break;
            case "route-unknown-knowledge":{var f=g.Fleets![0];var e=d.NavigationConnections!.First(e=>e.EndpointA==f.LocationNodeId||e.EndpointB==f.LocationNodeId);var target=e.EndpointA==f.LocationNodeId?e.EndpointB:e.EndpointA;f.RouteNodeIds=new(){target};g.NavigationKnowledge!.Single(k=>k.CivilizationId==f.OwnerCivilizationId).Nodes!.Single(n=>n.Id==target).Level=NavigationKnowledgeLevel.Unknown;break;}
            case "fleet-transit":g.Fleets![0].TransitTotalCost=1;g.Fleets[0].TransitProgress=2;break;
            default:throw new ArgumentOutOfRangeException(nameof(name),name,"Unknown test mutation");
        }
    }
}
