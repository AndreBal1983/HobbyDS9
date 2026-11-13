"""Author/check the separate close-contact fixture from the accepted military loop."""
import json
from pathlib import Path
import sys
import uuid

root = Path(__file__).resolve().parents[1]
original = root / "scenarios/singleplayer-demo-v1"
source = root / "scenarios/combat-playtest-v1"
resource = root / "client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/CombatPlaytestV1"
files = {name: (original / (name + ".json")).read_bytes() for name in ("demo", "package", "physical", "core", "scenario")}
scenario = json.loads(files["scenario"])
scenario["ScenarioId"] = "scenario.combat-playtest-v1"
physical = json.loads(files["physical"])
for order in physical["InitialOrders"]:
    order["Intent"] = 2  # Existing Leave order; no instant movement or combat allocation.
    order["RouteNodeIds"] = ["nav.bajor"] if order["FleetId"] == "fleet.federation-scout" else ["nav.badlands", "nav.bajor"]
for name, value in (("scenario", scenario), ("physical", physical)):
    files[name] = (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
if "--write" in sys.argv:
    source.mkdir(parents=True, exist_ok=True)
    resource.mkdir(parents=True, exist_ok=True)
    for name, raw in files.items():
        (source / (name + ".json")).write_bytes(raw)
        asset = resource / (name + ".json")
        asset.write_bytes(raw)
        guid = uuid.uuid5(uuid.NAMESPACE_URL, "HobbyDS9/COMBAT-PLAYTEST-01/" + asset.relative_to(root).as_posix()).hex
        Path(str(asset) + ".meta").write_text("fileFormatVersion: 2\nguid: " + guid + "\n", encoding="utf-8")
    Path(str(resource) + ".meta").write_text("fileFormatVersion: 2\nguid: c66d6fbb6cb35198891470c37f36dd45\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n", encoding="utf-8")
for name, raw in files.items():
    assert (source / (name + ".json")).read_bytes() == raw
    assert (resource / (name + ".json")).read_bytes() == raw
print("Combat fixture: accepted military-loop state/equipment/stocks unchanged; separate identity and ordinary initial Bajor routes only; five exact resource pairs")
