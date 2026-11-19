#nullable enable
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    // Literal disclosed-body fixture; no campaign host, commands or save access.
    public sealed class PlanetPortraitInteractionTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private UnityEditor.EditorWindow? window;private IDisposable? owned;
        private static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private)!.GetValue(target)!;
        private static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Private)!.Invoke(target,args);
        private static ColonisationPlanetSnapshot Earth()=>new ColonisationPlanetSnapshot(new ColonisationPlanetObservationDto{PlanetId="planet.sol",BodyId="body.sol",SystemId="system.fixture",DisplayName="Earth",Surveyed=true,Environment=new PlanetDefinitionDto{PlanetClass="Temperate",Atmosphere="atmosphere.oxygen-nitrogen",Pressure=10000,Gravity=10000}});
        private VisualElement Host()
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-sol-asset-tests"))Assert.Ignore("Dedicated graphics-capable portrait receiving run required.");
            window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();window.titleContent=new GUIContent("Planet portrait interaction fixture");window.position=new Rect(80,80,400,600);window.Show();window.Focus();return window.rootVisualElement;
        }
        private static void Pointer(VisualElement target,EventType type,Vector2 local,int button=0)
        {
            var native=new Event{type=type,button=button,mousePosition=target.LocalToWorld(local)};
            if(type==EventType.MouseDown){using(var e=PointerDownEvent.GetPooled(native)){e.target=target;target.SendEvent(e);}}
            else if(type==EventType.MouseUp){using(var e=PointerUpEvent.GetPooled(native)){e.target=target;target.SendEvent(e);}}
            else {using(var e=PointerMoveEvent.GetPooled(native)){e.target=target;target.SendEvent(e);}}
        }
        [TearDown]public void Cleanup()
        {owned?.Dispose();owned=null;window?.rootVisualElement.Clear();if(window!=null){window.Close();if(window!=null)UnityEngine.Object.DestroyImmediate(window);}window=null;}
        [UnityTest]public IEnumerator ActualGlobeRotatesWithCapturedDragPausesIdleAndReleasesItsOwnedStage()
        {
            var host=Host();var changes=0;var type=typeof(ReviewPlayerView).Assembly.GetType("HobbyDS9.UnityReview.ReviewPlanetPortrait",true)!;
            Assert.That(type.GetConstructor(Private,null,new[]{typeof(ColonisationPlanetSnapshot)},null),Is.Not.Null,"existing constructor retained");
            var view=(VisualElement)Activator.CreateInstance(type,Private,null,new object[]{Earth(),false,(Action<bool>)(_=>changes++)},null)!;owned=(IDisposable)view;
            view.style.width=360;host.Add(view);yield return null;yield return null;
            Call(view,"FocusChanged",true);
            var image=view.Q<Image>("planet-detail-portrait");Assert.That(image.image,Is.Not.Null);
            var viewport=view.Q("planet-portrait-drag-surface");var motion=Field<PlanetPortraitMotion>(view,"motion");var stage=Field<object>(view,"stage");var globe=Field<MeshRenderer>(stage,"portraitGlobe");
            var texture=image.image;var initialRotation=globe.transform.localRotation;var material=globe.sharedMaterial;
            var before=new MaterialPropertyBlock();globe.GetPropertyBlock(before);var surface=before.GetTexture("_Surface");var light=before.GetVector("_StarCenter");
            Pointer(viewport,EventType.MouseDown,new Vector2(100,100));yield return null;
            Assert.That(motion.Dragging,Is.True);Assert.That(viewport.HasPointerCapture(0),Is.True);
            Pointer(viewport,EventType.MouseDrag,new Vector2(180,140));yield return null;yield return null;
            Assert.That(Quaternion.Angle(globe.transform.localRotation,initialRotation),Is.GreaterThan(5),"actual mesh orientation changes");
            Assert.That(image.resolvedStyle.rotate.angle.value,Is.Zero,"the raster Image itself is not rotated");
            var draggedYaw=motion.Yaw;Assert.That(motion.Advance(.2,true),Is.False);Assert.That(motion.Yaw,Is.EqualTo(draggedYaw));
            Assert.That(image.image,Is.SameAs(texture));Assert.That(globe.sharedMaterial,Is.SameAs(material));
            var after=new MaterialPropertyBlock();globe.GetPropertyBlock(after);Assert.That(after.GetTexture("_Surface"),Is.SameAs(surface));Assert.That(after.GetVector("_StarCenter"),Is.EqualTo(light),"illumination stays fixed while the globe turns");
            Pointer(viewport,EventType.MouseUp,new Vector2(180,140));yield return null;Assert.That(motion.Dragging,Is.False);
            var toggle=view.Q<Toggle>("planet-portrait-reduce-motion");toggle.value=true;Assert.That(changes,Is.EqualTo(1));var held=motion.Yaw;
            view.GetType().GetField("previousTick",Private)!.SetValue(view,Time.realtimeSinceStartup-.2f);Call(view,"Tick");yield return null;
            Assert.That(motion.Yaw,Is.EqualTo(held));Assert.That(view.Query<Foldout>().ToList().Any(f=>f.text=="Surface map credits"),Is.False);
            viewport.Focus();using(var key=KeyDownEvent.GetPooled(new Event{type=EventType.KeyDown,keyCode=KeyCode.RightArrow})){key.target=viewport;viewport.SendEvent(key);}yield return null;
            Assert.That(motion.Yaw,Is.Not.EqualTo(held),"reduced motion still permits explicit keyboard rotation");
            toggle.value=false;var idleStart=motion.Yaw;view.GetType().GetField("previousTick",Private)!.SetValue(view,Time.realtimeSinceStartup-.2f);Call(view,"Tick");yield return null;
            Assert.That(motion.Yaw,Is.Not.EqualTo(idleStart),"idle rotation resumes after opting back in");
            Pointer(viewport,EventType.MouseDown,new Vector2(100,100));yield return null;Assert.That(motion.Dragging,Is.True);
            Call(view,"FocusChanged",false);yield return null;Assert.That(motion.Dragging,Is.False);Assert.That(viewport.HasPointerCapture(0),Is.False);
            held=motion.Yaw;view.GetType().GetField("previousTick",Private)!.SetValue(view,Time.realtimeSinceStartup-.2f);Call(view,"Tick");Assert.That(motion.Yaw,Is.EqualTo(held),"losing application focus also pauses automatic rotation");
            var parentClicks=0;host.RegisterCallback<ClickEvent>(_=>parentClicks++);
            using(var click=ClickEvent.GetPooled(new Event{type=EventType.MouseUp,button=0,clickCount=2})){click.target=viewport;viewport.SendEvent(click);}Assert.That(parentClicks,Is.Zero);
            view.RemoveFromHierarchy();Assert.That(image.image,Is.Null);Assert.That(Field<object?>(view,"stage"),Is.Null);Assert.That(Field<object?>(view,"ticker"),Is.Null);
        }
    }
}
