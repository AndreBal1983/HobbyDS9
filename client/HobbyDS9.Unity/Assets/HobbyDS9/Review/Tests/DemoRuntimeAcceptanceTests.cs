#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    public sealed class DemoRuntimeAcceptanceTests
    {
        private InputSettings.EditorInputBehaviorInPlayMode? oldInputRouting;
        private InputAction? submitAction;
        private Action<InputAction.CallbackContext>? submitTrace;
        [TearDown] public void RestoreInputRouting()
        {
            if(submitAction!=null&&submitTrace!=null)submitAction.performed-=submitTrace;
            submitAction=null;submitTrace=null;
            if(oldInputRouting.HasValue){InputSystem.settings.editorInputBehaviorInPlayMode=oldInputRouting.Value;oldInputRouting=null;}
            foreach(var device in InputSystem.devices.Where(d=>d.name.StartsWith("DemoAcceptance",StringComparison.Ordinal)).ToArray())InputSystem.RemoveDevice(device);
        }
        [UnityTest] public IEnumerator ActualDemoMenuAndPanelsRenderWithoutActorSwitchOrMutation()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-demo-render-tests")<0)Assert.Ignore("Dedicated demo bootstrap run.");
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(entry.Presentation!.Model.SingleplayerDemo,Is.True);
            Assert.That(GameObject.Find("HobbyDS9 Client"),Is.Null);var doc=entry.GetComponent<UIDocument>();
            void Click(string id){var b=doc.rootVisualElement.Q<Button>(id);Assert.That(b,Is.Not.Null,id);Assert.That(b.enabledInHierarchy,Is.True,id);b.Focus();using(var e=NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}}
            var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/singleplayer-demo-ui/rendered"));Directory.CreateDirectory(output);
            IEnumerator Capture(string name)
            {
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                {
                    var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();doc.panelSettings.targetTexture=target;
                    var root=doc.rootVisualElement;root.style.width=size.x;root.style.height=size.y;yield return null;yield return null;yield return null;
                    Assert.That(root.Q("demo-objective").worldBound.yMax,Is.LessThan(root.Q("player-footer").worldBound.yMin));
                    Assert.That(root.Q("player-footer").worldBound.yMax,Is.LessThanOrEqualTo(size.y+1));
                    var prior=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,size.x,size.y),0,0);png.Apply();File.WriteAllBytes(Path.Combine(output,size.x+"x"+size.y+"-"+name+".png"),png.EncodeToPNG());RenderTexture.active=prior;
                    doc.panelSettings.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(png);
                }
            }
            yield return Capture("Start-menu");Click("demo-start");yield return null;
            var model=entry.Presentation!.Model;var frame=model.Current.Frame;Assert.That(frame.Demo!.Outcome,Is.EqualTo(CampaignDemoOutcome.Active));
            foreach(var page in new[]{ReviewWorkspacePage.Map,ReviewWorkspacePage.Worlds,ReviewWorkspacePage.Research,ReviewWorkspacePage.Production,ReviewWorkspacePage.FleetOrders,ReviewWorkspacePage.Saves,ReviewWorkspacePage.Audio,ReviewWorkspacePage.Display,ReviewWorkspacePage.Designer})
            {
                Click("review-nav-"+page);yield return null;yield return Capture(page.ToString());Assert.That(model.Current.Frame,Is.SameAs(frame));
                if(page==ReviewWorkspacePage.Research)Assert.That(doc.rootVisualElement.Query<VisualElement>().ToList().Count(e=>e.name.StartsWith("science-card-")),Is.EqualTo(6));
            }
            Click("review-nav-Tools");yield return null;Assert.That(doc.rootVisualElement.Query<Button>().ToList().Any(b=>b.name.StartsWith("player-switch-")),Is.False);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator InputSystemKeyboardProviderNavigatesAndSubmitsActualModal()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-demo-render-tests")<0)Assert.Ignore("Dedicated actual input provider run.");
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            oldInputRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>("DemoAcceptanceKeyboard");
            doc.rootVisualElement.RegisterCallback<PointerDownEvent>(e=>Debug.Log("INPUT PROBE pointer target="+(e.target as VisualElement)?.name));
            doc.rootVisualElement.RegisterCallback<NavigationSubmitEvent>(_=>Debug.Log("INPUT PROBE navigation submit"));
            Debug.Log("INPUT PROBE screen="+Screen.width+"x"+Screen.height+" root="+doc.rootVisualElement.worldBound+" UIMap="+InputSystem.actions?.FindActionMap("UI")?.enabled+" SubmitControls="+InputSystem.actions?.FindAction("UI/Submit")?.controls.Count);
            submitAction=InputSystem.actions.FindAction("UI/Submit");submitTrace=_=>Debug.Log("INPUT PROBE actual Submit.performed");submitAction.performed+=submitTrace;
            IEnumerator Press(Key key)
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();yield return null;yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();yield return null;yield return null;
            }
            // Focus is test arrangement; activation still traverses the actual keyboard provider.
            // The hidden batch Editor's pointer path is separately unverified.
            doc.rootVisualElement.Q<Button>("demo-start").Focus();yield return Press(Key.Enter);
            Assert.That(doc.rootVisualElement.Q("demo-start"),Is.Null,"Real keyboard provider starts the demo");
            doc.rootVisualElement.Q<Button>("player-primary-action").Focus();yield return Press(Key.Enter);
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Not.Null);
            var focused=doc.rootVisualElement.panel.focusController.focusedElement;yield return Press(Key.Tab);
            Assert.That(doc.rootVisualElement.panel.focusController.focusedElement,Is.Not.SameAs(focused),"Held Tab through actual InputSystem provider changes focus");
            yield return Press(Key.Tab);var turn=entry.Presentation!.Model.Current.Frame.Status.Turn;yield return Press(Key.Enter);
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null,"Held Enter activates the focused actual dialog button");
            Assert.That(entry.Presentation!.Model.Current.Frame.Status.Turn,Is.EqualTo(turn+1),"Confirm submits one real turn, without repeated activation");
            InputSystem.RemoveDevice(keyboard);yield return new ExitPlayMode();
        }
    }
}
