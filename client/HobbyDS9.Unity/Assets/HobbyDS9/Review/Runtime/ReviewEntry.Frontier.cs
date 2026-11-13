#nullable enable
using System;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private string profileMenuNotice="";
        private bool PlayerProfile=>model?.PlayableFleet==true||model?.SingleplayerDemo==true||model?.FrontierPreview==true;
        private static bool FrontierResourcesAvailable()=>new[]{"frontier","package","physical","core","scenario"}
            .All(name=>Resources.Load<TextAsset>(FrontierPreviewOptions.ResourceRoot+name)!=null);
        private ReviewUiModel NewFrontierModel()
        {
            byte[] Bytes(string name)
            {
                var asset=Resources.Load<TextAsset>(FrontierPreviewOptions.ResourceRoot+name);
                if(asset==null)throw new InvalidOperationException("Frontier resources unavailable.");return asset.bytes;
            }
            var codec=new ReviewNewtonsoftCodec();
            var package=ReviewExactPackageLoader.LoadFrontier(Bytes("frontier"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
            return new ReviewUiModel(package,codec,new ReviewFrontierSlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,FrontierPreviewOptions.SaveDirectory),codec));
        }
        private sealed class PlayerPublicationAttempt { internal bool Failed; }
        private PlayerPublicationAttempt? playerPublicationAttempt;
#if UNITY_EDITOR
        // Future admitted EditMode failure probe: throw after detached render and
        // assert exact prior session/draft/tree/service refs plus candidate closure.
        private Action<string>? playerPublicationBeforeCommit=null;
