#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    // Real PlanetInterface/PlanetDetails widgets from literal actor observations, no saves or session commands.
    public sealed class WorldInfoCompactPresentationTests
    {
        private readonly List<ReviewColonisationPanel> panels=new List<ReviewColonisationPanel>();
        private UnityEditor.EditorWindow? window;
        private int interactions;
        private static object? Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(target,args);
        private static string Texts(VisualElement element)=>string.Join("\n",element.Query<Label>().ToList().Select(l=>l.text));
        private static ColonisationPlanetSnapshot Planet(bool known=true,int controlTurn=3,string actor="civ.hidden-empire",string? activeSurvey=null,int done=0)=>new ColonisationPlanetSnapshot(new ColonisationPlanetObservationDto{
            PlanetId="planet.fixture",BodyId="body.fixture",SystemId="system.fixture",DisplayName="Earth",Surveyed=known,ControlObservedTurn=controlTurn,
            ActiveSurveyId=activeSurvey,CompletedSurveyWeeks=done,
            KnownControl=new PlanetControlDto{Kind=PlanetControllerKind.Empire,ActorId=actor,PlanetId="planet.fixture"},
            Environment=new PlanetDefinitionDto{PlanetClass=known?"Temperate":"Volcanic",Atmosphere="atmosphere.oxygen-nitrogen",Gravity=10000,Pressure=10000,Radiation=777,
                Deposits=new List<ResourceAmountDto>{new ResourceAmountDto{ResourceDefinitionId="resource.dilithium",Amount=424242}}}});
        private ReviewColonisationPanel Panel(ColonisationPlanetSnapshot planet,bool owned=true,long? demand=11,long population=1013,bool queued=true,PlanetSurveyStage? surveyStage=null)
        {
            var worlds=owned?new[]{new ColonisationWorldSnapshot("colony.fixture",planet.PlanetId,planet.SystemId,"Earth",ColonyEstablishmentStage.Established,null,population,26,8,50,14,0,0,0,0,"",Array.Empty<ResourceAmountSnapshot>(),Array.Empty<ColonisationCohortSnapshot>(),foodDemand:demand)}:Array.Empty<ColonisationWorldSnapshot>();
            var queue=owned&&queued?new[]{new ColonisationQueueSnapshot("queue.fixture","colony.fixture",ColonisationProductionKind.Infrastructure,"infrastructure.research-laboratory.v1","Research laboratory",0,60,22,"Working",Array.Empty<ResourceAmountSnapshot>(),Array.Empty<ResourceAmountSnapshot>())}:Array.Empty<ColonisationQueueSnapshot>();
            var observation=new ColonisationCampaignSnapshot("civ.fixture",9,7,"literal-observation",worlds,Array.Empty<ColonisationTransportSnapshot>(),queue,
                new[]{new ColonisationPresenceSnapshot(planet.SystemId,2,new[]{"civ.hidden-empire"})},Array.Empty<ColonisationManifestReport>(),surveyStage.HasValue?new[]{new ColonisationSurveySnapshot("survey.fixture","ship.fixture",planet.PlanetId,surveyStage.Value,planet.CompletedSurveyWeeks)}:Array.Empty<ColonisationSurveySnapshot>(),Array.Empty<ColonisationIntentSnapshot>(),1,Array.Empty<ResourceAmountSnapshot>());
            var activities=new CampaignActivityObservationDto{ActorId=observation.Actor,Turn=observation.Turn,Revision=observation.Revision};
            activities.Ships.Add(new ActivityOwnShipDto{ShipId="ship.fixture",NodeId="node.fixture",DisplayName="Surveyor",Stationary=true,StudyWork=1});
            var nodes=new[]{planet}.ToLookup(p=>p.SystemId,_=>"node.fixture",StringComparer.Ordinal);
            var panel=new ReviewColonisationPanel(observation,new[]{planet},activities,(_,__)=>interactions++,_=>interactions++,(_,__)=>interactions++,_=>interactions++,systemNodes:nodes,environmentScale:10000);
            panels.Add(panel);Call(panel,"Show","Planet");return panel;
        }
        [TearDown]public void Cleanup()
        {
            try{foreach(var panel in panels)panel.Dispose();panels.Clear();window?.rootVisualElement.Clear();}
            finally{if(window!=null){window.Close();if(window!=null)UnityEngine.Object.DestroyImmediate(window);}window=null;interactions=0;}
        }
        [Test]public void ActualWorldPaneGroupsExactEconomyFoodAndQueueWithoutInventedGauges()
        {
            var panel=Panel(Planet());var root=panel.Q("world-info");Assert.That(root,Is.Not.Null);
            Assert.That(root.Query(className:"world-info-stat").ToList().Count,Is.EqualTo(6));
            Assert.That(root.Q<Label>("world-stat-value-population").text,Is.EqualTo("1,013"));
            Assert.That(root.Q<Label>("world-stat-value-industry").text,Is.EqualTo("8"));
            Assert.That(root.Q<Label>("world-stat-value-research").text,Is.EqualTo("50"));
            Assert.That(root.Q<Label>("world-stat-value-food-output").text,Is.EqualTo("14"));
            Assert.That(root.Q<Label>("world-stat-value-food-demand").text,Is.EqualTo("11"));
            Assert.That(root.Q<Label>("world-stat-value-food-stock").text,Is.EqualTo("26"));
            Assert.That(root.Q<Label>("world-food-balance-value").text,Is.EqualTo("+3 / week"));
            Assert.That(root.Query<ProgressBar>().ToList(),Is.Empty,"Economy/storage have no invented maximum.");
            var queue=root.Q<Foldout>("colonisation-planet-queue-colony.fixture");Assert.That(queue.value,Is.False);Assert.That(queue.text,Does.Contain("Research laboratory").And.Contain("22 / 60 Industry"));
            Assert.That(panel.Q<Button>("colonisation-planet-colony-colony.fixture"),Is.Not.Null,"Unique Manage action retained.");
            Assert.That(panel.Q<Button>("colonisation-survey-start-ship.fixture"),Is.Not.Null,"Environment knowledge alone does not remove the real survey action.");
            Assert.That(interactions,Is.Zero);
        }
        [Test]public void StaleControlAndPresenceDatesStayLocalAndRawCivilizationIdsAreAbsent()
        {
            var root=Panel(Planet()).Q("world-info");
            Assert.That(root.Q<Label>("world-control-date-chip").text,Is.EqualTo("Last observed week 3"));
            Assert.That(root.Q<Label>("world-presence-date-chip").text,Is.EqualTo("Last observed week 2"));
            Assert.That(root.Q<Label>("world-economy-stamp-colony.fixture").text,Is.EqualTo("Own figures · week 9"));
            Assert.That(root.Q<Label>("world-environment-date-chip").text,Does.Contain("not supplied"));
            Assert.That(root.Q<Label>("world-resources-date-chip").text,Does.Contain("Initial record").And.Contain("not supplied"));
            Assert.That(Texts(root),Does.Not.Contain("civ.hidden-empire").And.Not.Contain("civ.fixture"));
            Assert.That(root.Q<Label>("world-control-chip").text,Is.EqualTo("Other empire record"));
            Assert.That(root.Q<Foldout>("colonisation-planet-environment").value,Is.False);Assert.That(root.Q<Foldout>("colonisation-planet-resources").value,Is.False);
        }
        [Test]public void UnsurveyedHiddenFieldsAndMissingOwnEconomyRemainUnknownInsteadOfZeroOrIdle()
        {
            var root=Panel(Planet(known:false),owned:false).Q("world-info");var text=Texts(root);
            Assert.That(root.Q<Label>("world-type-chip").text,Is.EqualTo("Type not reported"));
            Assert.That(root.Query(className:"world-info-stat-value").ToList().Cast<Label>().All(l=>l.text=="—"),Is.True);
            Assert.That(root.Q<Label>("world-settlement-chip").text,Is.EqualTo("Settlement not reported"));
            Assert.That(text,Does.Not.Contain("Volcanic").And.Not.Contain("777").And.Not.Contain("424,242").And.Not.Contain("Idle"));
            Assert.That(root.Q<Label>("world-survey-chip").text,Is.EqualTo("Not surveyed"));Assert.That(interactions,Is.Zero);
        }
        [Test]public void MissingFoodDemandDoesNotBecomeAFalseSurplusAndFutureControlDateIsNotCurrent()
        {
            var root=Panel(Planet(controlTurn:15),demand:null).Q("world-info");
            Assert.That(root.Q<Label>("world-stat-value-food-demand").text,Is.EqualTo("—"));
            Assert.That(root.Q<Label>("world-food-balance-value").text,Is.EqualTo("Not reported"));
            Assert.That(root.Q("world-food-balance").ClassListContains("world-info-muted"),Is.True);
            Assert.That(root.Q<Label>("world-control-date-chip").text,Is.EqualTo("Date not supplied"));
        }
        [Test]public void ActiveSurveyKeepsIndeterminateDurationAndRealCancelAction()
        {
            var panel=Panel(Planet(activeSurvey:"survey.fixture",done:2),surveyStage:PlanetSurveyStage.Active);var root=panel.Q("world-info");
            Assert.That(root.Q<Label>("world-survey-chip").text,Is.EqualTo("Survey active · 2 weeks done"));
            Assert.That(root.Q("colonisation-survey-indeterminate").tooltip,Does.Contain("duration unavailable").And.Contain("no percentage or ETA"));
            Assert.That(root.Query<ProgressBar>().ToList(),Is.Empty);Assert.That(root.Q<Label>("world-survey-ship-chip").text,Is.EqualTo("Surveyor"));
            Assert.That(panel.Q<Button>("colonisation-survey-cancel"),Is.Not.Null);Assert.That(panel.Q<Button>("colonisation-survey-start-ship.fixture"),Is.Null);Assert.That(interactions,Is.Zero);
        }
        [UnityTest]public IEnumerator ActualStatCellsWrapWithoutOverlapAtNarrowAndWideWidths()
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-sol-asset-tests"))Assert.Ignore("QA must admit the graphics-capable world pane receiving run.");
            var root=Panel(Planet(),population:long.MaxValue).Q("world-info");root.RemoveFromHierarchy();
            window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();window.position=new Rect(80,80,1000,800);window.titleContent=new GUIContent("Compact world-info receiving");window.Show();window.rootVisualElement.Add(root);
            foreach(var width in new[]{320f,780f})
            {
                root.style.width=width;yield return null;yield return null;yield return null;
                var cells=root.Query(className:"world-info-stat").ToList();
                Assert.That(cells.Count,Is.EqualTo(6));
                for(var i=0;i<cells.Count;i++)
                {
                    Assert.That(cells[i].worldBound.width,Is.GreaterThan(0));Assert.That(cells[i].worldBound.xMin,Is.GreaterThanOrEqualTo(root.worldBound.xMin-1));Assert.That(cells[i].worldBound.xMax,Is.LessThanOrEqualTo(root.worldBound.xMax+1));
                    for(var j=0;j<i;j++)Assert.That(cells[i].worldBound.Overlaps(cells[j].worldBound),Is.False);
                }
                Assert.That(root.Q<Label>("world-stat-value-population").tooltip,Does.Contain(long.MaxValue.ToString("N0",System.Globalization.CultureInfo.InvariantCulture)),"Exact full value remains accessible if shortened visually.");
            }
            Assert.That(interactions,Is.Zero);
        }
    }
}
