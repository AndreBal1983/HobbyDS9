using System;
using System.IO;
using System.Reflection;
using HobbyDS9.UnityClient;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Editor
{
    // Real Game view captures only. No browser recreation or synthetic screenshots.
    public static class StrategicPrototypeCapture
    {
        private static readonly Vector2Int[] Sizes = { new Vector2Int(1920,1080), new Vector2Int(3440,1440) };
        private static int index, firstFrame;
        private static double deadline;
        private static string pendingFile;
        private static object gameView;
        private static PropertyInfo selectedSize;
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static string Output => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../..", "artifacts/unity/lcars-strategic-prototype"));

        [MenuItem("HobbyDS9/LCARS/Capture Mars at both review resolutions")]
        public static void CaptureBoth()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode in SampleScene first; the initial selection must be Mars.");
            if (UnityEngine.Application.unityVersion != "6000.6.5f1") throw new InvalidOperationException("Use Unity 6000.6.5f1.");
            EditorApplication.update -= Tick;
            Directory.CreateDirectory(Output);
            index = 0; pendingFile = null;
            // Unity exposes no public Game view fixed-resolution API. Reflection
            // is confined to this optional Editor tool; runtime has no dependency.
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView", true);
            gameView = EditorWindow.GetWindow(type);
            ((EditorWindow)gameView).Show(); ((EditorWindow)gameView).Focus();
            selectedSize = type.GetProperty("selectedSizeIndex", Flags) ?? throw new MissingMemberException("GameView.selectedSizeIndex");
            try { SelectSize(); EditorApplication.update += Tick; }
            catch { Stop(); throw; }
        }
        private static void SelectSize()
        {
            var assembly = typeof(EditorWindow).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType", true);
            var group = sizesType.GetMethod("GetGroup", Flags).Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
            var kindType = assembly.GetType("UnityEditor.GameViewSizeType", true);
            var target = Sizes[index]; var groupClass = group.GetType();
            var count = (int)groupClass.GetMethod("GetTotalCount", Flags).Invoke(group, null);
            var found = -1;
            for (var i = 0; i < count; i++)
            {
                var existing = groupClass.GetMethod("GetGameViewSize", Flags).Invoke(group, new object[] { i });
                var width = (int)sizeType.GetProperty("width", Flags).GetValue(existing);
                var height = (int)sizeType.GetProperty("height", Flags).GetValue(existing);
                var kind = sizeType.GetProperty("sizeType", Flags).GetValue(existing);
                if (width == target.x && height == target.y && kind.ToString() == "FixedResolution") { found = i; break; }
            }
            if (found < 0)
            {
                var size = Activator.CreateInstance(sizeType, Flags, null,
                    new object[] { Enum.Parse(kindType, "FixedResolution"), target.x, target.y, "LCARS review " + target.x + "x" + target.y }, null);
                groupClass.GetMethod("AddCustomSize", Flags).Invoke(group, new[] { size }); found = count;
            }
            selectedSize.SetValue(gameView, found);
            firstFrame = Time.frameCount; deadline = EditorApplication.timeSinceStartup + 30;
            ((EditorWindow)gameView).Repaint();
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play mode ended during capture.");
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Game view did not render/capture the requested resolution within 30 seconds.");
                var target = Sizes[index];
                if (pendingFile != null)
                {
                    if (!File.Exists(pendingFile)) return;
                    var texture = new Texture2D(2,2);
                    try
                    {
                        if (!texture.LoadImage(File.ReadAllBytes(pendingFile)) || texture.width != target.x || texture.height != target.y)
                            throw new InvalidOperationException("Capture dimensions do not match the requested resolution.");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                    Debug.Log("Mars strategic screenshot captured: " + pendingFile);
                    pendingFile = null;
                    if (++index == Sizes.Length) { Stop(); Debug.Log("Both Mars captures complete. Review wrapping, map input and visual balance before accepting."); return; }
                    SelectSize(); return;
                }
                if (Screen.width != target.x || Screen.height != target.y || Time.frameCount - firstFrame < 15) return;
                var entry = UnityEngine.Object.FindAnyObjectByType<ClientEntry>();
                var root = entry == null ? null : entry.GetComponent<UIDocument>()?.rootVisualElement;
                var shell = root?.Q<LcarsShell>();
                if (shell == null || shell.DetailsFrame.Heading.text != "MARS / WORLD INSPECTOR")
                    throw new InvalidOperationException("Mars must be selected before capture. Restart Play mode for the initial prototype view.");
                if (shell.ProductionOpen) throw new InvalidOperationException("Close the empire production dock before the initial-screen captures.");
                var queue = shell.Q<Button>("queue-selected-design");
                if (queue == null || !shell.DetailsFrame.worldBound.Contains(queue.worldBound.min) || !shell.DetailsFrame.worldBound.Contains(queue.worldBound.max))
                    throw new InvalidOperationException("The production action is clipped or below the inspector viewport.");
                var map = shell.Q<GalaxyMap>();
                if (map == null || map.worldBound.width < shell.worldBound.width * .60f)
                    throw new InvalidOperationException("Map must occupy at least 60% of the initial strategic screen width.");
                pendingFile = Path.Combine(Output, "mars-selected-" + target.x + "x" + target.y + ".png");
                if (File.Exists(pendingFile)) File.Delete(pendingFile); // Only this tool's generated capture.
                ScreenCapture.CaptureScreenshot(pendingFile);
            }
            catch (Exception error) { Stop(); Debug.LogException(error); }
        }
        private static void Stop() { EditorApplication.update -= Tick; pendingFile = null; }
    }
}
