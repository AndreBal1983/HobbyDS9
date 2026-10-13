"""Package validation tests, not gameplay or Unity execution."""
import copy
import importlib.util
from pathlib import Path
import shutil
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("review_import", ROOT/"scripts/check-unity-review-integration.py")
check = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check)

class ImportTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.real_inventory = check.inspect_plugins(ROOT)

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.inventory = copy.deepcopy(self.real_inventory)
        # Only import inputs/outputs; test corruption never touches a real worktree.
        for meta in (ROOT/check.ASSET_PREFIX).rglob("*.meta"):
            target=self.root/meta.relative_to(ROOT)
            target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(meta,target)
        for relative in (check.PLUGIN, check.PACKAGE, "scenarios/tactical-v2-physical-review",
                         "prototypes/HobbyDS9.TacticalReviewClient/bin/Release/netstandard2.1"):
            shutil.copytree(ROOT/relative, self.root/relative, dirs_exist_ok=True)

    def invalid(self, fragment):
        with self.assertRaisesRegex(ValueError, fragment):
            check.verify(self.root, self.inventory)

    def test_actual_produced_metadata_and_exact_packages(self):
        self.assertEqual(check.EXPECTED_PACKAGE_HASHES, check.verify(self.root, self.inventory))

    def test_stale_plugin_bytes(self):
        p = self.root/(check.PLUGIN+"HobbyDS9.Contracts.dll")
        p.write_bytes(p.read_bytes()+b"stale")
        self.invalid("Stale portable DLL")

    def test_host_only_plugin_injection(self):
        shutil.copyfile(self.root/(check.PLUGIN+"HobbyDS9.Contracts.dll"), self.root/(check.PLUGIN+"HobbyDS9.TacticalReviewStorage.dll"))
        self.invalid("host-only imports")

    def test_target_framework_rejected(self):
        self.inventory[0]["framework"] = ".NETCoreApp,Version=v10.0"
        self.invalid("Actual target framework")

    def test_compiled_dependency_surface_rejected(self):
        self.inventory[0]["references"].append("System.Text.Json")
        self.invalid("compiled reference surface")

    def test_concrete_host_api_rejected(self):
        self.inventory[0]["typeReferences"].append("System.IO.File")
        self.invalid("Concrete host-only API")

    def test_missing_compiled_facade_rejected(self):
        entry = next(item for item in self.inventory if item["name"] == "HobbyDS9.Application")
        entry["publicTypes"].remove("HobbyDS9.Application.ICampaignTacticalTravelSession")
        self.invalid("Missing compiled public API")

    def test_asset_guid_drift(self):
        p = self.root/(check.PACKAGE+"faster/core.json.meta")
        p.write_text("fileFormatVersion: 2\nguid: "+"0"*32+"\n", encoding="utf-8")
        self.invalid("Owned metadata drift")

    def test_duplicate_guid(self):
        a = self.root/(check.PACKAGE+"faster/core.json.meta")
        b = self.root/(check.PACKAGE+"slower/core.json.meta")
        b.write_bytes(a.read_bytes())
        self.invalid("Duplicate asset GUID")

    def test_plugin_auto_reference_rejected(self):
        p = self.root/(check.PLUGIN+"HobbyDS9.TacticalReviewClient.dll.meta")
        p.write_text(p.read_text().replace("isExplicitlyReferenced: 1", "isExplicitlyReferenced: 0"), encoding="utf-8")
        self.invalid("Incomplete explicit plugin importer")

    def test_exact_resource_byte_corruption(self):
        p = self.root/(check.PACKAGE+"faster/scenario.json")
        p.write_bytes(p.read_bytes()+b" ")
        self.invalid("resource bytes/LF")

    def test_activation_change_even_in_both_copies(self):
        for path in ("scenarios/tactical-v2-physical-review/faster/package.json", check.PACKAGE+"faster/package.json"):
            p = self.root/path
            p.write_bytes(p.read_bytes().replace(b'"DefaultActivationAllowed": false', b'"DefaultActivationAllowed": true'))
        self.invalid("activation gate changed")

    def test_explicit_target_has_no_editor_or_wildcard_assembly_import(self):
        project = ET.parse(ROOT/"scripts/PrepareUnityReview.proj").getroot()
        self.assertEqual(["Prepare"], [target.attrib["Name"] for target in project.findall("Target")])
        self.assertFalse(project.findall(".//Exec"))
        self.assertFalse(project.findall(".//Delete"))
        assemblies = project.find(".//PortableAssembly").attrib["Include"]
        self.assertNotIn("*", assemblies)
        self.assertEqual(4, len(assemblies.split(";")))

if __name__ == "__main__":
    unittest.main()
