#nullable enable
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private static bool CombatPlaytestResourcesAvailable()=>CombatPlaytestOptions.InputNames()
            .All(name=>Resources.Load<TextAsset>(CombatPlaytestOptions.ResourceRoot+name)!=null);
        private ReviewUiModel NewCombatPlaytestModel()
        {
            var inputs=CombatPlaytestOptions.InputNames().Select(name=>
            {
                var asset=Resources.Load<TextAsset>(CombatPlaytestOptions.ResourceRoot+name);
                if(asset==null)throw new InvalidOperationException("Combat playtest resources unavailable.");
                return asset.bytes;
            }).ToArray();
            return ReviewUiModel.StartCombatPlaytest(inputs,UnityEngine.Application.persistentDataPath);
        }
        private void OpenCombatPlaytestMenu()
        {
            campaignArchiveSaving=false;campaignArchiveOverrideProfile="combat";
            campaignArchiveRows=null;campaignMenuNotice="";
            ShowCampaignMenuPage(CampaignMenuPage.Combat);
        }
        private void DrawCombatPlaytestMenu()
        {
            campaignArchiveSaving=false;campaignArchiveOverrideProfile="combat";
            var panel=PagePanel("Combat playtest");panel.AddToClassList("campaign-menu");
            panel.Add(Readout(CombatPlaytestOptions.DisplayName));
            panel.Add(Readout(CombatPlaytestOptions.Description));
            panel.Add(Readout("Keep the initial travel orders and use End turn three times to reach the tested encounter. Choose your orders in the normal battle planner, commit each round, then finalize aftermath. Changing orders or withdrawing can change contact and results. Victory is not guaranteed."));
            panel.Add(Readout("Combat playtest saves have their own slots. Continue loads only this authored fixture."));
            EnsureCampaignArchive();
            var next=NewestCampaignSave();
            if(next!=null)
            {
                panel.Add(Readout(next.Summary));
                var slot=next.Slot;
                AddButton(panel,"Continue combat playtest",()=>RequestCampaignDeparture("Load the combat playtest?","Load",
                    ()=>LoadCampaignMenuSlot(slot,"combat"),CurrentCampaignArchiveProfile()=="combat"?slot:null,next.Summary))
                    .name="combat-playtest-continue";
            }
            else panel.Add(Readout(campaignArchiveRows==null?"Checking combat playtest saves…":"No compatible combat playtest save. Start a new test or inspect its slots."));
            var start=AddButton(panel,"New combat playtest",()=>RequestCampaignDeparture("Start a new combat playtest?","Start",StartCombatPlaytest));
            start.name="combat-playtest-start";start.SetEnabled(CombatPlaytestResourcesAvailable());
            AddButton(panel,"Combat playtest saves",()=>OpenCampaignArchive(false,"combat")).name="combat-playtest-saves";
            AddButton(panel,"Back",()=>{
                campaignArchiveOverrideProfile=null;campaignArchiveRows=null;campaignMenuNotice="";
                ShowCampaignMenuPage(CampaignMenuPage.Start);
            }).name="combat-playtest-back";
            if(campaignMenuNotice.Length>0)panel.Add(Readout(campaignMenuNotice));
        }
        private void StartCombatPlaytest()
        {
            ReviewUiModel? candidate=null;
            try
            {
                candidate=NewCombatPlaytestModel();
                PublishPlayerProfile(candidate,CombatPlaytestOptions.Slots()[0]);candidate=null;
                campaignSavedOwner=null;campaignArchiveRows=null;campaignArchiveOverrideProfile=null;campaignMenuNotice="";
            }
            catch(Exception)
            {campaignMenuNotice="The combat playtest could not be prepared. Your current campaign and saves are kept.";Draw();}
            finally{CloseUnpublishedPlayer(candidate);}
        }
    }
}
