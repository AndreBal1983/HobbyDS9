# Integrated acceptance consolidation v2 audit

Source of truth: `local/integrated-acceptance-handoff` at **34c2c6660d2901de2257d808e1b852504bd38b50**, verified descendant of **e439b9b0e64c7705bc694107e826bb25375eace1**. Work branch: `codex/integrated-acceptance-consolidation-v1`, created directly from the handoff. Main was **90b324abd1701fe52b3b017e7d4ee69df288a575** at inspection; only a verified fast-forward is authorized after candidate CI passes.

## Retained local handoff changes

- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-access-denied.wav`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-access-denied.wav.meta`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-input-successful-1.wav`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-input-successful-1.wav.meta`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-key-beep-1.wav`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-key-beep-1.wav.meta`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-lcars-search.wav`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-lcars-search.wav.meta`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-processing-1.wav`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-processing-1.wav.meta`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-screen-display-1.wav`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/federation-screen-display-1.wav.meta`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Resources/LcarsSfxCatalog.asset`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/LcarsShell.cs`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Tests/Editor/LcarsTests.cs`
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Tests/Editor/SfxTests.cs`

All six WAVs and their metadata are byte-for-byte identical to the handoff. The canonical Resources catalog retains all six non-null clip references matching those GUIDs. Its existing metadata is unchanged. The StyleLength(0f) compatibility fixes are preserved without changing layout rules. Headless-safe semantic/policy and repeated logical-layout tests remain; no EditorWindow.Show or synthetic detached UI event dispatch is reintroduced.

## Narrow consolidation changes

- Removed only `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/New Lcars Sfx Catalog.asset` and its `.meta`: unused troubleshooting duplicate with empty semantic labels.
- Restored `VectorElbowAcceptsContentSizesWithoutFrameDrivenGeometry`, cases 20×20, 104×64 and 180×90, with construction, PickingMode.Ignore, CanDraw and non-throwing MarkDirtyRepaint assertions. No graphics device is required. Renamed the repeated layout test to reflect its actual headless execution rather than attached geometry dispatch.
- Updated provenance, source-folder/client/root READMEs and LCARS/SFX status documentation to describe actual accepted assets and local reports.
- Static checks now require exact accepted WAV SHA256, semantic-to-GUID clip mappings, preserved importer settings, absence of the duplicate catalog, restored elbow cases and clean tracked-file hygiene. Earlier scaffold preload assumptions were replaced with the locally accepted metadata (preload disabled); no importer settings were changed.

There are no missing expected local fixes/assets. No other unexpected handoff files were found. Music/SoundFX scratch folders, .vsconfig, Assets/Resources.meta, Backup, UpgradeLog and uncommitted Settings/ProjectSettings changes were not in the handoff and are not added. Existing project settings are preserved.

## Acceptance evidence and limits

User-reported on Unity 6000.6.5f1: 50/50 EditMode passed before restoration of the three elbow cases; Windows x64 IL2CPP build succeeded; music played and transition/crossfade sounded correct; LCARS SFX played; Music/SFX mute/volume independence passed; responsive Windows-player UI was exercised; AI-only combat non-interruption and human combat/report flow were part of the accepted integrated build. Final authored count is 53, including 8 SFX cases. Cloud has no Unity Editor; no new 53/53 execution or audible/manual verification is claimed.

Required cloud gates: locked restore, zero-warning/error Release build, full .NET suite (including save/reload), portable Unity preparation/static audit and canonical/reverse/repeated 25-turn runs. CI runs these gates on Ubuntu, Windows and macOS. Final candidate and exact main CI results are reported separately after each push.

Invariant save SHA256: `10009857d0396dea4bd0a81001cadfb6b5ebfad2c8772223d8288bd70d3559e4`. Save 12, content 8, rules `population-politics-geography-v1`, resolver `combat-resolver.phased` v1, RNG unchanged. No authoritative source/content changes from the accepted SFX stack; no VSync, graphics options, balance or gameplay changes.

[Six asset hashes/provenance](audio-sources.md). [Local verification commands and service policy](lcars-sfx-foundation.md).
