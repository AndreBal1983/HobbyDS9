"""Adversarial source/configuration checks, not Unity or UI execution."""
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("binding_check", ROOT / "scripts/check-unity-review-binding.py")
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)


class BindingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="hobby-binding-static-")
        self.root = Path(self.temp.name)
        self.assets = self.root / "client/HobbyDS9.Unity/Assets/HobbyDS9"
        original = ROOT / "client/HobbyDS9.Unity/Assets/HobbyDS9"
        shutil.copytree(original / "Review", self.assets / "Review")
        (self.assets / "Runtime").mkdir()
        shutil.copyfile(original / "Runtime/ClientEntry.cs", self.assets / "Runtime/ClientEntry.cs")
        shutil.copyfile(original / "link.xml", self.assets / "link.xml")

    def tearDown(self):
        self.temp.cleanup()

    def test_delivered_configuration(self):
        self.assertEqual([], checker.check(self.root))

    def test_net10_storage_reference_rejected(self):
        path = self.assets / "Review/Runtime/HobbyDS9.UnityReview.asmdef"
        config = json.loads(path.read_text());config["precompiledReferences"].append("HobbyDS9.TacticalReviewStorage.dll")
        path.write_text(json.dumps(config))
        self.assertTrue(any("reference" in x for x in checker.check(self.root)))

    def test_missing_link_preservation_rejected(self):
        path = self.assets / "link.xml"
        path.write_text(path.read_text().replace('<assembly fullname="HobbyDS9.UnityReview" preserve="all" />', ""))
        self.assertTrue(any("preserved" in x for x in checker.check(self.root)))

    def test_default_activation_rejected(self):
        path = self.assets / "Review/Runtime/ReviewLaunchOptions.cs"
        path.write_text(path.read_text().replace("DefaultEnabled=false", "DefaultEnabled=true"))
        self.assertTrue(any("activation" in x for x in checker.check(self.root)))

    def test_render_dispatch_rejected(self):
        path = self.assets / "Review/Runtime/ReviewEntry.cs"
        source = path.read_text()
        self.assertIn("private void Draw()", source)
        path.write_text(source.replace("private void Draw()\n        {", "private void Draw()\n        {\n            model.Advance();", 1))
        self.assertTrue(any("Painting" in x for x in checker.check(self.root)))

    def test_default_output_overwrite_rejected(self):
        path = self.assets / "Review/Editor/ReviewBuild.cs"
        path.write_text(path.read_text().replace("artifacts/unity-review/Windows/HobbyDS9Review.exe", "artifacts/unity/Windows/HobbyDS9.exe"))
        self.assertTrue(any("disjoint" in x for x in checker.check(self.root)))

    def test_bypassed_portable_modal_permission_rejected(self):
        path = self.assets / "Review/Runtime/ReviewEntry.cs"
        path.write_text(path.read_text().replace('"Confirm",callbacks.Confirm', '"Confirm",action'))
        self.assertTrue(any("Modal buttons" in x for x in checker.check(self.root)))

    def test_duplicate_keyboard_submit_exclusion_cannot_be_removed(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("if(SubmitAlreadyPerformed())", "if(false)"))
        self.assertTrue(any("Keyboard buttons" in x for x in checker.check(self.root)))

    def test_keyboard_field_ownership_cannot_be_removed(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("element is TextField||element is DropdownField", "false"))
        self.assertTrue(any("field-safe" in x for x in checker.check(self.root)))

    def test_keyboard_fixture_invalid_guid_is_rejected(self):
        path = self.assets / "Review/Tests/DemoKeyboardControlTests.cs.meta"
        path.write_text(path.read_text().replace("guid: ", "guid: f"))
        self.assertTrue(any("Invalid keyboard source metadata" in x for x in checker.check(self.root)))

    def test_document_only_keyboard_release_route_is_rejected(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("var next=currentPanel?.visualTree;", "var next=keyboardRoot;"))
        self.assertTrue(any("release observation" in x for x in checker.check(self.root)))

    def test_rebuild_cannot_clear_held_press_to_reenable_next_modal(self):
        path = self.assets / "Review/Runtime/ReviewEntry.cs"
        path.write_text(path.read_text().replace("UnbindDemoKeyboard();ReleaseAudio();", "UnbindDemoKeyboard();ResetDemoKeyboard();ReleaseAudio();"))
        self.assertTrue(any("preserve a held press" in x for x in checker.check(self.root)))

    def test_unfocused_tab_cannot_handle_other_event_targets(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("ReferenceEquals(e.target,keyboardReleaseRoot)", "true"))
        self.assertTrue(any("Unfocused Tab" in x for x in checker.check(self.root)))

    def test_unfocused_tab_cannot_leave_a_stale_panel_subscription(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("keyboardReleaseRoot?.UnregisterCallback<KeyDownEvent>(DemoUnfocusedTab);", ""))
        self.assertTrue(any("Unfocused Tab" in x for x in checker.check(self.root)))

    def test_demo_rebuild_cannot_replace_the_dispatch_panel(self):
        path = self.assets / "Review/Runtime/ReviewEntry.cs"
        path.write_text(path.read_text().replace("if(!PlayerProfile||panel==null||doc.panelSettings!=panel)", "if(true)"))
        self.assertTrue(any("original panel" in x for x in checker.check(self.root)))

    def test_retired_origin_cannot_remain_actionable(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("origin.SetEnabled(false);origin.focusable=false;", ""))
        self.assertTrue(any("hidden and disabled" in x for x in checker.check(self.root)))

    def test_release_cannot_ignore_queued_event_timestamp(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("e.timestamp>=returnStarted", "true"))
        self.assertTrue(any("accepted physical key/time" in x for x in checker.check(self.root)))

    def test_keypad_release_cannot_clear_main_return_hold(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("e.keyCode==returnCode", "true"))
        self.assertTrue(any("accepted physical key/time" in x for x in checker.check(self.root)))

    def test_stale_panel_target_cannot_bypass_live_focus_check(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("ReferenceEquals(target,focused)&&keyboardRoot.Contains(target)&&target.enabledInHierarchy", "true"))
        self.assertTrue(any("obsolete nonfocused panel targets" in x for x in checker.check(self.root)))

    def test_cleanup_cannot_leave_retired_origins_attached(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("keyboardRetired?.RemoveFromHierarchy();keyboardRetired=null;", ""))
        self.assertTrue(any("clean retired origins" in x for x in checker.check(self.root)))

    def test_direct_render_cannot_skip_origin_retention(self):
        path = self.assets / "Review/Runtime/ReviewEntry.cs"
        path.write_text(path.read_text().replace("try{action();}finally{PreserveDemoKeyOrigins();}", "action();"))
        self.assertTrue(any("after draw/actions" in x for x in checker.check(self.root)))

    def test_shared_origin_cannot_be_removed_while_second_key_is_held(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Keyboard.cs"
        path.write_text(path.read_text().replace("if(returnHeld&&ReferenceEquals(origin,returnOrigin)||spaceHeld&&ReferenceEquals(origin,spaceOrigin))return;", ""))
        self.assertTrue(any("another held key" in x for x in checker.check(self.root)))

    def test_player_keyboard_gate_cannot_enable_legacy_profiles(self):
        path = self.assets / "Review/Runtime/ReviewEntry.Frontier.cs"
        path.write_text(path.read_text().replace("model?.SingleplayerDemo==true||model?.FrontierPreview==true", "true"))
        self.assertTrue(any("explicit demo/frontier" in x for x in checker.check(self.root)))

    def test_frontier_map_cannot_ignore_observation_actor(self):
        path = self.assets / "Review/Runtime/ReviewFrontierMap.cs"
        path.write_text(path.read_text().replace("known==null||known.ActorId!=frame.Actor", "known==null"))
        self.assertTrue(any("selected actor discovery" in x for x in checker.check(self.root)))

    def test_frontier_map_cannot_fall_back_to_unrestricted_navigation(self):
        path = self.assets / "Review/Runtime/ReviewFrontierMap.cs"
        path.write_text(path.read_text()+"\n// facts.Navigation fallback\n")
        self.assertTrue(any("unrestricted navigation" in x for x in checker.check(self.root)))

    def test_frontier_slots_cannot_alias_bajor_directory(self):
        path = self.assets / "Review/Runtime/FrontierPreviewOptions.cs"
        path.write_text(path.read_text().replace("HobbyDS9FrontierPreviewOnly-v5", "HobbyDS9SingleplayerDemoOnly-v4"))
        self.assertTrue(any("disjoint wrapper5" in x for x in checker.check(self.root)))


if __name__ == "__main__":
    unittest.main()
