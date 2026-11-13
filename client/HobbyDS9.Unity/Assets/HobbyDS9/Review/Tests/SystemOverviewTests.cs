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
    // Literal actor-observation fixtures only. These tests do not load a campaign or save.
    public sealed class SystemOverviewTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private UnityEditor.EditorWindow? window;
        private readonly List<IDisposable> ownedViews=new List<IDisposable>();
        private static ColonisationPlanetSnapshot Planet(string id="planet.demo",bool surveyed=true,string kind="Temperate",string name="Example",string atmosphere="atmosphere.oxygen-nitrogen",int pressure=10000,int weeks=0,string? activeSurvey=null)
            =>new ColonisationPlanetSnapshot(new ColonisationPlanetObservationDto{
                PlanetId=id,BodyId="body."+id,SystemId="system.fixture",DisplayName=name,Surveyed=surveyed,CompletedSurveyWeeks=weeks,ActiveSurveyId=activeSurvey,
                Environment=new PlanetDefinitionDto{PlanetClass=kind,Atmosphere=atmosphere,Pressure=pressure,Gravity=10000,Radiation=41}});
        private static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,Private)!.GetValue(value)!;
        private static void Set(object value,string name,object field)=>value.GetType().GetField(name,Private)!.SetValue(value,field);
        private static object? Call(object value,string method,params object[] args)=>value.GetType().GetMethod(method,Private)!.Invoke(value,args);
        private static void RequireGraphicsRun()
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-sol-asset-tests"))
                Assert.Ignore("Root must admit a dedicated graphics-capable Sol receiving run.");
        }
        private VisualElement Host()
        {
            RequireGraphicsRun();
            // Own EditMode dispatcher, not an existing game/window and no PlayMode bootstrap.
            window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            window.titleContent=new GUIContent("System overview event fixture");window.position=new Rect(80,80,940,700);window.Show();
            Assert.That(window.rootVisualElement.panel,Is.Not.Null);
            return window.rootVisualElement;
        }
        [TearDown] public void DisposeOwnedPanel()
        {
            try {foreach(var view in ownedViews)view.Dispose();ownedViews.Clear();window?.rootVisualElement.Clear();}
            finally
            {
                if(window!=null){window.Close();if(window!=null)UnityEngine.Object.DestroyImmediate(window);}window=null;
            }
        }
        private VisualElement Overview(ColonisationPlanetSnapshot[] bodies,Action<ColonisationPlanetSnapshot>? focus=null,Action<ColonisationPlanetSnapshot>? open=null,PlanetActivationScope? activation=null)
        {
            var type=typeof(ReviewPlayerView).Assembly.GetType("HobbyDS9.UnityReview.ReviewSolOverview",true)!;
            var view=(VisualElement)Activator.CreateInstance(type,Private,null,new object[]{bodies,bodies.FirstOrDefault()?.PlanetId??"",0f,1f,
                focus??(_=>{}),(Action<float,float>)((_,__)=>{}),open??(_=>{}),false,(Action<bool>)(_=>{}),activation!},null)!;
            ownedViews.Add((IDisposable)view);return view;
        }
        private static void SampleEqual(PlanetSurfaceSample a,PlanetSurfaceSample b)
        {Assert.That(a.R,Is.EqualTo(b.R));Assert.That(a.G,Is.EqualTo(b.G));Assert.That(a.B,Is.EqualTo(b.B));Assert.That(a.Water,Is.EqualTo(b.Water));}
        [Test] public void UnsurveyedMalformedEnvironmentCannotChooseCanonicalOrClassSpecificAppearance()
        {
            var values=new[]{Planet("planet.sol",false,"Oceanic"),Planet("planet.mars",false,"Vacuum"),Planet("planet.hidden",false,"Volcanic",pressure:90000)};
            var profiles=values.Select(PlanetVisualProfile.From).ToArray();
            foreach(var profile in profiles)
            {
                Assert.That(profile.Kind,Is.EqualTo(PlanetSurfaceKind.Unknown));Assert.That(profile.Seed,Is.Zero);
                Assert.That(profile.AuthoredMap,Is.Null);Assert.That(profile.HasAtmosphere,Is.False);
                Assert.That(profile.AtmosphereStrength,Is.Zero);Assert.That(profile.CloudCoverage,Is.Zero);
                Assert.That(PlanetSurfaceGenerator.Clouds(profile,.6,.8,0),Is.Zero);
                SampleEqual(PlanetSurfaceGenerator.Sample(profile,.6,.8,0),PlanetSurfaceGenerator.Sample(profiles[0],.6,.8,0));
                Assert.That(profile.Caption,Is.EqualTo(profiles[0].Caption));
            }
            var classMissing=PlanetVisualProfile.From(Planet(kind:""));
            Assert.That(classMissing.Kind,Is.EqualTo(PlanetSurfaceKind.Unknown));Assert.That(classMissing.Seed,Is.Zero);
        }
        [TestCase("Temperate",PlanetSurfaceKind.Temperate)]
        [TestCase("Arid",PlanetSurfaceKind.Arid)]
        [TestCase("Oceanic",PlanetSurfaceKind.Oceanic)]
        [TestCase("Frozen",PlanetSurfaceKind.Frozen)]
        [TestCase("Vacuum",PlanetSurfaceKind.Airless)]
        [TestCase("Wetland",PlanetSurfaceKind.Wetland)]
        public void DisclosedGeneratedFamiliesMapExplicitly(string kind,PlanetSurfaceKind expected)
            =>Assert.That(PlanetVisualProfile.From(Planet(kind:kind)).Kind,Is.EqualTo(expected));
        [Test] public void OnlySurveyedCanonicalIdentitiesSelectAuthoredEarthAndMarsMaps()
        {
            Assert.That(PlanetVisualProfile.From(Planet("planet.sol",name:"Renamed home")).AuthoredMap,Is.EqualTo("earth_daymap"));
            Assert.That(PlanetVisualProfile.From(Planet("planet.mars",kind:"Arid",name:"Renamed frontier")).AuthoredMap,Is.EqualTo("mars"));
            foreach(var name in new[]{"Earth","Mars"})Assert.That(PlanetVisualProfile.From(Planet("planet.foreign",name:name)).AuthoredMap,Is.Null);
            Assert.That(PlanetVisualProfile.From(Planet("planet.SOL")).AuthoredMap,Is.Null,"Canonical identity is exact, not a display-name/case heuristic.");
        }
        [Test] public void ReloadRenameAndSurveyWeekChangesRetainSeededTerrain()
        {
            var a=PlanetVisualProfile.From(Planet(name:"Before",weeks:0));
            var b=PlanetVisualProfile.From(Planet(name:"After",weeks:9));
            var other=PlanetVisualProfile.From(Planet("planet.other"));
            Assert.That(a.Seed,Is.EqualTo(b.Seed));Assert.That(a.Seed,Is.Not.EqualTo(other.Seed));
            var points=new[]{new Vector3(.6f,.8f,0),new Vector3(0,0,1),new Vector3(-1,0,0)};
            var differs=false;
            foreach(var point in points)
            {
                var first=PlanetSurfaceGenerator.Sample(a,point.x,point.y,point.z);
                SampleEqual(first,PlanetSurfaceGenerator.Sample(b,point.x,point.y,point.z));
                var changed=PlanetSurfaceGenerator.Sample(other,point.x,point.y,point.z);
                differs|=first.R!=changed.R||first.G!=changed.G||first.B!=changed.B;
            }
            Assert.That(differs,Is.True,"Distinct observed bodies must not all share an accidentally constant surface.");
        }
        [Test] public void VacuumAndMissingPressureCannotEnableCloudsOrAtmosphere()
        {
            foreach(var value in new[]{Planet(kind:"Vacuum"),Planet(atmosphere:"atmosphere.vacuum"),Planet(pressure:0)})
            {
                var profile=PlanetVisualProfile.From(value);Assert.That(profile.HasAtmosphere,Is.False);
                Assert.That(profile.AtmosphereStrength,Is.Zero);Assert.That(PlanetSurfaceGenerator.Clouds(profile,0,1,0),Is.Zero);
            }
        }
        // Direct normalized UI events test the real registered callbacks. The OS/provider
        // click classifier, Enter translation and physical target delivery remain native checks.
        private static void Click(VisualElement target,int count,int button=0,int pointerId=0)
        {
            using(var e=ClickEvent.GetPooled(new Event{type=EventType.MouseUp,button=button,clickCount=count}))
            {
                // Supply a valid alternate-pointer payload without invoking the OS/provider.
                typeof(ClickEvent).GetProperty("pointerId")!.SetValue(e,pointerId);
                e.target=target;target.SendEvent(e);
            }
        }
        private static void Space(VisualElement target)
        {
            using(var e=KeyDownEvent.GetPooled(new Event{type=EventType.KeyDown,keyCode=KeyCode.Space}))
            {e.target=target;target.SendEvent(e);}
        }
        private static void Submit(VisualElement target)
        {using(var e=NavigationSubmitEvent.GetPooled()){e.target=target;target.SendEvent(e);}}
        [Test] public void MountedCardsKeepIdentityThroughFocusOpenKeyboardAndRejectedInputs()
        {
            var host=Host();var focus=new List<string>();var opened=new List<string>();
            var a=Planet("planet.a");var b=Planet("planet.b");var activation=new PlanetActivationScope(host);
            PlanetBodyCard? first=null,second=null;
            void Focus(string id){focus.Add(id);first!.SetSelected(id==a.PlanetId);second!.SetSelected(id==b.PlanetId);}
            first=new PlanetBodyCard(a,null,false,()=>Focus(a.PlanetId),()=>opened.Add(a.PlanetId),activation);
            second=new PlanetBodyCard(b,"Founding camp",false,()=>Focus(b.PlanetId),()=>opened.Add(b.PlanetId),activation);
            host.Add(first);host.Add(second);var initialChildren=first.Children().ToArray();
            Click(first,1);Assert.That(focus,Is.EqualTo(new[]{a.PlanetId}));Assert.That(opened,Is.Empty);
            Assert.That(host.Q<PlanetBodyCard>(first.name),Is.SameAs(first));Assert.That(first.Children().ToArray(),Is.EqualTo(initialChildren));
            Click(first,2,1);Assert.That(opened,Is.Empty,"Right double-click must be inert.");
            Click(second,1);Assert.That(opened,Is.Empty,"Different-body first clicks do not open a planet.");
            Click(second,2);Assert.That(opened,Is.EqualTo(new[]{b.PlanetId}));
            Assert.That(host.Q<PlanetBodyCard>(second.name),Is.SameAs(second));
            var priorFocus=focus.Count;Space(first);Assert.That(focus.Count,Is.EqualTo(priorFocus+1));Assert.That(focus.Last(),Is.EqualTo(a.PlanetId));
            Assert.That(opened.Count,Is.EqualTo(1),"Space focuses without navigation.");
            Submit(first);Assert.That(opened,Is.EqualTo(new[]{b.PlanetId,a.PlanetId}));
            second.SetEnabled(false);var accepted=focus.Count;Click(second,2);Submit(second);Space(second);
            Assert.That(focus.Count,Is.EqualTo(accepted));Assert.That(opened.Count,Is.EqualTo(2));
            first.RemoveFromHierarchy();Click(first,2);Submit(first);Space(first);
            Assert.That(focus.Count,Is.EqualTo(accepted));Assert.That(opened.Count,Is.EqualTo(2),"Detached controls cannot invoke callbacks.");
        }
        [Test] public void OverviewCreatesOnlyDisclosedMarkersAndFocusDoesNotReplaceThem()
        {
            var host=Host();var a=Planet("planet.a",false);var b=Planet("planet.b",false);
            var view=Overview(new[]{a,b});host.Add(view);
            var markers=Field<Dictionary<string,VisualElement>>(view,"markers");
            Assert.That(markers.Keys,Is.EquivalentTo(new[]{a.PlanetId,b.PlanetId}));
            Assert.That(view.Q("sol-body-planet.sol"),Is.Null);Assert.That(view.Q("sol-body-planet.mars"),Is.Null);
            var original=markers[a.PlanetId];Call(view,"Select",a.PlanetId);
            Assert.That(markers[a.PlanetId],Is.SameAs(original));Assert.That(original.parent,Is.SameAs(view.Q("sol-viewport")));
        }
        private static void Down(VisualElement target,int button=0)
        {
            using(var e=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=button}))
            {e.target=target;target.SendEvent(e);}
        }
        [Test] public void NativeTwoAloneAndMismatchedPointerOnlyFocusUntilARealFirstClickExists()
        {
            var host=Host();var focus=0;var opens=0;var scope=new PlanetActivationScope(host);
            var card=new PlanetBodyCard(Planet(),null,false,()=>focus++,()=>opens++,scope);host.Add(card);
            Click(card,2);Assert.That(focus,Is.EqualTo(1));Assert.That(opens,Is.Zero);
            Click(card,1,pointerId:0);Click(card,2,pointerId:1);Assert.That(opens,Is.Zero,"A second pointer cannot complete the first pointer's chain.");
            Click(card,2,pointerId:0);Assert.That(opens,Is.Zero,"Rejected completion consumes the chain.");
            Click(card,1,pointerId:0);Click(card,2,pointerId:0);Assert.That(opens,Is.EqualTo(1));
            Click(card,2,pointerId:0);Assert.That(opens,Is.EqualTo(1),"A consumed chain cannot open again.");
        }
        [Test] public void SceneAndListShareOneElementSpecificChainEvenForTheSamePlanet()
        {
            var host=Host();var scope=new PlanetActivationScope(host);var opened=new List<string>();
            var planet=Planet();var card=new PlanetBodyCard(planet,null,false,()=>{},()=>opened.Add("list"),scope);host.Add(card);
            var scene=new VisualElement{name="scene-target"};host.Add(scene);
            typeof(PlanetBodyCard).GetMethod("BindActivation",BindingFlags.Static|BindingFlags.NonPublic)!
                .Invoke(null,new object[]{scene,(Action)(()=>{}),(Action)(()=>opened.Add("scene")),scope});
            Click(card,1);Down(scene);Click(scene,2);Assert.That(opened,Is.Empty,"Same planet identity is not the same mounted target.");
            Click(scene,1);Down(card);Click(card,2);Assert.That(opened,Is.Empty);
            Click(scene,1);Down(scene);Click(scene,2);Assert.That(opened,Is.EqualTo(new[]{"scene"}));
        }
        [TestCase("right")]
        [TestCase("wheel")]
        [TestCase("cancel")]
        [TestCase("keyboard")]
        [TestCase("detach")]
        [TestCase("other-target")]
        public void InterveningInputCancelsTheSharedClickChain(string interruption)
        {
            var host=Host();var scope=new PlanetActivationScope(host);var opens=0;
            var card=new PlanetBodyCard(Planet(),null,false,()=>{},()=>opens++,scope);host.Add(card);
            var other=new VisualElement();host.Add(other);Click(card,1);
            switch(interruption)
            {
                case "right":Down(card,1);break;
                case "wheel":using(var e=WheelEvent.GetPooled(new Event{type=EventType.ScrollWheel,delta=Vector2.up})){e.target=card;card.SendEvent(e);}break;
                case "cancel":using(var e=PointerCancelEvent.GetPooled()){e.target=card;card.SendEvent(e);}break;
                case "keyboard":Space(card);break;
                case "detach":card.RemoveFromHierarchy();host.Add(card);break;
                case "other-target":Down(other);break;
                default:Assert.Fail("Unknown fixture interruption");break;
            }
            Click(card,2);Assert.That(opens,Is.Zero,interruption);
            Click(card,1);Click(card,2);Assert.That(opens,Is.EqualTo(1),"A fresh complete chain must still work.");
        }
        [Test] public void ViewControlsCancelTheChainSharedWithTheList()
        {
            var host=Host();var scope=new PlanetActivationScope(host);var opens=0;
            var p=Planet(surveyed:false);var card=new PlanetBodyCard(p,null,false,()=>{},()=>opens++,scope);host.Add(card);
            var view=Overview(new[]{p},activation:scope);host.Add(view);
            Click(card,1);Call(view,"SetView",25f,1f);Click(card,2);Assert.That(opens,Is.Zero);
            Click(card,1);view.Q<Toggle>("system-reduce-motion").value=true;Click(card,2);Assert.That(opens,Is.Zero);
            Click(card,1);Click(card,2);Assert.That(opens,Is.EqualTo(1));
        }
        private ReviewColonisationPanel PanelFor(ColonisationPlanetSnapshot planet,int? scale=null,bool own=false,PlanetSurveyStage? recorded=null)
        {
            var worlds=own?new[]{new ColonisationWorldSnapshot(id:"colony.fixture",planet:planet.PlanetId,system:planet.SystemId,name:"Own world",
                stage:ColonyEstablishmentStage.Established,camp:null,people:100,food:20,industry:9,research:7,output:4,work:0,fed:0,shortage:0,deaths:0,report:"",
                stocks:Array.Empty<ResourceAmountSnapshot>(),cohorts:Array.Empty<ColonisationCohortSnapshot>())}:Array.Empty<ColonisationWorldSnapshot>();
            var surveys=recorded.HasValue?new[]{new ColonisationSurveySnapshot("survey.fixture","ship.fixture",planet.PlanetId,recorded.Value,1)}:Array.Empty<ColonisationSurveySnapshot>();
            var observation=new ColonisationCampaignSnapshot("actor.fixture",1,1,"fixture-not-a-package",worlds,Array.Empty<ColonisationTransportSnapshot>(),
                Array.Empty<ColonisationQueueSnapshot>(),Array.Empty<ColonisationPresenceSnapshot>(),Array.Empty<ColonisationManifestReport>(),surveys,
                Array.Empty<ColonisationIntentSnapshot>(),1,Array.Empty<ResourceAmountSnapshot>());
            var activities=new CampaignActivityObservationDto{ActorId="actor.fixture",Turn=1,Revision=1};
            activities.Ships.Add(new ActivityOwnShipDto{ShipId="ship.fixture",NodeId="node.fixture",DisplayName="Surveyor",Stationary=true,StudyWork=1});
            var nodes=new[]{planet}.ToLookup(p=>p.SystemId,_=>"node.fixture",StringComparer.Ordinal);
            var panel=new ReviewColonisationPanel(observation,new[]{planet},activities,(_,__)=>Assert.Fail("Inspection cannot review an order"),
                _=>Assert.Fail("Inspection cannot submit an order"),(_,__)=>Assert.Fail("Inspection cannot review an activity"),
                _=>Assert.Fail("Inspection cannot submit an activity"),systemNodes:nodes,environmentScale:scale);
            ownedViews.Add(panel);Call(panel,"Show","Planet");return panel;
        }
        private static string Labels(VisualElement element)=>string.Join("\n",element.Query<Label>().ToList().Select(l=>l.text));
        [Test] public void UnknownEnvironmentScaleRetainsObservedNumbersWithoutInventedUnits()
        {
            var text=Labels(PanelFor(Planet()));
            Assert.That(text,Does.Contain("Gravity 10000 · Pressure 10000 (measurement units unavailable)"));
            Assert.That(text,Does.Not.Contain("Gravity 1 g"));
            text=Labels(PanelFor(Planet(),10000));
            Assert.That(text,Does.Contain("Gravity 1 g · Pressure 1 (reference scale)"));
        }
        [TestCase(0)] [TestCase(-10000)]
        public void InvalidEnvironmentScaleIsRejected(int scale)
            =>Assert.Throws<ArgumentOutOfRangeException>(()=>PanelFor(Planet(),scale));
        [TestCase(false)] [TestCase(true)]
        public void EnvironmentKnownOrOwnWorldDoesNotImplyACompletedSurvey(bool own)
        {
            var panel=PanelFor(Planet(),own:own);
            Assert.That(Labels(panel),Does.Contain("Environment known · no completed survey recorded"));
            Assert.That(Labels(panel),Does.Not.Contain("Survey complete"));
            Assert.That(panel.Q<Button>("colonisation-survey-start-ship.fixture"),Is.Not.Null,"Known environment must not remove a legally available survey action.");
        }
        [TestCase(PlanetSurveyStage.Completed)]
        [TestCase(PlanetSurveyStage.Cancelled)]
        [TestCase(PlanetSurveyStage.Interrupted)]
        public void SurveyStatusAndAvailabilityFollowTheActualSurveyRecord(PlanetSurveyStage stage)
        {
            var panel=PanelFor(Planet(),recorded:stage);var completed=stage==PlanetSurveyStage.Completed;
            Assert.That(panel.Q<Button>("colonisation-survey-start-ship.fixture")==null,Is.EqualTo(completed));
            if(completed)Assert.That(Labels(panel),Does.Contain("Survey complete"));
            else {Assert.That(Labels(panel),Does.Not.Contain("Survey complete"));Assert.That(Labels(panel),Does.Contain("Survey: "+stage));}
        }
        [Test] public void ActiveSurveyUsesIndeterminateProgressAndExposesOnlyCancelReview()
        {
            var panel=PanelFor(Planet(activeSurvey:"survey.fixture",weeks:1),recorded:PlanetSurveyStage.Active);
            Assert.That(panel.Q("colonisation-survey-indeterminate"),Is.Not.Null);
            Assert.That(Labels(panel),Does.Contain("duration unavailable"));Assert.That(Labels(panel),Does.Contain("Surveyor"));
            Assert.That(panel.Q<Button>("colonisation-survey-start-ship.fixture"),Is.Null);
            Assert.That(panel.Q<Button>("colonisation-survey-cancel"),Is.Not.Null);
        }
        [UnityTest] public IEnumerator InterruptedShortestPathRotationMovesActualTransformsWithoutChangingRadiiOrTextures()
        {
            var host=Host();var a=Planet("planet.a",false);var b=Planet("planet.b",false);var c=Planet("planet.c",false);
            var view=Overview(new[]{a,b,c});host.Add(view);yield return null;yield return null;
            var viewport=view.Q("sol-viewport");Assert.That(viewport.layout.width,Is.GreaterThan(80));Assert.That(viewport.layout.height,Is.GreaterThan(80));
            Call(view,"SetView",-179f,1f);Call(view,"Select",b.PlanetId);Call(view,"Draw");
            Assert.That(Field<bool>(view,"failed"),Is.False,"Graphics fixture must render, not silently exercise fallback.");
            var stage=Field<object>(view,"stage");var transforms=Field<Dictionary<string,Transform>>(stage,"planetTransforms");
            var original=transforms.ToDictionary(x=>x.Key,x=>x.Value.localPosition,StringComparer.Ordinal);
            var textureCount=Field<List<Texture2D>>(stage,"ownedTextures").Count;
            var delta=Field<float>(view,"targetYaw")-Field<float>(view,"startYaw");
            Assert.That(delta,Is.GreaterThan(0).And.LessThan(40),"The actual implementation must cross the wrap boundary by the short arc.");
            Set(view,"animationStart",Time.realtimeSinceStartup-.6f);Call(view,"Draw");
            var interruptedYaw=Field<float>(view,"yaw");
            Call(view,"Select",a.PlanetId);Call(view,"Draw");
            Assert.That(Field<float>(view,"startYaw"),Is.EqualTo(interruptedYaw).Within(.001f),"New selection starts from the actual displayed intermediate pose.");
            var interruptedDelta=Field<float>(view,"targetYaw")-Field<float>(view,"startYaw");
            Assert.That(interruptedDelta,Is.LessThan(0).And.GreaterThan(-180));
            // Test-owned clock arrangement exercises Draw's production completion path;
            // this does not measure real-time frame cadence or animation performance.
            Set(view,"animationStart",Time.realtimeSinceStartup-2);Call(view,"Draw");
            Assert.That(Field<bool>(view,"animating"),Is.False);
            var root=Field<GameObject>(stage,"root");var offset=transforms[a.PlanetId].position-root.transform.position;
            Assert.That(Mathf.Abs(offset.x),Is.LessThan(.001f));Assert.That(offset.z,Is.LessThan(0),"Final body is on the actual near-side meridian.");
            foreach(var entry in transforms)
            {
                Assert.That(entry.Value.localPosition,Is.EqualTo(original[entry.Key]));
                Assert.That(Vector3.Distance(entry.Value.position,root.transform.position),Is.EqualTo(original[entry.Key].magnitude).Within(.001f));
            }
            Assert.That(Field<List<Texture2D>>(stage,"ownedTextures").Count,Is.EqualTo(textureCount),"Selection/animation must not regenerate terrain.");
            var marker=Field<Dictionary<string,VisualElement>>(view,"markers")[c.PlanetId];
            view.Q<Toggle>("system-reduce-motion").value=true;Call(view,"Select",c.PlanetId);Call(view,"Draw");
            Assert.That(Field<bool>(view,"animating"),Is.False);
            offset=transforms[c.PlanetId].position-root.transform.position;
            Assert.That(Mathf.Abs(offset.x),Is.LessThan(.001f));Assert.That(offset.z,Is.LessThan(0));
            Assert.That(Field<Dictionary<string,VisualElement>>(view,"markers")[c.PlanetId],Is.SameAs(marker));
        }
    }
}