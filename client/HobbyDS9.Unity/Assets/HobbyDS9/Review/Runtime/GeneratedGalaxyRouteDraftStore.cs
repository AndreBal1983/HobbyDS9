#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    public sealed class GeneratedGalaxyRouteDraftAnchorDto
    {
        public string NodeId { get; set; }="";
        public GeneratedGalaxyDraftAnchorKind Kind { get; set; }
    }
    public sealed class GeneratedGalaxyRouteDraftDocument
    {
        public int FormatVersion { get; set; }=1;
        public string MapContentHash { get; set; }="";
        public string PolicyVersion { get; set; }=GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1;
        public GeneratedGalaxyRouteDraftAnchorDto Start { get; set; }=null!;
        public List<GeneratedGalaxyRouteDraftAnchorDto> Targets { get; set; }=null!;
    }
    public sealed class ValidatedGalaxyRouteDraft
    {
        public GeneratedGalaxyRouteDraft Draft { get; }
        public GalaxyReviewRoutePlanSnapshot? Plan { get; }
        internal ValidatedGalaxyRouteDraft(GeneratedGalaxyRouteDraft draft,GalaxyReviewRoutePlanSnapshot? plan){Draft=draft;Plan=plan;}
    }
    // Separate namespace and explicit operation. Map export and companion save are never one transaction.
    public sealed class GeneratedGalaxyRouteDraftStore
    {
        public const string DirectoryName="HobbyDS9GeneratedGalaxyRouteDrafts-v1";
        public const int MaximumBytes=32768;
        private readonly GeneratedGalaxyMapStore storage;
        private readonly ReviewNewtonsoftCodec codec=new ReviewNewtonsoftCodec();
        public GeneratedGalaxyRouteDraftStore(string directory,Action<UnityReviewWriteStage>? probe=null)=>storage=new GeneratedGalaxyMapStore(directory,probe,MaximumBytes);
        public bool Exists(int slot)=>storage.Exists(slot);
        public void Save(int slot,GeneratedGalaxyRouteDraft draft,bool overwrite)
        {
            if(draft.Start==null)throw new InvalidOperationException("Choose a route start first.");
            GeneratedGalaxyRouteDraftAnchorDto Copy(GeneratedGalaxyDraftAnchor a)=>new GeneratedGalaxyRouteDraftAnchorDto{NodeId=a.NodeId,Kind=a.Kind};
            storage.Write(slot,codec.EncodeRouteDraft(new GeneratedGalaxyRouteDraftDocument{MapContentHash=draft.MapIdentity,
                Start=Copy(draft.Start),Targets=draft.Targets.Select(Copy).ToList()}),overwrite);
        }
        // Returns detached validated state. The caller publishes only after this entire operation succeeds.
        // Permissions are supplied now, never read from or granted by the companion file.
        public ValidatedGalaxyRouteDraft Load(int slot,GeneratedGalaxyWorkspace workspace,GalaxyReviewRouteFilterDto currentFilter)
        {
            var saved=codec.DecodeRouteDraft(storage.Read(slot));var map=workspace.Document??throw new InvalidOperationException("No map is open.");
            if(!StringComparer.Ordinal.Equals(saved.MapContentHash,map.ContentHash)||saved.PolicyVersion!=GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1)
                throw new InvalidDataException("Route draft belongs to another map or policy.");
            var start=map.Regions.FirstOrDefault(r=>r.NodeId==saved.Start.NodeId);
            if(start==null||saved.Start.Kind==GeneratedGalaxyDraftAnchorKind.StarLocation&&start.SystemId==null)throw new InvalidDataException("Route start is unavailable.");
            var draft=new GeneratedGalaxyRouteDraft(map.ContentHash,GalaxyReviewRoutePolicy.MaximumTargets);
            draft.SetStart(new GeneratedGalaxyDraftAnchor(saved.Start.NodeId,saved.Start.Kind));
            foreach(var target in saved.Targets)if(!draft.SetTarget(new GeneratedGalaxyDraftAnchor(target.NodeId,target.Kind),true))throw new InvalidDataException("Too many targets.");
            var query=draft.Query(map,currentFilter);
            // A start-only draft still validates its origin/filter through a real zero-crossing query.
            if(query.Targets.Count==0)query.Targets.Add(new GalaxyReviewTargetDto{NodeId=saved.Start.NodeId});
            var plan=workspace.FindRoutePlan(query);
            if(!plan.Success)throw new InvalidDataException("Route draft cannot be restored: "+plan.ErrorCode+" at leg "+plan.FailedLegIndex);
            return new ValidatedGalaxyRouteDraft(draft,saved.Targets.Count==0?null:plan);
        }
    }
}
