#nullable enable
using System;
using System.Collections.Generic;

namespace HobbyDS9.UnityReview
{
    // Location anchors only: neither kind submits a visit, survey or fleet order.
    public enum GeneratedGalaxyDraftAnchorKind { RegionTransit,StarLocation }
    public sealed class GeneratedGalaxyDraftAnchor
    {
        public string NodeId { get; }
        public GeneratedGalaxyDraftAnchorKind Kind { get; }
        public GeneratedGalaxyDraftAnchor(string nodeId,GeneratedGalaxyDraftAnchorKind kind)
        {
            if(string.IsNullOrEmpty(nodeId))throw new ArgumentException("A node is required.",nameof(nodeId));
            NodeId=nodeId;Kind=kind;
        }
    }

    // Host editing state, never a pathfinder. Portable ordered-leg validation remains authoritative.
    // The receiving host must supply the portable contract's queue bound, not a UI-only hard cap.
    public sealed class GeneratedGalaxyRouteDraft
    {
        private readonly List<GeneratedGalaxyDraftAnchor> targets=new List<GeneratedGalaxyDraftAnchor>();
        public string MapIdentity { get; private set; }
        public int MaximumTargets { get; }
        public GeneratedGalaxyDraftAnchor? Start { get; private set; }
        public IReadOnlyList<GeneratedGalaxyDraftAnchor> Targets=>Array.AsReadOnly(targets.ToArray());
        public GeneratedGalaxyRouteDraft(string mapIdentity,int maximumTargets)
        {
            if(string.IsNullOrEmpty(mapIdentity))throw new ArgumentException("Map identity is required.",nameof(mapIdentity));
            if(maximumTargets<1)throw new ArgumentOutOfRangeException(nameof(maximumTargets));
            MapIdentity=mapIdentity;MaximumTargets=maximumTargets;
        }
        public void ChangeMap(string mapIdentity)
        {
            if(string.IsNullOrEmpty(mapIdentity))throw new ArgumentException("Map identity is required.",nameof(mapIdentity));
            if(StringComparer.Ordinal.Equals(MapIdentity,mapIdentity))return;
            MapIdentity=mapIdentity;Start=null;targets.Clear();
        }
        public void SetStart(GeneratedGalaxyDraftAnchor start)=>Start=start??throw new ArgumentNullException(nameof(start));
        public bool SetTarget(GeneratedGalaxyDraftAnchor target,bool append)
        {
            if(target==null)throw new ArgumentNullException(nameof(target));
            if(Start==null)return false;
            if(append&&targets.Count>=MaximumTargets)return false;
            if(!append)targets.Clear();
            targets.Add(target);return true;
        }
        public bool RemoveLast()
        {
            if(targets.Count==0)return false;
            targets.RemoveAt(targets.Count-1);return true;
        }
        public void Clear()=>targets.Clear();
    }
}
