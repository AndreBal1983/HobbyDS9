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
        path.write_text(path.read_text().replace("UnbindDemoKeyboard();ReleasePanel();", "UnbindDemoKeyboard();ResetDemoKeyboard();ReleasePanel();"))
        self.assertTrue(any("preserve a held press" in x for x in checker.check(self.root)))


if __name__ == "__main__":
    unittest.main()
