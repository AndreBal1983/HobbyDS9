import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location("integrated_check",ROOT/"scripts/check-unity-integrated.py")
checker=importlib.util.module_from_spec(spec);spec.loader.exec_module(checker)
class IntegratedBoundaries(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)
        for path in ("client/HobbyDS9.Unity/Assets/HobbyDS9/Review","scenarios/integrated-playtest-v1"):
            shutil.copytree(ROOT/path,self.root/path)
        for path in ("client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/ClientEntry.cs","prototypes/HobbyDS9.TacticalReviewClient/PhysicalReviewHost.Integrated.cs"):
            target=self.root/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/path,target)
    def tearDown(self):self.temp.cleanup()
    def change(self,path,before,after):
        p=self.root/path;p.write_text(p.read_text().replace(before,after))
    def test_source(self):self.assertEqual([],checker.check(self.root))
    def test_missing_outer_export(self):
        self.change("prototypes/HobbyDS9.TacticalReviewClient/PhysicalReviewHost.Integrated.cs",".ExportIntegratedSave()",".ExportSave()")
        self.assertTrue(any("carrier" in e for e in checker.check(self.root)))
    def test_private_strategy_root(self):
        self.change("client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewStrategyView.cs","Frame.Strategy","ExportIntegratedSave()")
        self.assertTrue(any("projections" in e for e in checker.check(self.root)))
    def test_resource_newline_normalization(self):
        path=self.root/"client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/IntegratedPackages/v1/core.json";path.write_bytes(path.read_bytes().replace(b"\n",b"\r\n"))
        self.assertTrue(any("bytes differ" in e for e in checker.check(self.root)))
    def test_output_overwrite(self):
        self.change("client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Editor/IntegratedBuild.cs","artifacts/unity-integrated/Windows/HobbyDS9IntegratedPlaytest.exe","artifacts/unity-review/Windows/HobbyDS9Review.exe")
        self.assertTrue(any("output" in e for e in checker.check(self.root)))
    def test_default_activation(self):
        self.change("client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/IntegratedLaunchOptions.cs","DefaultEnabled=false","DefaultEnabled=true")
        self.assertTrue(any("explicit" in e for e in checker.check(self.root)))
if __name__=="__main__":unittest.main()
