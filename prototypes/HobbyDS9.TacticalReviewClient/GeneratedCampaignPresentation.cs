using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    // Explicit capability: v1 codecs/stores are never probed as a fallback.
    public interface IKnownGeneratedMapCodec
    {
        CampaignGeneratedMapDto CopyKnownGeneratedMap(CampaignGeneratedMapDto map);
    }
    public interface IStaticAstronomyReviewHostCodec : IGeneratedCampaignReviewHostCodec,IKnownGeneratedMapCodec
    {
        CampaignGeneratedSaveDto CopyStaticAstronomySave(CampaignGeneratedSaveDto root);
    }
    public interface IStaticAstronomyReviewCheckpointStore
    {
        void Write(string slot, CampaignGeneratedSaveDto root, bool overwrite);
        CampaignGeneratedSaveDto Read(string slot);
    }

    // Optional save-only capability. The host supplies its private closed-codec
    // snapshot; implementations must run complete authoritative load validation.
    // This is never an admission path for a caller-owned or file-owned root.
    public interface IStaticAstronomyOwnedExportValidator
    {
        void ValidateOwnedStaticAstronomyExport(CampaignPlayableFleetPackageDto template, CampaignGeneratedSaveDto ownedRoot);
    }

    // Constructed only by the host while holding its observation gate. Map access
    // returns a fresh detached actor-known DTO; callers cannot mutate the cached pair.
    public sealed class GeneratedCampaignPresentation
    {
        private readonly CampaignGeneratedMapDto map;
        private readonly IKnownGeneratedMapCodec codec;
        internal Guid HostId { get; }
        public ReviewFrame Frame { get; }
        public long Generation => Frame.Generation;
        public string Actor => Frame.Actor;
        public string MapContentHash => map.MapContentHash;
        public CampaignGeneratedMapDto KnownMap => codec.CopyKnownGeneratedMap(map);
        public CampaignStaticAstronomySnapshot Astronomy { get; }
        internal GeneratedCampaignPresentation(ReviewFrame frame, CampaignGeneratedMapDto knownMap,
            CampaignStaticAstronomySnapshot astronomy, IKnownGeneratedMapCodec codec)
        {
            if(knownMap.ActorId!=frame.Actor || astronomy.ActorId!=frame.Actor
                ||knownMap.MapContentHash!=astronomy.MapContentHash
                ||knownMap.ObservationTurn!=frame.Status.Turn || astronomy.ObservationTurn!=frame.Status.Turn
                ||knownMap.CampaignRevision!=frame.Status.CampaignRevision || astronomy.CampaignRevision!=frame.Status.CampaignRevision
                ||astronomy.ObservationPolicy!=CampaignStaticAstronomyIdentity.ObservationPolicy)
                throw new InvalidOperationException("Mismatched generated presentation observations.");
            this.codec=codec; map=codec.CopyKnownGeneratedMap(knownMap);
            HostId=frame.HostId; Frame=frame; Astronomy=astronomy;
        }
    }
}
