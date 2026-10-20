using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private CampaignFrontierPackageDto? frontier;
        private List<CampaignFrontierReceiptDto> discoveries = new List<CampaignFrontierReceiptDto>();
        private readonly List<CampaignFrontierOperationDto> frontierTranscript = new List<CampaignFrontierOperationDto>();
        internal Action? FrontierCommitProbe { get; set; }
        private bool FrontierTranscriptFull=>(frontier!=null&&frontierTranscript.Count>=CampaignFrontierIdentity.TranscriptLimit)
            || (playableHash!=null&&playableTranscript.Count>=playableOperationLimit);

        private static void ValidateFrontierPackage(CampaignFrontierPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==CampaignFrontierIdentity.Format&&p.ProfileId==CampaignFrontierIdentity.Profile
                &&Hash(p.PackageHash)&&p.PolicyId==CampaignFrontierIdentity.Policy&&p.PolicyStatus=="proposed"
                &&p.DefaultActivationAllowed==false&&p.Integrated!=null&&p.PublicLaneIds!=null,"Unsupported frontier opt-in package.");
            ValidateIntegratedPackage(p.Integrated!);
            var d=p.Integrated!.Physical!.CorePackage!.Campaign!.Definitions;
            Require(p.PublicLaneIds!.Count>0&&p.PublicLaneIds.Distinct(StringComparer.Ordinal).Count()==p.PublicLaneIds.Count
                &&p.PublicLaneIds.All(id=>d.NavigationConnections!.Any(e=>e.ConnectionId==id&&e.Kind==GalaxyEdgeKind.Normal)),"Invalid public ordinary lane catalogue.");
            Require(d.NavigationConnections!.All(e=>e.Kind==GalaxyEdgeKind.Normal&&p.PublicLaneIds.Contains(e.ConnectionId,StringComparer.Ordinal))
                &&d.NavigationRegions!.All(n=>n.PhenomenonDefinitionIds!.Count==0)
                &&p.Integrated.Physical.CorePackage.Campaign.InitialState.TraversalEffects!.Count==0,"Frontier review supports only public ordinary hazard-free navigation.");
        }
        private static CampaignFrontierPackageDto CopyFrontierSettings(CampaignFrontierPackageDto p)
            =>new CampaignFrontierPackageDto {FormatVersion=p.FormatVersion,ProfileId=p.ProfileId,PackageHash=p.PackageHash,
                PolicyId=p.PolicyId,PolicyStatus=p.PolicyStatus,DefaultActivationAllowed=p.DefaultActivationAllowed,
                PublicLaneIds=p.PublicLaneIds!.OrderBy(id=>id,StringComparer.Ordinal).ToList()};
        private static CampaignFrontierReceiptDto CopyDiscovery(CampaignFrontierReceiptDto r)
            =>new CampaignFrontierReceiptDto {ActorId=r.ActorId,FleetId=r.FleetId,ArrivalNodeId=r.ArrivalNodeId,Turn=r.Turn,
                ChartedNodeIds=r.ChartedNodeIds.ToList(),ChartedLaneIds=r.ChartedLaneIds.ToList()};
        private static CommitCampaignTacticalRoundDto? CopyFrontierRound(CommitCampaignTacticalRoundDto? c)=>c==null?null:new CommitCampaignTacticalRoundDto {
            ActorId=c.ActorId,CommandId=c.CommandId,EncounterId=c.EncounterId,ExpectedTurn=c.ExpectedTurn,ExpectedRound=c.ExpectedRound,
            ExpectedBoundaryRevision=c.ExpectedBoundaryRevision,Replacements=c.Replacements?.Select(o=>o==null?null!:Copy(o)).ToList()};
        private static CampaignFrontierOperationDto CopyFrontierOperation(CampaignFrontierOperationDto o)
            =>new CampaignFrontierOperationDto {Kind=o.Kind,ActorId=o.ActorId,CommandId=o.CommandId,FleetId=o.FleetId,Enabled=o.Enabled,Turn=o.Turn,Revision=o.Revision,
                Travel=o.Travel==null?null:new SubmitCampaignTravelOrderDto {ActorId=o.Travel.ActorId,CommandId=o.Travel.CommandId,
                    ExpectedTurn=o.Travel.ExpectedTurn,ExpectedRevision=o.Travel.ExpectedRevision,Order=o.Travel.Order==null?null:CampaignTacticalTravelData.Copy(o.Travel.Order)},
                Strategic=SimulationSession.CopyCommand(o.Strategic),Round=CopyFrontierRound(o.Round)};
        private void RecordFrontier(CampaignFrontierOperationDto operation)
        {
            if(frontier!=null)frontierTranscript.Add(CopyFrontierOperation(operation));
            if(playableHash!=null)
            {
                var accepted = CopyFrontierOperation(operation);
                if(accepted.Round?.Replacements!=null)
                    accepted.Round.Replacements=accepted.Round.Replacements.OrderBy(o=>o.FleetId,StringComparer.Ordinal).ToList();
                playableTranscript.Add(new CampaignPlayableOperationDto {Common=accepted});
            }
        }

        public static CampaignTacticalReviewSession StartFrontier(CampaignFrontierPackageDto p)
        {
            ValidateFrontierPackage(p);var i=p.Integrated!;
            return new CampaignTacticalReviewSession(i.Physical!.CorePackage!,null,i.Physical,null,i,null,null,p);
        }
        public static CampaignTacticalReviewSession LoadFrontier(CampaignFrontierPackageDto p,CampaignFrontierSaveDto s)
        {
            ValidateFrontierPackage(p);
            Require(s!=null&&s.FormatVersion==CampaignFrontierIdentity.Format&&s.ProfileId==p.ProfileId&&s.PackageHash==p.PackageHash
                &&s.PolicyId==p.PolicyId&&s.Integrated!=null&&s.Discoveries!=null&&s.Transcript!=null
                &&s.Transcript.Count<=CampaignFrontierIdentity.TranscriptLimit,"Unsupported/incomplete frontier save; no migration.");
            // Never bootstrap authority from historical world snapshots or caller receipts.
            // Rebuild using the exact accepted operations through the normal authoritative APIs.
            var replay=StartFrontier(p);
            foreach(var op in s.Transcript!)
            {
                Require(op!=null,"Missing frontier operation.");
                var expected=CopyFrontierOperation(op);
                CampaignTacticalMutationResult? result=null;
                switch(op.Kind)
                {
                    case FrontierOperationKind.Travel:result=replay.SubmitTravelOrder(op.Travel);break;
                    case FrontierOperationKind.Strategic:result=replay.SubmitStrategicCommand(op.Strategic);break;
                    case FrontierOperationKind.Cloak:
                        Require(op.ActorId!=null&&op.CommandId!=null&&op.FleetId!=null&&op.Enabled.HasValue&&op.Turn.HasValue&&op.Revision.HasValue,"Incomplete cloak operation.");
                        result=replay.SetApproachCloak(op.ActorId!,op.CommandId!,op.FleetId!,op.Enabled!.Value,op.Turn!.Value,op.Revision!.Value);break;
                    case FrontierOperationKind.Advance:case FrontierOperationKind.Finalize:
                        Require(op.Turn.HasValue&&op.Revision.HasValue,"Incomplete boundary operation.");
                        result=op.Kind==FrontierOperationKind.Advance?replay.AdvanceTurn(op.Turn!.Value,op.Revision!.Value):replay.FinalizeBatch(op.Turn!.Value,op.Revision!.Value);break;
                    case FrontierOperationKind.Round:result=replay.CommitRound(op.Round);break;
                }
                Require(result!=null&&result.Accepted,"Illegal/replayed/reordered frontier transcript operation.");
                // Reject extra payloads as well as altered stamps: accepted operation shape is closed.
                Require(FrontierSame(expected,replay.frontierTranscript.Last()),"Unexpected frontier operation payload.");
            }
            Require(FrontierSame(replay.ExportFrontierSave(),s),"Frontier checkpoint/receipts differ from complete accepted-history replay.");
            return replay;
        }
        // Exact structural equality of the closed DTO tree, not a host JSON/hash contract.
        // Types, nulls and list order matter. Property order is explicitly ordinal/portable.
        private static bool FrontierSame(object? a,object? b)
        {
            if(a==null||b==null)return a==null&&b==null;
            var type=a.GetType();if(type!=b.GetType())return false;
            if(type.IsValueType||a is string)return a.Equals(b);
            if(a is IEnumerable left&&b is IEnumerable right)
            {
                var x=left.GetEnumerator();var y=right.GetEnumerator();
                try {while(x.MoveNext()){if(!y.MoveNext()||!FrontierSame(x.Current,y.Current))return false;}return !y.MoveNext();}
                finally {(x as IDisposable)?.Dispose();(y as IDisposable)?.Dispose();}
            }
            return type.GetProperties().OrderBy(p=>p.Name,StringComparer.Ordinal).All(p=>FrontierSame(p.GetValue(a),p.GetValue(b)));
        }
        public CampaignFrontierSaveDto ExportFrontierSave()
        {
            lock(gate)
            {
                Require(frontier!=null,"Not a frontier session.");
                return new CampaignFrontierSaveDto {FormatVersion=CampaignFrontierIdentity.Format,ProfileId=frontier!.ProfileId,
                    PackageHash=frontier.PackageHash,PolicyId=frontier.PolicyId,Integrated=ExportIntegratedCore(),
                    Discoveries=discoveries.Select(CopyDiscovery).ToList(),Transcript=frontierTranscript.Select(CopyFrontierOperation).ToList()};
            }
        }
        public CampaignDiscoveryOverview CaptureDiscovery(string actor)
        {
            lock(gate)
            {
                Require(frontier!=null&&Actor(actor)==CampaignTacticalError.None,"Only a controlled frontier actor may inspect discovery.");
                var graph=new WeightedNavigationGraph(definitions);var game=state.Campaign.GameState;
                var nodes=definitions.NavigationRegions!.Where(n=>graph.KnownNode(game,actor,n.NodeId)).OrderBy(n=>n.NodeId,StringComparer.Ordinal)
                    .Select(n=>new CampaignFrontierNode(n.NodeId,n.DisplayName,n.AssociatedSystemId==null?FrontierNodeKind.Waypoint:FrontierNodeKind.System));
                var lanes=definitions.NavigationConnections!.Where(e=>frontier!.PublicLaneIds!.Contains(e.ConnectionId,StringComparer.Ordinal)
                    &&graph.KnownEdge(game,actor,e.ConnectionId)&&graph.KnownNode(game,actor,e.EndpointA)&&graph.KnownNode(game,actor,e.EndpointB))
                    .OrderBy(e=>e.ConnectionId,StringComparer.Ordinal).Select(e=>new CampaignFrontierLane(e.ConnectionId,e.EndpointA,e.EndpointB,e.BaseTraversalCost));
                return new CampaignDiscoveryOverview(actor,nodes,lanes,discoveries.Where(r=>r.ActorId==actor).Select(r=>new CampaignFrontierReceipt(r)));
            }
        }
        private List<CampaignFrontierReceiptDto> CompleteFrontierPlanning(CampaignTacticalTravelSaveDto next)
        {
            var history=discoveries.Select(CopyDiscovery).ToList();
            if(next.Phase!=SessionPhase.Planning||next.MovementProof==null)return history;
            var proof=next.MovementProof;var before=SimulationSession.CopyState(next.Core.Campaign.GameState);
            var knowledge=next.Core.Campaign.GameState.NavigationKnowledge!;
            var visited=new HashSet<string>(history.Select(r=>r.ActorId+"|"+r.ArrivalNodeId),StringComparer.Ordinal);
            // LastMovement was produced by the physical resolver in this staged transaction,
            // and is checked by the unchanged physical proof replay before publication.
            foreach(var motion in next.LastMovement.OrderBy(r=>r.FleetId,StringComparer.Ordinal))
            {
                var fleet=proof.Start.Fleets!.Single(f=>f.FleetId==motion.FleetId);
                var own=knowledge.Single(k=>k.CivilizationId==fleet.OwnerCivilizationId);
                foreach(var arrived in fleet.RouteNodeIds!.Take(checked((int)motion.EdgesCompleted)))
                {
                    if(!visited.Add(fleet.OwnerCivilizationId+"|"+arrived))continue;
                    var lanes=definitions.NavigationConnections!.Where(e=>frontier!.PublicLaneIds!.Contains(e.ConnectionId,StringComparer.Ordinal)
                        &&(e.EndpointA==arrived||e.EndpointB==arrived)).OrderBy(e=>e.ConnectionId,StringComparer.Ordinal).ToList();
                    var nodes=lanes.SelectMany(e=>new[]{e.EndpointA,e.EndpointB}).Distinct(StringComparer.Ordinal).OrderBy(n=>n,StringComparer.Ordinal).ToList();
                    var row=new CampaignFrontierReceiptDto {ActorId=fleet.OwnerCivilizationId,FleetId=fleet.FleetId,ArrivalNodeId=arrived,Turn=proof.Start.TurnNumber,
                        ChartedNodeIds=new List<string>(),ChartedLaneIds=new List<string>()};
                    void Chart(List<NavigationKnowledgeEntryDto> entries,string id,List<string> changed)
                    {
                        var entry=entries.SingleOrDefault(n=>n.Id==id);
                        if(entry!=null&&entry.Level!=NavigationKnowledgeLevel.Unknown)return;
                        if(entry==null){entry=new NavigationKnowledgeEntryDto {Id=id};entries.Add(entry);}
                        entry.Level=NavigationKnowledgeLevel.Charted;changed.Add(id);
                    }
                    foreach(var id in nodes)Chart(own.Nodes!,id,row.ChartedNodeIds);
                    foreach(var edge in lanes)Chart(own.Connections!,edge.ConnectionId,row.ChartedLaneIds);
                    history.Add(row);
                }
            }
            next.Core.Campaign.GameState.NavigationKnowledge=knowledge.OrderBy(k=>k.CivilizationId,StringComparer.Ordinal).Select(PopulationData.Copy).ToList();
            // Exact knowledge-only allowance after the unchanged movement phase guard.
            var guard=SimulationSession.CopyState(next.Core.Campaign.GameState);guard.NavigationKnowledge=before.NavigationKnowledge;
            SimulationSession.RequirePhaseChanges(before,guard,allowEconomy:false);
            FrontierCommitProbe?.Invoke();
            return history;
        }
        private void PlanFrontierTravel(CampaignTacticalTravelSaveDto next)
        {
            var game=next.Core.Campaign.GameState;var graph=new WeightedNavigationGraph(definitions);
            var visited=new HashSet<string>(discoveries.Select(r=>r.ActorId+"|"+r.ArrivalNodeId),StringComparer.Ordinal);
            foreach(var fleet in game.Fleets!.Where(f=>controllers[f.OwnerCivilizationId]==ControllerKind.AI).OrderBy(f=>f.FleetId,StringComparer.Ordinal))
            {
                if(fleet.RouteNodeIds!.Count>0||fleet.TransitTotalCost>0
                    ||next.Orders.Single(o=>o.FleetId==fleet.FleetId).Intent==CampaignTravelIntent.StayEngage)continue;
                // Candidates are this actor's known IDs, never hidden ownership/neighbors.
                var known=game.NavigationKnowledge!.Single(k=>k.CivilizationId==fleet.OwnerCivilizationId).Nodes!
                    .Where(n=>n.Level!=NavigationKnowledgeLevel.Unknown&&n.Id!=fleet.LocationNodeId&&!visited.Contains(fleet.OwnerCivilizationId+"|"+n.Id))
                    .OrderBy(n=>n.Id,StringComparer.Ordinal).Select(n=>graph.FindRoute(game,fleet.OwnerCivilizationId,fleet.LocationNodeId,n.Id))
                    .Where(r=>r.Success&&r.Steps.Count>0).OrderBy(r=>r.TotalCost).ThenBy(r=>r.Steps.Last(),StringComparer.Ordinal).FirstOrDefault();
                if(known==null)continue;
                var command=new SubmitCampaignTravelOrderDto {ActorId=fleet.OwnerCivilizationId,
                    CommandId="ai-command.frontier.t"+game.TurnNumber.ToString("D10",CultureInfo.InvariantCulture)+"."+fleet.FleetId,
                    ExpectedTurn=game.TurnNumber,ExpectedRevision=game.StateRevision,Order=new CampaignTravelOrderDto {
                        FleetId=fleet.FleetId,Intent=CampaignTravelIntent.Leave,RouteNodeIds=known.Steps.ToList()}};
                Require(ValidateTravelCommand(command,next,true)==CampaignTacticalError.None,"Invalid scoped frontier AI route.");
                ApplyTravelCommand(next,command);
            }
        }
    }
}
