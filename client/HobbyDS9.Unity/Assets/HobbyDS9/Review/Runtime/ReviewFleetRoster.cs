#nullable enable
using System;
using System.Linq;
using UnityEngine;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed class ReviewFleetRoster
    {
        private readonly ReviewUiModel model;
        private readonly ReviewWorkspace workspace;
        private readonly Func<VisualElement,string,Action,Button> button;
        private readonly Action<Action> perform;
        private readonly Action<string,Action> confirm;
        private readonly Action<ReviewWorkspacePage> navigate;
        // Exact original 30-unit no-fill strokes from art/playtest-20261118/icon-strokes.json.
        // Kept fleet-local: no global icon enum, SVG importer or additional package.
        private sealed class FleetStatusIcon:VisualElement
        {
            private static readonly float[][] Supply={new float[]{6,11,24,11,24,26,6,26,6,11},new float[]{10,11,10,6,20,6,20,11},new float[]{6,17,24,17},new float[]{15,11,15,26}};
            private static readonly float[][] Shield={new float[]{15,3,25,7,24,18,21,23,15,27,9,23,6,18,5,7,15,3}};
            private static readonly float[][] Mission={new float[]{5,23,11,23,11,15,20,15,20,7,26,7},new float[]{22,3,26,7,22,11},new float[]{3,20,7,20,7,26,3,26,3,20}};
            private static readonly float[][] Ship={new float[]{3,19,7,13,11,13,11,7,15,4,19,7,19,13,23,13,27,19,21,21,9,21,3,19},new float[]{7,13,7,6},new float[]{23,13,23,6},new float[]{12,24,18,24}};
            internal FleetStatusIcon(string kind,string label,bool reported)
            {
                name="fleet-status-icon-"+kind;tooltip=label;pickingMode=PickingMode.Ignore;style.width=style.height=30;style.flexShrink=0;
                style.color=reported?new Color(.73f,.67f,.9f):new Color(.57f,.63f,.7f);
                var lines=kind=="supply"?Supply:kind=="shield"?Shield:kind=="mission"?Mission:Ship;
                generateVisualContent+=context=>{
                    var painter=context.painter2D;painter.lineWidth=2;painter.strokeColor=resolvedStyle.color;
                    foreach(var line in lines){painter.BeginPath();for(var i=0;i<line.Length;i+=2){var point=new Vector2(line[i],line[i+1]);if(i==0)painter.MoveTo(point);else painter.LineTo(point);}painter.Stroke();}
                };
            }
        }
        public ReviewFleetRoster(ReviewUiModel model,ReviewWorkspace workspace,Func<VisualElement,string,Action,Button> button,
            Action<Action> perform,Action<string,Action> confirm,Action<ReviewWorkspacePage> navigate)
        {this.model=model;this.workspace=workspace;this.button=button;this.perform=perform;this.confirm=confirm;this.navigate=navigate;}
        private static void Text(VisualElement p,string value,string cls="console-copy")
        {var l=new Label(value);l.AddToClassList(cls);p.Add(l);}
        private static void Status(VisualElement parent,string title,string value,string name)
        {
            var cell=new VisualElement{name=name,tooltip=title+": "+value};cell.AddToClassList("fleet-roster-status");parent.Add(cell);
            var heading=new VisualElement();heading.style.flexDirection=FlexDirection.Row;heading.style.alignItems=Align.Center;cell.Add(heading);
            heading.Add(new FleetStatusIcon(title=="Shields"?"shield":title.ToLowerInvariant(),title+": "+value,value!="Not reported"));
            Text(heading,title,"fleet-roster-status-title");Text(cell,value,"fleet-roster-status-value");
        }
        private static void Meter(VisualElement parent,string title,long current,long maximum,string id)
        {
            var group=new VisualElement{name=id};group.AddToClassList("fleet-condition-meter");parent.Add(group);
            Text(group,title+" "+current+" / "+maximum);
            group.Add(new ProgressBar{lowValue=0,highValue=100,value=maximum<=0?0:(float)Math.Max(0,Math.Min(100,100d*current/maximum)),name=id+"-bar"});
        }
        public void Render(VisualElement parent)
        {
            var frame=model.Current.Frame;var facts=frame.Strategy!;var fleets=model.NamedFleets();
            CampaignActivityObservationDto? activity=null;
            if((model.ColonisationCampaign||model.ActivitiesCampaign)&&!model.Busy&&!model.Closed&&!model.Current.RefreshRequired&&!model.Current.HasPendingWork)
            {
                var observed=model.ColonisationCampaign?model.ColonisationActivities():model.Activities();
                if(ReferenceEquals(frame,model.Current.Frame)&&observed.ActorId==frame.Actor&&observed.Turn==frame.Status.Turn&&observed.Revision==frame.Status.CampaignRevision)activity=observed;
            }
            string Location(CampaignNamedFleet f)=>facts.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==f.Fleet.LocationNodeId.Value)?.DisplayName??"Known location";
            decimal Damage(CampaignNamedFleet f)=>frame.OwnShips.Where(s=>s.Condition.FleetId==f.Fleet.FleetId.Value).Sum(s=>(decimal)s.Condition.MaxHull-s.Condition.Hull);
            var state=FleetRosterViewState.For(workspace);
            var main=new VisualElement{name="fleet-roster-main"};main.AddToClassList("console-panel");main.AddToClassList("console-work");main.AddToClassList("fleet-card-workspace");main.AddToClassList("fleet-compact-roster");parent.Add(main);
            var aside=new ScrollView(ScrollViewMode.Vertical);aside.AddToClassList("console-panel");aside.AddToClassList("console-aside");aside.AddToClassList("fleet-roster-aside");aside.AddToClassList("fleet-card-workspace");parent.Add(aside);
            aside.AddToClassList("fleet-compact-roster");var sheet=Resources.Load<StyleSheet>("FleetRosterCompact");if(sheet!=null){main.styleSheets.Add(sheet);aside.styleSheets.Add(sheet);}
            var renderController=model.Current;var renderEpoch=workspace.NavigationEpoch;
            bool CurrentFleetRender()=>ReferenceEquals(model.Current,renderController)&&ReferenceEquals(model.Current.Frame,frame)
                &&workspace.NavigationEpoch==renderEpoch&&!model.Busy&&!model.Closed
                &&!model.Current.HasPendingWork&&!model.Current.RefreshRequired
                &&main.panel!=null&&aside.panel!=null&&ReferenceEquals(main.parent,parent)&&ReferenceEquals(aside.parent,parent);
            Text(main,"Ships & fleets","console-section");
            var filters=new VisualElement{name="fleet-roster-filters"};filters.AddToClassList("review-field-row");main.Add(filters);
            var search=new TextField("Search ships"){name="fleet-roster-search",value=state.Search,isDelayed=true,tooltip="Ship name, class, fleet, location or current mission"};search.AddToClassList("review-input");search.style.flexGrow=1;search.style.flexDirection=FlexDirection.Column;filters.Add(search);
            search.RegisterValueChangedCallback(e=>{if(CurrentFleetRender())perform(()=>state.Search=e.newValue);});
            var fleetNames=new[]{"All fleets"}.Concat(fleets.Select(f=>f.DisplayName).Distinct(StringComparer.Ordinal).OrderBy(n=>n,StringComparer.Ordinal)).ToArray();
            if(!fleetNames.Contains(state.Fleet,StringComparer.Ordinal))state.Fleet="All fleets";
            var fleetFilter=new ReviewLcarsSelector("Fleet",fleetNames,state.Fleet,value=>{if(CurrentFleetRender())perform(()=>state.Fleet=value);},CurrentFleetRender){name="fleet-roster-fleet-filter"};filters.Add(fleetFilter);
            var conditionFilter=new ReviewLcarsSelector("Condition",new[]{"All conditions","Hull damage","Reduced shields","Status not reported"},state.Condition,value=>{if(CurrentFleetRender())perform(()=>state.Condition=value);},CurrentFleetRender){name="fleet-roster-condition-filter"};filters.Add(conditionFilter);
            button(filters,"Clear",()=>{if(CurrentFleetRender())perform(()=>{state.Search="";state.Fleet="All fleets";state.Condition="All conditions";});}).name="fleet-roster-clear";
            var sorts=new VisualElement();sorts.AddToClassList("review-field-row");main.Add(sorts);
            foreach(var sort in new[]{"Name","System","Hull damage","Ship count"})
                button(sorts,sort+(workspace.FleetSort==sort?(workspace.FleetSortDescending?" ↓":" ↑"):""),()=>perform(()=>{
                    if(!CurrentFleetRender())return;
                    if(workspace.FleetSort==sort)workspace.FleetSortDescending=!workspace.FleetSortDescending;
                    else{workspace.FleetSort=sort;workspace.FleetSortDescending=false;}
                }));
            Text(main,"Current owned condition · Supply not reported · Shields are the actual sum of directional sectors.","fleet-roster-key");
            var ordered=fleets.OrderBy(f=>f.Fleet.FleetId.Value,StringComparer.Ordinal).ToList();
            ordered.Sort((a,b)=>{
                int result=workspace.FleetSort=="System"?StringComparer.OrdinalIgnoreCase.Compare(Location(a),Location(b)):
                    workspace.FleetSort=="Hull damage"?Damage(a).CompareTo(Damage(b)):
                    workspace.FleetSort=="Ship count"?a.Fleet.ShipIds.Count.CompareTo(b.Fleet.ShipIds.Count):
                    StringComparer.OrdinalIgnoreCase.Compare(a.DisplayName,b.DisplayName);
                if(workspace.FleetSortDescending)result=-result;
                return result!=0?result:StringComparer.Ordinal.Compare(a.Fleet.FleetId.Value,b.Fleet.FleetId.Value);
            });
            bool editable=frame.Status.Phase==SessionPhase.Planning&&!model.Busy&&!model.Closed&&!model.Current.HasPendingWork&&!model.Current.RefreshRequired;
            var list=new ScrollView(ScrollViewMode.VerticalAndHorizontal){name="fleet-roster-list"};list.style.flexGrow=1;list.style.minHeight=0;main.Add(list);
            var table=new VisualElement{name="fleet-roster-table"};table.AddToClassList("fleet-roster-table");list.Add(table);
            var visibleShips=0;var totalShips=facts.Ships.Count;
            foreach(var fleet in ordered)
            {
                var id=fleet.Fleet.FleetId.Value;var expanded=workspace.SelectedFleet==id;
                var order=frame.Travel?.OwnOrders.FirstOrDefault(v=>v.FleetId==id);
                var finalDestination=order?.RouteNodeIds.LastOrDefault()??(fleet.Fleet.RouteNodeIds.Count>0?fleet.Fleet.RouteNodeIds[fleet.Fleet.RouteNodeIds.Count-1].Value:null);
                var destinationName=finalDestination==null?null:facts.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==finalDestination)?.DisplayName;
                var mission=FleetRosterPresentation.Mission(order?.Intent,fleet.Fleet.RouteNodeIds.Count,destinationName,activity?.Findings.Any(f=>f.FleetId==id&&f.Stage==ActivityProgressStage.Active)==true,fleet.Fleet.TransitTotalCost>0);
                var ships=facts.Ships.Where(s=>s.FleetId==fleet.Fleet.FleetId).OrderBy(s=>s.ShipId.Value,StringComparer.Ordinal).Where(ship=>{
                    var sid=ship.ShipId.Value;var known=frame.OwnShips.FirstOrDefault(s=>s.Condition.ShipId==sid);var source=activity?.Ships.FirstOrDefault(s=>s.ShipId==sid&&s.FleetId==id);
                    var design=source!=null&&!string.IsNullOrWhiteSpace(source.DisplayName)?source.DisplayName:facts.Construction.FirstOrDefault(c=>c.Design.DefinitionId==ship.ShipDesignDefinitionId)?.Design.DisplayName??"Class not reported";
                    return FleetRosterPresentation.Matches(state.Search,state.Condition,state.Fleet,fleet.DisplayName,ReviewShipNames.Ship(sid,ship.OwnerCivilizationId.Value)?.Label??"Vessel",design,Location(fleet),mission,known==null?(bool?)null:known.Condition.Hull<known.Condition.MaxHull,FleetRosterPresentation.Shields(known?.Condition.Shields,known?.ShieldMaximum));
                }).ToList();
                if(ships.Count==0)continue;visibleShips+=ships.Count;
                var card=new VisualElement();card.AddToClassList("fleet-roster-group");table.Add(card);
                var select=button(card,fleet.DisplayName+" · "+Location(fleet)+" · "+fleet.Fleet.ShipIds.Count+" ships",()=>{if(CurrentFleetRender())perform(()=>workspace.SelectedFleet=expanded?null:id);});
                select.tooltip="Inspect fleet navigation and organization; ship rows remain visible.";
                select.name="fleet-roster-"+id;select.EnableInClassList("selected-nav",expanded);
                select.AddToClassList("fleet-roster-group-title");
                if(expanded)
                {
                Text(aside,fleet.DisplayName,"console-section");
                var summary=new VisualElement{name="fleet-navigation-summary"};summary.AddToClassList("fleet-navigation-summary");aside.Add(summary);
                Text(summary,"Navigation & tasks","console-section");Text(summary,"Location: "+Location(fleet));
                Text(summary,order==null?"Accepted travel/task details unavailable in this campaign view.":"Accepted intent: "+order.Intent);
                if(order!=null&&order.RouteNodeIds.Count>0)
                {
                    var destination=order.RouteNodeIds[order.RouteNodeIds.Count-1];
                    Text(summary,"Destination: "+(facts.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==destination)?.DisplayName??"Known route destination"));
                }
                Text(summary,"Accepted route: "+(fleet.Fleet.RouteNodeIds.Count==0?"No route steps":string.Join(" → ",fleet.Fleet.RouteNodeIds.Select(n=>facts.Navigation.Nodes.FirstOrDefault(v=>StringComparer.Ordinal.Equals(v.NodeId.Value,n.Value))?.DisplayName??"Known region"))));
                if(fleet.Fleet.TransitTotalCost>0)Text(summary,"Transit work paid: "+fleet.Fleet.TransitProgress+" / "+fleet.Fleet.TransitTotalCost);
                var tasks=activity?.Findings.Where(f=>f.FleetId==id&&f.Stage==ActivityProgressStage.Active).ToList();
                if(tasks==null)Text(summary,"Activity assignments unavailable in this view.");
                else if(tasks.Count==0)Text(summary,"No active study assignments reported.");
                else foreach(var task in tasks)Text(summary,"Active study: "+task.FindingId+" · work "+task.WorkDone);
                Text(summary,"ETA unavailable in this view. Review travel on the map.");
                if(model.HasLocalEdits)Text(summary,"Unsent edits remain separate from accepted orders.");
                }
                var members=new VisualElement{name=expanded?"fleet-selected-ship-cards":"fleet-members-"+id};card.Add(members);
                foreach(var ship in ships)
                {
                    var sid=ship.ShipId.Value;var condition=frame.OwnShips.FirstOrDefault(s=>s.Condition.ShipId==sid);
                    var design=facts.Construction.FirstOrDefault(c=>c.Design.DefinitionId==ship.ShipDesignDefinitionId)?.Design.DisplayName??"unavailable";
                    var row=new VisualElement{name="fleet-ship-card-"+sid};row.AddToClassList("fleet-roster-ship-row");row.EnableInClassList("fleet-roster-ship-selected",workspace.SelectedOwnShip==sid);members.Add(row);
                    var source=activity?.Ships.FirstOrDefault(v=>v.ShipId==sid&&v.FleetId==id);
                    var identity=ReviewShipNames.Ship(sid,ship.OwnerCivilizationId.Value);
                    var texture=source==null?null:ConstructionCardArt.Ship(source.HullVersionId);
                    var icon=new VisualElement{name="fleet-ship-icon-"+sid,tooltip=texture==null?"Matching approved ship icon not available":source!.HullVersionId=="hull.defiant-combat.v1"?"Defiant art · review-only reference":"Approved hull reference icon"};icon.AddToClassList("fleet-roster-ship-icon");
                    if(texture!=null){var image=new Image{image=texture,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};image.StretchToParentSize();icon.Add(image);}else{icon.style.alignItems=Align.Center;icon.Add(new FleetStatusIcon("ship","Generic vessel; matching hull image not reported",false));icon.Add(new Label("Art —"));}row.Add(icon);
                    var identityCell=new VisualElement();identityCell.AddToClassList("fleet-roster-identity");row.Add(identityCell);
                    var inspect=button(identityCell,identity?.Label??"Vessel",()=>{if(CurrentFleetRender())perform(()=>{workspace.SelectedFleet=id;workspace.SelectedOwnShip=sid;});});inspect.name="fleet-inspect-"+sid;inspect.AddToClassList("fleet-ship-title");
                    Text(identityCell,"Class / fit: "+(source!=null&&!string.IsNullOrWhiteSpace(source.DisplayName)?source.DisplayName:design),"fleet-roster-class");
                    var shield=FleetRosterPresentation.Shields(condition?.Condition.Shields,condition?.ShieldMaximum);
                    Status(row,"Supply","Not reported","fleet-supply-"+sid);
                    Status(row,"Shields",shield.Caption,"fleet-shield-summary-"+sid);
                    Status(row,"Mission",mission,"fleet-mission-"+sid);
                    var pick=new Toggle("Select"){value=model.OrganizationShips.Contains(sid),name="fleet-member-"+sid,tooltip="Select member for organization"};pick.AddToClassList("fleet-roster-member-toggle");
                    pick.SetEnabled(editable&&fleet.CanOrganize);row.Add(pick);
                    pick.RegisterValueChangedCallback(e=>
                    {
                        if(!ReferenceEquals(e.target,pick)||!CurrentFleetRender()||!members.Contains(pick)||!pick.enabledInHierarchy)return;
                        var memberSelected=e.newValue;
                        perform(()=>
                        {
                            if(!CurrentFleetRender()||!members.Contains(pick)||!pick.enabledInHierarchy)return;
                            if(memberSelected){model.OrganizationShips.Add(sid);workspace.FleetOrganizationExpanded=true;}
                            else model.OrganizationShips.Remove(sid);
                        });
                    });
                    if(workspace.SelectedOwnShip==sid)
                    {
                        var inspection=new VisualElement{name="fleet-selected-ship-inspector"};aside.Add(inspection);
                        Text(inspection,identity?.Label??"Selected vessel","fleet-ship-title");
                        Text(inspection,"Class / fit: "+(source?.DisplayName??design));
                        inspection.Add(new ReviewShipArtPanel(facts.Civilization.DefinitionId.Value,ship.ShipDesignDefinitionId.Value));
                        if(condition!=null)
                        {
                            var stocks=new VisualElement();stocks.AddToClassList("fleet-card-meters");inspection.Add(stocks);
                            Meter(stocks,"Hull",condition.Condition.Hull,condition.Condition.MaxHull,"fleet-selected-hull");
                            var sectors=new[]{"Fore","Port","Starboard","Aft"};
                            for(int i=0;i<4;i++)if(condition.Condition.Shields.Count>i&&condition.ShieldMaximum.Count>i)
                                Meter(stocks,sectors[i],condition.Condition.Shields[i],condition.ShieldMaximum[i],"fleet-selected-shield-"+i);
                            foreach(var weapon in condition.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))
                                Text(inspection,"Torpedoes "+weapon.Ammunition+" / "+weapon.MagazineCapacity);
                        }
                    }
                }
            }
            var count=new Label(visibleShips+" / "+totalShips+" owned ships"){name="fleet-roster-visible-count"};main.Insert(main.IndexOf(list),count);
            if(visibleShips==0)Text(table,totalShips==0?"No surviving owned ships.":"No ships match these filters.");
            if(ordered.Count==0)Text(main,"No surviving owned fleets. Supported ships can still be built at eligible shipyards.");
            if(!ordered.Any(f=>f.Fleet.FleetId.Value==workspace.SelectedFleet))Text(aside,"Select a fleet to inspect its ships.");
            else if(aside.Q("fleet-selected-ship-inspector")==null)Text(aside,"Inspect a member in the roster to view its full hull and current condition.");
            var organization=new Foldout{name="fleet-organization-foldout",text="Organize fleet",value=workspace.FleetOrganizationExpanded};main.Add(organization);
            bool CurrentOrganizationRender()=>CurrentFleetRender()&&main.Contains(organization);
            organization.RegisterValueChangedCallback(e=>
            {
                if(!ReferenceEquals(e.target,organization)||!CurrentOrganizationRender())return;
                var expanded=e.newValue;
                perform(()=>{if(CurrentOrganizationRender())workspace.FleetOrganizationExpanded=expanded;});
            });
            var organizationContent=new ScrollView(ScrollViewMode.Vertical){name="fleet-organization-content"};organizationContent.style.maxHeight=235;organizationContent.style.minHeight=0;organization.Add(organizationContent);
            Text(organizationContent,"Select co-located ships with cleared routes. Existing condition and hardware are retained.");
            Text(organizationContent,model.OrganizationShips.Count+" selected members · unsent changes");
            var name=new TextField("Fleet name"){value=model.OrganizationName,isDelayed=true,name="fleet-organization-name"};
            name.AddToClassList("review-input");
            name.SetEnabled(editable);organizationContent.Add(name);name.RegisterValueChangedCallback(e=>{if(CurrentOrganizationRender())perform(()=>model.OrganizationName=e.newValue);});
            void Review(CampaignFleetOrganizationKind kind,string? target)
            {
                if(!CurrentOrganizationRender())return;
                var reviewed=model.Current.Frame;var members=model.OrganizationShips.OrderBy(s=>s,StringComparer.Ordinal).ToArray();
                var display=model.OrganizationName;
                confirm(kind+" fleet?\n"+(kind==CampaignFleetOrganizationKind.Rename?"":members.Length+" selected ships\n")+
                    (kind==CampaignFleetOrganizationKind.Transfer?"Destination: "+fleets.Single(f=>f.Fleet.FleetId.Value==target).DisplayName:"Name: "+display)+
                    "\nThe campaign validates membership, position and accepted orders.",()=>perform(()=>{
                        if(!CurrentOrganizationRender())return;
                        if(model.OrganizeFleet(reviewed,kind,kind==CampaignFleetOrganizationKind.Rename?Array.Empty<string>():members,target,
                            kind==CampaignFleetOrganizationKind.Transfer?null:display,ReviewUiModel.NewCommandId()))
                        {
                            if(target!=null)workspace.SelectedFleet=target;
                            else workspace.SelectedFleet=model.NamedFleets().FirstOrDefault(f=>members.All(s=>f.Fleet.ShipIds.Any(id=>id.Value==s)))?.Fleet.FleetId.Value;
                        }
                    }));
            }
            var create=button(organizationContent,"Review new fleet",()=>Review(CampaignFleetOrganizationKind.Create,null));create.name="fleet-create";
            create.SetEnabled(editable&&model.OrganizationShips.Count>0&&!string.IsNullOrWhiteSpace(model.OrganizationName));
            var selected=fleets.FirstOrDefault(f=>f.Fleet.FleetId.Value==workspace.SelectedFleet);
            if(selected!=null)
            {
                var rename=button(organizationContent,"Review rename of "+selected.DisplayName,()=>Review(CampaignFleetOrganizationKind.Rename,selected.Fleet.FleetId.Value));
                rename.name="fleet-rename";rename.SetEnabled(editable&&selected.CanOrganize&&!string.IsNullOrWhiteSpace(model.OrganizationName));
                Text(organizationContent,"Selected fleet: "+selected.DisplayName,"console-section");
                button(organizationContent,"Locate on map",()=>{workspace.SelectedNode=selected.Fleet.LocationNodeId.Value;navigate(ReviewWorkspacePage.Map);});
                var stay=button(organizationContent,"Remain / clear accepted travel",()=>confirm("Replace this fleet's route and intent with Remain? This does not move or repair ships.",()=>perform(()=>{
                    if(model.DraftTravel(selected.Fleet.FleetId.Value,CampaignTravelIntent.Stay,null,null))model.SendTravel(ReviewUiModel.NewCommandId());
                })));stay.name="fleet-clear-route";stay.SetEnabled(editable&&!model.HasLocalEdits);
                var defend=button(organizationContent,"Defend this region",()=>confirm("Hold here and engage hostile fleets when a lawful encounter is possible.",()=>perform(()=>{
                    if(model.CanDefendFleet(frame,selected.Fleet.FleetId.Value)&&model.DraftTravel(selected.Fleet.FleetId.Value,CampaignTravelIntent.StayEngage,null,null))
                        model.SendTravel(ReviewUiModel.NewCommandId());
                })));defend.name="player-stay-engage";defend.SetEnabled(model.CanDefendFleet(frame,selected.Fleet.FleetId.Value));
                var setting=frame.CloakSettings.FirstOrDefault(s=>s.FleetId==selected.Fleet.FleetId.Value);
                if(setting!=null)
                    button(organizationContent,"Approach cloak: "+(setting.Enabled?"enabled":"disabled"),()=>confirm("Change the selected fleet's approach cloak setting? Only explicitly capable ships can cloak.",()=>perform(()=>model.SetCloak(selected.Fleet.FleetId.Value,!setting.Enabled,ReviewUiModel.NewCommandId()))))
                        .SetEnabled(editable&&!model.HasLocalEdits);
            }
            Text(organizationContent,"Transfer selected members to:","console-section");
            foreach(var destination in ordered)
            {
                var transfer=button(organizationContent,destination.DisplayName+" · "+Location(destination),()=>Review(CampaignFleetOrganizationKind.Transfer,destination.Fleet.FleetId.Value));
                transfer.name="fleet-transfer-"+destination.Fleet.FleetId;transfer.SetEnabled(editable&&destination.CanOrganize&&model.OrganizationShips.Count>0);
            }
            button(organizationContent,"Discard organization draft",()=>perform(model.ResetOrganization)).SetEnabled(model.OrganizationDirty);
            button(organizationContent,"Build existing ships",()=>navigate(ReviewWorkspacePage.Production));
        }
    }
}
