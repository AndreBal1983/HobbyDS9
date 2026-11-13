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
        [Test] public void ActualProjectsAreGroupedOnceAndEmptyKindsHaveNoPretendActions()
        {
            var frame=model.Current.Frame;var projects=frame.Strategy!.Research.Engineering;Draw();
            foreach(var kind in new[]{EngineeringProjectKind.NewCapability,EngineeringProjectKind.Refinement,EngineeringProjectKind.CivilApplication})
            {
                var section=root.Q("engineering-kind-"+kind);Assert.That(section,Is.Not.Null);
                var expected=projects.Where(p=>p.Definition.Kind==kind).ToList();
                Assert.That(section.Query<VisualElement>(className:"engineering-card").ToList().Count,Is.EqualTo(expected.Count));
                if(expected.Count==0){Assert.That(section.Q("engineering-empty-"+kind),Is.Not.Null);Assert.That(section.Query<Button>().ToList(),Is.Empty);}
                foreach(var project in expected)
                {
                    var card=section.Q("engineering-project-"+project.Definition.DefinitionId);Assert.That(card,Is.Not.Null);
                    Assert.That(card.Q<Label>("engineering-project-heading").text,Is.EqualTo(project.Definition.DisplayName));
                    Assert.That(card.Q<Button>("integrated-develop-"+project.Definition.DefinitionId).enabledSelf,Is.EqualTo(project.CanTarget));
                    Assert.That(card.Q<Label>("engineering-project-cost").text,Does.EndWith("Engineering points"));
                    Assert.That(card.Q<Label>("engineering-project-progress"),Is.Not.Null);
                    Assert.That(card.Q<Label>("engineering-effect-qualifier"),Is.Not.Null);
                    Assert.That(card.Q<Label>("engineering-authored-effect").text,Is.EqualTo(project.Definition.EffectSummary));
                }
            }
            Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(performed,Is.Zero);Assert.That(confirmed,Is.Zero);
        }
        [Test] public void SummaryUsesCurrentTargetAndDoesNotPromoteMockProjects()
        {
            var research=model.Current.Frame.Strategy!.Research;Draw();
            var summary=root.Q("engineering-current-summary");Assert.That(summary,Is.Not.Null);
            var current=research.Engineering.FirstOrDefault(p=>p.IsTarget);
            Assert.That(summary.Query<Label>().ToList().Any(l=>l.text==(current==null?"No active Engineering project":"Current project: "+current.Definition.DisplayName)),Is.True);
            Assert.That(root.Query<VisualElement>(className:"engineering-card").ToList().Count,Is.EqualTo(research.Engineering.Count));
            Assert.That(root.Q<Button>("integrated-clear-engineering").enabledSelf,Is.EqualTo(research.Target.HasValue));
            Assert.That(root.Q("engineering-turn-report"),Is.Not.Null);
            Assert.That(performed,Is.Zero);Assert.That(confirmed,Is.Zero);
        }
        [Test] public void FreshPresenterTabRoundTripRetainsResearchDraftAndHasNoSharedScopeClass()
        {
            var frame=model.Current.Frame;var draft=model.ResearchInputs();Draw();
            workspace.ResearchTab="science";Draw();
            Assert.That(root.Q("engineering-current-summary"),Is.Null);
            Assert.That(root.Q("engineering-kind-Refinement"),Is.Null);
            Assert.That(root.Q("integrated-apply-science"),Is.Not.Null);
            workspace.ResearchTab="engineering";Draw();
            Assert.That(model.ResearchInputs(),Is.SameAs(draft));Assert.That(model.Current.Frame,Is.SameAs(frame));
            Assert.That(root.ClassListContains("engineering-group"),Is.False);
            Assert.That(root.Q("integrated-apply-split"),Is.Not.Null);
            Assert.That(performed,Is.Zero);Assert.That(confirmed,Is.Zero);
        }
    }
}
