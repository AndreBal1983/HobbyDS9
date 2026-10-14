#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Actual UIElements view. All callbacks use Entry's epoch/modal guard; painting submits nothing.
    public sealed class ReviewCentralView
    {
        private readonly ReviewCombatFlow flow;
        private readonly Func<VisualElement,string,Action,Button> button;
        private readonly Action<Action> perform;
        private readonly Action<string,Action> confirm;
        private VisualElement root;
        private string selectedFleet="",selectedShip="",tab="stocks",destination="nav.bajor",contact="none",focus="retain standing focus";
        private CampaignTravelIntent travel=CampaignTravelIntent.StayEngage;
        private CampaignFleetManeuver maneuver=CampaignFleetManeuver.Hold;
        private string actor="";
        private bool selectedTravelIsDraft;
        public ReviewCentralView(VisualElement root,ReviewCombatFlow flow,Func<VisualElement,string,Action,Button> button,Action<Action> perform,Action<string,Action> confirm)
        {
            this.root=root;this.flow=flow;this.button=button;this.perform=perform;this.confirm=confirm;root.AddToClassList("review-central");
            actor=flow.Model.Current.Frame.Actor;
            var draft=flow.Model.Current.TravelDraft;
            if(draft!=null){selectedFleet=draft.Fleet;travel=draft.Intent;contact=draft.ContactFleet??"none";destination=draft.Route.LastOrDefault()??"";selectedTravelIsDraft=true;}
        }
        // Keep pending field selections when shell navigation replaces the view tree.
        // Entry creates a new view instead when actor/controller/load context changes.
        public void Bind(VisualElement target){root=target;root.AddToClassList("review-central");}
        public void Render()
        {
            root.Clear();var model=flow.Model;var c=model.Current;var frame=c.Frame;var stage=flow.Stage;var battle=frame.Battle;
            if(actor!=frame.Actor){actor=frame.Actor;selectedFleet="";selectedShip="";focus="retain standing focus";tab="stocks";}
            var fleets=(battle!=null?battle.StandingOrders.Select(o=>o.FleetId):frame.OwnStrategicFleets.Select(f=>f.FleetId.Value)).ToList();
            if(!fleets.Contains(selectedFleet)){selectedFleet=fleets.FirstOrDefault()??"";SyncFleetOrder();}
            var title=Text(flow.Heading);title.name="combat-stage-heading";title.AddToClassList("review-heading");root.Add(title);
            if(stage==ReviewCombatStage.Closed){root.Add(Text("Review closed. No further commands are available."));return;}
            if(!flow.SurfaceOpen&&battle!=null)
            {
                root.Add(Text("Battle checkpoint retained. Unsent draft preserved; returning does not submit or advance time."));
                button(root,"Resume battle",()=>perform(()=>flow.Open()));return;
            }
            if(battle==null&&frame.Status.Phase==SessionPhase.Planning)
                root.Add(Text("OPT-IN PHYSICAL TACTICAL V2. To enter battle: draft Re-engage, review/submit, then advance the strategic turn. Each human actor supplies their own round choice through the labelled test-host controls. No opponent AI is invented."));
            var stageRow=new VisualElement{name="central-combat-surface"};stageRow.AddToClassList("review-stage-row");root.Add(stageRow);
            var roster=new ScrollView(ScrollViewMode.Vertical){name="central-fleet-rail"};roster.AddToClassList("review-panel");roster.AddToClassList("review-roster");roster.Add(Text("Owned fleet / ship inspection"));stageRow.Add(roster);
            var fleet=Choice("Owned fleet",fleets);fleet.SetValueWithoutNotify(string.IsNullOrEmpty(selectedFleet)?"none":selectedFleet);
            fleet.RegisterValueChangedCallback(e=>{selectedFleet=e.newValue;SyncFleetOrder();Render();});roster.Add(fleet);
            var ships=frame.OwnShips.Select(s=>s.Condition.ShipId).Concat(battle?.VisibleEnemies.Select(s=>s.ShipId)??Array.Empty<string>()).ToList();
            if(!ships.Contains(selectedShip))selectedShip=ships.FirstOrDefault()??"";
            var ship=Choice("Owned or visible ship",ships);ship.SetValueWithoutNotify(string.IsNullOrEmpty(selectedShip)?"none":selectedShip);
            ship.RegisterValueChangedCallback(e=>selectedShip=e.newValue);roster.Add(ship);
            button(roster,"Inspect selected ship",()=>perform(()=>{model.Inspect(selectedShip);tab="inspection";})).SetEnabled(ships.Count>0);
            if(frame.Encounters.Count>0)
            {
                var encounters=Choice("Your encounters",frame.Encounters.ToList());encounters.name="owned-encounter-picker";if(battle!=null)encounters.SetValueWithoutNotify(battle.EncounterId);roster.Add(encounters);
                button(roster,"Open selected encounter",()=>confirm("Change encounter? Explicitly discard unsent edits if present.",()=>perform(()=>model.SelectEncounter(encounters.value,true))));
                roster.Add(Text("Other human orders, hidden ships/counts and enemy ammunition are unavailable."));
            }
            var viewer=Panel(stageRow,"Central battle / survivor view");viewer.AddToClassList("review-viewer");
            var stageScroll=new ScrollView(ScrollViewMode.Vertical);stageScroll.AddToClassList("review-schematic-scroll");viewer.Add(stageScroll);
            var schematic=Text(flow.StageDiagram());schematic.name="combat-schematic";stageScroll.Add(schematic);
            var tabs=new VisualElement();tabs.AddToClassList("review-tabs");viewer.Add(tabs);
            foreach(var name in new[]{"stocks","report","inspection"})button(tabs,name=="stocks"?"Own hull / shields / ammunition":name=="report"?"Round report":"Inspection",()=>{tab=name;Render();});
            var details=new ScrollView(ScrollViewMode.Vertical){name="central-combat-details"};details.AddToClassList("review-detail-scroll");viewer.Add(details);
            details.Add(Text(tab=="report"?model.Report():tab=="inspection"?model.Inspection():flow.Conditions()));
            if(stage==ReviewCombatStage.Waiting)
            {
                viewer.Add(Text("YOUR ACCEPTED RECEIPT: "+battle!.OwnAcceptedCommandId));
                foreach(var order in battle.OwnCommittedOrders)viewer.Add(Text(order.FleetId+": "+order.Maneuver+" | retained focus "+order.FocusFleetId));
                viewer.Add(Text("Waiting for authoritative resolution. No opposing commitment count or readiness is disclosed."));
            }
            if(stage==ReviewCombatStage.AftermathPending&&!frame.Status.CanFinalizeBatch)
                viewer.Add(Text("This battle is complete. Other battles are not ready; campaign aftermath cannot be finalized yet. Select another owned encounter when available."));
            var dock=new ScrollView(ScrollViewMode.Vertical){name="central-order-dock"};dock.AddToClassList("review-order-dock");stageRow.Add(dock);
            if(battle!=null)
            {
                dock.Add(Text(flow.OrderReview()));
                var row=new VisualElement();row.AddToClassList("review-field-row");dock.Add(row);
                var maneuvers=battle.LegalManeuvers.Select(m=>m.ToString()).ToList();var move=Choice("Fleet maneuver",maneuvers);
                if(maneuvers.Contains(maneuver.ToString()))move.SetValueWithoutNotify(maneuver.ToString());
                move.RegisterValueChangedCallback(e=>{if(Enum.TryParse(e.newValue,out CampaignFleetManeuver m))maneuver=m;});row.Add(move);
                var targets=new[]{"retain standing focus","clear focus"}.Concat(battle.LegalFocusFleetIds).ToList();var target=Choice("Enemy-fleet preference",targets);
                if(targets.Contains(focus))target.SetValueWithoutNotify(focus);target.RegisterValueChangedCallback(e=>focus=e.newValue);row.Add(target);
                button(row,"Apply to local draft",()=>perform(()=>flow.DraftFleet(selectedFleet,maneuver,focus=="retain standing focus"||focus=="clear focus"?null:focus,focus=="retain standing focus"))).SetEnabled(flow.CanCommitRound&&battle.StandingOrders.Any(o=>o.FleetId==selectedFleet&&o.CanOrder));
                dock.Add(Text("Same-fleet screening only. Focus is a preference; ships choose lawful targets automatically. Approach cloak is read-only battle context."));
            }
            else if(frame.Status.Phase==SessionPhase.Planning&&fleets.Count>0)
            {
                dock.Add(Text("FRESH ORDER FOR CURRENT PLANNING TURN "+frame.Status.Turn+" — selecting or previewing does not submit."));
                var choices=new VisualElement();choices.AddToClassList("review-field-row");dock.Add(choices);
                foreach(var pair in new[]{(CampaignTravelIntent.Stay,"Remain"),(CampaignTravelIntent.StayEngage,"Re-engage"),(CampaignTravelIntent.Leave,"Move away"),(CampaignTravelIntent.Follow,"Follow known contact")})
                    button(choices,pair.Item2,()=>{travel=pair.Item1;selectedTravelIsDraft=false;Render();}).SetEnabled(flow.CanDraftNextOrder&&fleets.Count>0&&(pair.Item1!=CampaignTravelIntent.Follow||(frame.Travel?.KnownContacts.Any(k=>k.Fresh)??false)));
                dock.Add(Text("Selected: "+TravelLabel(travel)+". Move away is strategic travel, not a guaranteed tactical escape. Follow has no catch roll or promised interception."));
                var cloak=frame.CloakSettings.FirstOrDefault(s=>s.FleetId==selectedFleet);
                if(cloak!=null)
                {
                    dock.Add(Text("Accepted approach cloak setting: "+cloak.Enabled+". Only authoritative capable members cloak; no stock changes."));
                    button(dock,cloak.Enabled?"Disable approach cloak":"Enable approach cloak",()=>perform(()=>model.SetCloak(selectedFleet,!cloak.Enabled,ReviewUiModel.NewCommandId()))).SetEnabled(flow.CanDraftNextOrder&&!c.Dirty&&cloak.CanChange);
                }
                if(!(frame.Travel?.KnownContacts.Any(k=>k.Fresh)??false))dock.Add(Text("Follow unavailable: no fresh lawful contact. No current enemy state is inferred."));
                var fields=new VisualElement();fields.AddToClassList("review-field-row");dock.Add(fields);
                var dest=Field("Explicit known destination",destination);dest.RegisterValueChangedCallback(e=>{destination=e.newValue;selectedTravelIsDraft=false;});fields.Add(dest);
                var known=new[]{"none"}.Concat(frame.Travel?.KnownContacts.Where(k=>k.Fresh).Select(k=>k.FleetId)??Array.Empty<string>()).ToList();
                var contacts=Choice("Fresh known contact",known);if(known.Contains(contact))contacts.SetValueWithoutNotify(contact);else contact="none";
                contacts.RegisterValueChangedCallback(e=>{contact=e.newValue;selectedTravelIsDraft=false;});fields.Add(contacts);
                button(fields,"Preview known route",()=>perform(()=>{
                    var offer=flow.PreviewRoute(selectedFleet,destination);
                    routeText=offer==null?"Known route unavailable.":offer.Route.Success?"Known route: "+string.Join(" → ",offer.Route.Steps)+" | authoritative cost "+offer.Route.TotalCost:"Route unavailable: "+offer.Route.ErrorCode;
                })).SetEnabled(flow.CanDraftNextOrder&&fleets.Count>0);
                dock.Add(Text(routeText));
                button(dock,"Draft selected next-turn order",()=>perform(()=>selectedTravelIsDraft=flow.DraftNextOrder(selectedFleet,travel,travel==CampaignTravelIntent.Leave||travel==CampaignTravelIntent.Follow?destination:null,travel==CampaignTravelIntent.Follow&&contact!="none"?contact:null))).SetEnabled(flow.CanDraftNextOrder&&fleets.Count>0);
                var accepted=frame.Travel?.OwnOrders.FirstOrDefault(o=>o.FleetId==selectedFleet);
                dock.Add(Text("ACCEPTED order: "+(accepted==null?"unavailable":TravelLabel(accepted.Intent)+" | route "+string.Join(" → ",accepted.RouteNodeIds))));
                var draft=c.TravelDraft;dock.Add(Text(draft==null?"No unsent replacement.":"UNSENT draft: "+TravelLabel(draft.Intent)+" | route "+string.Join(" → ",draft.Route)));
                foreach(var k in frame.Travel?.KnownContacts??Array.Empty<CampaignKnownContact>())dock.Add(Text("LAST SEEN "+k.FleetId+": "+k.LastSeenNodeId+", turn "+k.LastSeenTurn+"; fresh "+k.Fresh+". Current enemy position/destination unavailable."));
                foreach(var r in frame.Travel?.OwnReceipts??Array.Empty<CampaignOwnTravelReceipt>())dock.Add(Text("Owned movement "+r.FleetId+": end "+r.EndNodeId+" | progress "+r.TransitProgress+"/"+r.TransitTotalCost+" | stopped by encounter "+r.StoppedByEncounter));
            }
            else dock.Add(Text(stage==ReviewCombatStage.NoOwnedFleet?"No owned surviving fleet can receive a strategic order. Current campaign facts remain inspectable.":"Choose an owned encounter to review its battle. Fresh strategic orders wait for complete-batch aftermath."));
            var footer=new VisualElement{name="central-combat-footer"};footer.AddToClassList("review-footer");root.Add(footer);
            if(battle!=null)button(footer,"Back / keep draft",()=>{flow.Dismiss();Render();});
            if(stage==ReviewCombatStage.Orders)button(footer,"Review / commit round "+battle!.Round,()=>confirm(flow.OrderReview()+"\nCommit one round for this human actor?",()=>perform(()=>flow.CommitRound(ReviewUiModel.NewCommandId(),true)))).SetEnabled(flow.CanCommitRound);
            else if(stage==ReviewCombatStage.RoundResult)button(footer,"Plan next round",()=>perform(()=>flow.PlanNextRound()));
            else if(stage==ReviewCombatStage.AftermathPending)button(footer,"Confirm campaign aftermath",()=>confirm("Apply complete-batch aftermath exactly once? Stocks are retained; no refill or extra movement.",()=>perform(()=>flow.FinalizeAftermath(true)))).SetEnabled(flow.CanFinalize);
            else if(stage==ReviewCombatStage.Waiting)footer.Add(Text("Waiting — no duplicate commitment"));
            else if(flow.CanDraftNextOrder)
            {
                button(footer,"Review / submit next-turn draft",()=>confirm("Submit this UNSENT next-turn order?\n"+model.Facts(),()=>perform(()=>flow.SubmitNextOrder(ReviewUiModel.NewCommandId(),true)))).SetEnabled(c.TravelDraft!=null&&selectedTravelIsDraft);
                button(footer,"Advance strategic turn",()=>confirm("Advance once with ACCEPTED strategic orders? No unsubmitted draft will be applied.",()=>perform(()=>flow.AdvancePlanning(true)))).SetEnabled(!c.Dirty);
            }
        }
        private string routeText="Route costs and steps come from Application; no local ETA or pursuit calculation.";
        private void SyncFleetOrder()
        {var o=flow.Model.Current.Frame.Battle?.StandingOrders.FirstOrDefault(x=>x.FleetId==selectedFleet);var edit=flow.Model.Current.Draft.FirstOrDefault(x=>x.Fleet==selectedFleet);if(edit!=null)maneuver=edit.Maneuver;else if(o!=null)maneuver=o.Maneuver;focus="retain standing focus";selectedTravelIsDraft=false;routeText="Choose an explicit known route.";}
        private static VisualElement Panel(VisualElement parent,string title){var p=new VisualElement();p.AddToClassList("review-panel");p.Add(Text(title));parent.Add(p);return p;}
        private static Label Text(string value){var l=new Label(value);l.style.whiteSpace=WhiteSpace.Normal;return l;}
        private static TextField Field(string label,string value){var f=new TextField(label){value=value};f.AddToClassList("review-input");return f;}
        private static DropdownField Choice(string label,List<string> values){if(values.Count==0)values.Add("none");var f=new DropdownField(label,values,0);f.AddToClassList("review-input");return f;}
        private static string TravelLabel(CampaignTravelIntent intent)=>intent==CampaignTravelIntent.Stay?"Remain":intent==CampaignTravelIntent.StayEngage?"Re-engage":intent==CampaignTravelIntent.Leave?"Move away":"Follow known contact";
    }
}
