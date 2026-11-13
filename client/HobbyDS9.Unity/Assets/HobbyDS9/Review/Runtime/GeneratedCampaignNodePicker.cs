#nullable enable
using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;

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
        public static string? ResolveRegion(IEnumerable<GeneratedGalaxyRegionDto> known,double x,double y)
        {
            if(!Finite(x)||!Finite(y))return null;
            string? selected=null;
            foreach(var region in known)
            {
                var points=region.Boundary;if(points==null||points.Count<3)continue;
                bool inside=false,onEdge=false;
                for(int i=0,j=points.Count-1;i<points.Count;j=i++)
                {
                    var a=points[j];var b=points[i];
                    double cross=((double)b.X-a.X)*(y-a.Y)-((double)b.Y-a.Y)*(x-a.X);
                    if(cross==0&&x>=Math.Min(a.X,b.X)&&x<=Math.Max(a.X,b.X)&&y>=Math.Min(a.Y,b.Y)&&y<=Math.Max(a.Y,b.Y)){onEdge=true;break;}
                    if((a.Y>y)!=(b.Y>y)&&x<a.X+(y-a.Y)*((double)b.X-a.X)/((double)b.Y-a.Y))inside=!inside;
                }
                if((inside||onEdge)&&(selected==null||StringComparer.Ordinal.Compare(region.NodeId,selected)<0))selected=region.NodeId;
            }
            return selected;
        }
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
