#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Layout only. Knowledge, route offers, travel and charting come from the host.
    public sealed class ReviewFrontierMap
    {
        private readonly ReviewUiModel model;
        private readonly ReviewWorkspace workspace;
        private readonly Func<VisualElement,string,Action,Button> button;
        private readonly Action<Action> perform;
        private readonly Action<string,Action> confirm;
        private readonly Action<ReviewWorkspacePage> navigate;
        public ReviewFrontierMap(ReviewUiModel model,ReviewWorkspace workspace,Func<VisualElement,string,Action,Button> button,
            Action<Action> perform,Action<string,Action> confirm,Action<ReviewWorkspacePage> navigate)
        {this.model=model;this.workspace=workspace;this.button=button;this.perform=perform;this.confirm=confirm;this.navigate=navigate;}
        private static void Text(VisualElement parent,string value,string name="")
        {var label=new Label(value){name=name};label.style.whiteSpace=WhiteSpace.Normal;parent.Add(label);}
        private static ScrollView Panel(VisualElement parent,string title)
        {var panel=new ScrollView(ScrollViewMode.Vertical);panel.AddToClassList("console-panel");panel.style.flexGrow=1;panel.style.minHeight=0;parent.Add(panel);Text(panel,title);return panel;}
        public void Render(VisualElement parent)
        {
            var frame=model.Current.Frame;var known=frame.Frontier;
            if(known==null||known.ActorId!=frame.Actor)
            {Text(parent,"Frontier observations unavailable for this player. No unrestricted map is shown.","frontier-map-unavailable");return;}
            string Name(string id)=>known.Nodes.FirstOrDefault(n=>n.NodeId==id)?.DisplayName??"Location unavailable";
            var columns=new VisualElement();columns.AddToClassList("console-columns");parent.Add(columns);
            var mapPanel=Panel(columns,"Frontier — known navigation only");
            var details=Panel(columns,"Your fleet / next action");details.AddToClassList("console-aside");
            Text(mapPanel,"Arrival charts adjacent public lanes at the next Planning boundary. This preview does not survey, settle or award a military victory.","frontier-scope");
            var nodes=known.Nodes.OrderBy(n=>n.NodeId,StringComparer.Ordinal).ToList();
            var lanes=known.Lanes.Where(l=>nodes.Any(n=>n.NodeId==l.EndpointA)&&nodes.Any(n=>n.NodeId==l.EndpointB)).ToList();
            var map=new VisualElement{name="frontier-known-map"};map.style.minHeight=workspace.Compact?300:470;map.style.flexGrow=1;mapPanel.Add(map);
            var backdrop=new Image{image=Resources.Load<Texture2D>("PlayerArt/space-backdrop"),scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};backdrop.StretchToParentSize();map.Add(backdrop);
            var lines=new VisualElement{pickingMode=PickingMode.Ignore};lines.StretchToParentSize();map.Add(lines);
            Vector2 Point(string id){var i=nodes.FindIndex(n=>n.NodeId==id);var angle=2*Mathf.PI*i/Math.Max(1,nodes.Count)-Mathf.PI/2;return new Vector2(map.contentRect.width*.5f+Mathf.Cos(angle)*map.contentRect.width*.30f,map.contentRect.height*.5f+Mathf.Sin(angle)*map.contentRect.height*.31f);}
            lines.generateVisualContent+=ctx=>{
                var painter=ctx.painter2D;painter.strokeColor=new Color(.61f,.76f,.87f);painter.lineWidth=3;
                foreach(var lane in lanes){painter.BeginPath();painter.MoveTo(Point(lane.EndpointA));painter.LineTo(Point(lane.EndpointB));painter.Stroke();}
            };
            foreach(var node in nodes)
            {
                var marker=button(map,node.DisplayName,()=>perform(()=>workspace.SelectedNode=node.NodeId));marker.name="frontier-node-"+node.NodeId;
                marker.style.position=Position.Absolute;marker.style.width=150;marker.style.whiteSpace=WhiteSpace.Normal;
                map.RegisterCallback<GeometryChangedEvent>(_=>{var point=Point(node.NodeId);marker.style.left=point.x-75;marker.style.top=point.y-20;lines.MarkDirtyRepaint();});
            }
            Text(mapPanel,"Schematic arrangement only; lane cost and movement come from the campaign. No unknown systems, ownership or enemy fleets are drawn.");
            foreach(var lane in lanes)Text(mapPanel,Name(lane.EndpointA)+" ↔ "+Name(lane.EndpointB)+" · cost "+lane.Cost,"frontier-lane-"+lane.LaneId);
            var own=frame.OwnStrategicFleets;
            if(!own.Any(f=>f.FleetId.Value==workspace.SelectedFleet))workspace.SelectedFleet=own.FirstOrDefault()?.FleetId.Value;
            foreach(var fleet in own)
            {
                var choose=button(details,"Your fleet · "+fleet.ShipIds.Count+" ship(s) · "+Name(fleet.LocationNodeId.Value),()=>perform(()=>workspace.SelectedFleet=fleet.FleetId.Value));
                choose.name="frontier-fleet-"+fleet.FleetId;choose.style.whiteSpace=WhiteSpace.Normal;
            }
            var selected=own.FirstOrDefault(f=>f.FleetId.Value==workspace.SelectedFleet);
            if(workspace.SelectedNode!=null&&!nodes.Any(n=>n.NodeId==workspace.SelectedNode))workspace.SelectedNode=null;
            if(workspace.RoutePreview!=null&&!workspace.RoutePreviewMatchesCurrent)workspace.RoutePreview=null;
            Text(details,"Destination: "+(workspace.SelectedNode==null?"select a known node":Name(workspace.SelectedNode)),"frontier-selected-node");
            bool editable=frame.Status.Phase==SessionPhase.Planning&&!model.Busy&&!model.Closed&&!model.HasLocalEdits&&!model.Current.RefreshRequired&&!model.Current.HasPendingWork;
            bool Safe(KnownRouteOffer offer)=>offer.Actor==known.ActorId&&offer.Route.Success&&offer.Route.Steps.All(id=>nodes.Any(n=>n.NodeId==id))
                &&offer.Route.Steps.Skip(1).Select((id,i)=>lanes.Any(l=>l.EndpointA==offer.Route.Steps[i]&&l.EndpointB==id||l.EndpointB==offer.Route.Steps[i]&&l.EndpointA==id)).All(v=>v);
            var preview=button(details,"Preview known route",()=>perform(()=>{
                var offer=model.Current.FindKnownRoute(workspace.SelectedFleet!,workspace.SelectedNode!);workspace.RoutePreview=offer!=null&&Safe(offer)?offer:null;
            }));preview.name="frontier-preview-route";preview.SetEnabled(editable&&selected!=null&&workspace.SelectedNode!=null&&workspace.SelectedNode!=selected.LocationNodeId.Value);
            var route=workspace.RoutePreview;
            if(route!=null&&Safe(route)&&workspace.RoutePreviewMatchesCurrent)
            {
                Text(details,string.Join(" → ",route.Route.Steps.Select(Name))+" · total cost "+route.Route.TotalCost,"frontier-route-preview");
                var send=button(details,"Review / send route",()=>confirm("Send "+string.Join(" → ",route.Route.Steps.Select(Name))+"? Total cost "+route.Route.TotalCost+". Movement uses the campaign's budget. Preview and partial travel reveal no new nodes.",()=>perform(()=>model.SendPreviewedTravel(route,ReviewUiModel.NewCommandId()))));
                send.name="frontier-send-route";send.SetEnabled(editable);
            }
            else Text(details,"Select your fleet and a known destination, then preview. If no route is offered, none is sent.","frontier-next-action");
            if(selected!=null)
            {
                var receipt=frame.Travel?.OwnReceipts.LastOrDefault(r=>r.FleetId==selected.FleetId.Value);
                if(receipt!=null)Text(details,receipt.TransitDestinationId==null?"Last travel ended at "+Name(receipt.EndNodeId):"In transit toward "+Name(receipt.TransitDestinationId)+" · progress "+receipt.TransitProgress+" / "+receipt.TransitTotalCost,"frontier-own-travel");
                button(details,"Inspect your fleet",()=>navigate(ReviewWorkspacePage.FleetOrders));
            }
            if(frame.Status.Phase!=SessionPhase.Planning)button(details,"Open pending combat",()=>navigate(ReviewWorkspacePage.Combat));
            Text(mapPanel,"Your arrival discoveries","frontier-receipts-heading");
            if(known.Receipts.Count==0)Text(mapPanel,"No arrival discovery receipts yet. Starting presence, preview and partial travel do not chart branches.","frontier-no-receipts");
            foreach(var receipt in known.Receipts)
            {
                var names=receipt.ChartedNodeIds.Where(id=>nodes.Any(n=>n.NodeId==id)).Select(Name).ToArray();
                Text(mapPanel,"Turn "+receipt.Turn+" · arrival "+Name(receipt.ArrivalNodeId)+" · "+(names.Length==0?"no new node identities":string.Join(", ",names)),"frontier-arrival-receipt");
            }
        }
    }
}
