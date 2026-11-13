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
        public ReviewFleetRoster(ReviewUiModel model,ReviewWorkspace workspace,Func<VisualElement,string,Action,Button> button,
            Action<Action> perform,Action<string,Action> confirm,Action<ReviewWorkspacePage> navigate)
        {this.model=model;this.workspace=workspace;this.button=button;this.perform=perform;this.confirm=confirm;this.navigate=navigate;}
        private static void Text(VisualElement p,string value,string cls="console-copy")
        {var l=new Label(value);l.AddToClassList(cls);p.Add(l);}
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
            var main=new ScrollView(ScrollViewMode.Vertical);main.AddToClassList("console-panel");main.AddToClassList("console-work");main.AddToClassList("fleet-card-workspace");parent.Add(main);
            var aside=new ScrollView(ScrollViewMode.Vertical);aside.AddToClassList("console-panel");aside.AddToClassList("console-aside");aside.AddToClassList("fleet-roster-aside");aside.AddToClassList("fleet-card-workspace");parent.Add(aside);
            var renderController=model.Current;var renderEpoch=workspace.NavigationEpoch;
            bool CurrentFleetRender()=>ReferenceEquals(model.Current,renderController)&&ReferenceEquals(model.Current.Frame,frame)
                &&workspace.NavigationEpoch==renderEpoch&&!model.Busy&&!model.Closed
                &&!model.Current.HasPendingWork&&!model.Current.RefreshRequired
                &&main.panel!=null&&aside.panel!=null&&ReferenceEquals(main.parent,parent)&&ReferenceEquals(aside.parent,parent);
            Text(main,"Fleet roster","console-section");
            var sorts=new VisualElement();sorts.AddToClassList("review-field-row");main.Add(sorts);
            foreach(var sort in new[]{"Name","System","Hull damage","Ship count"})
                button(sorts,sort+(workspace.FleetSort==sort?(workspace.FleetSortDescending?" ↓":" ↑"):""),()=>perform(()=>{
                    if(workspace.FleetSort==sort)workspace.FleetSortDescending=!workspace.FleetSortDescending;
                    else{workspace.FleetSort=sort;workspace.FleetSortDescending=false;}
                }));
            Text(main,"Power rating unavailable. Hull damage is the sum of known missing hull points; it is not combat strength.");
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
            foreach(var fleet in ordered)
            {
                var id=fleet.Fleet.FleetId.Value;var expanded=workspace.SelectedFleet==id;
                var card=new VisualElement();card.AddToClassList("console-queue");main.Add(card);
                var select=button(card,(expanded?"▾ ":"▸ ")+fleet.DisplayName+" · "+Location(fleet)+" · "+fleet.Fleet.ShipIds.Count+" ships",()=>perform(()=>workspace.SelectedFleet=expanded?null:id));
                select.name="fleet-roster-"+id;select.EnableInClassList("selected-nav",expanded);
                Text(card,"Missing hull: "+Damage(fleet)+" · "+(fleet.CanOrganize?"Stationary · available to organize":"Organization unavailable: "+fleet.UnavailableReason));
                if(!expanded)continue;
                Text(aside,fleet.DisplayName,"console-section");
                var summary=new VisualElement{name="fleet-navigation-summary"};summary.AddToClassList("fleet-navigation-summary");aside.Add(summary);
                Text(summary,"Navigation & tasks","console-section");Text(summary,"Location: "+Location(fleet));
                var order=frame.Travel?.OwnOrders.FirstOrDefault(v=>v.FleetId==id);
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
                var members=new VisualElement{name="fleet-selected-ship-cards"};card.Add(members);
                Text(members,"Members of "+fleet.DisplayName,"console-section");
                Text(members,"Select members for assembly or transfer. Inspection does not change fleet membership.");
                foreach(var ship in facts.Ships.Where(s=>s.FleetId==fleet.Fleet.FleetId).OrderBy(s=>s.ShipId.Value,StringComparer.Ordinal))
                {
                    var sid=ship.ShipId.Value;var condition=frame.OwnShips.FirstOrDefault(s=>s.Condition.ShipId==sid);
                    var design=facts.Construction.FirstOrDefault(c=>c.Design.DefinitionId==ship.ShipDesignDefinitionId)?.Design.DisplayName??"unavailable";
                    var row=new VisualElement{name="fleet-ship-card-"+sid};row.AddToClassList("fleet-ship-card");members.Add(row);
                    var source=activity?.Ships.FirstOrDefault(v=>v.ShipId==sid&&v.FleetId==id);
                    var identity=ReviewShipNames.Ship(sid,ship.OwnerCivilizationId.Value);
                    Text(row,identity?.Label??"Vessel","fleet-ship-title");
                    Text(row,"Class / fit: "+(source!=null&&!string.IsNullOrWhiteSpace(source.DisplayName)?source.DisplayName:design));
                    var conditionArea=new VisualElement();conditionArea.AddToClassList("fleet-card-condition-area");row.Add(conditionArea);
                    var meters=new VisualElement();meters.AddToClassList("fleet-card-meters");conditionArea.Add(meters);
                    var pick=new Toggle("Select member for organization"){value=model.OrganizationShips.Contains(sid),name="fleet-member-"+sid};
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
                    if(condition!=null)
                    {
                        Meter(meters,"Hull",condition.Condition.Hull,condition.Condition.MaxHull,"fleet-hull-"+sid);
                        meters.Q("fleet-hull-"+sid).AddToClassList("fleet-hull-meter");
                        var sectors=new[]{"Fore","Port","Starboard","Aft"};
                        for(var i=0;i<4;i++)
                        {
                            if(condition.Condition.Shields.Count>i&&condition.ShieldMaximum.Count>i)
                                Meter(meters,sectors[i],condition.Condition.Shields[i],condition.ShieldMaximum[i],"fleet-shield-"+sid+"-"+i);
                            else Text(meters,sectors[i]+": unavailable");
                        }
                        foreach(var weapon in condition.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))
                            Text(row,"Torpedoes "+weapon.Ammunition+" / "+weapon.MagazineCapacity);
                    }
                    else Text(row,"Hull, shields and ammunition unavailable.");
                    var inspect=button(row,"Inspect ship",()=>perform(()=>workspace.SelectedOwnShip=sid));inspect.name="fleet-inspect-"+sid;
                    if(workspace.SelectedOwnShip==sid)
                    {
                        var inspection=new VisualElement{name="fleet-selected-ship-inspector"};aside.Add(inspection);
                        Text(inspection,identity?.Label??"Selected vessel","fleet-ship-title");
                        Text(inspection,"Class / fit: "+(source?.DisplayName??design));
                        inspection.Add(new ReviewShipArtPanel(facts.Civilization.DefinitionId.Value,ship.ShipDesignDefinitionId.Value,exactHullVersionId:source?.HullVersionId));
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
            Text(organization,"Organize fleet","console-section");
            Text(organization,"Choose ships from owned fleets in the same location. Clear routes and use Remain before organizing. No damage, ammunition or hardware is reset.");
            Text(organization,model.OrganizationShips.Count+" selected members · unsent changes");
            var name=new TextField("Fleet name"){value=model.OrganizationName,isDelayed=true,name="fleet-organization-name"};
            name.AddToClassList("review-input");
            name.SetEnabled(editable);organization.Add(name);name.RegisterValueChangedCallback(e=>perform(()=>model.OrganizationName=e.newValue));
            void Review(CampaignFleetOrganizationKind kind,string? target)
            {
                var reviewed=model.Current.Frame;var members=model.OrganizationShips.OrderBy(s=>s,StringComparer.Ordinal).ToArray();
                var display=model.OrganizationName;
                confirm(kind+" fleet?\n"+(kind==CampaignFleetOrganizationKind.Rename?"":members.Length+" selected ships\n")+
                    (kind==CampaignFleetOrganizationKind.Transfer?"Destination: "+fleets.Single(f=>f.Fleet.FleetId.Value==target).DisplayName:"Name: "+display)+
                    "\nThe campaign validates membership, position and accepted orders.",()=>perform(()=>{
                        if(model.OrganizeFleet(reviewed,kind,kind==CampaignFleetOrganizationKind.Rename?Array.Empty<string>():members,target,
                            kind==CampaignFleetOrganizationKind.Transfer?null:display,ReviewUiModel.NewCommandId()))
                        {
                            if(target!=null)workspace.SelectedFleet=target;
                            else workspace.SelectedFleet=model.NamedFleets().FirstOrDefault(f=>members.All(s=>f.Fleet.ShipIds.Any(id=>id.Value==s)))?.Fleet.FleetId.Value;
                        }
                    }));
            }
            var create=button(organization,"Review new fleet",()=>Review(CampaignFleetOrganizationKind.Create,null));create.name="fleet-create";
            create.SetEnabled(editable&&model.OrganizationShips.Count>0&&!string.IsNullOrWhiteSpace(model.OrganizationName));
            var selected=fleets.FirstOrDefault(f=>f.Fleet.FleetId.Value==workspace.SelectedFleet);
            if(selected!=null)
            {
                var rename=button(organization,"Review rename of "+selected.DisplayName,()=>Review(CampaignFleetOrganizationKind.Rename,selected.Fleet.FleetId.Value));
                rename.name="fleet-rename";rename.SetEnabled(editable&&selected.CanOrganize&&!string.IsNullOrWhiteSpace(model.OrganizationName));
                Text(organization,"Selected fleet: "+selected.DisplayName,"console-section");
                button(organization,"Locate on map",()=>{workspace.SelectedNode=selected.Fleet.LocationNodeId.Value;navigate(ReviewWorkspacePage.Map);});
                var stay=button(organization,"Remain / clear accepted travel",()=>confirm("Replace this fleet's route and intent with Remain? This does not move or repair ships.",()=>perform(()=>{
                    if(model.DraftTravel(selected.Fleet.FleetId.Value,CampaignTravelIntent.Stay,null,null))model.SendTravel(ReviewUiModel.NewCommandId());
                })));stay.name="fleet-clear-route";stay.SetEnabled(editable&&!model.HasLocalEdits);
                var defend=button(organization,"Defend this region",()=>confirm("Hold here and engage hostile fleets when a lawful encounter is possible.",()=>perform(()=>{
                    if(model.CanDefendFleet(frame,selected.Fleet.FleetId.Value)&&model.DraftTravel(selected.Fleet.FleetId.Value,CampaignTravelIntent.StayEngage,null,null))
                        model.SendTravel(ReviewUiModel.NewCommandId());
                })));defend.name="player-stay-engage";defend.SetEnabled(model.CanDefendFleet(frame,selected.Fleet.FleetId.Value));
                var setting=frame.CloakSettings.FirstOrDefault(s=>s.FleetId==selected.Fleet.FleetId.Value);
                if(setting!=null)
                    button(organization,"Approach cloak: "+(setting.Enabled?"enabled":"disabled"),()=>confirm("Change the selected fleet's approach cloak setting? Only explicitly capable ships can cloak.",()=>perform(()=>model.SetCloak(selected.Fleet.FleetId.Value,!setting.Enabled,ReviewUiModel.NewCommandId()))))
                        .SetEnabled(editable&&!model.HasLocalEdits);
            }
            Text(organization,"Transfer selected members to:","console-section");
            foreach(var destination in ordered)
            {
                var transfer=button(organization,destination.DisplayName+" · "+Location(destination),()=>Review(CampaignFleetOrganizationKind.Transfer,destination.Fleet.FleetId.Value));
                transfer.name="fleet-transfer-"+destination.Fleet.FleetId;transfer.SetEnabled(editable&&destination.CanOrganize&&model.OrganizationShips.Count>0);
            }
            button(organization,"Discard organization draft",()=>perform(model.ResetOrganization)).SetEnabled(model.OrganizationDirty);
            button(organization,"Build existing ships",()=>navigate(ReviewWorkspacePage.Production));
        }
    }
}
