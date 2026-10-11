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

# Stylesheets need their built-in importer serialization, not just a GUID.
for name, importer in (("HobbyClient.uss", 12385), ("HobbyTheme.tss", 12388)):
    meta = (assets / "HobbyDS9/Resources" / (name + ".meta")).read_text(encoding="utf-8-sig")
    require("ScriptedImporter:" in meta and "serializedVersion: 2" in meta, f"Incomplete stylesheet importer: {name}")
    require(f"script: {{fileID: {importer}, guid: 0000000000000000e000000000000000, type: 0}}" in meta,
            f"Incorrect built-in stylesheet importer: {name}")

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
# Orchestration remains explicit; painting a panel must not advance a checkpoint.
combat_panel = (runtime / "CombatPanelView.cs").read_text(encoding="utf-8-sig")
require(".Pump()" not in combat_panel, "Combat panel rendering must not auto-resolve combat")
entry = (runtime / "ClientEntry.cs").read_text(encoding="utf-8-sig")
require("combat.RequiresPlayer" in entry and 'ShowEncounters(snapshot);' in entry,
        "Client must support non-interrupting AI combat and Planning report history")
client = (runtime / "StrategicClient.cs").read_text(encoding="utf-8-sig")
require("session.ResolveHumanCombat" in client and "session.SubmitCombatResult" in client,
        "Client combat must use frozen Application resolution plus stamped submission")
# LCARS presentation resources and boundary. These checks validate assets/tokens,
# not pixel-perfect rendering or Unity compilation.
for resource_name in ("LcarsTokens", "Lcars"):
    style = assets / "HobbyDS9/Resources" / (resource_name + ".uss")
    require(style.exists(), f"Missing LCARS stylesheet: {resource_name}")
    meta = Path(str(style) + ".meta").read_text(encoding="utf-8-sig")
    require("ScriptedImporter:" in meta and "serializedVersion: 2" in meta and
            "fileID: 12385" in meta, f"Incomplete LCARS stylesheet importer: {resource_name}")
    require(resource_name in re.findall(r'Resources\.Load<StyleSheet>\("([^"]+)"\)', entry),
            f"Unreferenced LCARS stylesheet: {resource_name}")
for file_name in ("ResponsiveLayoutPolicy.cs", "LcarsComponents.cs", "LcarsShell.cs"):
    code = (runtime / file_name).read_text(encoding="utf-8-sig")
    require(all(term not in code for term in ("HobbyDS9.Application", "HobbyDS9.Contracts", "SplitMix64")),
            f"LCARS components must remain presentation-only: {file_name}")
    require(not re.search(r'\bvoid\s+Update\s*\(', code), f"Frame-driven LCARS layout/geometry: {file_name}")
    require(not re.search(r'Resources\.Load<(?:Texture2D|Sprite|Font)>',code), f"Raster/font dependency in LCARS core: {file_name}")
for path in assets.rglob("*"):
    require(path.suffix.lower() not in {".ttf", ".otf", ".woff", ".woff2"},
            f"New font needs explicit license/provenance review: {path.relative_to(root)}")
# Cross-check the vector bar thickness with its stylesheet token to prevent drift.
tokens = (assets / "HobbyDS9/Resources/LcarsTokens.uss").read_text(encoding="utf-8-sig")
bar_css = re.search(r'--lcars-bar\s*:\s*([0-9.]+)px\s*;',tokens)
bar_geometry = re.search(r'const\s+double\s+Bar\s*=\s*([0-9.]+)\s*;', (runtime / "ResponsiveLayoutPolicy.cs").read_text())
require(bar_css is not None and bar_geometry is not None and float(bar_css.group(1)) == float(bar_geometry.group(1)),
        "LCARS vector bar thickness differs from the central style token")
# Bootstrap is singular; OnEnable reuses its document rather than accumulating it.
require(len(re.findall(r'AddComponent<ClientEntry>\s*\(',entry)) == 1,
        "Client bootstrap must create exactly one entry point")
require("GetComponent<UIDocument>" in entry, "Client re-enable must reuse UIDocument")

