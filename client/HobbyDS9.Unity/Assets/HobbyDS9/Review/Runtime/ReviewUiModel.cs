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
        private readonly CampaignTacticalTravelPackageDto? package;
        private readonly CampaignIntegratedPlaytestPackageDto? integratedPackage;
        private readonly ReviewIntegratedSlotStore? integratedSlots;
        private readonly ReviewNewtonsoftCodec codec;
        private readonly ReviewUnitySlotStore? slots;
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
        public ReviewUiModel(CampaignIntegratedPlaytestPackageDto package,ReviewNewtonsoftCodec codec,ReviewIntegratedSlotStore slots,string actor)
        {
            integratedPackage=codec.CopyPackage(package);this.codec=codec;integratedSlots=slots;
            host=PhysicalReviewHost.StartIntegrated(integratedPackage,codec,slots);Current=Controller(host,actor);
        }
        public bool Integrated=>integratedPackage!=null;
        public ReviewResearchDraft? ResearchDraft { get; private set; }
        public bool HasLocalEdits=>Current.Dirty||(ResearchDraft?.Dirty??false);
        public ReviewResearchDraft? ResearchInputs()
        {
            if(Current.Frame.Strategy==null)return null;
            if(ResearchDraft==null||!ResearchDraft.Dirty&&!ResearchDraft.Current(Current.Frame))ResearchDraft=new ReviewResearchDraft(Current.Frame);
            return ResearchDraft;
        }
        public CommandEnvelope StrategicEnvelope(CommandKind kind,string command)=>new CommandEnvelope {
            CommandId=command,ActorCivilizationId=Current.Frame.Actor,ExpectedTurn=Current.Frame.Status.Turn,
            ExpectedRevision=Current.Frame.Status.CampaignRevision,Kind=kind };
        public bool Strategic(CommandEnvelope command)=>Run(()=>{
            if(!Integrated||Current.HasPendingWork||Current.Dirty||Current.RefreshRequired)return Unavailable();
            var result=host.SubmitStrategicCommand(Current.Frame,command);Message="Authoritative result: "+result.Error;
            if(!result.Accepted){Current.MarkStrategicFailure(result.Error);return false;}
            var refreshed=Current.Refresh();
            if(refreshed&&ResearchDraft!=null)ResearchDraft=ResearchDraft.Acknowledge(Current.Frame,command.Kind);
            return refreshed;
        });
        public bool AllocateResearch(int units,string command)
        {var c=StrategicEnvelope(CommandKind.SetResearchAllocation,command);c.SetResearchAllocation=new SetResearchAllocationPayload{ScienceAllocationUnits=units};return Strategic(c);}
        public bool AllocateScience(System.Collections.Generic.IReadOnlyList<ScienceAllocationDto> values,string command)
        {var c=StrategicEnvelope(CommandKind.SetScienceAllocation,command);c.SetScienceAllocation=new SetScienceAllocationPayload{Allocations=values.Select(v=>new ScienceAllocationDto{Discipline=v.Discipline,Weight=v.Weight}).ToList()};return Strategic(c);}
        public bool Engineering(string? target,string command)
        {var c=StrategicEnvelope(CommandKind.SetEngineeringTarget,command);c.SetEngineeringTarget=new SetEngineeringTargetPayload{TargetDefinitionId=target};return Strategic(c);}
        public bool Enqueue(string colony,string design,string command)
        {var c=StrategicEnvelope(CommandKind.EnqueueProduction,command);c.EnqueueProduction=new EnqueueProductionPayload{ColonyId=colony,DefinitionId=design};return Strategic(c);}
        public bool CancelProduction(string colony,string queue,string command)
        {var c=StrategicEnvelope(CommandKind.CancelProduction,command);c.CancelProduction=new CancelProductionPayload{ColonyId=colony,QueueItemId=queue};return Strategic(c);}
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
            if(Current.HasPendingWork || HasLocalEdits&&!discard){Message="Explicit discard is required before switching actor.";return false;}
            var next=Controller(host,actor); Current=next;ResearchDraft=null; Message="Actor view changed; inspection and local edits cleared."; return true;
        });
        public bool SelectEncounter(string id,bool discard=false)=>Run(()=>{
            var result=Current.SelectEncounter(id,discard);Message=result?"Encounter selected.":"Encounter unavailable or explicit discard required.";return result;
        });
        public bool Refresh()=>Run(()=>{var r=Current.Refresh();Message=r?"Authoritative view refreshed.":"Refresh unavailable.";return r;});
        public bool Inspect(string ship)=>Run(()=>{var r=Current.InspectShip(ship);Message=r?"Lawful ship inspection selected.":"No lawful ship observation.";return r;});
        public bool DraftTravel(string fleet,CampaignTravelIntent intent,string? destination,string? contact)=>Run(()=>{
            if(ResearchDraft?.Dirty==true){Message="Apply or explicitly discard unsent research before preparing travel.";return false;}
            var offer=string.IsNullOrEmpty(destination)?null:Current.FindKnownRoute(fleet,destination!);
            if(!string.IsNullOrEmpty(destination)&&offer==null){Message="Known route unavailable.";return false;}
            var r=Current.EditTravel(fleet,intent,offer,contact);Message=r?"Local travel draft; no command submitted.":"Travel input unavailable; backend rules remain authoritative.";return r;
        });
        public bool SendTravel(string command)=>Run(()=>(ResearchDraft?.Dirty??false)?Unavailable():Current.ReviewTravel()?Dispatch(Current.ConfirmTravel(command)):Unavailable());
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
            if(HasLocalEdits||Current.HasPendingWork){Message="Submit or explicitly reset local edits before advance.";return false;}
            var r=host.AdvancePlanning(Current.Frame);Message="Authoritative result: "+r.Error;
            if(!r.Accepted)return false; Current.Refresh();
            if(Current.Frame.Encounters.Count==1)Current.SelectEncounter(Current.Frame.Encounters[0]);return true;
        });
        public bool FinalizeBatch()=>Run(()=>Current.ReviewCompletion()?Dispatch(Current.ConfirmCompletion()):Unavailable());
        public bool SetCloak(string fleet,bool enabled,string command)=>Run(()=>{
            if(HasLocalEdits||Current.HasPendingWork)return Unavailable();
            var r=host.SetApproachCloak(Current.Frame,command,fleet,enabled);Message="Authoritative result: "+r.Error;
            if(r.Accepted)Current.Refresh();return r.Accepted;
        });
        public bool Save(string slot,bool overwrite=false)=>Run(()=>{
            var r=Current.SaveCommittedToSlot(slot,overwrite);Message="Checkpoint result: "+r.Error;return r.Succeeded;
        });
        public bool Load(string slot,bool discard=false)=>Run(()=>{
            if(Current.HasPendingWork||HasLocalEdits&&!discard){Message="Explicit discard required before load.";return false;}
            var opened=integratedPackage!=null?PhysicalReviewHost.OpenIntegratedFromSlot(integratedPackage,codec,integratedSlots!,slot,Current.Frame.Actor,Current.Frame.Battle?.EncounterId)
                :PhysicalReviewHost.OpenFromSlot(package!,codec,slots!,slot,Current.Frame.Actor,Current.Frame.Battle?.EncounterId);
            if(!opened.Succeeded){Message="Checkpoint result: "+opened.Error;return false;}
            var next=Controller(opened.Host!,Current.Frame.Actor);host=opened.Host!;Current=next;ResearchDraft=null;Message="Validated complete-root checkpoint loaded.";return true;
        });
        public bool Reset()=>Run(()=>{
            if(Current.HasPendingWork)return Unavailable();
            var r=Current.TravelDraft!=null?Current.ResetTravelDraft():!Current.Dirty||Current.ResetDraft();
            if(r)ResearchDraft=null;
            Message=r?"Local edits cleared; no command submitted.":"Reset unavailable.";return r;
        });
        public bool Close(bool discard=false)=>Run(()=>{
            if(Current.HasPendingWork||HasLocalEdits&&!discard){Message="Confirm discard before exit.";return false;}
            Closed=true;Message="Review closed. Restart without the explicit review flag for default v1.";return true;
        });
        public string Facts()=>Closed?"Review closed.":ReviewTextRenderer.Render(Current);
        public string Report()=>Closed?"Review closed.":ReviewTextRenderer.RenderReport(Current);
        public string Inspection()=>Current.SelectedShip==null?"Select an owned or visible ship.":ReviewTextRenderer.RenderInspection(Current,Current.SelectedShip);
    }
}
