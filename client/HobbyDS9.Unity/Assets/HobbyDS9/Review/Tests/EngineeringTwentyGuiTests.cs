#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed class EngineeringTwentyGuiTests
    {
        [UnityTest] public IEnumerator PreparedCycleCatalogueBindsAllTwentyActualProjectsIncludingLockedHullGoals()
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-engineering20-gui-tests"))
                Assert.Ignore("Dedicated receiving GUI run required; this is not a native executable test.");
            byte[] Bytes(string path)
            {var asset=Resources.Load<TextAsset>(path);Assert.That(asset,Is.Not.Null,path);return asset.bytes;}
            var codec=new ReviewNewtonsoftCodec();var scenario=Bytes("PlayableFleetPackages/v1/engineering-scenario");
            using(var sha=SHA256.Create())Assert.That(string.Concat(sha.ComputeHash(scenario).Select(b=>b.ToString("x2"))),
                Is.EqualTo("147af76fabf1903a195ab58d571b3ed7c6eb9bad5efd170b13d2656b223cbbb1"));
            var template=ReviewExactPackageLoader.LoadPlayableFleet(Bytes("PlayableFleetPackages/v1/playable"),Bytes("PlayableFleetPackages/v1/package"),
                Bytes("PlayableFleetPackages/v1/physical"),Bytes("PlayableFleetPackages/v1/core"),scenario,codec);
            var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Bytes("ActivitiesV1/"+n)).ToArray(),codec);
            var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Bytes("ColonisationV1/"+n)).ToArray(),codec);
            var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Bytes("ColonisationRosterV2/roster"),codec);
            var package=ColonisationCyclePlaytestOptions.CreatePackage(ColonisationGrowthPlaytestOptions.CreatePackage(roster,Bytes("ColonisationExpansionV1/planner"),codec),Bytes("ColonisationCycleV1/cycle-policy"),codec);
            var directory=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../../artifacts/engineering-gui-receiving",Guid.NewGuid().ToString("N")));
            var settings=new NaturalGalaxySettingsDto{Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
                HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
            var model=new ReviewUiModel(package,settings,codec,new ReviewColonisationCycleSlotStore(directory,codec));
            var frame=model.Current.Frame;var flow=new ReviewCombatFlow(model);var workspace=new ReviewWorkspace(flow){ResearchTab="engineering"};
            Assert.That(workspace.Navigate(ReviewWorkspacePage.Research),Is.True);
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            try
            {
                window.position=new Rect(80,80,1280,720);window.Show();var root=window.rootVisualElement;
                root.AddToClassList("player-console");root.style.flexGrow=1;
                root.styleSheets.Add(Resources.Load<StyleSheet>("PlayerConsole"));
                var view=new ReviewPlayerView(model,workspace,(parent,text,action)=>
                {var button=new Button(action){text=text};parent.Add(button);return button;},action=>action(),
                    (_,__)=>Assert.Fail("Inspection must not request gameplay confirmation"),_=>{},(_,__,___)=>{},
                    (button,action)=>button.clicked+=action);
                view.Render(root,ReviewWorkspacePage.Research);yield return null;yield return null;
                var projects=frame.Strategy!.Research.Engineering;Assert.That(projects.Count,Is.EqualTo(20));
                Assert.That(root.Query<Button>().ToList().Count(e=>e.name.StartsWith("engineering-goal-engineering.",StringComparison.Ordinal)),Is.EqualTo(20));
                foreach(var project in projects)
                {
                    var goal=root.Q<Button>("engineering-goal-"+project.Definition.DefinitionId);
                    Assert.That(goal,Is.Not.Null,project.Definition.DefinitionId.Value);
                    Assert.That(goal.Q<Label>(className:"engineering-goal-name").text,Is.EqualTo(project.Definition.DisplayName));
                    goal.Focus();using(var e=NavigationSubmitEvent.GetPooled()){e.target=goal;goal.SendEvent(e);}
                    Assert.That(workspace.EngineeringProjectId,Is.EqualTo(project.Definition.DefinitionId.Value));
                    view.Render(root,ReviewWorkspacePage.Research);yield return null;
                    var card=root.Q("engineering-project-"+project.Definition.DefinitionId);
                    Assert.That(card,Is.Not.Null,project.Definition.DefinitionId.Value);
                    Assert.That(card.Q<Label>("engineering-project-heading").text,Is.EqualTo(project.Definition.DisplayName));
                    Assert.That(card.Q<Label>("engineering-authored-effect").text,Is.EqualTo(project.Definition.EffectSummary));
                    Assert.That(card.Q<Button>("integrated-develop-"+project.Definition.DefinitionId).enabledSelf,Is.False,"All initial source projects are lawfully locked, not omitted.");
                }
                foreach(var id in new[]{"engineering.reinforced-hull","engineering.expanded-hull"})Assert.That(root.Q("engineering-goal-"+id),Is.Not.Null);
                var scout=root.Q<Button>("engineering-goal-engineering.advanced-starship-engineering");
                using(var e=NavigationSubmitEvent.GetPooled()){e.target=scout;scout.SendEvent(e);}view.Render(root,ReviewWorkspacePage.Research);yield return null;
                var legacy=root.Q("engineering-project-engineering.advanced-starship-engineering");
                Assert.That(legacy.Q<Label>("engineering-cycle-construction").text,Does.Contain("unavailable"));
                var selected=workspace.EngineeringProjectId;
                root.Q<TextField>("engineering-search").value="no-such-project";
                Assert.That(root.Query<Button>().ToList().Where(b=>b.name.StartsWith("engineering-goal-")).All(b=>b.style.display.value==DisplayStyle.None),Is.True);
                Assert.That(workspace.EngineeringProjectId,Is.EqualTo(selected),"Filtering must not silently select or start a project.");
                root.Q<TextField>("engineering-search").value="";
                root.Q<DropdownField>("engineering-kind-filter").index=2;
                Assert.That(root.Query<Button>().ToList().Count(b=>b.name.StartsWith("engineering-goal-")&&b.style.display.value!=DisplayStyle.None),Is.EqualTo(projects.Count(p=>p.Definition.Kind==EngineeringProjectKind.Refinement)));
                Assert.That(workspace.EngineeringProjectId,Is.EqualTo(selected));
                Assert.That(model.Current.Frame,Is.SameAs(frame),"Rendering must submit no commands or advance state.");
            }
            finally{window.Close();if(window!=null)UnityEngine.Object.DestroyImmediate(window);model.Close(true);}
        }
    }
}
