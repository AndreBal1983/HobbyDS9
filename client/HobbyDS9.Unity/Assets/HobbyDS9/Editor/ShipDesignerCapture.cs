using System;
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

            var original=view.Draft.TemplateId; var profile=view.Draft.Profile;
            view.Draft.RequestTemplate("class.surveyor"); view.Draft.SelectProfile(DesignerProfile.Survey);
            StrategicPrototypeCapture.CaptureSequence("ship-designer-prototype","surveyor",Sizes,Validate,()=>
            {
                view.Draft.RequestTemplate("class.escort"); view.Draft.SelectProfile(DesignerProfile.Combat);
                StrategicPrototypeCapture.CaptureSequence("ship-designer-prototype","escort",Sizes,Validate,()=>
                {
                    view.Draft.RequestTemplate(original); view.Draft.SelectProfile(profile);
                    Debug.Log("Ship designer: six real Game view screenshots complete. Inspect and perform native interaction checks before accepting.");
                    completed();
                });
            });
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
            EditorApplication.isPlaying=true;
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
        }
    }
}
