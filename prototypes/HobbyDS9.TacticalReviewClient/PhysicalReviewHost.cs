using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    // Complete physical root/package only. The B1 host/codec/store remain separately typed.
    public interface IPhysicalReviewHostCodec
    {
        CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto package);
        CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto save);
    }
    public interface IPhysicalReviewCheckpointStore
    {
        void Write(string slot,CampaignTacticalTravelSaveDto privateRoot,bool overwrite);
        CampaignTacticalTravelSaveDto Read(string slot);
    }
    public sealed class PhysicalHostOpenResult
    {
        public ReviewCheckpointError Error { get; }
        public bool Succeeded => Error == ReviewCheckpointError.None;
        public PhysicalReviewHost? Host { get; }
        public ReviewFrame? Frame { get; }
        internal PhysicalHostOpenResult(ReviewCheckpointError error,PhysicalReviewHost? host=null,ReviewFrame? frame=null)
        { Error=error; Host=host; Frame=frame; }
    }
    public sealed partial class PhysicalReviewHost : ITacticalClientHost
    {
        private readonly object gate=new object();
        private readonly Guid id=Guid.NewGuid();
        private readonly CampaignTacticalTravelPackageDto? package;
        private readonly IPhysicalReviewHostCodec codec;
        private readonly IPhysicalReviewCheckpointStore? store;
        private readonly Dictionary<long,CampaignTacticalTravelSaveDto> saves=new Dictionary<long,CampaignTacticalTravelSaveDto>();
        private ICampaignTacticalTravelSession session;
        private long generation,nextSave;
        private PhysicalReviewHost(CampaignTacticalTravelPackageDto package,IPhysicalReviewHostCodec codec,IPhysicalReviewCheckpointStore? store)
        {
            this.codec=codec; this.package=codec.CopyPackage(package); this.store=store;
            session=CampaignTacticalTravelApplication.StartReviewPackage(this.package);
        }
        public static PhysicalReviewHost Start(CampaignTacticalTravelPackageDto package,IPhysicalReviewHostCodec codec,IPhysicalReviewCheckpointStore? store=null)
            => new PhysicalReviewHost(package,codec,store);
        private ReviewFrame Capture(ICampaignTacticalTravelSession candidate,string actor,string? encounter,long epoch)
        {
            var status=candidate.CaptureStatus(); var encounters=candidate.GetEncounters(actor);
            var battle=encounter != null && encounters.Contains(encounter,StringComparer.Ordinal) ? candidate.Observe(encounter,actor) : null;
            var own=battle?.OwnShips ?? candidate.InspectOwnShips(actor);
            var travel=new PhysicalTravelView(candidate.GetOwnTravelOrders(actor),candidate.GetOwnTravelReceipts(actor),candidate.GetKnownContacts(actor));
            return new ReviewFrame(id,epoch,actor,status,encounters,own,candidate.GetFleetCloakSettings(actor),candidate.GetOwnStrategicFleets(actor),battle,travel,(candidate as ICampaignIntegratedPlaytestSession)?.CaptureStrategy(actor));
        }
        public ReviewFrame Read(string actor,string? encounter=null)
        { lock(gate) return Capture(session,actor,encounter,generation); }
        private void RequireCurrent(Guid host,long epoch)
        { if(host != id || epoch != generation) throw new InvalidOperationException("Obsolete or foreign physical host request."); }
        bool ITacticalClientHost.IsCurrent(ReviewWork work)
        { lock(gate) return work.HostId == id && work.Generation == generation; }
        public KnownRouteOffer FindKnownRoute(ReviewFrame observed,string fleet,string destination)
        {
            lock(gate)
            {
                RequireCurrent(observed.HostId,observed.Generation);
                var own=session.GetOwnStrategicFleets(observed.Actor).Single(f=>f.FleetId.Value == fleet);
                var status=session.CaptureStatus();
                var route=session.FindKnownRoute(observed.Actor,own.LocationNodeId.Value,destination);
                return new KnownRouteOffer(id,generation,observed.Actor,fleet,own.LocationNodeId.Value,destination,status,route);
            }
        }
        public CampaignTacticalMutationResult SubmitTravelOrder(SubmitCampaignTravelOrderDto command)
        { lock(gate) return session.SubmitTravelOrder(command); }
        public CampaignTacticalMutationResult AdvancePlanning(ReviewFrame observed)
        { lock(gate) { RequireCurrent(observed.HostId,observed.Generation); return session.AdvanceTurn(observed.Status.Turn,observed.Status.CampaignRevision); } }
        public CampaignTacticalMutationResult SetApproachCloak(ReviewFrame observed,string command,string fleet,bool enabled)
        { lock(gate) { RequireCurrent(observed.HostId,observed.Generation); return session.SetApproachCloak(observed.Actor,command,fleet,enabled,observed.Status.Turn,observed.Status.CampaignRevision); } }
        public ReviewReceipt Execute(ReviewWork work)
        {
            lock(gate)
            {
                RequireCurrent(work.HostId,work.Generation);
                CampaignTacticalMutationResult result;
                if(work.Action == ReviewAction.Travel)
                {
                    var edit=work.Travel!;
                    result=session.SubmitTravelOrder(new SubmitCampaignTravelOrderDto {
                        ActorId=work.Actor,CommandId=work.CommandId,ExpectedTurn=work.Turn,ExpectedRevision=work.CampaignRevision,
                        Order=new CampaignTravelOrderDto { FleetId=edit.Fleet,Intent=edit.Intent,ContactFleetId=edit.ContactFleet,
                            ExpectedContactSeenTurn=edit.ContactSeenTurn,RouteNodeIds=edit.Route.ToList() } });
                }
                else if(work.Action == ReviewAction.Round)
                    result=session.CommitRound(new CommitCampaignTacticalRoundDto {
                        ActorId=work.Actor,CommandId=work.CommandId,EncounterId=work.Encounter,ExpectedTurn=work.Turn,
                        ExpectedRound=work.Round,ExpectedBoundaryRevision=work.BoundaryRevision,
                        Replacements=work.Replacements.Select(o=>new CampaignTacticalFleetOrderDto { FleetId=o.Fleet,Maneuver=o.Maneuver,FocusFleetId=o.Focus }).ToList() });
                else result=session.FinalizeBatch(work.Turn,work.CampaignRevision);
                return new ReviewReceipt(work,result,Capture(session,work.Actor,work.Encounter,generation));
            }
        }
        public ReviewSaveHandle SaveCommitted()
        {
            lock(gate)
            {
                if(integratedPackage!=null)return SaveIntegratedCommitted();
                var save=codec.CopySave(session.ExportSave()); var number=checked(nextSave+1);
                var handle=new ReviewSaveHandle(id,number,session.CaptureStatus());
                saves.Add(number,save); nextSave=number; return handle;
            }
        }
        public ReviewFrame LoadCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            lock(gate)
            {
                if(integratedPackage!=null)return LoadIntegratedCommitted(handle,actor,encounter);
                if(handle.HostId != id || !saves.TryGetValue(handle.Id,out var save)) throw new ArgumentException("Unknown private physical checkpoint.");
                var candidate=CampaignTacticalTravelApplication.LoadReviewSave(package!,codec.CopySave(save));
                var epoch=checked(generation+1); var frame=Capture(candidate,actor,encounter,epoch);
                session=candidate; generation=epoch; return frame;
            }
        }
        public ReviewCheckpointResult SaveCommittedToSlot(string slot,bool overwrite=false)
        {
            lock(gate)
            {
                if(integratedPackage!=null)return SaveIntegratedToSlot(slot,overwrite);
                if(store == null) return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
                CampaignTacticalTravelSaveDto save;
                try { save=codec.CopySave(session.ExportSave()); } catch(Exception) { return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed); }
                var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
                try { store.Write(slot,save,overwrite); }
                catch(ReviewCheckpointStoreException e) { return new ReviewCheckpointResult(e.Error); }
                catch(Exception) { return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed); }
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);
            }
        }
        public ReviewCheckpointResult LoadCommittedFromSlot(string slot,string actor,string? encounter=null)
        {
            lock(gate)
            {
                if(integratedPackage!=null)return LoadIntegratedFromSlot(slot,actor,encounter);
                if(store == null) return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
                CampaignTacticalTravelSaveDto save;
                try { save=store.Read(slot); }
                catch(ReviewCheckpointStoreException e) { return new ReviewCheckpointResult(e.Error); }
                catch(Exception) { return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed); }
                try
                {
                    var candidate=CampaignTacticalTravelApplication.LoadReviewSave(package!,codec.CopySave(save));
                    var epoch=checked(generation+1); var frame=Capture(candidate,actor,encounter,epoch);
                    var metadata=new ReviewSlotCheckpoint(slot,frame.Status);
                    session=candidate; generation=epoch;
                    return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);
                }
                catch(Exception) { return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint); }
            }
        }
        public static PhysicalHostOpenResult OpenFromSlot(CampaignTacticalTravelPackageDto package,IPhysicalReviewHostCodec codec,
            IPhysicalReviewCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try
            {
                var candidate=new PhysicalReviewHost(package,codec,store);
                var loaded=candidate.LoadCommittedFromSlot(slot,actor,encounter);
                return loaded.Succeeded ? new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,loaded.Frame)
                    : new PhysicalHostOpenResult(loaded.Error);
            }
            catch(Exception) { return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint); }
        }
        ClientHostReopenResult ITacticalClientHost.Reopen(string slot,string actor,string? encounter)
        {
            lock(gate)
            {
                if(integratedPackage!=null)
                {
                    if(integratedStore==null)return new ClientHostReopenResult(ReviewCheckpointError.NoStore);
                    var opened=OpenIntegratedFromSlot(integratedPackage,integratedCodec!,integratedStore,slot,actor,encounter);
                    return new ClientHostReopenResult(opened.Error,opened.Host,opened.Frame);
                }
                if(store == null) return new ClientHostReopenResult(ReviewCheckpointError.NoStore);
                var physicalOpened=OpenFromSlot(package!,codec,store,slot,actor,encounter);
                return new ClientHostReopenResult(physicalOpened.Error,physicalOpened.Host,physicalOpened.Frame);
            }
        }
    }
}
