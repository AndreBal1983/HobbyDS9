using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = HobbyDS9.Contracts.EntityId;

namespace HobbyDS9.UnityClient.Editor
{
    // Optional acceptance only: normal campaign turns/commands, real Game-view
    // screenshots, and test-owned checkpoint files. No stock/Science grants.
    [InitializeOnLoad]
    public static class EngineeringTreeCapture
    {
        private const string Key = "HobbyDS9.EngineeringTreeCapture";
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly EntityId Actor = new EntityId("civ.federation");
        private static readonly Vector2Int[] Sizes = { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(3440,1440) };
        private static ClientEntry entry;
        private static CampaignHost host;
        private static double deadline;
        private static string Output => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../..", "artifacts/unity/engineering-tree"));
        static EngineeringTreeCapture()
        {
            if (SessionState.GetBool(Key, false)) { deadline = EditorApplication.timeSinceStartup + 180; EditorApplication.update += Start; }
        }
        public static void CaptureForAcceptance()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView", true));
            game.Show(); game.Focus(); UnityEngine.Application.runInBackground = true;
            SessionState.SetBool(Key, true); deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= Start; EditorApplication.update += Start;
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Start()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > deadline) { Fail("Engineering capture startup timed out."); return; }
            if (!EditorApplication.isPlaying || Time.frameCount < 20) return;
            entry = UnityEngine.Object.FindAnyObjectByType<ClientEntry>(); if (entry == null) return;
            EditorApplication.update -= Start; SessionState.SetBool(Key, false);
            deadline = EditorApplication.timeSinceStartup + 900; EditorApplication.update += Watch;
            try
            {
                Directory.CreateDirectory(Output);
                host = (CampaignHost)typeof(ClientEntry).GetField("host", Flags).GetValue(entry);
                Section(); Capture("initial-top", "engineering.advanced-starship-engineering", () =>
                    Capture("initial-last", "engineering.resilient-shield", ProgressNaturally));
            }
            catch (Exception error) { Fail(error.ToString()); }
        }
        private static ResearchOverviewSnapshot Research => ResearchOverviewSnapshot.For(host.Snapshot, Actor);
        private static void Section() => typeof(ClientEntry).GetMethod("RequestStrategicSection", Flags).Invoke(entry, new object[] { "Research" });
        private static void CompleteTurn()
        {
            Require(host.EndTurn().Status != TurnStatus.Rejected, "Normal campaign turn rejected.");
            if (host.Snapshot.Campaign.Phase == SessionPhase.AwaitingCombat)
            {
                foreach (var request in host.Pending())
                    Require(host.Resolve(request).Status == CombatSubmissionStatus.Buffered, "Normal phased combat rejected.");
                Require(host.FinalizeCombats().Status == TurnStatus.Completed, "Campaign finalization rejected.");
            }
            Require(host.Snapshot.Campaign.Phase == SessionPhase.Planning, "Turn did not return to Planning.");
        }
        private static void ProgressNaturally()
        {
            for (var i = 0; i < 300 && !Research.Engineering.Single(c => c.Definition.DefinitionId.Value == "engineering.efficient-drive").CanTarget; i++) CompleteTurn();
            Require(Research.Engineering.Single(c => c.Definition.DefinitionId.Value == "engineering.efficient-drive").CanTarget, "Engine project did not become available through Science.");
            File.WriteAllText(Path.Combine(Output, "native-available.json"), host.SaveJson());
            Section(); Capture("available-engine", "engineering.efficient-drive", CompleteEngine);
        }
        private static void Click(string name)
        {
            var button = entry.GetComponent<UIDocument>().rootVisualElement.Q<Button>(name);
            Require(button != null && button.enabledInHierarchy, "Real Engineering action unavailable: " + name);
            typeof(Clickable).GetMethod("Invoke", Flags).Invoke(button.clickable, new object[] { null });
        }
        private static void CompleteEngine()
        {
            Click("target-engineering.efficient-drive");
            Require(Research.Target.HasValue && Research.Target.Value.Value == "engineering.efficient-drive", "Target button did not submit.");
            CompleteTurn();
            for (var i = 0; i < 100 && Research.Target.HasValue; i++) CompleteTurn();
            Require(Research.Engineering.Single(c => c.Definition.DefinitionId.Value == "engineering.efficient-drive").MaturityPercent == 100, "Engine completion missing.");
            Require(host.Client.ShipDesignCatalogue(Actor).AvailableVersionIds.Contains("component.propulsion.v2"), "Completed engine version unavailable.");
            var saved = host.SaveJson(); host.LoadJson(saved); Require(host.SaveJson() == saved, "Completion save/load changed state.");
            File.WriteAllText(Path.Combine(Output, "native-completed.json"), saved);
            Section(); Capture("completed-engine", "engineering.efficient-drive", PartialAndRetarget);
        }
        private static void PartialAndRetarget()
        {
            Require(host.Client.AllocateResearch(host.Snapshot, Actor, 600).Accepted, "Could not temporarily bank all new R&D as Science.");
            // Spend an existing bank only on real available projects until a later
            // project needs more work than that bank. Never edit reserves or levels.
            for (var i = 0; i < 300 && !Research.Engineering.Any(c => c.CanTarget && c.Cost > Research.EngineeringReserve + 720); i++)
            {
                var affordable = Research.Engineering.Where(c => c.CanTarget && c.Cost <= Research.EngineeringReserve)
                    .OrderBy(c => c.Cost).FirstOrDefault();
                if (affordable != null)
                    Require(host.Client.Engineering(host.Snapshot, Actor, affordable.Definition.DefinitionId).Accepted, "Could not develop a real bank-consuming project.");
                CompleteTurn();
            }
            Require(host.Client.AllocateResearch(host.Snapshot, Actor, 420).Accepted, "Could not restore the normal R&D split.");
            Section();
            var target = Research.Engineering.Where(c => c.CanTarget && c.Cost > Research.EngineeringReserve + 720)
                .OrderByDescending(c => c.Cost).FirstOrDefault();
            Require(target != null, "No naturally available partial-progress project.");
            Click("target-" + target.Definition.DefinitionId.Value); CompleteTurn(); Section();
            var progress = Research.Engineering.Single(c => c.Definition.DefinitionId == target.Definition.DefinitionId).Progress;
            Require(progress > 0 && progress < target.Cost, "Expected actual partial Engineering work.");
            File.WriteAllText(Path.Combine(Output, "native-partial.json"), host.SaveJson());
            Capture("partial-project", target.Definition.DefinitionId.Value, () =>
            {
                Click("clear-engineering-target");
                Require(!Research.Target.HasValue && Research.Engineering.Single(c => c.Definition.DefinitionId == target.Definition.DefinitionId).Progress == progress, "Clear discarded progress.");
                var other = Research.Engineering.First(c => c.CanTarget && c.Definition.DefinitionId != target.Definition.DefinitionId);
                Click("target-" + other.Definition.DefinitionId.Value);
                var saved = host.SaveJson(); host.LoadJson(saved); Require(host.SaveJson() == saved, "Retarget checkpoint failed round trip.");
                Require(Research.Engineering.Single(c => c.Definition.DefinitionId == target.Definition.DefinitionId).Progress == progress, "Retarget/load lost former progress.");
                Section(); Capture("retargeted", other.Definition.DefinitionId.Value, () =>
                {
                    File.WriteAllText(Path.Combine(Output, "acceptance.txt"), "18 real Game-view captures; normal Science/Engineering progression, actual target/clear buttons, immutable engine unlock and checkpoint round trips passed.\nTurn " + host.Snapshot.Campaign.TurnNumber);
                    EditorApplication.update -= Watch; Debug.Log("Engineering tree acceptance complete."); EditorApplication.Exit(0);
                });
            });
        }
        private static void Capture(string name, string project, Action next)
        {
            StrategicPrototypeCapture.CaptureSequence("engineering-tree", name, Sizes, root =>
            {
                var shell = root.Q<LcarsShell>(); var view = root.Q<ResearchPanelView>();
                Require(view != null, "Research panel missing.");
                var projects = view.Q<ScrollView>("engineering-projects");
                Require(projects != null && projects.Query<Button>().ToList().Count == 20, "Expected all twenty development controls.");
                Require(projects.worldBound.width > 150 && projects.resolvedStyle.height >= 180, "Project list collapsed.");
                shell.Details.ScrollTo(projects); projects.ScrollTo(view.Q<VisualElement>("engineering-" + project));
                foreach (var card in Research.Engineering)
                {
                    var button = projects.Q<Button>("target-" + card.Definition.DefinitionId.Value);
                    Require(button.enabledSelf == card.CanTarget, "Displayed target eligibility differs from authority.");
                    Require(button.worldBound.width <= projects.contentViewport.worldBound.width + 1, "Essential target control horizontally clipped.");
                }
                Require(view.Q<Button>("clear-engineering-target") != null, "Clear action missing.");
            }, () => { try { next(); } catch (Exception error) { Fail(error.ToString()); } });
        }
        private static void Watch() { EditorApplication.QueuePlayerLoopUpdate(); if (EditorApplication.timeSinceStartup > deadline) Fail("Engineering capture timed out."); }
        private static void Fail(string message) { EditorApplication.update -= Start; EditorApplication.update -= Watch; SessionState.SetBool(Key, false); Debug.LogError(message); EditorApplication.Exit(1); }
    }
}
