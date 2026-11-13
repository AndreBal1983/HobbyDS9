#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private static bool PlayableResourcesAvailable()=>new[]{"playable","package","physical","core","scenario"}
            .All(name=>Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+name)!=null);
        private HobbyDS9.Contracts.CampaignPlayableFleetPackageDto LoadPlayableFleetTemplate(bool engineeringContent=false)
        {
            byte[] Bytes(string name)
            {
                var asset=Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+name);
                if(asset==null)throw new InvalidOperationException("Playable campaign resources unavailable.");
                return asset.bytes;
            }
            var codec=new ReviewNewtonsoftCodec();
#if HOBBYDS9_PB_20261113
            var scenario=engineeringContent?"engineering-scenario":"scenario";
#else
            const string scenario="scenario";
#endif
            return ReviewExactPackageLoader.LoadPlayableFleet(Bytes("playable"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes(scenario),codec);
        }
        private ReviewUiModel NewPlayableFleetModel()
        {
            var codec=new ReviewNewtonsoftCodec();
            return new ReviewUiModel(LoadPlayableFleetTemplate(),codec,new ReviewPlayableFleetSlotStore(
                Path.Combine(UnityEngine.Application.persistentDataPath,PlayableFleetOptions.SaveDirectory),codec));
        }
        private void OpenPlayableFleet(string? slot)
        {
            ReviewUiModel next;
            try
            {
                // Never mutate the currently displayed profile until the complete replacement is valid.
                next=NewPlayableFleetModel();
                if(slot!=null&&!next.Load(slot))
                {profileMenuNotice="Saved fleet campaign was not loaded. Current progress and edits remain intact. "+next.Message;Draw();return;}
            }
            catch(Exception){profileMenuNotice="Fleet campaign unavailable. Current progress, edits and saves remain intact.";Draw();return;}
            PublishPlayerProfile(next,slot??"fleet-1");
        }
        private void DrawPlayableFleetMenu(VisualElement parent,bool initialFocus=true)
        {
            parent.Add(Readout("Fleet campaign · Federation / Cardassian"));
            parent.Add(Readout("Build existing Cruisers, assemble a named fleet, follow a known enemy and choose orders for each battle round. Survivors retain damage and ammunition."));
            var start=AddButton(parent,"Start fleet campaign",()=>{
                if(demoStarted||model!.HasLocalEdits)Confirm("Start a fleet campaign? Unsaved progress and edits are replaced only after a valid new campaign is ready. Existing saves remain intact.",()=>OpenPlayableFleet(null));
                else OpenPlayableFleet(null);
            });
            start.name="playable-fleet-start";start.AddToClassList("primary-action");start.SetEnabled(PlayableResourcesAvailable());
            if(initialFocus&&!demoStarted&&PlayableResourcesAvailable())ScheduleDemoMenuFocus(start);
            foreach(var slot in PlayableFleetOptions.Slots())
            {
                var load=AddButton(parent,"Continue fleet campaign "+slot.Substring(slot.Length-1),()=>Confirm(
                    "Load fleet campaign "+slot.Substring(slot.Length-1)+"? Successful loading replaces unsaved progress and local edits.",()=>OpenPlayableFleet(slot)));
                load.name="playable-fleet-continue-"+slot;load.SetEnabled(PlayableResourcesAvailable());
            }
            if(!PlayableResourcesAvailable())parent.Add(Readout("Fleet campaign resources are unavailable in this build."));
        }
    }
}
