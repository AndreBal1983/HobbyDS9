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
            var nodes=facts.Navigation.Nodes.OrderBy(n=>n.NodeId.Value,StringComparer.Ordinal).ToList();
            var nodeIds=new HashSet<string>(nodes.Select(n=>n.NodeId.Value),StringComparer.Ordinal);
            var links=facts.Navigation.Links.Where(l=>nodeIds.Contains(l.EndpointA.Value)&&nodeIds.Contains(l.EndpointB.Value)).ToList();
            var fleets=frame.OwnStrategicFleets.OrderBy(f=>f.FleetId.Value,StringComparer.Ordinal).ToList();
            var contacts=frame.Travel?.KnownContacts.Where(c=>c.Fresh&&nodeIds.Contains(c.LastSeenNodeId)).ToList()??new List<CampaignKnownContact>();
            string Name(string id)=>nodes.FirstOrDefault(n=>n.NodeId.Value==id)?.DisplayName??"Location unavailable";
            var names=model.NamedFleets().ToDictionary(f=>f.Fleet.FleetId.Value,f=>f.DisplayName,StringComparer.Ordinal);
            string FleetName(string id)=>names.TryGetValue(id,out var value)?value:"Owned fleet";
            bool Editable()=>ReferenceEquals(frame,model.Current.Frame)&&frame.Status.Phase==SessionPhase.Planning
                &&!model.HasLocalEdits&&!model.Current.RefreshRequired&&!model.Current.HasPendingWork&&!model.Busy&&!model.Closed;

            var main=new VisualElement();main.AddToClassList("console-panel");main.AddToClassList("console-work");parent.Add(main);
            Text(main,"Galaxy / known space").AddToClassList("console-section");
            var toolbar=new VisualElement();toolbar.AddToClassList("review-field-row");main.Add(toolbar);
            var map=new VisualElement{name="playable-known-map"};map.AddToClassList("playable-map");main.Add(map);
            var backdrop=new Image{image=Resources.Load<Texture2D>("PlayerArt/space-backdrop"),scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};
            backdrop.StretchToParentSize();map.Add(backdrop);
            var lines=new VisualElement{pickingMode=PickingMode.Ignore};lines.StretchToParentSize();map.Add(lines);
            var details=new ScrollView(ScrollViewMode.Vertical);details.AddToClassList("console-panel");details.AddToClassList("console-aside");parent.Add(details);
            Text(details,"Selection & orders").AddToClassList("console-section");
            Text(details,"Star: system · ring: fleets · diamond: observed contact. Right-click a destination or contact to review an order.");
            var notice=Text(details,"Selection never submits an order.","map-selection-notice");
            var placements=new List<Action>();
            Vector2 Point(string id)
            {
                var index=nodes.FindIndex(n=>n.NodeId.Value==id);
                // A stable sparse visual arrangement; positions never supply travel rules or map knowledge.
                var positions=new[]{new Vector2(.21f,.54f),new Vector2(.62f,.24f),new Vector2(.79f,.70f),new Vector2(.38f,.83f),
                    new Vector2(.13f,.18f),new Vector2(.52f,.48f),new Vector2(.84f,.13f),new Vector2(.19f,.83f)};
                var p=positions[Math.Max(0,index)%positions.Length];
                if(index>=positions.Length)p+=new Vector2((index/positions.Length)*.027f,-(index/positions.Length)*.023f);
                return new Vector2(map.contentRect.width*(.5f+(p.x-.5f)*workspace.MapZoom)+workspace.MapPanX,
                    map.contentRect.height*(.5f+(p.y-.5f)*workspace.MapZoom)+workspace.MapPanY);
            }
            void Reposition(){foreach(var place in placements)place();lines.MarkDirtyRepaint();}
            void Zoom(float factor){workspace.MapZoom=Mathf.Clamp(workspace.MapZoom*factor,.7f,2.4f);Reposition();}
            button(toolbar,"−",()=>Zoom(.8f)).name="map-zoom-out";
            button(toolbar,"+",()=>Zoom(1.25f)).name="map-zoom-in";
            button(toolbar,"Fit",()=>{workspace.MapZoom=1;workspace.MapPanX=workspace.MapPanY=0;Reposition();}).name="map-fit";
            Text(toolbar,"Scroll to zoom · middle-drag to pan");
            map.RegisterCallback<WheelEvent>(e=>{Zoom(e.delta.y>0?.9f:1.1f);e.StopPropagation();});
            int panPointer=-1;Vector2 panStart=default,panOrigin=default;
            map.RegisterCallback<PointerDownEvent>(e=>{
                if(e.button!=2)return;panPointer=e.pointerId;panStart=e.position;panOrigin=new Vector2(workspace.MapPanX,workspace.MapPanY);
                map.CapturePointer(panPointer);e.StopPropagation();
            });
            map.RegisterCallback<PointerMoveEvent>(e=>{
                if(e.pointerId!=panPointer)return;var delta=(Vector2)e.position-panStart;
                workspace.MapPanX=panOrigin.x+delta.x;workspace.MapPanY=panOrigin.y+delta.y;Reposition();e.StopPropagation();
            });
            map.RegisterCallback<PointerUpEvent>(e=>{if(e.pointerId==panPointer){var id=panPointer;panPointer=-1;map.ReleasePointer(id);e.StopPropagation();}});
            map.RegisterCallback<PointerCaptureOutEvent>(_=>panPointer=-1);
            map.RegisterCallback<GeometryChangedEvent>(_=>Reposition());
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
                for(int y=0;y<5;y++)for(int x=0;x<8;x++)
                {p.BeginPath();p.MoveTo(Corner(x,y));p.LineTo(Corner(x+1,y));p.LineTo(Corner(x+1,y+1));p.LineTo(Corner(x,y+1));p.ClosePath();p.Stroke();}
                foreach(var link in links)
                {
                    var a=Point(link.EndpointA.Value);var b=Point(link.EndpointB.Value);
                    p.strokeColor=link.Kind==GalaxyEdgeKind.Wormhole?new Color(.73f,.67f,.9f,.8f):new Color(.62f,.85f,.98f,.6f);
                    p.lineWidth=2;p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke();
                    if(link.Kind==GalaxyEdgeKind.Wormhole){p.BeginPath();p.MoveTo(a+Vector2.up*5);p.LineTo(b+Vector2.up*5);p.Stroke();}
                }
                var offer=workspace.RoutePreview;
                if(offer!=null&&workspace.RoutePreviewMatchesCurrent&&offer.Route.Success)
                {
                    p.strokeColor=new Color(.94f,.69f,.42f);p.lineWidth=4;
                    for(int i=1;i<offer.Route.Steps.Count;i++)
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
                workspace.SelectedNode=destination;workspace.RoutePreview=offer;Reposition();
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
                    Place(count,()=>Point(id),140,24,new Vector2(0,53));
                }
                var star=new Button{name="map-star-"+id,text="●",tooltip=node.DisplayName+" · select system; right-click to prepare navigation."};
                bindButton(star,()=>perform(()=>workspace.SelectedNode=id));
                star.AddToClassList("map-star");star.EnableInClassList("selected-map-marker",workspace.SelectedNode==id);
                Place(star,()=>Point(id),38,38);ContextAction(star,()=>Prepare(id));
                var label=new Label(node.DisplayName){pickingMode=PickingMode.Ignore};label.AddToClassList("map-node-name");
                Place(label,()=>Point(id),175,28,new Vector2(103,-2));
            }
            foreach(var contact in contacts)
            {
                var marker=new Button{name="map-contact-"+contact.FleetId,text="◇",tooltip="Observed contact. Right-click or Shift+F10 to review Follow."};
                bindButton(marker,()=>{notice.text="Observed hostile contact at "+Name(contact.LastSeenNodeId)+", turn "+contact.LastSeenTurn+". Current destination and reserves are unavailable.";});
                var offset=contacts.Where(c=>c.LastSeenNodeId==contact.LastSeenNodeId).ToList().IndexOf(contact);
                marker.AddToClassList("map-contact");Place(marker,()=>Point(contact.LastSeenNodeId),44,44,new Vector2(-52,-42-offset*48));
                ContextAction(marker,()=>Prepare(contact.LastSeenNodeId,contact));
            }
            foreach(var fleet in fleets)
            {
                var receipt=frame.Travel?.OwnReceipts.LastOrDefault(r=>r.FleetId==fleet.FleetId.Value);
                if(receipt?.TransitDestinationId==null||receipt.TransitTotalCost<=0||!nodeIds.Contains(receipt.TransitDestinationId))continue;
                var moving=new Button{name="map-moving-fleet-"+fleet.FleetId,text="▸",tooltip="Owned fleet in transit: "+receipt.TransitProgress+" / "+receipt.TransitTotalCost};
                bindButton(moving,()=>perform(()=>workspace.SelectedFleet=fleet.FleetId.Value));
                moving.AddToClassList("map-contact");
                Place(moving,()=>Vector2.Lerp(Point(receipt.EndNodeId),Point(receipt.TransitDestinationId),(float)((double)receipt.TransitProgress/receipt.TransitTotalCost)),38,38);
            }
            Text(main,"Known lanes · double line: wormhole · amber: unsent route. Schematic spacing does not determine travel cost.");
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
