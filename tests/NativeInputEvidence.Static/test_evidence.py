"""Adversarial observer contract checks, not engine/native behavior tests."""
import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("observer_check", ROOT / "scripts/check-native-input-evidence.py")
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)


class ObserverTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="hobby-input-observer-")
        self.root = Path(self.temp.name)
        self.path = self.root / checker.REL
        self.path.parent.mkdir(parents=True)
        shutil.copyfile(ROOT / checker.REL, self.path)

    def tearDown(self):
        self.temp.cleanup()

    def mutate(self, old, new):
        source = self.path.read_text()
        self.assertIn(old, source)
        self.path.write_text(source.replace(old, new))

    def test_delivered_observer(self):
        self.assertEqual([], checker.check(self.root))

    def test_default_activation_rejected(self):
        self.mutate('"--hobbyds9-native-input-evidence") < 0) return;', '"unused") < 0) return;')
        self.assertTrue(any("opt-in" in x for x in checker.check(self.root)))

    def test_unbounded_duration_rejected(self):
        self.mutate("DurationSeconds = 1800", "DurationSeconds = double.MaxValue")
        self.assertTrue(any("bounded" in x for x in checker.check(self.root)))

    def test_old_panel_release_leak_rejected(self):
        self.mutate("panel.UnregisterCallback<KeyUpEvent>(PanelKeyUp,TrickleDown.TrickleDown);", "")
        self.assertTrue(any("unsubscribe" in x for x in checker.check(self.root)))

    def test_event_consumption_rejected(self):
        self.mutate('Emit("panel-key",key,evt);', 'evt.StopPropagation();Emit("panel-key",key,evt);')
        self.assertTrue(any("never consume" in x for x in checker.check(self.root)))

    def test_arbitrary_key_capture_rejected(self):
        self.mutate("var key=NamedKey(evt.keyCode);", "var key=evt.character.ToString();")
        self.assertTrue(any("named controls" in x for x in checker.check(self.root)))

    def test_other_component_field_reflection_rejected(self):
        self.mutate('type.GetField("returnHeld",InstanceFields)', 'type.GetField("secret",InstanceFields)')
        self.assertTrue(any("own latch" in x for x in checker.check(self.root)))


if __name__ == "__main__":
    unittest.main()
