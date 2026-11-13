"""Check/prepare this receiving scenario using preserved PR85 namespace transforms.

Only pure data helpers are reused. The historical writer's admission/capsule entry
point and its obsolete machine-specific pins are never invoked or rewritten.
"""
import copy
import importlib.util
from pathlib import Path
import sys

sys.dont_write_bytecode = True

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("engineering_recipe", root / "scripts/build_engineering_content_variant.py")
recipe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(recipe)
def load(path):
    return recipe.parse((root / path).read_bytes())

tree = load("scenarios/ds9-slice/scenario.json")
legacy = load("scenarios/playable-fleet-v1/scenario.json")
mapping = recipe.mapping_for(tree["Definitions"]["ShipDesignCatalogue"])
assert mapping == load("scripts/engineering-content-variant-inputs.json")["namespaceOriginalToNew"]
inverse = {v: k for k, v in mapping.items()}
assert len(mapping) == 43
ready = recipe.catalogue_union(load("scenarios/activities-v1/catalogue.json"), load("scenarios/colonisation-v1/catalogue.json"))
imported = recipe.remap(tree["Definitions"], mapping)
expected = recipe.remap(legacy, mapping)
defs = expected["Definitions"]
defs["EngineeringApplications"] = imported["EngineeringApplications"]
defs["ShipDesignCatalogue"] = recipe.catalogue_union(ready, imported["ShipDesignCatalogue"])
ready_versions = ready["Hulls"] + ready["Components"]
defs["ShipDesignVersionAccess"] = recipe.union(imported["ShipDesignVersionAccess"] + [
    {"VersionId": v["VersionId"], "RequiredEngineeringApplicationDefinitionIds": []} for v in ready_versions], "VersionId")
versions = defs["ShipDesignCatalogue"]["Hulls"] + defs["ShipDesignCatalogue"]["Components"]
defs["DefinitionIds"] = sorted(set(defs["DefinitionIds"]) | {v["VersionId"] for v in versions} |
    {p["DefinitionId"] for p in defs["EngineeringApplications"]})
recipe.closure(defs["ShipDesignCatalogue"], defs["ShipDesignVersionAccess"], defs["EngineeringApplications"])
assert len(defs["EngineeringApplications"]) == 20
assert len(versions) == len(defs["ShipDesignVersionAccess"]) == 53
for group, key in recipe.KEYS.items():
    actual = {r[key]: r for r in defs["ShipDesignCatalogue"][group]}
    assert all(actual[r[key]] == r for r in ready[group])
    assert all(recipe.remap(actual[r[key]], inverse) == old for r, old in
        zip(imported["ShipDesignCatalogue"][group], tree["Definitions"]["ShipDesignCatalogue"][group]))
assert recipe.remap(defs["EngineeringApplications"], inverse) == tree["Definitions"]["EngineeringApplications"]
for gate in imported["ShipDesignVersionAccess"]:
    assert gate in defs["ShipDesignVersionAccess"]
untouched = recipe.remap(copy.deepcopy(expected), inverse)
for field in ("EngineeringApplications", "ShipDesignCatalogue", "ShipDesignVersionAccess", "DefinitionIds"):
    untouched["Definitions"][field] = legacy["Definitions"][field]
assert untouched == legacy
for name in ("playable", "package", "physical", "core"):
    profile = load(f"scenarios/playable-fleet-v1/{name}.json")
    assert recipe.remap(profile, mapping) == profile, name
source = root / "scenarios/engineering-development-v1/scenario.json"
if "--write" in sys.argv:
    source.write_bytes(recipe.encoded(expected))
assert load("scenarios/engineering-development-v1/scenario.json") == expected
resource = root / "client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/PlayableFleetPackages/v1/engineering-scenario.json"
if "--write" not in sys.argv:
    assert resource.read_bytes() == source.read_bytes()
print("Engineering playable: 20 PR85 projects; 43 preserved namespace pairs; 53 gated/ready versions; unchanged initial facts and ready catalogue")
