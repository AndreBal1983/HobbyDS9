#!/usr/bin/env python3
"""Static integration checks only; these do not compile or execute Unity."""
import hashlib
import json
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
project = root / "client/HobbyDS9.Unity"
assets = project / "Assets"
errors = []
def require(condition, message):
    if not condition:
        errors.append(message)

version = (project / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8-sig")
require("m_EditorVersion: 6000.6.5f1\n" in version, "Unexpected Unity Editor pin")
guids = {}
for path in assets.rglob("*"):
    if path.suffix == ".meta":
        match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(encoding="utf-8-sig"), re.MULTILINE)
        require(match is not None, f"Missing/invalid GUID: {path.relative_to(root)}")
        if match:
            guid = match.group(1)
            require(guid not in guids, f"Duplicate GUID: {path.relative_to(root)} and {guids.get(guid)}")
            guids[guid] = path.relative_to(root)
    else:
        require(Path(str(path) + ".meta").is_file(), f"Missing .meta: {path.relative_to(root)}")

runtime = assets / "HobbyDS9/Runtime"
asmdef = json.loads((runtime / "HobbyDS9.UnityClient.asmdef").read_text(encoding="utf-8-sig"))
require(asmdef.get("overrideReferences") is True, "Client must use explicit precompiled references")
require(set(asmdef["precompiledReferences"]) == {"HobbyDS9.Application.dll", "HobbyDS9.Contracts.dll", "Newtonsoft.Json.dll"}, "Unexpected client precompiled references")
for path in runtime.glob("*.cs"):
    require("HobbyDS9.Simulation" not in path.read_text(encoding="utf-8-sig"), f"Direct Simulation reference: {path.name}")
for name in ("Contracts", "Application", "Simulation"):
    file_name = f"HobbyDS9.{name}.dll"
    source = root / "src/HobbyDS9.Application/bin/Release/netstandard2.1" / file_name
    plugin = assets / "Plugins/HobbyDS9" / file_name
    require(source.exists() and plugin.exists(), f"Missing {file_name}; run PrepareUnity.proj")
    if source.exists() and plugin.exists():
        require(source.read_bytes() == plugin.read_bytes(), f"Stale {file_name}")
    require("isExplicitlyReferenced: 1" in Path(str(plugin) + ".meta").read_text(encoding="utf-8-sig"), f"Auto-referenced plugin: {file_name}")
scenario = root / "scenarios/ds9-slice/scenario.json"
resource = assets / "HobbyDS9/Resources/ds9-slice.json"
require(resource.exists(), "Scenario resource missing; run PrepareUnity.proj")
if resource.exists():
    require(scenario.read_bytes() == resource.read_bytes(), "Scenario bytes changed during integration")
    print("Scenario SHA256:", hashlib.sha256(resource.read_bytes()).hexdigest())
if errors:
    raise SystemExit("\n".join(errors))
print("Unity assets, metadata, reference boundary and portable copies: static checks passed (Unity not executed).")
