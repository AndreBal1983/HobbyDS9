#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public enum ReviewCombatStage { Planning, SelectEncounter, Orders, Waiting, RoundResult, AftermathPending, AftermathConfirmed, NoOwnedFleet, Closed, CampaignComplete }

    // Presentation state only. Every mutation still passes the actual host/controller validation.
    public sealed class ReviewCombatFlow
    {
        public ReviewUiModel Model { get; }
        public string Notice { get; private set; }="";
        private ReviewController context;
        private readonly Dictionary<string,long> reviewedResults=new Dictionary<string,long>(StringComparer.Ordinal);
        private readonly HashSet<string> ownLosses=new HashSet<string>(StringComparer.Ordinal);
        private bool aftermathConfirmed;
        private ReviewAftermathSummary? aftermathSummary;
        public ReviewAftermathSummary? AftermathSummary { get {Context();return aftermathSummary;} }
        public bool SurfaceOpen { get; private set; }=true;
        public ReviewCombatFlow(ReviewUiModel model){Model=model;context=model.Current;}
        private void Context()
        {
            if(!ReferenceEquals(context,Model.Current))
            {context=Model.Current;reviewedResults.Clear();ownLosses.Clear();aftermathConfirmed=false;aftermathSummary=null;SurfaceOpen=true;}
            if(aftermathSummary!=null&&!aftermathSummary.Applies(Model.Current.Frame,Model.Closed))
            {aftermathSummary=null;aftermathConfirmed=false;}
        }
        public ReviewCombatStage Stage
        {
            get
            {
                Context();if(Model.Closed)return ReviewCombatStage.Closed;
                var frame=Model.Current.Frame;var battle=frame.Battle;
                if(CampaignComplete)return ReviewCombatStage.CampaignComplete;
                if(battle!=null)
                {
                    if(battle.Complete)
                    {
                        foreach(var ship in battle.OwnShips.Where(s=>s.Condition.Hull==0))ownLosses.Add(ship.Condition.ShipId);
                        return ReviewCombatStage.AftermathPending;
                    }
                    if(battle.OwnCommitmentAccepted)return ReviewCombatStage.Waiting;
                    if(battle.Reports.Count>0&&(!reviewedResults.TryGetValue(battle.EncounterId,out var boundary)||boundary!=battle.BoundaryRevision))
                        return ReviewCombatStage.RoundResult;
                    return ReviewCombatStage.Orders;
                }
                if(frame.Status.Phase!=SessionPhase.Planning)return ReviewCombatStage.SelectEncounter;
                if(frame.OwnStrategicFleets.Count==0)return ReviewCombatStage.NoOwnedFleet;
                return aftermathConfirmed?ReviewCombatStage.AftermathConfirmed:ReviewCombatStage.Planning;
            }
        }
        public bool CanCommitRound=>Stage==ReviewCombatStage.Orders&&!Model.Busy&&!Model.Current.HasPendingWork&&!Model.Current.RefreshRequired;
        public bool CanFinalize=>Stage==ReviewCombatStage.AftermathPending&&Model.Current.Frame.Status.CanFinalizeBatch&&!Model.Busy&&!Model.Current.HasPendingWork&&!Model.Current.RefreshRequired;
        public bool CampaignComplete=>Model.Current.Frame.Demo!=null&&Model.Current.Frame.Demo.Outcome!=CampaignDemoOutcome.Active;
        public bool CanDraftNextOrder=>!CampaignComplete&&Model.Current.Frame.Status.Phase==SessionPhase.Planning&&Model.Current.Frame.OwnStrategicFleets.Count>0&&!Model.Busy&&!Model.Current.HasPendingWork&&!Model.Current.RefreshRequired&&!Model.Closed&&Model.Current.Mode==ReviewMode.Planning;
        public IReadOnlyList<string> OwnLosses { get {Context();return ownLosses.OrderBy(s=>s,StringComparer.Ordinal).ToList().AsReadOnly();} }
        public void Open(){Context();SurfaceOpen=true;}
        public void Dismiss(){SurfaceOpen=false;} // No command, discard or time advance.
        public bool PlanNextRound()
        {
            if(Stage!=ReviewCombatStage.RoundResult)return false;
            var b=Model.Current.Frame.Battle!;reviewedResults[b.EncounterId]=b.BoundaryRevision;return true;
        }
        private bool Guard(bool allowed,string reason,Func<bool> action)
        {Notice=allowed?"":reason;return allowed&&action();}
        public bool DraftFleet(string fleet,CampaignFleetManeuver maneuver,string? focus,bool retainFocus=true)
            =>Guard(CanCommitRound,"Fleet edits unavailable: review the result, wait for resolution or refresh authority.",()=>Model.DraftFleet(fleet,maneuver,focus,retainFocus));
        public bool CommitRound(string command,bool confirmed)
            =>Guard(confirmed&&CanCommitRound,"Round not committed: confirmation/current editable boundary required.",()=>Model.CommitRound(command));
        public bool FinalizeAftermath(bool confirmed)
        {
            if(!confirmed||!CanFinalize){Notice="Aftermath not applied: confirmation and whole-batch readiness required.";return false;}
            Notice="";
            var controller=Model.Current;var completed=controller.Frame;
            var success=Model.FinalizeBatch();
            if(success)
            {
                aftermathSummary=ReferenceEquals(controller,Model.Current)&&!Model.Current.RefreshRequired
                    ?ReviewAftermathSummary.Confirmed(completed,Model.Current.Frame):null;
                aftermathConfirmed=aftermathSummary!=null;reviewedResults.Clear();SurfaceOpen=true;
            }
            return success;
        }
        public KnownRouteOffer? PreviewRoute(string fleet,string destination)
            =>CanDraftNextOrder?Model.Current.FindKnownRoute(fleet,destination):null;
        public bool DraftNextOrder(string fleet,CampaignTravelIntent intent,string? destination=null,string? contact=null)
            =>Guard(CanDraftNextOrder,"Fresh strategic orders are unavailable until authoritative aftermath completion / Planning.",()=>Model.DraftTravel(fleet,intent,destination,contact));
        public bool SubmitNextOrder(string command,bool confirmed)
            =>Guard(confirmed&&CanDraftNextOrder&&Model.Current.TravelDraft!=null,"Next-turn order not submitted: a confirmed current Planning draft is required.",()=>Model.SendTravel(command));
        public bool AdvancePlanning(bool confirmed)
        {
            if(!confirmed||!CanDraftNextOrder||Model.HasLocalEdits){Notice="Advance unavailable: confirm accepted Planning orders; submit or explicitly discard the draft first.";return false;}
            Notice="";
            var success=Model.Advance();if(success){aftermathConfirmed=false;ownLosses.Clear();SurfaceOpen=true;}return success;
        }
        public string Heading
        {
            get
            {
                switch(Stage)
                {
                    case ReviewCombatStage.Orders:return "Battle — plan fleet orders";
                    case ReviewCombatStage.Waiting:return "Own orders accepted — waiting for authoritative resolution";
                    case ReviewCombatStage.RoundResult:return "Round result — review before planning the next round";
                    case ReviewCombatStage.AftermathPending:return "Battle complete — aftermath pending campaign confirmation";
                    case ReviewCombatStage.AftermathConfirmed:return "Aftermath confirmed — choose a fresh strategic order";
                    case ReviewCombatStage.NoOwnedFleet:return "No owned surviving fleet available for orders";
                    case ReviewCombatStage.SelectEncounter:return "Select an owned encounter";
                    case ReviewCombatStage.Closed:return Model.Current.Frame.Demo!=null?"Campaign closed":"Review closed";
                    case ReviewCombatStage.CampaignComplete:return "Campaign complete — "+Model.Current.Frame.Demo!.Outcome+" · inspection";
                    default:return Model.Current.Frame.Demo!=null?"Combat — strategic Planning":"Physical tactical-v2 review — strategic Planning";
                }
            }
        }
        public string OrderReview()
        {
            var b=Model.Current.Frame.Battle;var text=new StringBuilder("YOUR ORDERS ONLY — focus is a preference, targeting is automatic\n");
            if(b==null)return text.ToString();
            foreach(var order in b.StandingOrders)
            {
                var edit=Model.Current.Draft.FirstOrDefault(o=>o.Fleet==order.FleetId);
                text.AppendLine(order.FleetId+": "+(edit==null?"Retained "+order.Maneuver:"UNSENT "+edit.Maneuver)+" | focus "+(edit==null?order.FocusFleetId:edit.Focus));
            }
            return text.ToString();
        }
        public string Conditions()
        {
            var text=new StringBuilder();var frame=Model.Current.Frame;
            text.AppendLine(frame.Status.Phase==SessionPhase.Planning?"AUTHORITATIVE CURRENT CAMPAIGN STOCKS":"BATTLE STOCKS — staged until campaign confirmation");
            foreach(var own in frame.OwnShips)
            {
                var c=own.Condition;text.AppendLine(FormattableString.Invariant($"{c.ShipId} ({c.Type}) | Hull {c.Hull}/{c.MaxHull}"));
                text.AppendLine("Current cloak: "+own.Cloaked+" | approach setting: "+(frame.CloakSettings.FirstOrDefault(s=>s.FleetId==c.FleetId)?.Enabled.ToString()??"unavailable"));
                for(int i=0;i<c.Shields.Count;i++)text.Append(FormattableString.Invariant($"{(TacticalSector)i} {c.Shields[i]}/{own.ShieldMaximum[i]}  "));
                text.AppendLine();
                foreach(var w in own.Weapons)
                    text.AppendLine(w.Class==TacticalWeaponClass.Torpedo?FormattableString.Invariant($"{w.WeaponId}: ammunition {w.Ammunition}/{w.MagazineCapacity}"):w.WeaponId+": energy weapon — no physical ammunition");
            }
            foreach(var loss in OwnLosses)text.AppendLine("Own loss reported this session: "+loss);
            if(frame.OwnShips.Count==0)text.AppendLine("No owned ships in this observation.");
            text.AppendLine("No between-battle recovery or refill. Historical loss summary is session-local; saves restore authoritative current stocks.");
            return text.ToString();
        }
        public string StageDiagram()
        {
            var text=new StringBuilder("SCHEMATIC STAGING — no steering or authoritative 3D geometry\n");
            var b=Model.Current.Frame.Battle;
            if(b==null)return text.AppendLine(CampaignComplete?"Completed campaign boundary; retained ships and stocks are available for inspection.":Model.Current.Frame.Status.Phase==SessionPhase.Planning?"Current campaign boundary; fresh orders use known routes and contacts.":"Select an owned encounter. No hidden participants or identities are inferred.").ToString();
            text.AppendLine(FormattableString.Invariant($"Sector {b.LocationNodeId} | {(b.Complete?"Completed after up to 3 rounds":"Round "+b.Round+" / up to 3")}"));
            foreach(var relation in b.Relations)text.AppendLine(relation.OwnFleetId+" → "+relation.VisibleEnemyFleetId+": "+relation.Range+" | own facing "+relation.OwnFacing);
            if(b.VisibleEnemies.Count==0)text.AppendLine("No visible enemy. Proceed cautiously; hidden identities/counts are unavailable.");
            foreach(var enemy in b.VisibleEnemies)
                text.AppendLine(FormattableString.Invariant($"Visible {enemy.ShipId} ({enemy.Type}) | Hull {enemy.Hull}/{enemy.MaxHull} | shields ")+string.Join(" / ",enemy.Shields));
            text.AppendLine(Model.Current.Frame.Demo!=null?"Only observed enemy condition is shown. Enemy ammunition and upcoming orders remain hidden.":"Enemy condition precision is the proposed active review policy. Enemy stocks, readiness and fresh orders are not disclosed.");
            return text.ToString();
        }
    }
}
