"""Binding configuration/source checks only. Does not import, compile or execute Unity."""
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent


def check(root):
    errors = []
    def require(value, message):
        if not value:
            errors.append(message)
    base = root / "client/HobbyDS9.Unity/Assets/HobbyDS9"
    review = base / "Review"
    runtime = review / "Runtime"
    asm = json.loads((runtime / "HobbyDS9.UnityReview.asmdef").read_text(encoding="utf-8-sig"))
    require(asm["name"] == "HobbyDS9.UnityReview" and asm.get("overrideReferences") is True and asm.get("autoReferenced") is False,
            "Review runtime requires the preserved explicit assembly identity")
    require(asm.get("references") == ["HobbyDS9.ArtDemo", "Unity.InputSystem"] and set(asm.get("precompiledReferences", [])) == {
        "HobbyDS9.Contracts.dll", "HobbyDS9.Application.dll", "HobbyDS9.TacticalReviewClient.dll", "Newtonsoft.Json.dll"},
        "Unexpected review runtime reference; host-only/storage/Simulation imports are forbidden")
    for name in ("Editor", "Tests"):
        path = review / name / ("HobbyDS9.UnityReview." + name + ".asmdef")
        config = json.loads(path.read_text(encoding="utf-8-sig"))
        require(config.get("includePlatforms") == ["Editor"] and "HobbyDS9.UnityReview" in config.get("references", []),
                name + " must be Editor-only and reference the actual review runtime")
    preserved = {n.attrib["fullname"] for n in ET.parse(base / "link.xml").getroot().findall("assembly") if n.attrib.get("preserve") == "all"}
    require({"HobbyDS9.Contracts", "HobbyDS9.Application", "HobbyDS9.Simulation", "HobbyDS9.UnityClient", "HobbyDS9.TacticalReviewClient", "HobbyDS9.UnityReview"} <= preserved,
            "Missing preserved default or review assembly; this is not AOT proof")
    default = (base / "Runtime/ClientEntry.cs").read_text(encoding="utf-8-sig")
    require('Array.IndexOf(reviewArgs,"--hobbyds9-physical-review")>=0' in default and
            'HobbyDS9.UnityReview.ReviewLaunch, HobbyDS9.UnityReview' in default,
            "Missing explicit review selector/bridge")
    require("QualitySettings.vSyncCount = 1;" in default and "Application.targetFrameRate = -1;" in default,
            "VSync bootstrap changed")
    options = (runtime / "ReviewLaunchOptions.cs").read_text(encoding="utf-8-sig")
    require("DefaultEnabled=false" in options, "Review default activation must stay false")
    entry = (runtime / "ReviewEntry.cs").read_text(encoding="utf-8-sig")
    require("RuntimeInitializeOnLoadMethod" not in entry, "Review must not add a competing bootstrap")
    require('"ReviewPackages/"+options.Profile+"/"+name' in entry and "return asset.bytes" in entry,
            "Review resource loader must consume exact TextAsset bytes")
    require("viewEpoch++" in entry and "epoch==viewEpoch" in entry and "ReferenceEquals(before,model.Current.Frame)" in entry,
            "Missing view epoch replacement guard")
    require("modalPermission.TryOpen" in entry and '"Cancel",callbacks.Cancel' in entry and
            '"Confirm",callbacks.Confirm' in entry and "modalPermission.Invalidate()" in entry,
            "Modal buttons must use the actual portable one-use permission and lifetime invalidation")
    keyboard = (runtime / "ReviewEntry.Keyboard.cs").read_text(encoding="utf-8-sig")
    require('if(model!.SingleplayerDemo)BindDemoButtonKeyboard(button,guarded);' in entry and
            'new Button(guarded)' in entry and 'if(SubmitAlreadyPerformed()){if(enter)returnHeld=true;else spaceHeld=true;return;}' in keyboard and
            'WasPerformedThisFrame()' in keyboard and 'returnHeld' in keyboard and 'DemoKeyUp' in keyboard,
            "Keyboard buttons must share the guarded click and defer real Submit, with held-key suppression")
    require('tab.wasPressedThisFrame' in keyboard and 'NavigationMoveEvent.GetPooled' in keyboard and
            'element is TextField||element is DropdownField' in keyboard and 'focused.SendEvent(navigation)' in keyboard and
            'InputSystem.Update(' not in keyboard and '.Enable(' not in keyboard and 'SetInputSystemProvider' not in keyboard,
            "Keyboard focus fallback must be local, field-safe and defer raw Tab without changing provider/actions")
    draw = entry.split("private void Draw()", 1)[1].split("private static Label Readout", 1)[0]
    require(not re.search(r"\.(?:CommitRound|SendTravel|Advance|FinalizeBatch|Load|SetCloak|Save|Dispatch|Execute)\(", draw),
            "Painting must not dispatch authoritative work")
    for path in runtime.glob("*.cs"):
        source = path.read_text(encoding="utf-8-sig")
        require(not any(term in source for term in ("using HobbyDS9.Simulation", "using HobbyDS9.TacticalReviewStorage", "System.Text.Json", "JsonUtility", "Process.Start")),
                "Forbidden runtime dependency in " + path.name)
        require(Path(str(path) + ".meta").exists(), "Missing runtime source metadata: " + path.name)
    for path in (runtime / "ReviewEntry.Keyboard.cs.meta", review / "Tests/DemoKeyboardControlTests.cs.meta"):
        require(re.search(r"^guid: [0-9a-f]{32}$", path.read_text(encoding="utf-8-sig"), re.MULTILINE) is not None,
                "Invalid keyboard source metadata GUID: " + path.name)
    build = (review / "Editor/ReviewBuild.cs").read_text(encoding="utf-8-sig")
    require("artifacts/unity-review/Windows/HobbyDS9Review.exe" in build and "Review/Scenes/PhysicalReview.unity" in build,
            "Review builder must have disjoint output and explicit scene")
    require((review / "Scenes/PhysicalReview.unity").exists(), "Missing authored review scene")
    return errors


if __name__ == "__main__":
    failures = check(ROOT)
    for failure in failures:
        print("ERROR:", failure)
    if failures:
        raise SystemExit(1)
    print("Review asmdef/link/explicit selector/source boundary checks passed; Editor/AOT/native NOT executed.")
