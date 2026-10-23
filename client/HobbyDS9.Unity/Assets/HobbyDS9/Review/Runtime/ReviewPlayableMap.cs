#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Only observed nodes, links, owned fleets and lawful fresh contacts enter the map.
    public sealed class ReviewPlayableMap
    {
        private readonly ReviewUiModel model;
        private readonly ReviewWorkspace workspace;
        private readonly Func<VisualElement,string,Action,Button> button;
        private readonly Action<Button,Action> bindButton;
        private readonly Action<Action> perform;
        private readonly Action<string,Action> confirm;
        private readonly Action<ReviewWorkspacePage> navigate;
        public ReviewPlayableMap(ReviewUiModel model,ReviewWorkspace workspace,Func<VisualElement,string,Action,Button> button,
            Action<Action> perform,Action<string,Action> confirm,Action<ReviewWorkspacePage> navigate,Action<Button,Action> bindButton)
        {this.model=model;this.workspace=workspace;this.button=button;this.perform=perform;this.confirm=confirm;this.navigate=navigate;this.bindButton=bindButton;}
        private static Label Text(VisualElement parent,string text,string id="")
        {var label=new Label(text){name=id};label.AddToClassList("console-copy");parent.Add(label);return label;}
        public void Render(VisualElement parent)
        {
            var frame=model.Current.Frame;var facts=frame.Strategy!;
            var generated=model.GeneratedCampaign?model.GeneratedMap():null;
            var generatedRegions=generated?.Regions.ToDictionary(r=>r.NodeId,StringComparer.Ordinal);
            var generatedStars=generated?.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star).ToDictionary(f=>f.NodeId!,f=>f.Position,StringComparer.Ordinal);
            var generatedNames=generated?.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star).ToDictionary(f=>f.NodeId!,f=>f.DisplayName,StringComparer.Ordinal);
            var viewport=model.GeneratedViewport;
            var nodes=facts.Navigation.Nodes.OrderBy(n=>n.NodeId.Value,StringComparer.Ordinal).ToList();
            var nodeIds=new HashSet<string>(nodes.Select(n=>n.NodeId.Value),StringComparer.Ordinal);
            var links=facts.Navigation.Links.Where(l=>nodeIds.Contains(l.EndpointA.Value)&&nodeIds.Contains(l.EndpointB.Value)).ToList();
            var fleets=frame.OwnStrategicFleets.OrderBy(f=>f.FleetId.Value,StringComparer.Ordinal).ToList();
            var contacts=frame.Travel?.KnownContacts.Where(c=>c.Fresh&&nodeIds.Contains(c.LastSeenNodeId)).ToList()??new List<CampaignKnownContact>();
            string Name(string id)=>generatedNames!=null&&generatedNames.TryGetValue(id,out var starName)?starName:nodes.FirstOrDefault(n=>n.NodeId.Value==id)?.DisplayName??"Location unavailable";
            var names=model.NamedFleets().ToDictionary(f=>f.Fleet.FleetId.Value,f=>f.DisplayName,StringComparer.Ordinal);
            string FleetName(string id)=>names.TryGetValue(id,out var value)?value:"Owned fleet";
            bool Editable()=>ReferenceEquals(frame,model.Current.Frame)&&frame.Status.Phase==SessionPhase.Planning
                &&!model.HasLocalEdits&&!model.Current.RefreshRequired&&!model.Current.HasPendingWork&&!model.Busy&&!model.Closed;

            var main=new VisualElement();main.AddToClassList("console-panel");main.AddToClassList("console-work");parent.Add(main);
            Text(main,"Galaxy / known space").AddToClassList("console-section");
            var toolbar=new VisualElement();toolbar.AddToClassList("review-field-row");main.Add(toolbar);
            var map=new VisualElement{name="playable-known-map"};map.AddToClassList("playable-map");main.Add(map);
            var backdrop=new Image{image=Resources.Load<Texture2D>("PlayerArt/space-backdrop"),scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};
            backdrop.StretchToParentSize();if(generated==null)map.Add(backdrop);
            map.style.overflow=Overflow.Hidden;
            GeneratedGalaxyVolumeBackground? background=null;bool backgroundFailed=false;
            var cosmetic=generated==null?null:new GeneratedGalaxyMapData(generated);
            void RefreshBackground()
            {
                if(cosmetic==null||backgroundFailed||map.panel==null||map.contentRect.width<2||map.contentRect.height<2)return;
                try{if(background==null)background=new GeneratedGalaxyVolumeBackground(cosmetic);background.Render(viewport,map.contentRect.width,map.contentRect.height,true,true);map.style.backgroundImage=Background.FromRenderTexture(background.Texture!);}
                catch(Exception){background?.Dispose();background=null;backgroundFailed=true;map.style.backgroundImage=StyleKeyword.None;map.style.backgroundColor=new Color(.003f,.006f,.012f);}
            }
            map.RegisterCallback<DetachFromPanelEvent>(_=>{background?.Dispose();background=null;});
            var lines=new VisualElement{pickingMode=PickingMode.Ignore};lines.StretchToParentSize();map.Add(lines);
            var details=new ScrollView(ScrollViewMode.Vertical);details.AddToClassList("console-panel");details.AddToClassList("console-aside");details.AddToClassList("playable-map-aside");parent.Add(details);
            Text(details,"Selection & orders").AddToClassList("console-section");
            Text(details,"Star: system · ring: fleets · diamond: observed contact. Right-click a destination or contact to review an order.");
            var notice=Text(details,"Selection never submits an order.","map-selection-notice");
            var placements=new List<Action>();
            var generatedLabels=new Dictionary<string,Label>(StringComparer.Ordinal);string? hoveredNode=null;
            Vector2 WorldPoint(string id)
            {
                if(generatedStars!.TryGetValue(id,out var star))return new Vector2(star.X,star.Y);
                var region=generatedRegions![id];return new Vector2(region.Center.X,region.Center.Y);
            }
            Vector2 Point(string id)
            {
                if(generated!=null){var world=WorldPoint(id);return new Vector2((float)viewport.X(world.x),(float)viewport.Y(world.y));}
                var index=nodes.FindIndex(n=>n.NodeId.Value==id);
                // A stable sparse visual arrangement; positions never supply travel rules or map knowledge.
                var positions=new[]{new Vector2(.21f,.54f),new Vector2(.62f,.24f),new Vector2(.79f,.70f),new Vector2(.38f,.83f),
                    new Vector2(.13f,.18f),new Vector2(.52f,.48f),new Vector2(.84f,.13f),new Vector2(.19f,.83f)};
                var p=positions[Math.Max(0,index)%positions.Length];
                if(index>=positions.Length)p+=new Vector2((index/positions.Length)*.027f,-(index/positions.Length)*.023f);
                return new Vector2(map.contentRect.width*(.5f+(p.x-.5f)*workspace.MapZoom)+workspace.MapPanX,
                    map.contentRect.height*(.5f+(p.y-.5f)*workspace.MapZoom)+workspace.MapPanY);
            }
            void FitKnown()
            {
                if(generated==null)return;
                viewport.SetMap(generated.Width,generated.Height);viewport.Resize(map.contentRect.width,map.contentRect.height);
                var points=generated.Regions.SelectMany(r=>r.Boundary).ToArray();
                if(points.Length>0)viewport.FitWorldBounds(points.Min(p=>p.X),points.Min(p=>p.Y),points.Max(p=>p.X),points.Max(p=>p.Y));
                model.GeneratedViewportNeedsFit=false;
            }
            void Reposition()
            {
                if(generated!=null){viewport.Resize(map.contentRect.width,map.contentRect.height);if(model.GeneratedViewportNeedsFit&&map.contentRect.width>2&&map.contentRect.height>2)FitKnown();}
                foreach(var place in placements)place();LayoutLabels();RefreshBackground();lines.MarkDirtyRepaint();
            }
            void LayoutLabels()
            {
                if(generated==null)return;
                var candidates=new List<GeneratedLabelCandidate>();var glyphs=new List<GeneratedLabelRect>();
                foreach(var node in nodes)
                {
                    var id=node.NodeId.Value;var p=Point(id);bool system=generatedStars!.ContainsKey(id);
                    double radius=system?6:3;glyphs.Add(new GeneratedLabelRect(p.x-radius,p.y-radius,radius*2,radius*2));
                    bool owned=fleets.Any(f=>f.LocationNodeId.Value==id&&frame.Travel?.OwnReceipts.LastOrDefault(r=>r.FleetId==f.FleetId.Value)?.TransitDestinationId==null);
                    if(owned){glyphs.Add(new GeneratedLabelRect(p.x-37,p.y-37,74,74));glyphs.Add(new GeneratedLabelRect(p.x-35,p.y+44,70,18));}
                    int priority=workspace.SelectedNode==id?100:hoveredNode==id?90:owned?80:contacts.Any(c=>c.LastSeenNodeId==id)?70:0;
                    var label=generatedLabels[id];label.style.visibility=Visibility.Hidden;
                    if(!system&&priority==0)continue;
                    var measured=label.MeasureTextSize(label.text,0,VisualElement.MeasureMode.Undefined,20,VisualElement.MeasureMode.Exactly);
                    var width=Mathf.Clamp(float.IsNaN(measured.x)||measured.x<1?label.text.Length*8+6:measured.x+6,32,180);
                    candidates.Add(new GeneratedLabelCandidate(id,p.x,p.y,width,20,priority,owned?42:12));
                }
                foreach(var contact in contacts)
                {var p=Point(contact.LastSeenNodeId);int index=contacts.Where(c=>c.LastSeenNodeId==contact.LastSeenNodeId).ToList().IndexOf(contact);glyphs.Add(new GeneratedLabelRect(p.x-64,p.y-54-index*48,24,24));}
                foreach(var fleet in fleets)
                {var receipt=frame.Travel?.OwnReceipts.LastOrDefault(r=>r.FleetId==fleet.FleetId.Value);if(receipt?.TransitDestinationId==null||receipt.TransitTotalCost<=0||!nodeIds.Contains(receipt.TransitDestinationId))continue;
                    var p=Vector2.Lerp(Point(receipt.EndNodeId),Point(receipt.TransitDestinationId),(float)((double)receipt.TransitProgress/receipt.TransitTotalCost));glyphs.Add(new GeneratedLabelRect(p.x-12,p.y-12,24,24));}
                var packed=GeneratedCampaignLabelLayout.Place(candidates,glyphs,map.contentRect.width,map.contentRect.height);
                foreach(var item in packed)
                {var label=generatedLabels[item.Key];var r=item.Value;label.style.left=(float)r.X;label.style.top=(float)r.Y;label.style.width=(float)r.Width;label.style.height=(float)r.Height;label.style.visibility=Visibility.Visible;}
            }
            void Zoom(float factor){if(generated!=null)viewport.ZoomAt(factor,map.contentRect.width/2,map.contentRect.height/2);else workspace.MapZoom=Mathf.Clamp(workspace.MapZoom*factor,.7f,2.4f);Reposition();}
            button(toolbar,"−",()=>Zoom(.8f)).name="map-zoom-out";
            button(toolbar,"+",()=>Zoom(1.25f)).name="map-zoom-in";
            button(toolbar,generated==null?"Fit":"Fit known",()=>{if(generated!=null)FitKnown();else{workspace.MapZoom=1;workspace.MapPanX=workspace.MapPanY=0;}Reposition();}).name="map-fit";
            if(generated!=null)button(toolbar,"Fit galaxy",()=>{viewport.Fit();Reposition();}).name="map-fit-galaxy";
            Text(toolbar,"Scroll to zoom · middle-drag to pan");
            map.RegisterCallback<WheelEvent>(e=>{if(generated!=null){var at=map.WorldToLocal(e.mousePosition);viewport.ZoomAt(e.delta.y>0?.9:1.1,at.x,at.y);Reposition();}else Zoom(e.delta.y>0?.9f:1.1f);e.StopPropagation();});
            int panPointer=-1;Vector2 panStart=default,panOrigin=default;
            map.RegisterCallback<PointerDownEvent>(e=>{
                if(e.button!=2)return;panPointer=e.pointerId;panStart=e.position;panOrigin=new Vector2(workspace.MapPanX,workspace.MapPanY);
                map.CapturePointer(panPointer);e.StopPropagation();
            });
            map.RegisterCallback<PointerMoveEvent>(e=>{
                if(e.pointerId!=panPointer)return;var delta=(Vector2)e.position-panStart;
                if(generated!=null){viewport.Pan(delta.x,delta.y);panStart=e.position;}else{workspace.MapPanX=panOrigin.x+delta.x;workspace.MapPanY=panOrigin.y+delta.y;}Reposition();e.StopPropagation();
            });
            map.RegisterCallback<PointerUpEvent>(e=>{if(e.pointerId==panPointer){var id=panPointer;panPointer=-1;map.ReleasePointer(id);e.StopPropagation();}});
            map.RegisterCallback<PointerCaptureOutEvent>(_=>panPointer=-1);
            // Resolve only competing generated node hit boxes. Actual contact,
            // moving-fleet and hollow-ring targets retain their normal priority.
            if(generated!=null)
            {
                bool Priority(VisualElement? target)
                {
                    for(var at=target;at!=null&&at!=map;at=at.parent)
                        if(at.name.StartsWith("map-contact-",StringComparison.Ordinal)||at.name.StartsWith("map-moving-fleet-",StringComparison.Ordinal)||at.name.StartsWith("map-ring-",StringComparison.Ordinal))return true;
                    return false;
                }
                string? Resolve(Vector2 world)
                {
                    var local=map.WorldToLocal(world);
                    var anchors=generated.Regions.Select(region=>{
                        var point=Point(region.NodeId); // Same final projected floats as Place/Paint.
                        return new GeneratedCampaignNodeAnchor(region.NodeId,point.x,point.y);});
                    return GeneratedCampaignNodePicker.Resolve(anchors,local.x,local.y);
                }
                map.RegisterCallback<PointerMoveEvent>(e=>{
                    string? next=null;
                    for(var at=e.target as VisualElement;at!=null&&at!=map;at=at.parent)
                    {
                        if(at.name.StartsWith("map-contact-",StringComparison.Ordinal)){next=contacts.FirstOrDefault(c=>"map-contact-"+c.FleetId==at.name)?.LastSeenNodeId;break;}
                        if(at.name.StartsWith("map-ring-",StringComparison.Ordinal)){next=at.name.Substring("map-ring-".Length);break;}
                    }
                    next=next??Resolve(e.position);if(next==hoveredNode)return;hoveredNode=next;LayoutLabels();
                });
                map.RegisterCallback<PointerLeaveEvent>(_=>{if(hoveredNode!=null){hoveredNode=null;LayoutLabels();}});
                int nodePointer=-1,nodeButton=-1;string? pressedNode=null;
                map.RegisterCallback<PointerDownEvent>(e=>{
                    if((e.button!=0&&e.button!=1)||!map.enabledInHierarchy||!ReferenceEquals(frame,model.Current.Frame)||Priority(e.target as VisualElement))return;
                    var node=Resolve(e.position);if(node==null)return;
                    nodePointer=e.pointerId;nodeButton=e.button;pressedNode=node;map.CapturePointer(nodePointer);e.StopImmediatePropagation();
                },TrickleDown.TrickleDown);
                map.RegisterCallback<PointerUpEvent>(e=>{
                    if(nodePointer!=e.pointerId||nodeButton!=e.button)return;
                    var down=pressedNode;var node=Resolve(e.position);var id=nodePointer;nodePointer=nodeButton=-1;pressedNode=null;
                    map.ReleasePointer(id);e.StopImmediatePropagation();
                    if(node==null||node!=down||!map.enabledInHierarchy||!ReferenceEquals(frame,model.Current.Frame))return;
                    if(e.button==0)perform(()=>workspace.SelectedNode=node);else Prepare(node);
                },TrickleDown.TrickleDown);
                map.RegisterCallback<PointerCaptureOutEvent>(e=>{if(e.pointerId==nodePointer){nodePointer=nodeButton=-1;pressedNode=null;}});
            }
            map.RegisterCallback<GeometryChangedEvent>(_=>Reposition());
            GeneratedGalaxyCertifiedPath? certifiedRoute=null;
            void CertifyRoute()
            {
                certifiedRoute=null;var offer=workspace.RoutePreview;
                if(generated==null||offer==null||!workspace.RoutePreviewMatchesCurrent||!offer.Route.Success)return;
                // Detached known geometry only. Actual star anchors replace presentation
                // endpoint centres; the authoritative graph, steps and costs stay untouched.
                var geometry=generated.Regions.ToDictionary(r=>r.NodeId,r=>new GeneratedGalaxyRegionDto{
                    NodeId=r.NodeId,DisplayName=r.DisplayName,SystemId=r.SystemId,Boundary=r.Boundary,
                    Center=generatedStars!.TryGetValue(r.NodeId,out var star)?new GeneratedGalaxyPointDto{X=star.X,Y=star.Y}:r.Center},StringComparer.Ordinal);
                if(!geometry.ContainsKey(offer.Origin)||offer.Route.Steps.Any(id=>!geometry.ContainsKey(id)))return;
                certifiedRoute=GeneratedGalaxyCorridorPath.Build(geometry,offer.Origin,offer.Route.Steps);
                if(!certifiedRoute.Certified)notice.text="Route cost is valid; no certified route line: "+certifiedRoute.Reason;
            }
            CertifyRoute();
            lines.generateVisualContent+=ctx=>{
                var p=ctx.painter2D;
                // Unowned decorative region field. No extra systems, routes, control or sensor facts.
                p.strokeColor=new Color(.34f,.47f,.58f,.22f);p.lineWidth=1;
                Vector2 Corner(int x,int y)
                {
                    var nx=x/8f+(x==0||x==8?0:Mathf.Sin(x*7+y*3)*.025f);
                    var ny=y/5f+(y==0||y==5?0:Mathf.Sin(x*3+y*7)*.04f);
                    return new Vector2(map.contentRect.width*(.5f+(nx-.5f)*workspace.MapZoom)+workspace.MapPanX,
                        map.contentRect.height*(.5f+(ny-.5f)*workspace.MapZoom)+workspace.MapPanY);
                }
                if(generated!=null)foreach(var region in generated.Regions)
                {p.BeginPath();for(int i=0;i<region.Boundary.Count;i++){var v=region.Boundary[i];var xy=new Vector2((float)viewport.X(v.X),(float)viewport.Y(v.Y));if(i==0)p.MoveTo(xy);else p.LineTo(xy);}p.ClosePath();p.Stroke();}
                if(generated==null)for(int y=0;y<5;y++)for(int x=0;x<8;x++)
                {p.BeginPath();p.MoveTo(Corner(x,y));p.LineTo(Corner(x+1,y));p.LineTo(Corner(x+1,y+1));p.LineTo(Corner(x,y+1));p.ClosePath();p.Stroke();}
                if(generated==null)foreach(var link in links)
                {
                    var a=Point(link.EndpointA.Value);var b=Point(link.EndpointB.Value);
                    p.strokeColor=link.Kind==GalaxyEdgeKind.Wormhole?new Color(.73f,.67f,.9f,.8f):new Color(.62f,.85f,.98f,.6f);
                    p.lineWidth=2;p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke();
                    if(link.Kind==GalaxyEdgeKind.Wormhole){p.BeginPath();p.MoveTo(a+Vector2.up*5);p.LineTo(b+Vector2.up*5);p.Stroke();}
                }
                var offer=workspace.RoutePreview;
                if(offer!=null&&workspace.RoutePreviewMatchesCurrent&&offer.Route.Success)
                {
                    if(generated!=null&&certifiedRoute?.Certified==true)
                    {
                        foreach(var stroke in new[]{(width:8f,colour:new Color(.005f,.008f,.014f,.95f)),(width:3f,colour:new Color(.94f,.69f,.42f))})
                        {p.strokeColor=stroke.colour;p.lineWidth=stroke.width;for(int i=1;i<certifiedRoute.Points.Count;i++){var a=certifiedRoute.Points[i-1];var b=certifiedRoute.Points[i];p.BeginPath();p.MoveTo(new Vector2((float)viewport.X(a.X),(float)viewport.Y(a.Y)));p.LineTo(new Vector2((float)viewport.X(b.X),(float)viewport.Y(b.Y)));p.Stroke();}}
                    }
                    p.strokeColor=new Color(.94f,.69f,.42f);p.lineWidth=4;
                    if(generated==null)for(int i=1;i<offer.Route.Steps.Count;i++)
                    {p.BeginPath();p.MoveTo(Point(offer.Route.Steps[i-1]));p.LineTo(Point(offer.Route.Steps[i]));p.Stroke();}
                }
            };
            void Prepare(string destination,CampaignKnownContact? contact=null)
            {
                if(!Editable()){notice.text="Submit or discard unsent edits, finish combat, or refresh before preparing an order.";return;}
                var fleet=fleets.FirstOrDefault(f=>f.FleetId.Value==workspace.SelectedFleet);
                if(fleet==null){notice.text="Select one of your fleet rings or roster entries first.";return;}
                if(!nodeIds.Contains(destination)){notice.text="No permitted destination here.";return;}
                var offer=model.Current.FindKnownRoute(fleet.FleetId.Value,destination);
                if(offer==null||!offer.Route.Success){notice.text="No lawful route is available from this fleet's current position.";return;}
                workspace.SelectedNode=destination;workspace.RoutePreview=offer;CertifyRoute();Reposition();
                var summary=(contact==null?"Navigate":"Follow observed contact")+" · "+FleetName(fleet.FleetId.Value)+"\n"+
                    string.Join(" → ",offer.Route.Steps.Select(Name))+"\nTravel cost "+offer.Route.TotalCost+
                    (contact==null?"\nTravel progresses on End turn.":"\nLast seen turn "+contact.LastSeenTurn+". Following uses lawful tracking and physical movement; contact is not guaranteed.");
                confirm(summary,()=>perform(()=>{
                    if(contact==null)model.SendPreviewedTravel(offer,ReviewUiModel.NewCommandId());
                    else model.SendPreviewedFollow(offer,contact.FleetId,contact.LastSeenTurn,ReviewUiModel.NewCommandId());
                }));
            }
            void ContextAction(VisualElement target,Action action)
            {
                bool Live()=>target.panel!=null&&target.enabledInHierarchy&&ReferenceEquals(frame,model.Current.Frame);
                target.RegisterCallback<PointerUpEvent>(e=>{if(e.button==1&&Live()){action();e.StopPropagation();}});
                target.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.F10&&e.shiftKey&&Live()
                    &&ReferenceEquals(target.panel.focusController.focusedElement,target)){action();e.StopPropagation();}});
            }
            void Place(VisualElement element,Func<Vector2> point,float width,float height,Vector2 offset=default)
            {
                element.style.position=Position.Absolute;element.style.width=width;element.style.height=height;map.Add(element);
                placements.Add(()=>{var p=point()+offset;element.style.left=p.x-width/2;element.style.top=p.y-height/2;});
            }
            foreach(var node in nodes)
            {
                var id=node.NodeId.Value;
                var atNode=fleets.Where(f=>f.LocationNodeId.Value==id&&frame.Travel?.OwnReceipts.LastOrDefault(r=>r.FleetId==f.FleetId.Value)?.TransitDestinationId==null).ToList();
                if(atNode.Count>0)
                {
                    var ring=new ReviewMapRing{name="map-ring-"+id,tooltip=atNode.Count+" own fleets. Select the ring to open the roster."};
                    bindButton(ring,()=>perform(()=>{workspace.SelectedNode=id;workspace.SelectedFleet=atNode[0].FleetId.Value;}));
                    Place(ring,()=>Point(id),74,74);
                    ring.EnableInClassList("selected-map-marker",atNode.Any(f=>f.FleetId.Value==workspace.SelectedFleet));
                    var count=new Label(atNode.Count+" own fleet"+(atNode.Count==1?"":"s")){pickingMode=PickingMode.Ignore};count.AddToClassList("map-own-count");
                    if(generated!=null){count.text=atNode.Count+" fleet"+(atNode.Count==1?"":"s");count.style.fontSize=12;Place(count,()=>Point(id),70,18,new Vector2(0,53));}
                    else Place(count,()=>Point(id),140,24,new Vector2(0,53));
                }
                var isSystem=generated==null||generatedStars!.ContainsKey(id);
                var star=new Button{name="map-star-"+id,text=isSystem?"●":"·",tooltip=Name(id)+(isSystem?" · system":" · empty transit region")+"; right-click to prepare navigation."};
                bindButton(star,()=>perform(()=>workspace.SelectedNode=id));
                star.AddToClassList("map-star");star.EnableInClassList("selected-map-marker",workspace.SelectedNode==id);
                if(generated!=null)star.AddToClassList("generated-map-node-hit");
                Place(star,()=>Point(id),38,38);ContextAction(star,()=>Prepare(id));
                var label=new Label(Name(id)){name="map-label-"+id,pickingMode=PickingMode.Ignore,tooltip=Name(id)};label.AddToClassList("map-node-name");
                if(generated!=null){label.AddToClassList("generated-map-node-label");label.style.position=Position.Absolute;map.Add(label);generatedLabels.Add(id,label);}
                else Place(label,()=>Point(id),175,28,new Vector2(103,-2));
            }
            foreach(var contact in contacts)
            {
                var marker=new Button{name="map-contact-"+contact.FleetId,text="◇",tooltip="Observed contact. Right-click or Shift+F10 to review Follow."};
                bindButton(marker,()=>{notice.text="Observed hostile contact at "+Name(contact.LastSeenNodeId)+", turn "+contact.LastSeenTurn+". Current destination and reserves are unavailable.";});
                var offset=contacts.Where(c=>c.LastSeenNodeId==contact.LastSeenNodeId).ToList().IndexOf(contact);
                marker.AddToClassList("map-contact");Place(marker,()=>Point(contact.LastSeenNodeId),44,44,new Vector2(-52,-42-offset*48));
                if(generated!=null)marker.AddToClassList("generated-map-contact-hit");
                ContextAction(marker,()=>Prepare(contact.LastSeenNodeId,contact));
            }
            foreach(var fleet in fleets)
            {
                var receipt=frame.Travel?.OwnReceipts.LastOrDefault(r=>r.FleetId==fleet.FleetId.Value);
                if(receipt?.TransitDestinationId==null||receipt.TransitTotalCost<=0||!nodeIds.Contains(receipt.TransitDestinationId))continue;
                var moving=new Button{name="map-moving-fleet-"+fleet.FleetId,text="▸",tooltip="Owned fleet in transit: "+receipt.TransitProgress+" / "+receipt.TransitTotalCost};
                bindButton(moving,()=>perform(()=>workspace.SelectedFleet=fleet.FleetId.Value));
                moving.AddToClassList("map-contact");
                if(generated!=null)moving.AddToClassList("generated-map-contact-hit");
                Place(moving,()=>Vector2.Lerp(Point(receipt.EndNodeId),Point(receipt.TransitDestinationId),(float)((double)receipt.TransitProgress/receipt.TransitTotalCost)),38,38);
            }
            Text(main,generated==null?"Known lanes · double line: wormhole · amber: unsent route. Schematic spacing does not determine travel cost.":"Known generated regions only · borders define crossings · amber: reviewed route. Fit known frames explored space.");
            var selectedNode=nodes.FirstOrDefault(n=>n.NodeId.Value==workspace.SelectedNode);
            if(selectedNode!=null)
            {
                Text(details,selectedNode.DisplayName).AddToClassList("console-section");
                foreach(var colony in facts.Colonies.Where(c=>selectedNode.AssociatedSystemId.HasValue&&c.SystemId==selectedNode.AssociatedSystemId.Value))
                {
                    Text(details,colony.Environment.DisplayName+" · population "+colony.Population);
                    button(details,"Open world / construction",()=>{workspace.SelectedColony=colony.ColonyId.Value;navigate(ReviewWorkspacePage.Worlds);});
                }
            }
            Text(details,"Your fleets").AddToClassList("console-section");
            foreach(var fleet in fleets)
            {
                var pick=button(details,FleetName(fleet.FleetId.Value)+" · "+fleet.ShipIds.Count+" ships\n"+Name(fleet.LocationNodeId.Value),()=>perform(()=>workspace.SelectedFleet=fleet.FleetId.Value));
                pick.name="map-fleet-"+fleet.FleetId;pick.EnableInClassList("selected-nav",workspace.SelectedFleet==fleet.FleetId.Value);
            }
            if(workspace.SelectedFleet!=null&&!fleets.Any(f=>f.FleetId.Value==workspace.SelectedFleet))Text(details,"The selected fleet is no longer available. Select another explicitly.");
            button(details,"Open fleet roster",()=>navigate(ReviewWorkspacePage.FleetOrders));
            if(selectedNode!=null)button(details,"Review navigation here",()=>Prepare(selectedNode.NodeId.Value));
            foreach(var contact in contacts)button(details,"Review Follow · "+Name(contact.LastSeenNodeId),()=>Prepare(contact.LastSeenNodeId,contact));
            if(frame.Travel!=null)foreach(var contact in frame.Travel.KnownContacts.Where(c=>!c.Fresh))
                Text(details,"Stale contact · last seen "+Name(contact.LastSeenNodeId)+", turn "+contact.LastSeenTurn+". Follow unavailable.");
            if(frame.Status.Phase!=SessionPhase.Planning)button(details,"Open pending combat",()=>navigate(ReviewWorkspacePage.Combat));
        }
    }
    public sealed class ReviewMapRing : Button
    {
        public ReviewMapRing(){AddToClassList("map-fleet-ring");}
        public override bool ContainsPoint(Vector2 localPoint)
        {
            var distance=Vector2.Distance(localPoint,contentRect.center);
            return distance>=24&&distance<=38;
        }
    }
}
