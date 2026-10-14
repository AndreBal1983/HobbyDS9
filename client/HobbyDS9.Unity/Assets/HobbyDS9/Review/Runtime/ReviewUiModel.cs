#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    // Portable actual-code UI host model. Authoritative values and commands come from the existing client.
    public sealed class ReviewUiModel
    {
        private readonly CampaignTacticalTravelPackageDto package;
        private readonly ReviewNewtonsoftCodec codec;
        private readonly ReviewUnitySlotStore slots;
        private PhysicalReviewHost host;
        public ReviewController Current { get; private set; }
        public bool Busy { get; private set; }
        public bool Closed { get; private set; }
        public string Message { get; private set; }="";
        public ReviewUiModel(CampaignTacticalTravelPackageDto package,ReviewNewtonsoftCodec codec,ReviewUnitySlotStore slots,string actor)
        {
            this.package=codec.CopyPackage(package); this.codec=codec; this.slots=slots;
            host=PhysicalReviewHost.Start(this.package,codec,slots); Current=Controller(host,actor);
        }
        private static ReviewController Controller(PhysicalReviewHost host,string actor)
        { var c=new ReviewController(host,actor); if(c.Frame.Encounters.Count==1)c.SelectEncounter(c.Frame.Encounters[0]); return c; }
        public static string NewCommandId() => "command.review."+Guid.NewGuid().ToString("N");
        private bool Run(Func<bool> action)
        {
            if(Busy || Closed) { Message="Review operation unavailable while busy or closed."; return false; }
            Busy=true;
            try { return action(); }
            catch(Exception) { Message="Review operation unavailable; private diagnostics withheld."; return false; }
            finally { Busy=false; }
        }
        private bool Dispatch(ReviewWork? work)
        {
            if(work==null){Message="Command unavailable in the current observation.";return false;}
            var receipt=host.Execute(work); var received=Current.Receive(receipt);
            Message="Authoritative result: "+receipt.Result.Error;
            return received&&receipt.Result.Accepted;
        }
        public bool SwitchActor(string actor,bool discard=false)=>Run(()=>{
            if(Current.HasPendingWork || Current.Dirty&&!discard){Message="Explicit discard is required before switching actor.";return false;}
            var next=Controller(host,actor); Current=next; Message="Actor view changed; inspection and local edits cleared."; return true;
        });
        public bool SelectEncounter(string id,bool discard=false)=>Run(()=>{
            var result=Current.SelectEncounter(id,discard);Message=result?"Encounter selected.":"Encounter unavailable or explicit discard required.";return result;
        });
        public bool Refresh()=>Run(()=>{var r=Current.Refresh();Message=r?"Authoritative view refreshed.":"Refresh unavailable.";return r;});
        public bool Inspect(string ship)=>Run(()=>{var r=Current.InspectShip(ship);Message=r?"Lawful ship inspection selected.":"No lawful ship observation.";return r;});
        public bool DraftTravel(string fleet,CampaignTravelIntent intent,string? destination,string? contact)=>Run(()=>{
            var offer=string.IsNullOrEmpty(destination)?null:Current.FindKnownRoute(fleet,destination!);
            if(!string.IsNullOrEmpty(destination)&&offer==null){Message="Known route unavailable.";return false;}
            var r=Current.EditTravel(fleet,intent,offer,contact);Message=r?"Local travel draft; no command submitted.":"Travel input unavailable; backend rules remain authoritative.";return r;
        });
        public bool SendTravel(string command)=>Run(()=>Current.ReviewTravel()?Dispatch(Current.ConfirmTravel(command)):Unavailable());
        public bool DraftFleet(string fleet,CampaignFleetManeuver maneuver,string? focus,bool retainFocus=false)=>Run(()=>{
            if(retainFocus)
            {
                var standing=Current.Frame.Battle?.StandingOrders.FirstOrDefault(o=>o.FleetId==fleet);
                if(standing==null)return Unavailable();
                var draft=Current.Draft.FirstOrDefault(o=>o.Fleet==fleet);
                focus=draft==null?standing.FocusFleetId:draft.Focus;
            }
            var r=Current.EditFleet(fleet,maneuver,focus);
            Message=r?"Local fleet draft; no command submitted.":retainFocus?
                "This observation cannot edit that retained focus. Keep standing orders or explicitly choose a visible preference / clear focus.":"Fleet input unavailable.";
            return r;
        });
        public bool CommitRound(string command)=>Run(()=>Current.ReviewRound()?Dispatch(Current.ConfirmRound(command)):Unavailable());
        private bool Unavailable(){Message="Operation unavailable in the current authoritative boundary.";return false;}
        public bool Advance()=>Run(()=>{
            if(Current.Dirty||Current.HasPendingWork){Message="Submit or explicitly reset local edits before advance.";return false;}
            var r=host.AdvancePlanning(Current.Frame);Message="Authoritative result: "+r.Error;
            if(!r.Accepted)return false; Current.Refresh();
            if(Current.Frame.Encounters.Count==1)Current.SelectEncounter(Current.Frame.Encounters[0]);return true;
        });
        public bool FinalizeBatch()=>Run(()=>Current.ReviewCompletion()?Dispatch(Current.ConfirmCompletion()):Unavailable());
        public bool SetCloak(string fleet,bool enabled,string command)=>Run(()=>{
            if(Current.Dirty||Current.HasPendingWork)return Unavailable();
            var r=host.SetApproachCloak(Current.Frame,command,fleet,enabled);Message="Authoritative result: "+r.Error;
            if(r.Accepted)Current.Refresh();return r.Accepted;
        });
        public bool Save(string slot,bool overwrite=false)=>Run(()=>{
            var r=Current.SaveCommittedToSlot(slot,overwrite);Message="Checkpoint result: "+r.Error;return r.Succeeded;
        });
        public bool Load(string slot,bool discard=false)=>Run(()=>{
            if(Current.HasPendingWork||Current.Dirty&&!discard){Message="Explicit discard required before load.";return false;}
            var opened=PhysicalReviewHost.OpenFromSlot(package,codec,slots,slot,Current.Frame.Actor,Current.Frame.Battle?.EncounterId);
            if(!opened.Succeeded){Message="Checkpoint result: "+opened.Error;return false;}
            var next=Controller(opened.Host!,Current.Frame.Actor);host=opened.Host!;Current=next;Message="Validated complete-root checkpoint loaded.";return true;
        });
        public bool Reset()=>Run(()=>{
            if(Current.HasPendingWork)return Unavailable();
            var r=Current.TravelDraft!=null?Current.ResetTravelDraft():!Current.Dirty||Current.ResetDraft();
            Message=r?"Local edits cleared; no command submitted.":"Reset unavailable.";return r;
        });
        public bool Close(bool discard=false)=>Run(()=>{
            if(Current.HasPendingWork||Current.Dirty&&!discard){Message="Confirm discard before exit.";return false;}
            Closed=true;Message="Review closed. Restart without the explicit review flag for default v1.";return true;
        });
        public string Facts()=>Closed?"Review closed.":ReviewTextRenderer.Render(Current);
        public string Report()=>Closed?"Review closed.":ReviewTextRenderer.RenderReport(Current);
        public string Inspection()=>Current.SelectedShip==null?"Select an owned or visible ship.":ReviewTextRenderer.RenderInspection(Current,Current.SelectedShip);
    }
}
