"""Verify PR85's authored tree reaches the separate PB playable resource unchanged."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
tree = json.loads((root / "scenarios/ds9-slice/scenario.json").read_bytes())
legacy = json.loads((root / "scenarios/playable-fleet-v1/scenario.json").read_bytes())
source = root / "scenarios/engineering-development-v1/scenario.json"
current = json.loads(source.read_bytes())
assert len(current["Definitions"]["EngineeringApplications"]) == 20
for field in ("EngineeringApplications", "ShipDesignCatalogue", "ShipDesignVersionAccess"):
    assert current["Definitions"][field] == tree["Definitions"][field], field
for field in legacy:
    if field != "Definitions":
        assert legacy[field] == current[field], field
for field in legacy["Definitions"]:
    if field not in ("EngineeringApplications", "ShipDesignCatalogue", "ShipDesignVersionAccess", "DefinitionIds"):
        assert legacy["Definitions"][field] == current["Definitions"][field], field
expected_ids = set(legacy["Definitions"]["DefinitionIds"])
expected_ids.update(p["DefinitionId"] for p in tree["Definitions"]["EngineeringApplications"])
expected_ids.update(h["VersionId"] for h in tree["Definitions"]["ShipDesignCatalogue"]["Hulls"])
expected_ids.update(c["VersionId"] for c in tree["Definitions"]["ShipDesignCatalogue"]["Components"])
assert sorted(expected_ids) == current["Definitions"]["DefinitionIds"]
resource = root / "client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/PlayableFleetPackages/v1/engineering-scenario.json"
assert resource.read_bytes() == source.read_bytes()
print("Engineering playable: 20 unchanged PR85 projects, exact catalogue/gates, unchanged initial state and other rules; resource bytes match")
