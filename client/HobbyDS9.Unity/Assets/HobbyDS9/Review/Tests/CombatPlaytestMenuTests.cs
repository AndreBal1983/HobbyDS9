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
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(entry,Is.Not.Null);
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
                        &&!(bool)typeof(ReviewEntry).GetField("campaignArchiveScanQueued",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(entry)!
                        &&!entry.Presentation!.Model.Busy&&i>2)break;
                }
            }
            yield return Settle();
            var original=entry.Presentation!.Model.Current.Frame;
            Click("campaign-menu-new");yield return null;
            Assert.That(root.Q("generated-campaign-seed"),Is.Not.Null);
            Assert.That(root.Q<Button>("colonisation-cycle-campaign-start"),Is.Not.Null);
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
            foreach(var slot in CombatPlaytestOptions.Slots())Assert.That(root.Q<Button>("save-slot-"+slot),Is.Not.Null);
            Assert.That(root.Q<Button>("save-slot-demo-1"),Is.Null);
            var path=(string)typeof(ReviewEntry).GetMethod("CampaignArchivePath",BindingFlags.NonPublic|BindingFlags.Instance)!
                .Invoke(entry,new object[]{"combat","combat-1"})!;
            Assert.That(Path.GetDirectoryName(path),Is.EqualTo(Path.Combine(UnityEngine.Application.persistentDataPath,CombatPlaytestOptions.SaveDirectory)));
            // No disk write, combat advance or native acceptance is performed by this menu case.
        }
    }
}
