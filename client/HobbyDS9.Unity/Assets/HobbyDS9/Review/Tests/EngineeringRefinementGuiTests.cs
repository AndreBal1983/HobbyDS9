#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed class EngineeringRefinementGuiTests
    {
#if HOBBYDS9_PB_20261113
        [UnityTest,Timeout(180000)] public IEnumerator ActualNewEditionPreviewsBeforeStartingAndPausesThroughWorker()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-refinement-gui-tests")<0)
                Assert.Ignore("QA-owned dedicated refinement GUI receiving run.");
            yield return new EnterPlayMode();yield return null;
            var directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Refinement-Gui-"+Guid.NewGuid().ToString("N"));
            var fixture=new GameObject("Refinement GUI receiving fixture");fixture.SetActive(false);
            SingleplayerDemoLaunchOptions.TryParse(new[]{SingleplayerDemoLaunchOptions.Flag},out var options);
            var entry=fixture.AddComponent<ReviewEntry>();entry.ConfigureDemo(options!);
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",flags)!.SetValue(entry,directory);
            typeof(ReviewEntry).GetField("generatedCampaignSize",flags)!.SetValue(entry,30);
            fixture.SetActive(true);var root=entry.GetComponent<UIDocument>().rootVisualElement;
            void Click(Button button)
            {
                Assert.That(button,Is.Not.Null);Assert.That(button.enabledInHierarchy,Is.True,button.text);
                using var submit=NavigationSubmitEvent.GetPooled();submit.target=button;button.SendEvent(submit);
            }
            Button TextButton(string text)=>root.Query<Button>().ToList().Single(b=>b.text==text);
            IEnumerator Settle()
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();
                do
                {
                    yield return null;
                    Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(120),"GUI worker did not settle.");
                }
                while(entry.Presentation!.Model.Busy
                    ||typeof(ReviewEntry).GetField("campaignBusyToken",flags)!.GetValue(entry)!=null
                    ||typeof(ReviewEntry).GetField("campaignArchiveScanToken",flags)!.GetValue(entry)!=null
                    ||(bool)typeof(ReviewEntry).GetField("campaignArchiveScanQueued",flags)!.GetValue(entry)!);
                yield return null;yield return null;
            }
            try
            {
                yield return Settle();Click(root.Q<Button>("campaign-menu-new"));yield return null;
                Click(root.Q<Button>("colonisation-cycle-campaign-start"));yield return Settle();
                var model=entry.Presentation!.Model;Assert.That(model.SupportsEngineeringRefinement,Is.True);
                var workspace=(ReviewWorkspace)typeof(ReviewEntry).GetField("workspace",flags)!.GetValue(entry)!;
                workspace.ResearchTab="engineering";
                typeof(ReviewEntry).GetMethod("Navigate",flags)!.Invoke(entry,new object[]{ReviewWorkspacePage.Research});
                yield return null;yield return null;
                var frame=model.Current.Frame;
                var expected=model.BeginEngineeringRefinementInteraction(EngineeringRefinementInteractionKind.Preview,new EngineeringRefinementCommandDto{
                    ActorId=frame.Actor,CommandId=ReviewUiModel.NewCommandId(),ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
                    Action=EngineeringRefinementAction.Start,ParentVersionId="component.reactor.v1",Goal=EngineeringRefinementGoal.ReduceCapacity})!;
                Assert.That(expected,Is.Not.Null);var check=Task.Run(expected.Execute);
                while(!check.IsCompleted)yield return null;
                Assert.That(model.CompleteEngineeringRefinementInteraction(expected,check.Result),Is.True);
                Assert.That(check.Result.Preview!.CanStart,Is.True);
                var expectedWork=EngineeringRefinementPresentation.Work(check.Result.Preview.Work);
                Click(root.Q<Button>("engineering-generated-parent-component.reactor.v1"));yield return null;
                Click(TextButton("Review target · Reduce module hull space"));yield return Settle();
                Assert.That(model.Current.Frame,Is.SameAs(frame),"Preview must not publish a decision.");
                Assert.That(model.CaptureEngineeringRefinement(frame.Actor)!.Projects,Is.Empty);
                Assert.That(root.Query<Label>().ToList().Any(l=>l.text.Contains("Engineering work · "+expectedWork+" points")),Is.True,"Show the actual pace-adjusted preview work.");
                Click(TextButton("Start targeted refinement"));yield return Settle();
                var project=model.CaptureEngineeringRefinement(model.Current.Frame.Actor)!.Projects.Single();
                Assert.That(project.Stage,Is.EqualTo(EngineeringRefinementStage.Working));Assert.That(project.IsActive,Is.True);
                Assert.That(root.Q<Label>("engineering-active-name").text,Does.Not.Contain("No active project"));
                Click(root.Q<Button>("engineering-generated-project-"+project.ProjectId));yield return null;
                Click(TextButton("Pause development"));yield return Settle();
                var paused=model.CaptureEngineeringRefinement(model.Current.Frame.Actor)!.Projects.Single();
                Assert.That(paused.ProjectId,Is.EqualTo(project.ProjectId));Assert.That(paused.IsActive,Is.False);Assert.That(paused.CanResume,Is.True);
                // No save write, result manufacture or week advance in this case.
            }
            finally{UnityEngine.Object.Destroy(fixture);}
            yield return null;
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
#endif
    }
}
