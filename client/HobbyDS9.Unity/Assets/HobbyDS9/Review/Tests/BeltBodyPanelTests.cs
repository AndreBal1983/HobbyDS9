#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    public sealed class BeltBodyPanelTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private static BeltBodySnapshot Belt(bool current,BeltBodyObservationDto? observation=null)=>new BeltBodySnapshot(
            new BeltBodyDefinitionDto{BodyId="body.belt.fixture",FeatureId="belt.fixture",SystemId="system.fixture",DisplayName="Fixture belt",InitialDeposit=999,HabitabilityBps=0},
            current,observation,null,null,Array.Empty<string>(),Array.Empty<string>());
        private static ReviewColonisationPanel Panel(BeltBodiesSnapshot snapshot,Action<BeltBodyCommandDto,string>? review=null)
        {
            var observation=new ColonisationCampaignSnapshot("actor.fixture",5,9,"fixture-not-a-package",Array.Empty<ColonisationWorldSnapshot>(),Array.Empty<ColonisationTransportSnapshot>(),
                Array.Empty<ColonisationQueueSnapshot>(),Array.Empty<ColonisationPresenceSnapshot>(),Array.Empty<ColonisationManifestReport>(),Array.Empty<ColonisationSurveySnapshot>(),
                Array.Empty<ColonisationIntentSnapshot>(),0,Array.Empty<ResourceAmountSnapshot>());
            var activities=new CampaignActivityObservationDto{ActorId="actor.fixture",Turn=5,Revision=9};
            activities.Ships.Add(new ActivityOwnShipDto{ShipId="ship.fixture",DisplayName="Surveyor",NodeId="node.fixture",Stationary=true,StudyWork=1});
            return new ReviewColonisationPanel(observation,Array.Empty<ColonisationPlanetSnapshot>(),activities,
                (_,__)=>Assert.Fail("Selection must not review a planet order."),_=>Assert.Fail("Selection must not submit a planet order."),
                (_,__)=>Assert.Fail("Selection must not review construction."),_=>Assert.Fail("Selection must not submit construction."),
                beltBodies:snapshot,reviewBelt:review,supportsBeltBodies:true);
        }
        private static void Inspect(ReviewColonisationPanel panel)=>typeof(ReviewColonisationPanel).GetMethod("InspectSystem",Flags)!.Invoke(panel,new object[]{"system.fixture","Known system"});
        [Test] public void DisclosedBeltOnlySystemHasDistinctBodyAndUnknownValuesWithoutOrders()
        {
            var reviewed=0;
            using var panel=Panel(new BeltBodiesSnapshot("actor.fixture",5,9,new[]{Belt(true)}),(_,__)=>reviewed++);
            Inspect(panel);
            Assert.That(panel.Q<Button>("belt-body-card-body.belt.fixture"),Is.Not.Null);
            Assert.That(panel.Q<Label>("belt-body-kind").text,Is.EqualTo("Asteroid belt aggregate"));
            Assert.That(panel.Q<Label>("belt-habitability-value").text,Is.EqualTo("Not surveyed"));
            Assert.That(panel.Q<Label>("world-stat-value-belt-ore").text,Is.EqualTo("—"));
            Assert.That(panel.Query<Label>().ToList().Any(l=>l.text.Contains("999")),Is.False);
            Assert.That(panel.Q<Button>("belt-survey-start").enabledSelf,Is.False);
            Assert.That(reviewed,Is.Zero);
        }
        [Test] public void RetainedZeroReportHasItsOwnDateAndDoesNotBecomeUnknownOrCurrent()
        {
            var body=Belt(false,new BeltBodyObservationDto{BodyId="body.belt.fixture",ActorId="actor.fixture",SurveyedTurn=2,ObservedTurn=4,ObservedRemaining=0});
            using var panel=Panel(new BeltBodiesSnapshot("actor.fixture",5,9,new[]{body}));Inspect(panel);
            Assert.That(panel.Q<Label>("world-stat-value-belt-ore").text,Is.EqualTo("0"));
            Assert.That(panel.Q<Label>("belt-resource-stamp").text,Is.EqualTo("Last reported · week 4"));
            Assert.That(panel.Q<Label>("belt-habitability-value").text,Is.EqualTo("0%"));
            Assert.That(panel.Q<Label>("belt-settlement-status").text,Is.EqualTo("Settlement unavailable"));
        }
        [TestCase("other.actor",5,9)] [TestCase("actor.fixture",4,9)] [TestCase("actor.fixture",5,8)]
        public void ForeignOrDifferentFrameCaptureCannotSupplyBodyDetails(string actor,int turn,long revision)
        {
            using var panel=Panel(new BeltBodiesSnapshot(actor,turn,revision,new[]{Belt(true)}));Inspect(panel);
            Assert.That(panel.Q("belt-body-details"),Is.Null);
            Assert.That(panel.Query<Button>().ToList().Any(b=>b.name.StartsWith("belt-body-card-",StringComparison.Ordinal)),Is.False);
            Assert.That(panel.Q<Label>("celestial-body-availability").text,Does.Contain("temporarily unavailable"));
        }
        private static BeltBodySnapshot NamedBelt(string id,string system)=>new BeltBodySnapshot(
            new BeltBodyDefinitionDto{BodyId=id,FeatureId="feature."+id,SystemId=system,DisplayName=id=="body.first"?"First belt":"Second belt"},
            true,null,null,null,Array.Empty<string>(),new[]{"ship.fixture"});
        // Model a retained button callback already queued before a view rebuild.
        private static void InvokeRetained(Button button)=>typeof(Clickable).GetMethod("Invoke",Flags)!.Invoke(button.clickable,new object?[]{null});
        [Test] public void RetiredBodyCardCannotReplaceTheCurrentSystemSelection()
        {
            var rows=new[]{NamedBelt("body.first","system.fixture"),NamedBelt("body.second","system.other")};
            using var panel=Panel(new BeltBodiesSnapshot("actor.fixture",5,9,rows));Inspect(panel);
            var retired=panel.Q<Button>("belt-body-card-body.first");
            typeof(ReviewColonisationPanel).GetMethod("InspectSystem",Flags)!.Invoke(panel,new object[]{"system.other","Other system"});
            var capture=typeof(ReviewColonisationPanel).GetMethod("CaptureSelection",Flags)!;
            var before=capture.Invoke(panel,null)!;InvokeRetained(retired);var after=capture.Invoke(panel,null)!;
            Assert.That(before.GetType().GetField("Body",Flags)!.GetValue(before),Is.EqualTo(after.GetType().GetField("Body",Flags)!.GetValue(after)));
            Assert.That(after.GetType().GetField("SelectedSystem",Flags)!.GetValue(after),Is.EqualTo("system.other"));
        }
        [Test] public void RetiredBodyDetailActionCannotReviewAnOrderAfterAnotherBodyIsSelected()
        {
            var rows=new[]{NamedBelt("body.first","system.fixture"),NamedBelt("body.second","system.fixture")};var reviewed=0;
            using var panel=Panel(new BeltBodiesSnapshot("actor.fixture",5,9,rows),(_,__)=>reviewed++);Inspect(panel);
            var retired=panel.Q<Button>("belt-survey-start");Assert.That(retired.enabledSelf,Is.True);
            InvokeRetained(panel.Q<Button>("belt-body-card-body.second"));
            Assert.That(panel.Q<Label>("belt-body-name").text,Is.EqualTo("Second belt"));
            InvokeRetained(retired);Assert.That(reviewed,Is.Zero);
        }
        [TestCase(BeltMineStage.Queued,true)] [TestCase(BeltMineStage.Active,false)]
        public void CancellationAffordanceMatchesTheQueuedOnlyBackendAction(BeltMineStage stage,bool available)
        {
            var mine=new BeltMineSnapshot(new BeltMineDto{InstallationId="mine.fixture",QueueItemId="queue.fixture",ColonyId="colony.fixture",Stage=stage},0,0);
            var body=new BeltBodySnapshot(new BeltBodyDefinitionDto{BodyId="body.mine",FeatureId="belt.mine",SystemId="system.fixture",DisplayName="Mine belt"},
                true,null,null,mine,Array.Empty<string>(),Array.Empty<string>());
            using var panel=Panel(new BeltBodiesSnapshot("actor.fixture",5,9,new[]{body}),(_,__)=>{});Inspect(panel);
            Assert.That(panel.Q<Button>("belt-mine-cancel")!=null,Is.EqualTo(available));
        }
        [Test] public void PanelSelectionRestoresBeltKindWithoutCreatingAPlanet()
        {
            var snapshot=new BeltBodiesSnapshot("actor.fixture",5,9,new[]{Belt(true)});
            using var first=Panel(snapshot);Inspect(first);
            var saved=typeof(ReviewColonisationPanel).GetMethod("CaptureSelection",Flags)!.Invoke(first,null);
            using var next=Panel(snapshot);typeof(ReviewColonisationPanel).GetMethod("RestoreSelection",Flags)!.Invoke(next,new[]{saved});
            Assert.That(next.Q("belt-body-details"),Is.Not.Null);
            Assert.That(next.Query<PlanetBodyCard>().ToList(),Is.Empty);
        }
        [Test] public void EmptyRegionInspectionDoesNotFallBackToAnotherDisclosedSystem()
        {
            using var panel=Panel(new BeltBodiesSnapshot("actor.fixture",5,9,new[]{Belt(true)}));
            typeof(ReviewColonisationPanel).GetMethod("InspectSystem",Flags)!.Invoke(panel,new object?[]{null,"Empty region"});
            Assert.That(panel.Q("belt-body-details"),Is.Null);
            Assert.That(panel.Q<Label>("celestial-body-availability").text,Is.EqualTo("No celestial bodies have been disclosed here."));
            Assert.That(panel.Query<Button>().ToList().Any(b=>b.name.StartsWith("belt-body-card-",StringComparison.Ordinal)),Is.False);
        }
    }
}
