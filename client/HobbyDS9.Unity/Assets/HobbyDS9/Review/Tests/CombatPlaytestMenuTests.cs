#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed class CombatPlaytestMenuTests
    {
        [UnityTest] public IEnumerator ActualOptInMenuKeepsGeneratedNewAndUsesCombatArchive()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-combat-menu-tests")<0)
                Assert.Ignore("QA-owned dedicated player-menu receiving run.");
            yield return new EnterPlayMode();yield return null;yield return null;
            // Own the launch profile instead of assuming the Editor's bootstrap
            // flags/previous test left the application on its main menu.
            var fixture=new GameObject("Combat menu receiving fixture");fixture.SetActive(false);
            Assert.That(SingleplayerDemoLaunchOptions.TryParse(new[]{SingleplayerDemoLaunchOptions.Flag},out var options),Is.True);
            var entry=fixture.AddComponent<ReviewEntry>();entry.ConfigureDemo(options!);fixture.SetActive(true);
            try
            {
            yield return null;yield return null;
            Assert.That(entry.Presentation!.Model.SingleplayerDemo,Is.True);
            var root=entry.GetComponent<UIDocument>().rootVisualElement;
            void Click(string id)
            {
                var b=root.Q<Button>(id);Assert.That(b,Is.Not.Null,id);Assert.That(b.enabledInHierarchy,Is.True,id);
                b.Focus();using(var e=NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}
            }
            IEnumerator Settle()
            {
                for(int i=0;i<600;i++)
                {
                    yield return null;
                    if(typeof(ReviewEntry).GetField("campaignBusyToken",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(entry)==null
                        &&typeof(ReviewEntry).GetField("campaignArchiveScanToken",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(entry)==null
                        &&!(bool)typeof(ReviewEntry).GetField("campaignArchiveScanQueued",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(entry)!
                        &&!entry.Presentation!.Model.Busy&&i>2)break;
                }
            }
            yield return Settle();
            var original=entry.Presentation!.Model.Current.Frame;
#if HOBBYDS9_PB_20261113
            Assert.That(root.Q<Button>("campaign-menu-new").text,Is.EqualTo("New refinement campaign"));
            Assert.That(root.Q<Button>("campaign-menu-continue").text,Does.StartWith("Continue · Refinement campaign · "),"Archive discovery must retain the edition name.");
            Click("campaign-menu-load");yield return Settle();
            Assert.That(root.Query<Label>().ToList().Any(l=>l.text=="Archive: Refinement campaign"),Is.True);
            Assert.That(root.Query<DropdownField>().ToList().Single(d=>d.label=="Archive").value,Is.EqualTo("Refinement campaign"));
            var refinementPath=(string)typeof(ReviewEntry).GetMethod("CampaignArchivePath",BindingFlags.NonPublic|BindingFlags.Instance)!
                .Invoke(entry,new object[]{"cycle","generated-1"})!;
            Assert.That(Path.GetFileName(Path.GetDirectoryName(refinementPath)),Is.EqualTo(ReviewColonisationCycleSlotStore.Namespace+EngineeringRefinementPlaytestOptions.SaveDirectorySuffix));
            var archiveBack=root.Query<Button>().ToList().Single(b=>b.text=="Back");
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=archiveBack;archiveBack.SendEvent(e);}
            yield return Settle();
#endif
            Click("campaign-menu-new");yield return null;
            Assert.That(root.Q("generated-campaign-seed"),Is.Not.Null);
            Assert.That(root.Q<Button>("colonisation-cycle-campaign-start"),Is.Not.Null);
#if HOBBYDS9_PB_20261113
            Assert.That(root.Query<Label>().ToList().Any(l=>l.text=="New refinement campaign"),Is.True);
            Assert.That(root.Q<Button>("colonisation-cycle-campaign-start").text,Is.EqualTo("Start refinement campaign"));
            Assert.That(root.Query<Label>().ToList().Any(l=>l.text.Contains("This edition uses separate saves")),Is.True);
#endif
            var back=root.Query<Button>().ToList().Single(b=>b.text=="Back");
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=back;back.SendEvent(e);}
            yield return Settle();
            Click("campaign-menu-combat");yield return Settle();
            Assert.That(root.Query<Label>().ToList().Any(l=>l.text==CombatPlaytestOptions.DisplayName),Is.True);
            Assert.That(entry.Presentation!.Model.Current.Frame,Is.SameAs(original),"Opening the opt-in page must submit no command.");
            Assert.That(root.Q<Button>("combat-playtest-start").enabledInHierarchy,Is.True);
            Click("combat-playtest-start");yield return null;yield return null;
            Assert.That(entry.Presentation!.Model.CombatPlaytest,Is.True);
            Assert.That(entry.Presentation.Model.Current.Frame.Status.Turn,Is.EqualTo(1));
            Click("demo-menu");yield return null;Click("campaign-menu-save");yield return Settle();
            Assert.That(root.Query<Label>().ToList().Any(l=>l.text=="Archive: Combat playtest"),Is.True,"The named Sol edition must not relabel the combat archive.");
            foreach(var slot in CombatPlaytestOptions.Slots())Assert.That(root.Q<Button>("save-slot-"+slot),Is.Not.Null);
            Assert.That(root.Q<Button>("save-slot-demo-1"),Is.Null);
            var path=(string)typeof(ReviewEntry).GetMethod("CampaignArchivePath",BindingFlags.NonPublic|BindingFlags.Instance)!
                .Invoke(entry,new object[]{"combat","combat-1"})!;
            Assert.That(Path.GetFullPath(Path.GetDirectoryName(path)!),Is.EqualTo(Path.GetFullPath(Path.Combine(UnityEngine.Application.persistentDataPath,CombatPlaytestOptions.SaveDirectory))));
            var backToMenu=root.Query<Button>().ToList().Single(b=>b.text=="Back");
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=backToMenu;backToMenu.SendEvent(e);}yield return null;
            Click("campaign-menu-main");yield return null;Click("campaign-menu-new");yield return null;
            var backToRunningCampaign=root.Query<Button>().ToList().Single(b=>b.text=="Back");
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=backToRunningCampaign;backToRunningCampaign.SendEvent(e);}yield return null;
            Assert.That(root.Q<Button>("campaign-menu-resume"),Is.Not.Null,"Back from New while a campaign is running returns to its paused menu.");
            Assert.That(root.Q<Button>("campaign-menu-new"),Is.Null,"The paused menu keeps New behind the explicit Main menu route.");
            Click("campaign-menu-main");yield return null;
            Assert.That(root.Q<Button>("campaign-menu-new"),Is.Not.Null);Assert.That(root.Q<Button>("campaign-menu-combat"),Is.Not.Null);
            // No disk write, combat advance or native acceptance is performed by this menu case.
            }
            finally{UnityEngine.Object.Destroy(fixture);}
            yield return null;
        }
        [UnityTest,Timeout(120000)] public IEnumerator ArchiveDiscoveryKeepsMenuUsableAndDiscardsSupersededProfileResults()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-combat-menu-tests")<0)Assert.Ignore("QA-owned dedicated menu receiving run.");
            yield return new EnterPlayMode();yield return null;
            var fixture=new GameObject("Startup archive receiving fixture");fixture.SetActive(false);
            SingleplayerDemoLaunchOptions.TryParse(new[]{SingleplayerDemoLaunchOptions.Flag},out var options);
            var entry=fixture.AddComponent<ReviewEntry>();entry.ConfigureDemo(options!);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            using var entered=new System.Threading.ManualResetEventSlim();using var release=new System.Threading.ManualResetEventSlim();
            var probe=typeof(ReviewEntry).GetField("campaignArchiveMetadataProbe",flags)!;
            probe.SetValue(entry,(Action<string>)(_=>{entered.Set();if(!release.Wait(TimeSpan.FromSeconds(30)))throw new TimeoutException();}));
            fixture.SetActive(true);var root=entry.GetComponent<UIDocument>().rootVisualElement;
            void Click(string id)
            {var button=root.Q<Button>(id);Assert.That(button,Is.Not.Null,id);Assert.That(button.enabledInHierarchy,Is.True,id);using var e=NavigationSubmitEvent.GetPooled();e.target=button;button.SendEvent(e);}
            try
            {
                var clock=System.Diagnostics.Stopwatch.StartNew();
                while(!entered.IsSet){Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(20));yield return null;}
                var model=entry.Presentation!.Model;var frame=model.Current.Frame;
                Assert.That(root.Q("campaign-operation-busy"),Is.Null);Assert.That(model.Busy,Is.False);
                Assert.That(root.Q<Button>("campaign-menu-new").enabledInHierarchy,Is.True);
                Assert.That(root.Q<Button>("campaign-menu-settings").enabledInHierarchy,Is.True);
                Assert.That(root.Q<Button>("campaign-menu-continue").enabledInHierarchy,Is.False);
