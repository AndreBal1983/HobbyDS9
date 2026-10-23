#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public enum GeneratedGalaxySelectionKind { Region,System,HomeCandidate }
    // Draws supplied geometry. No generation, pathfinding, contact or ownership rules live here.
    public sealed class GeneratedGalaxyMapView : VisualElement
    {
        private readonly GeneratedGalaxyMapData document;
        private readonly Dictionary<string,GeneratedGalaxyRegionDto> regions;
        private readonly Dictionary<string,GeneratedGalaxyPointDto> systemPositions;
        private readonly Dictionary<string,Rect> regionBounds;
        private readonly GeneratedGalaxyStarIndex starIndex;
        private readonly Dictionary<string,string> candidateNames;
        private readonly Action<string,GeneratedGalaxySelectionKind> selected;
        private readonly GeneratedGalaxyViewport viewport=new GeneratedGalaxyViewport();
        private readonly List<(Button button,Label label,GeneratedGalaxyRegionDto region,bool candidate)> stars=
            new List<(Button,Label,GeneratedGalaxyRegionDto,bool)>();
        private string? selectedNode,routeOrigin;
        private GeneratedGalaxySelectionKind selectionKind;
        private readonly List<(Button button,string node)> candidateButtons=new List<(Button,string)>();
        private readonly List<(Button button,EnvironmentGalaxyFeatureDto feature)> featureButtons=new List<(Button,EnvironmentGalaxyFeatureDto)>();
        private string? selectedFeature;
        private IReadOnlyList<string> routeSteps=Array.Empty<string>();
        private IReadOnlyList<GeneratedGalaxyRoutePoint> routePath=Array.Empty<GeneratedGalaxyRoutePoint>();
        private IReadOnlyList<IReadOnlyList<GeneratedGalaxyRoutePoint>>? planPaths;
        private readonly List<(Label label,string node)> pinLabels=new List<(Label,string)>();
        private string[] pinnedNodes=Array.Empty<string>();
        private int pointer=-1;
        private Vector2 press,last;
        private bool moved;
        public bool ShowGraphLinks { get; private set; }
        public bool ShowFeatures { get; private set; }=true;
        public bool ShowInfluenceAreas { get; private set; }
        public double Zoom=>viewport.Zoom;
        public GeneratedGalaxyMapView(GeneratedGalaxyMapData map,IReadOnlyDictionary<string,string> civilizations,
            Action<Button,Action> bindButton,Action<string,GeneratedGalaxySelectionKind> select,Action<string> target,Action<string>? inspectFeature=null,
            Action<string,bool,GeneratedGalaxyDraftAnchorKind>? targetIntent=null)
        {
            void Target(string node,bool append,GeneratedGalaxyDraftAnchorKind kind)
            {if(targetIntent!=null)targetIntent(node,append,kind);else target(node);}
            document=map;regions=map.Regions.ToDictionary(r=>r.NodeId,StringComparer.Ordinal);
            systemPositions=map.Systems.ToDictionary(s=>s.SystemId,s=>map.SystemPosition(s.SystemId),StringComparer.Ordinal);
            starIndex=new GeneratedGalaxyStarIndex(map.Systems.Select(s=>(s.NodeId,systemPositions[s.SystemId])),map.Width,map.Height);
            regionBounds=map.Regions.ToDictionary(r=>r.NodeId,r=>Rect.MinMaxRect(r.Boundary.Min(p=>p.X),r.Boundary.Min(p=>p.Y),r.Boundary.Max(p=>p.X),r.Boundary.Max(p=>p.Y)),StringComparer.Ordinal);
            candidateNames=map.HomeCandidates.ToDictionary(h=>h.NodeId,h=>civilizations[h.CivilizationDefinitionId],StringComparer.Ordinal);
            selected=select;name="generated-galaxy-map";AddToClassList("galaxy-map");
            focusable=true;viewport.SetMap(map.Width,map.Height);
            viewport.MaximumZoom=!map.IsLarge?8:32;
            generateVisualContent+=Paint;
            foreach(var system in map.Systems)
            {
                var region=regions[system.NodeId];bool candidate=candidateNames.ContainsKey(region.NodeId);
                var button=new Button{name="galaxy-star-"+system.SystemId,text="●",tooltip=system.DisplayName+" · select star; right-click to preview a route."};
                button.AddToClassList("galaxy-star");button.EnableInClassList("galaxy-candidate",candidate);
                bindButton(button,()=>{if(Live())select(region.NodeId,GeneratedGalaxySelectionKind.System);});Add(button);
                button.RegisterCallback<PointerUpEvent>(e=>{if(e.button==1&&Live()){Target(region.NodeId,e.shiftKey,GeneratedGalaxyDraftAnchorKind.StarLocation);e.StopPropagation();}});
                button.RegisterCallback<KeyDownEvent>(e=>{
                    if(e.keyCode==KeyCode.F10&&e.shiftKey&&Live()
                        &&ReferenceEquals(panel.focusController.focusedElement,button)){Target(region.NodeId,false,GeneratedGalaxyDraftAnchorKind.StarLocation);e.StopPropagation();}
                });
                var label=new Label(candidate?system.DisplayName+"\n"+candidateNames[region.NodeId]+" · candidate":system.DisplayName)
                    {name="galaxy-label-"+system.SystemId,pickingMode=PickingMode.Ignore};
                label.AddToClassList("galaxy-system-label");label.EnableInClassList("galaxy-candidate-label",candidate);Add(label);
                stars.Add((button,label,region,candidate));
            }
            foreach(var candidate in map.HomeCandidates)
            {
                var button=new Button{name="galaxy-candidate-"+candidate.CivilizationDefinitionId,text="⌜",
                    tooltip=candidateNames[candidate.NodeId]+" · generated starting candidate; inspect"};
                button.AddToClassList("galaxy-candidate-target");Add(button);
                bindButton(button,()=>{if(Live())select(candidate.NodeId,GeneratedGalaxySelectionKind.HomeCandidate);});
                candidateButtons.Add((button,candidate.NodeId));
            }
            if(map.HasEnvironment)foreach(var feature in map.Features.Where(f=>f.Kind!=GalaxyEnvironmentFeatureKind.Star))
            {
                var button=new Button{name="galaxy-feature-"+feature.FeatureId,text=feature.Kind==GalaxyEnvironmentFeatureKind.Nebula?"◇":"",
                    tooltip=feature.DisplayName+" · inspect generation influence"};
                button.AddToClassList("galaxy-feature-target");Add(button);
                bindButton(button,()=>{if(Live())inspectFeature?.Invoke(feature.FeatureId);});
                featureButtons.Add((button,feature));
            }
            // Star hit targets win over any overlapping candidate corner. Candidates also have inspector access.
            foreach(var star in stars)star.button.BringToFront();
            // Arbitrate pointer hits before an overlapping child Button can claim them by paint order.
            RegisterCallback<PointerDownEvent>(e=>{
                if(!Live()||(e.button!=0&&e.button!=1)||NearestStar(this.WorldToLocal(e.position))==null)return;
                pointer=e.pointerId;press=last=this.WorldToLocal(e.position);moved=false;this.CapturePointer(pointer);
                e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(e=>{
                if(!Live()||(e.button!=0&&e.button!=1)||pointer==e.pointerId&&moved)return;
                var node=NearestStar(this.WorldToLocal(e.position));if(node==null)return;
                if(pointer==e.pointerId){this.ReleasePointer(pointer);pointer=-1;}
                if(e.button==1)Target(node,e.shiftKey,GeneratedGalaxyDraftAnchorKind.StarLocation);else selected(node,GeneratedGalaxySelectionKind.System);
                e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            RegisterCallback<GeometryChangedEvent>(_=>{viewport.Resize(contentRect.width,contentRect.height);Place();});
            RegisterCallback<WheelEvent>(e=>{if(!Live())return;var p=ToContent(e.localMousePosition);viewport.ZoomAt(e.delta.y>0?1/1.2:1.2,p.x,p.y);Place();e.StopPropagation();});
            RegisterCallback<PointerDownEvent>(e=>{
                if(!Live()||(e.button!=0&&e.button!=2)||e.button==0&&!ReferenceEquals(e.target,this))return;
                pointer=e.pointerId;press=last=e.localPosition;moved=false;this.CapturePointer(pointer);e.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(e=>{
                if(pointer!=e.pointerId||!Live())return;var position=(Vector2)e.localPosition;
                if((position-press).sqrMagnitude>16)moved=true;
                if(moved){viewport.Pan(position.x-last.x,position.y-last.y);Place();}last=position;e.StopPropagation();
            });
            RegisterCallback<PointerUpEvent>(e=>{
                if(!Live())return;
                if(e.pointerId==pointer)
                {
                    this.ReleasePointer(pointer);pointer=-1;
                    if(!moved&&e.button==0){var region=At(e.localPosition);if(region!=null)select(region.NodeId,GeneratedGalaxySelectionKind.Region);}
                    e.StopPropagation();
                }
                else if(e.button==1&&ReferenceEquals(e.target,this))
                {var region=At(e.localPosition);if(region!=null)Target(region.NodeId,e.shiftKey,GeneratedGalaxyDraftAnchorKind.RegionTransit);e.StopPropagation();}
            });
            RegisterCallback<PointerCaptureOutEvent>(_=>pointer=-1);
            RegisterCallback<KeyDownEvent>(e=>{
                if(!Live()||!ReferenceEquals(panel.focusController.focusedElement,this))return;
                switch(e.keyCode)
                {
                    case KeyCode.LeftArrow:Pan(60,0);break;
                    case KeyCode.RightArrow:Pan(-60,0);break;
                    case KeyCode.UpArrow:Pan(0,60);break;
                    case KeyCode.DownArrow:Pan(0,-60);break;
                    default:return;
                }
                e.StopPropagation();
            });
        }
        private bool Live()=>panel!=null&&enabledInHierarchy;
        public void Fit(){viewport.Fit();Place();}
        public void ZoomBy(double factor){viewport.ZoomAt(factor,contentRect.width/2,contentRect.height/2);Place();}
        public void Pan(double x,double y){viewport.Pan(x,y);Place();}
        public void ToggleGraphLinks(){ShowGraphLinks=!ShowGraphLinks;MarkDirtyRepaint();}
        public void ToggleFeatures(){ShowFeatures=!ShowFeatures;Place();}
        public void ToggleInfluenceAreas(){ShowInfluenceAreas=!ShowInfluenceAreas;MarkDirtyRepaint();}
        public void SelectFeature(string? feature){selectedFeature=feature;MarkDirtyRepaint();}
        public void Select(string node,GeneratedGalaxySelectionKind kind){selectedNode=node;selectionKind=kind;Place();}
        public void Route(string? origin,IReadOnlyList<string>? steps)
        {
            planPaths=null;pinnedNodes=Array.Empty<string>();foreach(var pin in pinLabels)pin.label.RemoveFromHierarchy();pinLabels.Clear();
            var acceptedSteps=steps??Array.Empty<string>();
            var path=origin==null?Array.Empty<GeneratedGalaxyRoutePoint>():GeneratedGalaxyRoutePath.Build(regions,origin,acceptedSteps);
            routeOrigin=origin;routeSteps=acceptedSteps;routePath=path;Place();
        }
        public void RoutePlan(string? origin,GalaxyReviewRoutePlanSnapshot? plan,IReadOnlyList<GeneratedGalaxyDraftAnchor> targets)
        {
            Route(null,null);routeOrigin=origin;routeSteps=plan?.Success==true?plan.Steps:Array.Empty<string>();
            planPaths=plan==null?Array.Empty<IReadOnlyList<GeneratedGalaxyRoutePoint>>():GeneratedGalaxyRoutePath.BuildPlan(regions,plan);
            routePath=planPaths.Count==1?planPaths[0]:Array.Empty<GeneratedGalaxyRoutePoint>();
            pinnedNodes=targets.Select(t=>t.NodeId).ToArray();
            for(int i=0;i<pinnedNodes.Length;i++)
            {
                var label=new Label((i+1).ToString()){name="galaxy-route-pin-"+(i+1),pickingMode=PickingMode.Ignore};
                label.AddToClassList("galaxy-route-pin");Add(label);pinLabels.Add((label,pinnedNodes[i]));
            }
            Place();
        }
        public IReadOnlyList<Vector2> RoutePositions()=>routePath.Select(p=>contentRect.position+
            new Vector2((float)viewport.X(p.X),(float)viewport.Y(p.Y))).ToArray();
        public IReadOnlyList<IReadOnlyList<Vector2>> RouteLegPositions()=>planPaths==null?Array.Empty<IReadOnlyList<Vector2>>():
            planPaths.Select(leg=>(IReadOnlyList<Vector2>)leg.Select(Project).ToArray()).ToArray();
        public Vector2 Position(string node)=>Point(regions[node].Center);
        public Vector2 SystemPosition(string systemId)=>Point(systemPositions[systemId]);
        public string? NearestStar(Vector2 local)
        {
            if(!contentRect.Contains(local))return null;var p=ToContent(local);
            return starIndex.Nearest(viewport.WorldX(p.x),viewport.WorldY(p.y),13/viewport.Scale);
        }
        public Vector2 FeaturePosition(string featureId)=>Point(document.Features.Single(f=>f.FeatureId==featureId).Position);
        // Painter and pointer coordinates are border-box local; the viewport is content-box local.
        private Vector2 Point(GeneratedGalaxyPointDto point)=>contentRect.position+new Vector2((float)viewport.X(point.X),(float)viewport.Y(point.Y));
        private Vector2 ToContent(Vector2 local)=>local-contentRect.position;
        private void PlaceChild(VisualElement child,Vector2 local)
        {
            // Absolute left/top start inside the parent's border, unlike Painter2D/LocalToWorld.
            child.style.left=local.x-resolvedStyle.borderLeftWidth;
            child.style.top=local.y-resolvedStyle.borderTopWidth;
        }
        private GeneratedGalaxyRegionDto? At(Vector2 screen)
        {
            screen=ToContent(screen);
            double x=viewport.WorldX(screen.x),y=viewport.WorldY(screen.y);
            foreach(var region in document.Regions)
            {
                bool inside=false;var points=region.Boundary;
                for(int i=0,j=points.Count-1;i<points.Count;j=i++)
                    if((points[i].Y>y)!=(points[j].Y>y)&&x<(double)(points[j].X-points[i].X)*(y-points[i].Y)/(points[j].Y-points[i].Y)+points[i].X)inside=!inside;
                if(inside)return region;
            }
            return null;
        }
        private void Place()
        {
            foreach(var pin in pinLabels){var p=Position(pin.node);PlaceChild(pin.label,p+new Vector2(10,-25));pin.label.style.display=Visible(p,20)?DisplayStyle.Flex:DisplayStyle.None;}
            var occupied=new List<Rect>();
            var diagonals=regionBounds.Values.Select(ScreenBounds).Where(r=>r.Overlaps(contentRect)).Select(r=>r.size.magnitude).OrderBy(d=>d).ToArray();
            float typical=diagonals.Length==0?0:diagonals[diagonals.Length/2];
            int labelBudget=!document.IsLarge?int.MaxValue:Math.Min(typical<40?12:typical<90?36:80,
                Math.Max(4,(int)(contentRect.width*contentRect.height/18000)));
            // Important labels get first choice. Suppression changes labels, never underlying nodes.
            foreach(var star in stars.OrderBy(s=>s.region.NodeId==selectedNode?0:s.region.NodeId==routeOrigin||routeSteps.LastOrDefault()==s.region.NodeId?1:s.candidate?2:3).ThenBy(s=>s.region.NodeId,StringComparer.Ordinal))
            {
                var point=SystemPosition(star.region.SystemId!);var bounds=contentRect;
                bool visible=point.x>=bounds.xMin-20&&point.y>=bounds.yMin-20&&point.x<=bounds.xMax+20&&point.y<=bounds.yMax+20;
                PlaceChild(star.button,point-new Vector2(14,14));
                star.button.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;
                star.button.EnableInClassList("galaxy-star-selected",star.region.NodeId==selectedNode&&selectionKind==GeneratedGalaxySelectionKind.System);
                bool important=star.region.NodeId==selectedNode||star.candidate||star.region.NodeId==routeOrigin||routeSteps.LastOrDefault()==star.region.NodeId;
                star.button.tabIndex=!document.IsLarge||important?0:-1;
                bool show=visible&&occupied.Count<labelBudget&&(important||document.IsLarge||viewport.Zoom>=1.6);
                var rect=new Rect(point.x+17,point.y-10,star.candidate?200:120,star.candidate?46:24);
                if(rect.xMax>bounds.xMax-8)rect.x=point.x-rect.width-17;
                rect.y=Mathf.Clamp(rect.y,bounds.yMin+6,Mathf.Max(bounds.yMin+6,bounds.yMax-rect.height-6));
                if(occupied.Any(r=>r.Overlaps(rect)))show=false;
                if(show)occupied.Add(rect);
                PlaceChild(star.label,rect.position);star.label.style.width=rect.width;
                star.label.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
            }
            foreach(var candidate in candidateButtons)
            {
                var point=Position(candidate.node);PlaceChild(candidate.button,point-new Vector2(38,38));
                candidate.button.style.display=Visible(point,40)?DisplayStyle.Flex:DisplayStyle.None;
                candidate.button.EnableInClassList("galaxy-star-selected",candidate.node==selectedNode&&selectionKind==GeneratedGalaxySelectionKind.HomeCandidate);
            }
            foreach(var feature in featureButtons)
            {
                PlaceChild(feature.button,Point(feature.feature.Position)-new Vector2(14,14));
                feature.button.style.display=ShowFeatures&&Visible(Point(feature.feature.Position),24)?DisplayStyle.Flex:DisplayStyle.None;
            }
            MarkDirtyRepaint();
        }
        private bool Visible(Vector2 p,float margin)=>p.x>=contentRect.xMin-margin&&p.x<=contentRect.xMax+margin&&p.y>=contentRect.yMin-margin&&p.y<=contentRect.yMax+margin;
        private Rect ScreenBounds(Rect world)=>Rect.MinMaxRect(contentRect.xMin+(float)viewport.X(world.xMin),contentRect.yMin+(float)viewport.Y(world.yMin),
            contentRect.xMin+(float)viewport.X(world.xMax),contentRect.yMin+(float)viewport.Y(world.yMax));
        private void Paint(MeshGenerationContext context)
        {
            var painter=context.painter2D;
            if(document.HasEnvironment)foreach(var feature in document.Features)
            {
                var center=Point(feature.Position);bool nebula=feature.Kind==GalaxyEnvironmentFeatureKind.Nebula;
                var extent=new Rect(feature.Position.X-feature.RadiusX,feature.Position.Y-feature.RadiusY,feature.RadiusX*2,feature.RadiusY*2);
                if(!Visible(center,24)&&!ScreenBounds(extent).Overlaps(contentRect))continue;
                if((nebula&&ShowFeatures)||ShowInfluenceAreas)
                {
                    var footprint=InfluencePolygon(feature);if(footprint.Count<3)continue;
                    painter.BeginPath();painter.MoveTo(footprint[0]);
                    foreach(var point in footprint.Skip(1))painter.LineTo(point);
                    painter.ClosePath();
                    if(nebula&&ShowFeatures){painter.fillColor=new Color(.25f,.63f,.64f,.12f);painter.Fill();}
                    if(ShowInfluenceAreas){painter.strokeColor=new Color(.60f,.73f,.80f,feature.FeatureId==selectedFeature ? .5f : .22f);painter.lineWidth=1;painter.Stroke();}
                }
                if(ShowFeatures&&feature.Kind==GalaxyEnvironmentFeatureKind.BlackHole)
                {
                    painter.BeginPath();painter.Arc(center,5,0,360);painter.fillColor=new Color(.015f,.02f,.035f);painter.Fill();
                    painter.strokeColor=new Color(.72f,.65f,.88f,.85f);painter.lineWidth=2;
                    painter.BeginPath();painter.Arc(center,9,20,165);painter.Stroke();painter.BeginPath();painter.Arc(center,9,200,345);painter.Stroke();
                }
                if(ShowFeatures&&feature.FeatureId==selectedFeature)
                {painter.strokeColor=new Color(.94f,.69f,.42f);painter.lineWidth=2;painter.BeginPath();painter.Arc(center,17,0,360);painter.Stroke();}
            }
            foreach(var region in document.Regions)
            {
                if(!ScreenBounds(regionBounds[region.NodeId]).Overlaps(contentRect))continue;
                painter.BeginPath();painter.MoveTo(Point(region.Boundary[0]));
                foreach(var vertex in region.Boundary.Skip(1))painter.LineTo(Point(vertex));painter.ClosePath();
                if(region.NodeId==selectedNode){painter.fillColor=new Color(.40f,.75f,.87f,.12f);painter.Fill();}
                painter.strokeColor=region.NodeId==selectedNode?new Color(.55f,.85f,.94f,.9f):new Color(.38f,.47f,.58f,.35f);
                painter.lineWidth=region.NodeId==selectedNode?2:1;painter.Stroke();
            }
            painter.strokeColor=new Color(.40f,.57f,.68f,.18f);painter.lineWidth=1;
            if(ShowGraphLinks)foreach(var edge in document.Connections)
            {
                painter.BeginPath();painter.MoveTo(Position(edge.EndpointA));painter.LineTo(Position(edge.EndpointB));painter.Stroke();
            }
            if(planPaths!=null)
            {
                foreach(var leg in planPaths)
                {
                    if(leg.Count<2)continue;
                    painter.strokeColor=new Color(.94f,.69f,.42f);painter.lineWidth=3;painter.BeginPath();
                    painter.MoveTo(Project(leg[0]));foreach(var point in leg.Skip(1))painter.LineTo(Project(point));painter.Stroke();
                }
            }
            else if(routeOrigin!=null)
            {
                var path=RoutePositions();
                painter.strokeColor=new Color(.94f,.69f,.42f);painter.lineWidth=3;painter.BeginPath();painter.MoveTo(path[0]);
                foreach(var point in path.Skip(1))painter.LineTo(point);painter.Stroke();
            }
            foreach(var node in new[]{routeOrigin}.Concat(pinnedNodes).Concat(planPaths==null?new[]{routeSteps.LastOrDefault()}:Array.Empty<string>()).Where(n=>n!=null).Distinct())
            {
                var point=Position(node!);painter.strokeColor=new Color(.94f,.69f,.42f);painter.lineWidth=2;
                painter.BeginPath();painter.Arc(point,19,0,360);painter.Stroke();
            }
            foreach(var candidate in document.HomeCandidates)
            {
                var p=Position(candidate.NodeId);painter.strokeColor=new Color(.72f,.65f,.88f);painter.lineWidth=2;
                painter.BeginPath();painter.MoveTo(p+new Vector2(12,20));painter.LineTo(p+new Vector2(20,20));painter.LineTo(p+new Vector2(20,12));painter.Stroke();
            }
        }
        private Vector2 Project(GeneratedGalaxyRoutePoint point)=>contentRect.position+new Vector2((float)viewport.X(point.X),(float)viewport.Y(point.Y));
        private List<Vector2> InfluencePolygon(EnvironmentGalaxyFeatureDto feature)
        {
            // Presentation-only ellipse tessellation and clipping to the authoritative map rectangle.
            // Region boundaries, feature coordinates and influence values are never changed.
            var points=new List<Vector2>();
            for(int i=0;i<128;i++)
            {
                double angle=i*Math.PI*2/128;
                points.Add(new Vector2((float)(feature.Position.X+Math.Cos(angle)*feature.RadiusX),
                    (float)(feature.Position.Y+Math.Sin(angle)*feature.RadiusY)));
            }
            for(int edge=0;edge<4;edge++)
            {
                if(points.Count==0)break;
                float limit=edge==1?document.Width:edge==3?document.Height:0;bool vertical=edge<2,lower=edge==0||edge==2;
                float Axis(Vector2 p)=>vertical?p.x:p.y;
                bool Inside(Vector2 p)=>lower?Axis(p)>=limit:Axis(p)<=limit;
                var clipped=new List<Vector2>();var previous=points[points.Count-1];bool priorInside=Inside(previous);
                foreach(var current in points)
                {
                    bool inside=Inside(current);
                    if(inside!=priorInside)clipped.Add(Vector2.LerpUnclamped(previous,current,(limit-Axis(previous))/(Axis(current)-Axis(previous))));
                    if(inside)clipped.Add(current);previous=current;priorInside=inside;
                }
                points=clipped;
            }
            return points.Select(p=>contentRect.position+new Vector2((float)viewport.X(p.x),(float)viewport.Y(p.y))).ToList();
        }
    }
}
