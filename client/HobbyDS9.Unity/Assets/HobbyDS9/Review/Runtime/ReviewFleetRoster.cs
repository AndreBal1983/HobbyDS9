#nullable enable
using System;
using System.Linq;
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
        public void Render(VisualElement parent)
        {
            var frame=model.Current.Frame;var facts=frame.Strategy!;var fleets=model.NamedFleets();
            string Location(CampaignNamedFleet f)=>facts.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==f.Fleet.LocationNodeId.Value)?.DisplayName??"Known location";
            decimal Damage(CampaignNamedFleet f)=>frame.OwnShips.Where(s=>s.Condition.FleetId==f.Fleet.FleetId.Value).Sum(s=>(decimal)s.Condition.MaxHull-s.Condition.Hull);
            var main=new ScrollView(ScrollViewMode.Vertical);main.AddToClassList("console-panel");main.AddToClassList("console-work");parent.Add(main);
            var aside=new ScrollView(ScrollViewMode.Vertical);aside.AddToClassList("console-panel");aside.AddToClassList("console-aside");aside.AddToClassList("fleet-roster-aside");parent.Add(aside);
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
                Text(card,"Members of "+fleet.DisplayName,"console-section");
                Text(card,"Select members for assembly or transfer. Inspection does not change fleet membership.");
                foreach(var ship in facts.Ships.Where(s=>s.FleetId==fleet.Fleet.FleetId).OrderBy(s=>s.ShipId.Value,StringComparer.Ordinal))
                {
                    var sid=ship.ShipId.Value;var condition=frame.OwnShips.FirstOrDefault(s=>s.Condition.ShipId==sid);
                    var design=facts.Construction.FirstOrDefault(c=>c.Design.DefinitionId==ship.ShipDesignDefinitionId)?.Design.DisplayName??"Owned ship";
                    var row=new VisualElement();row.AddToClassList("console-queue");card.Add(row);
                    var pick=new Toggle(design+" · "+sid){value=model.OrganizationShips.Contains(sid),name="fleet-member-"+sid};
                    pick.SetEnabled(editable&&fleet.CanOrganize);row.Add(pick);
                    pick.RegisterValueChangedCallback(e=>perform(()=>{if(e.newValue)model.OrganizationShips.Add(sid);else model.OrganizationShips.Remove(sid);}));
                    if(condition!=null)
                    {
                        Text(row,"Hull "+condition.Condition.Hull+" / "+condition.Condition.MaxHull);
                        Text(row,"Shields Fore / Port / Starboard / Aft: "+string.Join(" / ",condition.Condition.Shields));
                        foreach(var weapon in condition.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))
                            Text(row,"Torpedoes "+weapon.Ammunition+" / "+weapon.MagazineCapacity);
                    }
                    var inspect=button(row,"Inspect ship",()=>perform(()=>workspace.SelectedOwnShip=sid));inspect.name="fleet-inspect-"+sid;
                    if(workspace.SelectedOwnShip==sid)row.Add(new ReviewShipArtPanel(facts.Civilization.DefinitionId.Value,ship.ShipDesignDefinitionId.Value));
                }
            }
            if(ordered.Count==0)Text(main,"No surviving owned fleets. Supported ships can still be built at eligible shipyards.");
            Text(aside,"Organize fleet","console-section");
            Text(aside,"Choose ships from owned fleets in the same location. Clear routes and use Remain before organizing. No damage, ammunition or hardware is reset.");
            Text(aside,model.OrganizationShips.Count+" selected members · unsent changes");
            var name=new TextField("Fleet name"){value=model.OrganizationName,isDelayed=true,name="fleet-organization-name"};
            name.AddToClassList("review-input");
            name.SetEnabled(editable);aside.Add(name);name.RegisterValueChangedCallback(e=>perform(()=>model.OrganizationName=e.newValue));
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
            var create=button(aside,"Review new fleet",()=>Review(CampaignFleetOrganizationKind.Create,null));create.name="fleet-create";
            create.SetEnabled(editable&&model.OrganizationShips.Count>0&&!string.IsNullOrWhiteSpace(model.OrganizationName));
            var selected=fleets.FirstOrDefault(f=>f.Fleet.FleetId.Value==workspace.SelectedFleet);
            if(selected!=null)
            {
                var rename=button(aside,"Review rename of "+selected.DisplayName,()=>Review(CampaignFleetOrganizationKind.Rename,selected.Fleet.FleetId.Value));
                rename.name="fleet-rename";rename.SetEnabled(editable&&selected.CanOrganize&&!string.IsNullOrWhiteSpace(model.OrganizationName));
                Text(aside,"Selected fleet: "+selected.DisplayName,"console-section");
                button(aside,"Locate on map",()=>{workspace.SelectedNode=selected.Fleet.LocationNodeId.Value;navigate(ReviewWorkspacePage.Map);});
                var stay=button(aside,"Remain / clear accepted travel",()=>confirm("Replace this fleet's route and intent with Remain? This does not move or repair ships.",()=>perform(()=>{
                    if(model.DraftTravel(selected.Fleet.FleetId.Value,CampaignTravelIntent.Stay,null,null))model.SendTravel(ReviewUiModel.NewCommandId());
                })));stay.name="fleet-clear-route";stay.SetEnabled(editable&&!model.HasLocalEdits);
                var setting=frame.CloakSettings.FirstOrDefault(s=>s.FleetId==selected.Fleet.FleetId.Value);
                if(setting!=null)
                    button(aside,"Approach cloak: "+(setting.Enabled?"enabled":"disabled"),()=>confirm("Change the selected fleet's approach cloak setting? Only explicitly capable ships can cloak.",()=>perform(()=>model.SetCloak(selected.Fleet.FleetId.Value,!setting.Enabled,ReviewUiModel.NewCommandId()))))
                        .SetEnabled(editable&&!model.HasLocalEdits);
            }
            Text(aside,"Transfer selected members to:","console-section");
            foreach(var destination in ordered)
            {
                var transfer=button(aside,destination.DisplayName+" · "+Location(destination),()=>Review(CampaignFleetOrganizationKind.Transfer,destination.Fleet.FleetId.Value));
                transfer.name="fleet-transfer-"+destination.Fleet.FleetId;transfer.SetEnabled(editable&&destination.CanOrganize&&model.OrganizationShips.Count>0);
            }
            button(aside,"Discard organization draft",()=>perform(model.ResetOrganization)).SetEnabled(model.OrganizationDirty);
            button(aside,"Build existing ships",()=>navigate(ReviewWorkspacePage.Production));
        }
    }
}