#if HOBBYDS9_PB_20261113
                Assert.That(root.Q<Button>("campaign-menu-continue").text,Is.EqualTo("Continue · Refinement campaign · Checking saved slots…"));
                Assert.That(root.Q<Button>("campaign-menu-new").text,Is.EqualTo("New refinement campaign"));
#endif
                Click("campaign-menu-new");yield return null;Assert.That(root.Q("generated-campaign-seed"),Is.Not.Null);
                probe.SetValue(entry,null);
                typeof(ReviewEntry).GetMethod("OpenCombatPlaytestMenu",flags)!.Invoke(entry,null);yield return null;
                release.Set();clock.Restart();
                while(typeof(ReviewEntry).GetField("campaignArchiveScanToken",flags)!.GetValue(entry)!=null)
                {Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(20));yield return null;}
                yield return null;yield return null;
                Assert.That(typeof(ReviewEntry).GetField("campaignArchiveProfile",flags)!.GetValue(entry),Is.EqualTo("combat"));
                Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(root.Q<Button>("combat-playtest-start").enabledInHierarchy,Is.True);
                Assert.That(root.Q("campaign-operation-busy"),Is.Null);
            }
            finally{release.Set();probe.SetValue(entry,null);UnityEngine.Object.Destroy(fixture);}
            yield return null;
        }
    }
}