# Audio source integrity/import/reference checks. Never an audible-playback claim.
music = assets / "HobbyDS9/Audio/Music"
music_catalog = (assets / "HobbyDS9/Resources/MusicCatalog.asset").read_text(encoding="utf-8-sig")
for filename, digest, track_id in (
    ("Deep Station.mp3", "94bd236d2f1c1e6916cae87a240e4d3987bec0bae0f9857a8c502f6b7454a512", "music.deep-station"),
    ("Deep Station track 1.mp3", "91c3e99e255c6dc5590000e204747ac16de55b342c84fbd2542e9b78d51ce91a", "music.deep-station-1"),
):
    source_audio = music / filename
    require(source_audio.is_file(), f"Missing original soundtrack: {filename}")
    if source_audio.is_file():
        require(hashlib.sha256(source_audio.read_bytes()).hexdigest() == digest, f"Original soundtrack bytes changed: {filename}")
    meta = Path(str(source_audio) + ".meta").read_text(encoding="utf-8-sig")
    for setting in ("    loadType: 2", "    preloadAudioData: 0", "  loadInBackground: 1", "  forceToMono: 0", "  normalize: 0"):
        require(setting in meta.splitlines(), f"Unexpected music import setting {setting.strip()}: {filename}")
    guid = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.MULTILINE)
    entry = re.search(rf"(?ms)^  - id: {re.escape(track_id)}\n(.*?)(?=^  - id: |\Z)", music_catalog)
    require(entry is not None, f"Missing stable track ID: {track_id}")
    if entry is not None:
        require(guid is not None and f"guid: {guid.group(1)}" in entry.group(1), f"Catalog clip reference missing: {filename}")
        require("    loopPolicy: 0" in entry.group(1).splitlines() and "    category: 0" in entry.group(1).splitlines(),
                f"Supplied cue must be FullCue/Strategic: {track_id}")
for name in ("MusicPolicy.cs", "MusicCatalog.cs", "AudioDirector.cs"):
    code = (runtime / name).read_text(encoding="utf-8-sig")
    require(all(term not in code for term in ("HobbyDS9.Application", "HobbyDS9.Contracts", "HobbyDS9.Simulation", "SplitMix64")),
            f"Audio must remain independent of campaign APIs/RNG: {name}")
# SFX assets may deliberately be absent when source sites block retrieval.
sfx_catalog = (assets / "HobbyDS9/Resources/LcarsSfxCatalog.asset").read_text(encoding="utf-8-sig")
sfx_ids = {"buttonPress", "success", "error", "processing", "screenOpen", "search"}
require(set(re.findall(r"semanticId: (\w+)", sfx_catalog)) == sfx_ids, "Incomplete semantic SFX catalog")
catalog_guid = re.search(r"m_Script: .*guid: ([0-9a-f]{32})", sfx_catalog)
require(catalog_guid is not None and guids.get(catalog_guid.group(1)) == (runtime / "LcarsSfxCatalog.cs.meta").relative_to(root), "SFX catalog script reference is invalid")
provenance = root / "docs/audio-sources.md"
require(provenance.is_file(), "Missing SFX provenance")
if provenance.is_file():
    document = provenance.read_text()
    for semantic in sfx_ids: require("`" + semantic + "`" in document, "Missing SFX provenance: " + semantic)
    require("403" in document and "unclear" in document and "SHA256" in document, "Missing blocked-download/rights/hash status")
for name in ("SfxDirector.cs", "SfxPolicy.cs", "LcarsSfxCatalog.cs", "SfxFeedback.cs", "SfxActionFeedback.cs"):
    code = (runtime / name).read_text()
    require(not re.search(r"UnityWebRequest|HttpClient|WebClient|https?://|SplitMix64|RandomState|NextEntitySequence", code), "SFX runtime must be local and independent of campaign RNG: " + name)
sfx_service = (runtime / "SfxDirector.cs").read_text()
require(len(re.findall(r"AddComponent<AudioSource>",sfx_service)) == 1 and "SfxPolicy.PoolSize" in sfx_service, "SFX must centralize bounded AudioSource creation")
require("source.loop = false" in sfx_service and "source.spatialBlend = 0" in sfx_service, "SFX must remain non-looping and 2D")
for audio in (assets / "HobbyDS9/Audio/SFX/LCARS").iterdir():
    if audio.suffix.lower() in {".wav", ".mp3", ".ogg", ".aiff"}:
        require(audio.name in re.findall(r"intendedFilename: ([^\n]+)",sfx_catalog), "Uncatalogued SFX/possible voice file: " + audio.name)
        meta = Path(str(audio)+".meta").read_text()
        require("loadType: 0" in meta and "preloadAudioData: 1" in meta, "Short SFX should decompress on load and preload: " + audio.name)

scenario = root / "scenarios/ds9-slice/scenario.json"
resource = assets / "HobbyDS9/Resources/ds9-slice.json"
require(resource.exists(), "Scenario resource missing; run PrepareUnity.proj")
if resource.exists():
    require(scenario.read_bytes() == resource.read_bytes(), "Scenario bytes changed during integration")
    print("Scenario SHA256:", hashlib.sha256(resource.read_bytes()).hexdigest())
if errors:
    raise SystemExit("\n".join(errors))
print("Unity assets, metadata, reference boundary and portable copies: static checks passed (Unity not executed).")
