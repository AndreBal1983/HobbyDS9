"""Preserve verified owner-directed browser revision in a new stable directory."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import zipfile

source = Path(sys.argv[1])
target = Path(sys.argv[2])
assert source.is_dir() and not target.exists(), "Use a new stable destination; no overwrites"
checks = json.loads((source / "owner-revision-final-checks.json").read_text(encoding="utf-8"))
assert not checks["errors"] and checks["revisionImages"] == 63
shutil.copytree(source, target)
readme = """Owner-directed interface prototype /20 October2026
Open map-interaction-review.html for all revised areas, or index.html?screen=map-overview.
Interactive links: empire, worlds, fleet, science, designer-components.
Seven existing screens changed; all other gallery screens remain baseline designs.
Original contact sheets/storyboards and baseline-* receipts are historical.
Fixtures only: no campaign API, saves, backend pursuit/knowledge/fit claims or native acceptance.
63 screenshots,18 three-size interaction groups and6 final consistency groups passed.
Science600-unit local editor and coordinator-proposed lock redistribution are not new earning rules.
Source/reference handoff: docs/art/coherent-interface/owner-revision-2026-10-20.md.
The lightweight ZIP omits the screenshot gallery; open index.html after extracting it.
"""
(target / "START-HERE.txt").write_text(readme, encoding="utf-8")
zip_path = target / "HobbyDS9-Owner-Interface-Revision.zip"
with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as archive:
    for name in ["index.html", "app.js", "style.css", "map-interactions.js", "empire-overview.js", "fleet-roster.js", "science-bars.js", "designer-visual.js", "asset-manifest.json", "map-interaction-checks.json", "owner-revision-final-checks.json", "START-HERE.txt"]:
        archive.write(target / name, name)
    for file in sorted((target / "assets").iterdir()):
        archive.write(file, "assets/" + file.name)
receipt = {"status": "Design prototype only; no native/runtime acceptance", "entry": str(target / "map-interaction-review.html"), "gallery": str(target / "gallery.html"), "interactive": str(target / "index.html"), "changedExistingViews": 7, "revisionScreenshots": 63, "threeSizeInteractionGroups": 18, "finalConsistencyGroups": 6, "baselineR1Preserved": True, "zip": str(zip_path), "zipBytes": zip_path.stat().st_size, "zipSha256": hashlib.sha256(zip_path.read_bytes()).hexdigest()}
(target / "owner-revision-delivery.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
for item in json.loads((source / "coverage.json").read_text(encoding="utf-8"))["captures"]:
    assert hashlib.sha256((target / item["file"]).read_bytes()).hexdigest() == item["sha256"]
print(json.dumps(receipt))
