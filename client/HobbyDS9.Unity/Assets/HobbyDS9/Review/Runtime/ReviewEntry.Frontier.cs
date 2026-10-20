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
        private void PublishPlayerProfile(ReviewUiModel next,string slot)
        {
            viewEpoch++;modalPermission.Invalidate();model=next;flow=new ReviewCombatFlow(next);workspace=new ReviewWorkspace(flow);
            central=null;centralContext=null;designState=null;designContext=null;slotName=slot;profileMenuNotice="";demoLanding=false;demoStarted=true;Build();
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
            ReviewUiModel next;
            try
            {
                var currentMatches=frontier?model!.FrontierPreview:model!.SingleplayerDemo;
                var codec=new ReviewNewtonsoftCodec();
                next=currentMatches?model!:frontier?NewFrontierModel():new ReviewUiModel(demoSource!,codec,new ReviewDemoSlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9SingleplayerDemoOnly-v4"),codec));
                if(!next.Load(slot,true))
                {profileMenuNotice="Selected "+(frontier?"Frontier format-5":"Bajor format-4")+" slot was not loaded. Your current campaign and unsent edits are unchanged. "+next.Message;Draw();return;}
            }
            catch(Exception){profileMenuNotice="Selected profile could not be loaded. Your current campaign, saves and unsent edits are unchanged.";Draw();return;}
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
