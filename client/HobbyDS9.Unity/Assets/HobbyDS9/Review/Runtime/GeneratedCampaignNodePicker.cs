#nullable enable
using System;
using System.Collections.Generic;

namespace HobbyDS9.UnityReview
{
    public readonly struct GeneratedCampaignNodeAnchor
    {
        public string NodeId { get; }
        public double X { get; }
        public double Y { get; }
        public GeneratedCampaignNodeAnchor(string nodeId,double x,double y){NodeId=nodeId;X=x;Y=y;}
    }
    public static class GeneratedCampaignNodePicker
    {
        // Same38x38 projected node hit boxes as the actual Buttons. There is no
        // global nearest selection outside a visible hit area and no private map.
        public static string? Resolve(IEnumerable<GeneratedCampaignNodeAnchor> known,double x,double y)
        {
            if(!Finite(x)||!Finite(y))return null;
            string? selected=null;double best=double.PositiveInfinity;
            foreach(var anchor in known)
            {
                if(string.IsNullOrEmpty(anchor.NodeId)||!Finite(anchor.X)||!Finite(anchor.Y))continue;
                double dx=anchor.X-x,dy=anchor.Y-y;if(Math.Abs(dx)>19||Math.Abs(dy)>19)continue;
                double distance=dx*dx+dy*dy;
                if(distance<best||distance==best&&(selected==null||StringComparer.Ordinal.Compare(anchor.NodeId,selected)<0))
                {selected=anchor.NodeId;best=distance;}
            }
            return selected;
        }
        private static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
    }
}
