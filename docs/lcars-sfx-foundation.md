# LCARS SFX foundation v1 candidate

Stacked on responsive LCARS commit `85c4b9311ef31ff0b6856a1e1d39470244bb7c8b` on `codex/lcars-sfx-foundation-v1`. Main and earlier candidate branches remain unchanged. Presentation only: save 12, content 8, rules `population-politics-geography-v1`, phased combat resolver v1 and campaign RNG remain unchanged.

## Clips and import

**No SFX bytes could be downloaded:** both authorized sites failed with proxy HTTP 403. [Source manifest](audio-sources.md) lists all six requested Federation items, intended filenames and unavailable URL/hash status. `Resources/LcarsSfxCatalog.asset` contains explicit semantic entries with null AudioClip references. Missing catalog or clips produce silence. No generated or unrelated effects were substituted. There are no nonexistent-file metadata or runtime downloads.

Supply the original non-voice files in `Assets/HobbyDS9/Audio/SFX/LCARS`, preserving bytes and actual codec/extension. Assign references in LcarsSfxCatalog. Provisional `.wav` filenames should be updated to match originals. Set short clips to Decompress On Load, preload enabled, background loading disabled, normalization disabled, preserve channels; no streaming. Record actual paths, retrieval dates, direct URLs and SHA256 in audio-sources.md, and commit generated import metadata. Music bytes/settings are unchanged. Audible acceptance requires these clips first.

## Service and integration

`SfxDirector` is a separate persistent singleton, leaving the existing music director's two sources intact. Initialization creates exactly four one-shot sources and one processing source; playback creates no sources; existing components are reused on managed reinitialization. Sources are 2D, play-on-awake disabled and non-looping. Policy uses presentation frame/DSP time only, with no campaign RNG/state dependency.

First root pointer-down or key-down arms SFX. Bootstrap, geometry, resize, Refresh and AI orchestration cannot arm playback. The first button activation can beep. `SfxInputBinding` unregisters when the client disables. No synthetic input is produced during bootstrap. The `Requested` event is diagnostic notification of an armed semantic request, not evidence of audible playback; subscriber/audio failures are contained.

Identical cues are suppressed within the same frame; ButtonPress also has a 45 ms cooldown. Ordinary cues are dropped when all four sources are busy. Success/Error can replace the earliest-ending ordinary source (or earliest important source if all four are occupied). Each slot plays a single clip; no unbounded PlayOneShot layering. The missing-clip path doesn't consume a slot.

Processing has a dedicated source and plays once, without forced looping. A request while active is ignored; after natural completion or explicit Stop it can start again. No current action has justified perceptible latency, so processing is available but unused: no fake delay was added.

Audio adds SFX volume (default 60%) and Mute SFX with separate PlayerPrefs keys. Effective SFX gain is Master × SFX volume, or zero when SFX is muted. Music and SFX mutes are independent. Master changes update both directors without restarting music. No campaign-save fields are involved.

Explicit LCARS navigation/action buttons request ButtonPress. Deliberate section changes request ScreenOpen; reopening Production requests it only when the panel was closed. Same-tab Refresh and responsive layout changes request no cue. Human combat focus may request one opening cue; AI-only processing stays silent. Selecting a route destination requests Search when the authoritative preview succeeds, Error when invalid. Hover is silent.

`SfxActionFeedback` classifies typed CommandResult/TurnResult/CombatSubmissionResult; `SfxFeedback` shares accepted/rejected command reporting for research, production/cancel, governance and submitted routes. Save/load acknowledge only after their explicit operations succeed. Human combat buffering/finalization acknowledge verified accepted outcomes. User operation exceptions produce Error centrally; pure presentation and Continue-to-next-report get no fabricated success. Automatic AI combat results have no feedback hooks. Combat orchestration/resolver and Application APIs are unchanged.

## Verification and local acceptance

Cloud verification is locked .NET restore, Release build, full suite, portable integration preparation, metadata/assembly/SFX static checks and canonical/reverse/repeated 25-turn headless smokes. Static checks don't compile Unity or prove audible behavior. Expected final save SHA256:

`10009857d0396dea4bd0a81001cadfb6b5ebfad2c8772223d8288bd70d3559e4`

From repository root on Windows, close the Editor and use licensed Unity **6000.6.5f1**, Windows Build Support **IL2CPP**, Visual Studio Desktop development with C++ and Windows SDK:

```powershell
$unity = "C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=$unity"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=$unity"
.\artifacts\unity\Windows\HobbyDS9.exe
```

Windows target reruns EditMode before building x64 IL2CPP. Inspect `artifacts/unity/editmode.xml`, `editmode.log` and `windows-build.log`. Expected authored Unity suite: **54 cases**, including 9 SFX cases; these are not executed in cloud.

Manual integrated checklist:

- Supply/assign the six originals, verify provenance/hash and import settings; then check all six cues through catalog/service (Processing explicitly, since no current operation needs it).
- Startup silent until genuine pointer/keyboard input; first click works; rapid clicks remain bounded, processing cannot overlap and Stop ends it.
- Major navigation/Production opening cue once; repeated refresh, hover, resizing and AI-only combat silent. Successful commands/save/load/human combat acknowledge; rejected stale/invalid actions and failures use Error.
- Master affects music/SFX; both volumes and mutes independent; restart preserves preferences; music continues and crossfades without duplicate sources across saves/loads.
- Verify 1280×720, 1600×900, 1920×1080, 2560×1440, 3440×1440, 5120×1440 and live resize: map usable, no overlap, elbows and Production dock/drawer intact.
- Verify research, enqueue/cancel, governance, route preview/submit/partial movement, save/load, non-interrupting AI-only combat, human-only plans, buffered reports/history and explicit finalization.

Do not merge this candidate to main before local integrated acceptance.
