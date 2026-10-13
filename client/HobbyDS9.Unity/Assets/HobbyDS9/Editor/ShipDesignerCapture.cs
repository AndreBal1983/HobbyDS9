using System;
using System.Linq;
using HobbyDS9.UnityClient;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Editor
{
    [InitializeOnLoad]
    public static class ShipDesignerCapture
    {
        private const string AcceptanceKey="HobbyDS9.ShipDesignerAcceptanceCapture";
        private static double acceptanceDeadline;
        static ShipDesignerCapture()
        {
            if(SessionState.GetBool(AcceptanceKey,false))
            {
                acceptanceDeadline=EditorApplication.timeSinceStartup+120;
                EditorApplication.update+=PollAcceptance;
            }
        }
        private static readonly Vector2Int[] Sizes={new Vector2Int(1920,1080),new Vector2Int(3440,1440),new Vector2Int(1280,720)};
        [MenuItem("HobbyDS9/LCARS/Capture ship designer preview fixtures")]
        public static void CaptureFixtures() => CaptureFixtures(()=>{});
        private static void CaptureFixtures(Action completed)
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode and open Ship design from strategic navigation.");
            var entry=UnityEngine.Object.FindAnyObjectByType<ClientEntry>();
            var root=entry==null?null:entry.GetComponent<UIDocument>()?.rootVisualElement;
            var shell=root?.Q<LcarsShell>(); var view=root?.Q<ShipDesignerView>();
            if(shell==null || !shell.DesignerOpen || view==null) throw new InvalidOperationException("Open Ship design before capture.");
            if(view.Draft.Modified || view.Draft.ConfirmationPending) throw new InvalidOperationException("Keep your local edits or explicitly reset them before fixture capture; this tool will not discard a modified draft.");

            var original=view.Draft.TemplateId; var profile=view.Draft.Profile;var system=view.SelectedSystem;
            var cases=new[] {Tuple.Create("class.surveyor","Overview","surveyor-overview"),Tuple.Create("class.escort","Overview","escort-overview"),Tuple.Create("class.escort","Weapons","weapons"),Tuple.Create("class.escort","Power","power"),Tuple.Create("class.escort","Profiles","profiles-combat"),Tuple.Create("class.escort","Profiles","profiles-zero"),Tuple.Create("class.escort","Profiles","profiles-full"),Tuple.Create("class.escort","Profiles","profiles-invalid")};
            void CaptureCase(int index)
            {
                // Only discard the fixture edits this tool creates, after its clean-draft guard.
                if(view.Draft.Modified){view.Draft.RequestReset();view.Draft.ConfirmDiscard();}
                if(index==cases.Length)
                {
                    view.Draft.RequestTemplate(original);view.Draft.SelectProfile(profile);view.SelectSystem(system);
                    Debug.Log("Ship designer sliders: twenty-four real Game view screenshots complete.");completed();return;
                }
                var item=cases[index];view.Draft.RequestTemplate(item.Item1);view.Draft.SelectProfile(item.Item1=="class.surveyor"?DesignerProfile.Survey:DesignerProfile.Combat);view.SelectSystem(item.Item2);
                if(item.Item3=="profiles-zero")view.Draft.SetPower("component.energy-bank.v1",0,0);
                var range=view.Draft.Preview().PowerRanges.FirstOrDefault(r=>r.ComponentId=="component.energy-bank.v1");
                if(item.Item3=="profiles-full" && range!=null){view.Draft.SetPower("component.energy-bank.v1",range.Maximum,range.Maximum-1);view.Draft.SetPower("component.launcher.v1",6,2);}
                if(item.Item3=="profiles-invalid" && range!=null)view.Draft.SetPower("component.energy-bank.v1",range.Maximum,100);
                StrategicPrototypeCapture.CaptureSequence("vertical-lcars-controls",item.Item3,Sizes,Validate,()=>CaptureCase(index+1));
            }
            CaptureCase(0);
        }
        // Optional command-line entry for reproducible real Game view evidence.
        // Uses the ordinary navigation entry and fixture capture; no campaign edits.
        public static void CaptureForAcceptance()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView",true);
            var game=EditorWindow.GetWindow(gameType); game.Show(); game.Focus();
            UnityEngine.Application.runInBackground=true;
            SessionState.SetBool(AcceptanceKey,true);
            acceptanceDeadline=EditorApplication.timeSinceStartup+120;
            EditorApplication.update-=PollAcceptance;
            EditorApplication.update+=PollAcceptance;
            // Let executeMethod return before entering Play mode at Editor startup.
            EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
        }
        private static void PollAcceptance()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>acceptanceDeadline) { SessionState.SetBool(AcceptanceKey,false); EditorApplication.update-=PollAcceptance; Debug.LogError("Designer capture startup timed out"); EditorApplication.Exit(1); return; }
            if(!EditorApplication.isPlaying || Time.frameCount<20)return;
            var entry=UnityEngine.Object.FindAnyObjectByType<ClientEntry>();
            if(entry==null)return;
            EditorApplication.update-=PollAcceptance;
            SessionState.SetBool(AcceptanceKey,false);
            typeof(ClientEntry).GetMethod("RequestStrategicSection",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(entry,new object[] {"Designer"});
            CaptureFixtures(()=>EditorApplication.Exit(0));
        }
        private static void Validate(VisualElement root)
        {
            var shell=root.Q<LcarsShell>(); var view=root.Q<ShipDesignerView>();
            if(shell==null || !shell.DesignerOpen || view==null) throw new InvalidOperationException("Designer closed during capture.");
            foreach(var button in new[] {view.ResetButton,view.CloseButton})
            {
                var bounds=button.worldBound;
                if(bounds.width<80 || bounds.height<40 || !view.worldBound.Contains(bounds.min) || !view.worldBound.Contains(bounds.max))
                    throw new InvalidOperationException("A primary designer action is clipped or undersized.");
            }
            if(!view.Q<Label>("designer-provider-status").text.Contains("Backend validated")) throw new InvalidOperationException("Backend provenance is missing.");
            if(view.Q<Button>("designer-construction").enabledSelf) throw new InvalidOperationException("Construction must remain unavailable.");
            var workspace=view.Q<VisualElement>("designer-workspace");
            if(workspace.worldBound.height<150)throw new InvalidOperationException("The focused workspace has no useful viewport.");
            if(view.SelectedSystem=="Overview" && !view.Q<ShipSystemGraphic>().HasImage)throw new InvalidOperationException("Overview art failed to load.");
            if(view.SelectedSystem!="Profiles" && view.Query<LongField>().ToList().Exists(f=>f.name.StartsWith("power-")))throw new InvalidOperationException("Power editors leaked outside their page.");
            if(view.SelectedSystem=="Profiles")
            {
                var slider=view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1");
                if(slider==null || slider.Track.direction!=SliderDirection.Vertical || slider.Track.inverted || slider.Track.worldBound.width<60 || slider.Track.worldBound.height<120)
                    throw new InvalidOperationException("Slider hit area is missing or undersized.");
                view.Q<ScrollView>("designer-profile-results").ScrollTo(slider.Track);
            }
        }
    }
}
