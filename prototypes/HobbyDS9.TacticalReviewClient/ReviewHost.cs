using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    // Trusted host serialization, deliberately separate from presentation.
    // Implementations must deep-copy through their save serialization format.
    public interface IReviewHostCodec
    {
        CampaignTacticalReviewPackageDto CopyPackage(CampaignTacticalReviewPackageDto package);
        CampaignTacticalReviewSaveDto CopySave(CampaignTacticalReviewSaveDto save);
    }
    public sealed class ReviewSaveHandle
    {
        internal Guid HostId { get; }
        internal long Id { get; }
        public int Turn { get; }
        public long CampaignRevision { get; }
        internal ReviewSaveHandle(Guid host, long id, CampaignTacticalReviewStatus status)
        { HostId=host; Id=id; Turn=status.Turn; CampaignRevision=status.CampaignRevision; }
        public override string ToString() => "Private committed review checkpoint";
    }
    public sealed partial class ReviewHost
    {
        private readonly object gate = new object();
        private readonly Guid id = Guid.NewGuid();
        private readonly CampaignTacticalReviewPackageDto package;
        private readonly IReviewHostCodec codec;
        private readonly Dictionary<long,CampaignTacticalReviewSaveDto> saves = new Dictionary<long,CampaignTacticalReviewSaveDto>();
        private ICampaignTacticalReviewSession session;
        private long generation, nextSave;
        private readonly IReviewCheckpointStore? checkpointStore;
        private ReviewHost(CampaignTacticalReviewPackageDto package, IReviewHostCodec codec, IReviewCheckpointStore? store = null)
        {
            this.codec=codec ?? throw new ArgumentNullException(nameof(codec));
            this.package=codec.CopyPackage(package);
            checkpointStore=store;
            session=CampaignTacticalReviewApplication.StartReviewPackage(this.package);
        }
        public static ReviewHost Start(CampaignTacticalReviewPackageDto package, IReviewHostCodec codec)
            => new ReviewHost(package,codec);
        public static ReviewHost Start(CampaignTacticalReviewPackageDto package, IReviewHostCodec codec, IReviewCheckpointStore store)
            => new ReviewHost(package,codec,store);
        public ReviewFrame Read(string actor, string? encounter = null)
        { lock(gate) return Capture(session, actor, encounter, generation); }
        private ReviewFrame Capture(ICampaignTacticalReviewSession candidate, string actor, string? encounter, long epoch)
        {
            var status=candidate.CaptureStatus();
            var encounters=candidate.GetEncounters(actor);
            var battle=encounter != null && encounters.Contains(encounter,StringComparer.Ordinal)
                ? candidate.Observe(encounter,actor) : null;
            // InspectOwnShips is pre-batch campaign state while combat is pending.
            // For the selected battle use Observe's current stocks, never those pre-batch pools.
            var own=battle?.OwnShips ?? candidate.InspectOwnShips(actor);
            return new ReviewFrame(id,epoch,actor,status,encounters,own,candidate.GetFleetCloakSettings(actor),candidate.GetOwnStrategicFleets(actor),battle);
        }
        private void RequireCurrent(Guid host, long epoch)
        { if(host != id || epoch != generation) throw new InvalidOperationException("Obsolete or foreign host request; no backend call made."); }
        internal bool IsCurrent(ReviewWork work)
        { lock(gate) return work.HostId == id && work.Generation == generation; }
        public CampaignTacticalMutationResult AdvancePlanning(ReviewFrame observed)
        {
            lock(gate)
            { RequireCurrent(observed.HostId,observed.Generation);
                return session.AdvanceTurn(observed.Status.Turn,observed.Status.CampaignRevision); }
        }
        public CampaignTacticalMutationResult SetApproachCloak(ReviewFrame observed, string commandId, string fleet, bool enabled)
        {
            lock(gate)
            { RequireCurrent(observed.HostId,observed.Generation);
                return session.SetApproachCloak(observed.Actor,commandId,fleet,enabled,observed.Status.Turn,observed.Status.CampaignRevision); }
        }
        public ReviewReceipt Execute(ReviewWork work)
        {
            lock(gate)
            {
                RequireCurrent(work.HostId,work.Generation);
                CampaignTacticalMutationResult result;
                if(work.Action == ReviewAction.Round)
                    result=session.CommitRound(new CommitCampaignTacticalRoundDto {
                        ActorId=work.Actor, CommandId=work.CommandId, EncounterId=work.Encounter,
                        ExpectedTurn=work.Turn, ExpectedRound=work.Round, ExpectedBoundaryRevision=work.BoundaryRevision,
                        Replacements=work.Replacements.Select(o => new CampaignTacticalFleetOrderDto {
                            FleetId=o.Fleet, Maneuver=o.Maneuver, FocusFleetId=o.Focus }).ToList() });
                else
                    result=session.FinalizeBatch(work.Turn,work.CampaignRevision);
                return new ReviewReceipt(work,result,Capture(session,work.Actor,work.Encounter,generation));
            }
        }
        public ReviewSaveHandle SaveCommitted()
        {
            lock(gate)
            {
                // Complete private carrier goes only to the trusted codec/store.
                // Failed serialization leaves every previously valid handle intact.
                var copy=codec.CopySave(session.ExportSave());
                var number=checked(nextSave+1);
                var handle=new ReviewSaveHandle(id,number,session.CaptureStatus());
                saves.Add(number,copy); nextSave=number;
                return handle;
            }
        }
        public ReviewFrame LoadCommitted(ReviewSaveHandle handle, string actor, string? encounter)
        {
            lock(gate)
            {
                if(handle.HostId != id || !saves.TryGetValue(handle.Id,out var saved))
                    throw new ArgumentException("Unknown private checkpoint.",nameof(handle));
                var candidate=CampaignTacticalReviewApplication.LoadReviewSave(package,codec.CopySave(saved));
                var epoch=checked(generation+1);
                // Validate the actor projection before replacing the running session.
                var frame=Capture(candidate,actor,encounter,epoch);
                session=candidate; generation=epoch;
                return frame;
            }
        }
    }
}