#endif
        private static void CloseUnpublishedPlayer(ReviewUiModel? candidate)
        {
            if(candidate==null||candidate.Closed)return;
            try{candidate.Close(true);}catch(Exception){ /* Preserve the original publication/load failure. */ }
        }
        private void PublishPlayerProfile(ReviewUiModel next,string slot)
        {
            if(ReferenceEquals(next,model))throw new InvalidOperationException("Publication requires a detached candidate.");
            if(confirmation!=null){CloseUnpublishedPlayer(next);throw new InvalidOperationException("Dismiss the current decision before publication.");}
            // Snapshot explicit session/presentation ownership, never reflection-copy
            // the component or mutate/reset the previous model's draft objects.
            var oldModel=model;var oldFlow=flow;var oldWorkspace=workspace;
            var oldRoot=root;var oldContent=content;var oldMessage=message;var oldConfirmation=confirmation;
            var oldCentral=central;var oldCentralContext=centralContext;var oldStrategy=strategy;var oldPlayer=player;
            var oldTop=playerTopActions;var oldFooter=playerFooter;var oldStatus=playerStatus;var oldTitle=playerTitle;var oldGuidance=playerGuidance;
            var oldDesign=designState;var oldDesignContext=designContext;
            var oldSlot=slotName;var oldNotice=profileMenuNotice;var oldLanding=demoLanding;var oldStarted=demoStarted;
            var oldCommissionOpen=frontierCommissionOpen;var oldCommissionOwner=frontierCommissionOwner;
            var oldActivityOwner=activitiesPanelOwner;var oldActivityController=activitiesPanelController;var oldActivitySelection=activitiesRebuildSelection;
            var oldColonyOwner=colonisationPanelOwner;var oldColonyController=colonisationPanelController;var oldColonyFrame=colonisationPanelFrame;
            var oldColonySelection=colonisationRebuildSelection;var oldColonyReview=colonisationRebuildReview;
            var committedEpoch=viewEpoch;var previousAttempt=playerPublicationAttempt;
            var attempt=new PlayerPublicationAttempt();
            var staged=new VisualElement{name="player-published-campaign"};
            var host=GetComponent<UIDocument>()?.rootVisualElement;
            if(host==null){CloseUnpublishedPlayer(next);throw new InvalidOperationException("Player document is unavailable.");}
            var previousChildren=host.Children().ToList();
            try
            {
                // This reserved epoch is not published while the view is detached.
                // Failed controls additionally retain a permanently failed lease.
                playerPublicationAttempt=attempt;viewEpoch=checked(committedEpoch+1);
                model=next;flow=new ReviewCombatFlow(next);workspace=new ReviewWorkspace(flow);
                central=null;centralContext=null;designState=null;designContext=null;
                slotName=slot;profileMenuNotice="";demoLanding=false;demoStarted=true;
                Build(staged);
#if UNITY_EDITOR
                playerPublicationBeforeCommit?.Invoke("Rendered");
#endif
                // Attach can run view callbacks. The old tree and bindings are still
                // intact if attachment fails; remove only this attempt on rollback.
                host.Add(staged);
#if UNITY_EDITOR
                playerPublicationBeforeCommit?.Invoke("Attached");
#endif
            }
            catch
            {
                attempt.Failed=true;
                model=oldModel;flow=oldFlow;workspace=oldWorkspace;root=oldRoot;content=oldContent;message=oldMessage;confirmation=oldConfirmation;
                central=oldCentral;centralContext=oldCentralContext;strategy=oldStrategy;player=oldPlayer;
                playerTopActions=oldTop;playerFooter=oldFooter;playerStatus=oldStatus;playerTitle=oldTitle;playerGuidance=oldGuidance;
                designState=oldDesign;designContext=oldDesignContext;slotName=oldSlot;profileMenuNotice=oldNotice;demoLanding=oldLanding;demoStarted=oldStarted;
                frontierCommissionOpen=oldCommissionOpen;frontierCommissionOwner=oldCommissionOwner;
                activitiesPanelOwner=oldActivityOwner;activitiesPanelController=oldActivityController;activitiesRebuildSelection=oldActivitySelection;
                colonisationPanelOwner=oldColonyOwner;colonisationPanelController=oldColonyController;colonisationPanelFrame=oldColonyFrame;
                colonisationRebuildSelection=oldColonySelection;colonisationRebuildReview=oldColonyReview;
                viewEpoch=committedEpoch;playerPublicationAttempt=previousAttempt;
                try{staged.UnregisterCallback<GeometryChangedEvent>(Geometry);staged.SetEnabled(false);staged.RemoveFromHierarchy();}catch(Exception){ }
                CloseUnpublishedPlayer(next);
                throw;
            }
            // Commit is irreversible only after render AND attachment succeeded.
            // Cleanup failures here cannot turn a valid published view into a
            // misleading load rejection or roll a committed epoch backward.
            playerPublicationAttempt=previousAttempt;modalPermission.Invalidate();
            try{oldRoot?.UnregisterCallback<GeometryChangedEvent>(Geometry);UnbindDemoKeyboard();ReleaseAudio();}
            catch(Exception){Debug.LogWarning("Previous campaign view cleanup was incomplete.");}
            foreach(var child in previousChildren)
            {
                try{child.SetEnabled(false);child.RemoveFromHierarchy();}
                catch(Exception){Debug.LogWarning("Previous campaign element cleanup was incomplete.");}
            }
            try
            {
                host.ClearClassList();host.styleSheets.Clear();
                host.style.paddingLeft=0;host.style.paddingRight=0;host.style.paddingTop=0;host.style.paddingBottom=0;
                host.style.flexGrow=1;
            }
            catch(Exception){Debug.LogWarning("Campaign host styling requires recovery.");}
            activitiesPanelOwner=null;activitiesPanelController=null;activitiesRebuildSelection=null;
            colonisationPanelOwner=null;colonisationPanelController=null;colonisationPanelFrame=null;
            colonisationRebuildSelection=null;colonisationRebuildReview=null;
            try{PreserveDemoKeyOrigins();BindDemoKeyboard();BindAudio();}
            catch(Exception){Debug.LogWarning("Campaign input/audio binding requires recovery.");}
        }
        private void StartFrontierPreview()
        {
            ReviewUiModel next;
            try{next=NewFrontierModel();}
            catch(Exception){profileMenuNotice="Frontier preview unavailable. The current campaign, saves and local edits are unchanged.";Draw();return;}
            PublishPlayerProfile(next,"frontier-1");
        }
        private void ContinuePlayerProfile(bool frontier,string slot)
        {
            ReviewUiModel? next=null;
            try
            {
                var codec=new ReviewNewtonsoftCodec();
                next=frontier?NewFrontierModel():new ReviewUiModel(demoSource!,codec,new ReviewDemoSlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9SingleplayerDemoOnly-v4"),codec));
                if(!next.Load(slot,true))
                {CloseUnpublishedPlayer(next);profileMenuNotice="Selected "+(frontier?"Frontier format-5":"Bajor format-4")+" slot was not loaded. Your current campaign and unsent edits are unchanged. "+next.Message;Draw();return;}
            }
            catch(Exception){CloseUnpublishedPlayer(next);profileMenuNotice="Selected profile could not be loaded. Your current campaign, saves and unsent edits are unchanged.";Draw();return;}
            PublishPlayerProfile(next,slot);
        }
        private void DrawFrontierMenu(VisualElement parent)
        {
            parent.Add(Readout("Frontier exploration preview — opt-in, provisional arrival charting. Separate campaign and format-5 slots; no survey, settlement or Bajor victory objective."));
            var start=AddButton(parent,"Start Frontier exploration preview",()=>{
                if(demoStarted||model!.HasLocalEdits)Confirm("Start the separate Frontier preview? Unsaved campaign progress/local edits will be replaced only after a valid new campaign is ready. Existing saves remain intact.",StartFrontierPreview);
                else StartFrontierPreview();
            });start.name="frontier-start";start.SetEnabled(FrontierResourcesAvailable());
            foreach(var slot in FrontierPreviewOptions.Slots())
            {
                var load=AddButton(parent,"Continue Frontier "+slot.Substring(slot.Length-1)+" · format 5",()=>Confirm("Load Frontier "+slot.Substring(slot.Length-1)+"? Only a validated complete format-5 frontier save can replace this campaign. Failed loading keeps local edits.",()=>ContinuePlayerProfile(true,slot)));
                load.name="frontier-continue-"+slot;load.SetEnabled(FrontierResourcesAvailable());
            }
            if(!FrontierResourcesAvailable())parent.Add(Readout("Frontier resources are not prepared in this build. Bajor Start / Continue remain available."));
            if(profileMenuNotice.Length>0){var notice=Readout(profileMenuNotice);notice.name="profile-load-result";parent.Add(notice);}
        }
    }
}
