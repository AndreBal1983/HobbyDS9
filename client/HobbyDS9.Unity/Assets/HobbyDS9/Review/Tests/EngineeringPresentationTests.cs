#nullable enable
using System;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    // Neutral EditMode hierarchy checks against the prepared real Fleet package.
    // No PlayMode bootstrap, saves, campaign advances or native acceptance.
    public sealed class EngineeringPresentationTests
    {
        private ReviewUiModel model=null!;
        private ReviewWorkspace workspace=null!;
        private VisualElement root=null!;
        private int performed,confirmed;
        [SetUp] public void Setup()
        {
            byte[] Bytes(string name)
            {
                var asset=Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+name);
                Assert.That(asset,Is.Not.Null,"Prepared PlayableFleet resources are required.");return asset.bytes;
            }
            var codec=new ReviewNewtonsoftCodec();
            var package=ReviewExactPackageLoader.LoadPlayableFleet(Bytes("playable"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
            model=new ReviewUiModel(package,codec,new ReviewPlayableFleetSlotStore(Path.Combine(Path.GetTempPath(),"HobbyDS9-EngineeringView-"+Guid.NewGuid().ToString("N")),codec));
            workspace=new ReviewWorkspace(new ReviewCombatFlow(model)){ResearchTab="engineering"};
            root=new VisualElement();performed=confirmed=0;
        }
        private void Draw()
        {
            new ReviewPlayerView(model,workspace,(parent,text,action)=>{var button=new Button(action){text=text};parent.Add(button);return button;},
                action=>{performed++;action();},(_,__)=>confirmed++,_=>{},(_,__,___)=>{},(_,__)=>{}).Render(root,ReviewWorkspacePage.Research);
        }
        [Test] public void ActualProjectsAppearOnceInTheSearchableWorkbenchWithoutSubmittingCommands()
        {
            var frame=model.Current.Frame;var projects=frame.Strategy!.Research.Engineering;Draw();
            var list=root.Q<ScrollView>("engineering-goal-list");Assert.That(list,Is.Not.Null);
            var goals=list.Query<Button>(className:"engineering-goal").ToList();
            Assert.That(goals.Count,Is.EqualTo(projects.Count));
            Assert.That(goals.Select(g=>g.name),Is.Unique);
            Assert.That(root.Q<TextField>("engineering-search"),Is.Not.Null);
            var filter=root.Q<DropdownField>("engineering-kind-filter");Assert.That(filter,Is.Not.Null);
            Assert.That(filter.choices,Is.EquivalentTo(new[]{"All kinds","New capability","Refinement","Civil application"}));
            foreach(var project in projects)
            {
                var goal=list.Q<Button>("engineering-goal-"+project.Definition.DefinitionId);
                Assert.That(goal,Is.Not.Null);
                Assert.That(goal.Q<Label>(className:"engineering-goal-name").text,Is.EqualTo(project.Definition.DisplayName));
            }
            Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(performed,Is.Zero);Assert.That(confirmed,Is.Zero);
        }
        [Test] public void SummaryUsesCurrentTargetAndDoesNotPromoteMockProjects()
        {
            var research=model.Current.Frame.Strategy!.Research;Draw();
            var summary=root.Q("engineering-current-summary");Assert.That(summary,Is.Not.Null);
            var current=research.Engineering.FirstOrDefault(p=>p.IsTarget);
            Assert.That(summary.Q<Label>("engineering-active-name").text,Is.EqualTo(current?.Definition.DisplayName??"No active project"));
            Assert.That(root.Q<ScrollView>("engineering-goal-list").Query<Button>(className:"engineering-goal").ToList().Count,Is.EqualTo(research.Engineering.Count));
            Assert.That(root.Q<Button>("integrated-clear-engineering")!=null,Is.EqualTo(current!=null));
            Assert.That(root.Query<Foldout>().ToList().Any(f=>f.text=="Latest turn: Engineering"),Is.True);
            Assert.That(performed,Is.Zero);Assert.That(confirmed,Is.Zero);
        }
        [Test] public void FreshPresenterTabRoundTripRetainsResearchDraftAndHasNoSharedScopeClass()
        {
            var frame=model.Current.Frame;var draft=model.ResearchInputs();Draw();
            workspace.ResearchTab="science";Draw();
            Assert.That(root.Q("engineering-current-summary"),Is.Null);
            Assert.That(root.Q("engineering-goal-list"),Is.Null);
            Assert.That(root.Q("integrated-apply-science"),Is.Not.Null);
            workspace.ResearchTab="engineering";Draw();
            Assert.That(model.ResearchInputs(),Is.SameAs(draft));Assert.That(model.Current.Frame,Is.SameAs(frame));
            Assert.That(root.ClassListContains("engineering-group"),Is.False);
            Assert.That(root.Q<Button>("engineering-allocation"),Is.Not.Null);
            Assert.That(performed,Is.Zero);Assert.That(confirmed,Is.Zero);
        }
    }
}
