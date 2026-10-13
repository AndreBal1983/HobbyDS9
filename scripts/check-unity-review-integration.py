#!/usr/bin/env python3
"""Explicit review import evidence only: no Unity compile, AOT or runtime claim."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import sys
import uuid

NAMESPACE = uuid.UUID("2439c88e-c2c9-4fb3-b2cb-5bd9e9fb06bc")
ASSET_PREFIX = "client/HobbyDS9.Unity/Assets/"
PLUGIN = ASSET_PREFIX + "Plugins/HobbyDS9/"
PACKAGE = ASSET_PREFIX + "HobbyDS9/Review/Resources/ReviewPackages/"
ASSEMBLIES = ("HobbyDS9.Contracts", "HobbyDS9.Application", "HobbyDS9.Simulation", "HobbyDS9.TacticalReviewClient")
FORBIDDEN = ("System.Text.Json", "HobbyDS9.TacticalReviewStorage", "HobbyDS9.TacticalReviewShell",
             "HobbyDS9.TacticalTravelClient.Probe", "HobbyDS9.CampaignTacticalV2.Prototypes", "xunit", "UnityEngine", "UnityEditor")
REQUIRED = {
    "HobbyDS9.Contracts": {"HobbyDS9.Contracts.CampaignTacticalTravelSaveDto", "HobbyDS9.Contracts.CampaignWeaponAvailability", "HobbyDS9.Contracts.CampaignFocusFallbackCause"},
    "HobbyDS9.Application": {"HobbyDS9.Application.ICampaignTacticalTravelSession", "HobbyDS9.Application.CampaignTacticalTravelApplication"},
    "HobbyDS9.Simulation": {"HobbyDS9.Simulation.CampaignTacticalReviewSession"},
    "HobbyDS9.TacticalReviewClient": {"HobbyDS9.TacticalReviewClient.PhysicalReviewHost", "HobbyDS9.TacticalReviewClient.ReviewController",
                                    "HobbyDS9.TacticalReviewClient.IPhysicalReviewHostCodec", "HobbyDS9.TacticalReviewClient.IPhysicalReviewCheckpointStore"},
}
REFERENCES = {
    "HobbyDS9.Contracts": {"netstandard"},
    "HobbyDS9.Application": {"netstandard", "HobbyDS9.Contracts", "HobbyDS9.Simulation"},
    "HobbyDS9.Simulation": {"netstandard", "HobbyDS9.Contracts"},
    "HobbyDS9.TacticalReviewClient": {"netstandard", "HobbyDS9.Contracts", "HobbyDS9.Application"},
}
HOST_TYPES = {"System.IO.File", "System.IO.Directory", "System.IO.FileStream", "System.Diagnostics.Process", "System.OperatingSystem"}
EXPECTED_PACKAGE_HASHES = {"faster": "49e54967f76f8d84822731f8a31b715775ca673ea034d9643079979098735541",
                           "slower": "3b64263436ef37aa847d27d184e4cb973c69e21def201c8a483e7e4674344180"}

def digest(data):
    return hashlib.sha256(data).hexdigest()

def guid(path):
    return uuid.uuid5(NAMESPACE, path).hex

def owned_metadata():
    paths = [ASSET_PREFIX+"HobbyDS9/Review", ASSET_PREFIX+"HobbyDS9/Review/Resources", PACKAGE.rstrip("/"),
             PLUGIN+"HobbyDS9.TacticalReviewClient.dll"]
    for profile in ("faster", "slower"):
        paths.append(PACKAGE+profile)
        paths += [PACKAGE+profile+"/"+name for name in ("package.json", "core.json", "scenario.json")]
    return paths

def inspect_plugins(root):
    command = ["dotnet", "run", "--project", str(root/"scripts/UnityReviewImportProbe"), "-c", "Release",
               "--verbosity", "quiet", "--"] + [str(root/(PLUGIN+name+".dll")) for name in ASSEMBLIES]
    result = subprocess.run(command, cwd=root, text=True, capture_output=True, check=True)
    return json.loads(result.stdout)

def verify(root, inventory):
    errors = []
    def require(ok, message):
        if not ok:
            errors.append(message)
    require({p.name for p in (root/PLUGIN).glob("*.dll")} == {name+".dll" for name in ASSEMBLIES}, "Unexpected/missing plugin DLL; host-only imports prohibited")
    records = {item["name"]: item for item in inventory}
    require(set(records) == set(ASSEMBLIES), "Actual assembly-name inventory mismatch")
    for name in ASSEMBLIES:
        plugin = root/(PLUGIN+name+".dll")
        output = root/("prototypes/HobbyDS9.TacticalReviewClient/bin/Release/netstandard2.1/"+name+".dll")
        require(plugin.exists() and output.exists() and plugin.read_bytes() == output.read_bytes(), "Stale portable DLL: "+name)
        item = records.get(name, {})
        require(item.get("framework") == ".NETStandard,Version=v2.1", "Actual target framework not netstandard2.1: "+name)
        require(REQUIRED[name].issubset(set(item.get("publicTypes", []))), "Missing compiled public API: "+name)
        require(set(item.get("references", [])) == REFERENCES[name], "Unexpected compiled reference surface: "+name)
        require(not HOST_TYPES.intersection(item.get("typeReferences", [])), "Concrete host-only API imported: "+name)
        refs = item.get("references", []) + item.get("typeReferences", [])
        require(not any(value.startswith(prefix) for value in refs for prefix in FORBIDDEN), "Forbidden compiled dependency: "+name)
    all_guids = {}
    for meta in (root/ASSET_PREFIX).rglob("*.meta"):
        match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8-sig"), re.M)
        require(match is not None, "Missing/invalid meta GUID: "+str(meta.relative_to(root)))
        if match:
            value = match.group(1)
            require(value not in all_guids, "Duplicate asset GUID: "+value)
            all_guids[value] = meta
    for path in owned_metadata():
        meta = root/(path+".meta")
        text = meta.read_text(encoding="utf-8-sig") if meta.exists() else ""
        require("guid: "+guid(path) in text, "Owned metadata drift: "+path)
        if ".dll" in path:
            require("PluginImporter:" in text and "isExplicitlyReferenced: 1" in text and "validateReferences: 1" in text
                    and "enabled: 1" in text, "Incomplete explicit plugin importer: "+path)
        elif not path.endswith(".json"):
            require("folderAsset: yes" in text and "DefaultImporter:" in text, "Missing folder importer: "+path)
    packages = {}
    for profile in ("faster", "slower"):
        source = root/("scenarios/tactical-v2-physical-review/"+profile)
        destination = root/(PACKAGE+profile)
        require({p.name for p in destination.glob("*.json")} == {"package.json", "core.json", "scenario.json"}, "Unexpected/missing review resource")
        raw = {}
        for name in ("package.json", "core.json", "scenario.json"):
            original = (source/name).read_bytes()
            copied = (destination/name).read_bytes() if (destination/name).exists() else b""
            require(original == copied and b"\r" not in copied, "Review resource bytes/LF differ: "+profile+"/"+name)
            raw[name] = copied
        try:
            p, core, scenario = (json.loads(raw[name]) for name in ("package.json", "core.json", "scenario.json"))
            require(p["FormatVersion"] == 2 and core["FormatVersion"] == 1, "Review package formats changed")
            require(p["ProfileId"] == "campaign-tactical-v2-physical-review-1" and core["ProfileId"] == "campaign-tactical-v2-lifecycle-review-1", "Review profile identity changed")
            require(all(v["PolicyStatus"] == "proposed" and v["DefaultActivationAllowed"] is False for v in (p, core)), "Review activation gate changed")
            require(scenario["Definitions"]["ContentFormatVersion"] == 11, "Authored content version changed")
            inner = digest(("campaign-tactical-review-package-v1\n"+digest(raw["core.json"])+"\n"+digest(raw["scenario.json"])+"\n").encode("ascii"))
            outer = digest(("campaign-physical-review-package-v2\n"+digest(raw["package.json"])+"\n"+inner+"\n").encode("ascii"))
            require(outer == EXPECTED_PACKAGE_HASHES[profile], "Pinned package hash changed: "+profile)
            packages[profile] = outer
        except (ValueError, KeyError, TypeError):
            errors.append("Malformed copied review package: "+profile)
    if errors:
        raise ValueError("\n".join(errors))
    return packages

def main():
    root = Path(__file__).resolve().parents[1]
    inventory = inspect_plugins(root)
    packages = verify(root, inventory)
    print("Review import PE target/API/dependency, DLL byte, metadata and resource/hash checks passed.")
    print(json.dumps(packages, sort_keys=True))
    print("DEPENDENCY: review asmdefs/linker/launch/runtime adapter are outside this import package; combine and validate when delivered.")
    print("Unity compilation, Newtonsoft roundtrip, IL2CPP and native/input/render checks NOT performed.")

if __name__ == "__main__":
    try:
        main()
    except (ValueError, subprocess.CalledProcessError) as error:
        sys.exit(str(error))
