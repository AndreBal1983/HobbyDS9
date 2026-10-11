# LCARS SFX foundation v1 candidate

The integrated acceptance consolidation uses only handoff `34c2c6660d2901de2257d808e1b852504bd38b50`, descended from SFX candidate `e439b9b0e64c7705bc694107e826bb25375eace1`. Presentation only: save 12, content 8, rules `population-politics-geography-v1`, phased combat resolver v1 and campaign RNG are unchanged.

## Clips and import

Cloud downloads were blocked by HTTP 403. The user supplied all six Federation WAVs locally; they are now tracked with stable metadata and populated references in `Resources/LcarsSfxCatalog.asset`. [Provenance](audio-sources.md) records exact filenames, SHA256, source-page attribution limitations and actual accepted import settings. WAV and music bytes/import metadata are preserved. No runtime downloads or substitute effects were added.

## Service and integration

`SfxDirector` is a separate persistent singleton, leaving the existing music director's two sources intact. Initialization creates exactly four one-shot sources and one processing source; playback creates no sources; existing components are reused on managed reinitialization. Sources are 2D, play-on-awake disabled and non-looping. Policy uses presentation frame/DSP time only, with no campaign RNG/state dependency.

First root pointer-down or key-down arms SFX. Bootstrap, geometry, resize, Refresh and AI orchestration cannot arm playback. The first button activation can beep. `SfxInputBinding` unregisters when the client disables. No synthetic input is produced during bootstrap. The `Requested` event is diagnostic notification of an armed semantic request, not evidence of audible playback; subscriber/audio failures are contained.

Identical cues are suppressed within the same frame; ButtonPress also has a 45 ms cooldown. Ordinary cues are dropped when all four sources are busy. Success/Error can replace the earliest-ending ordinary source (or earliest important source if all four are occupied). Each slot plays a single clip; no unbounded PlayOneShot layering. The missing-clip path doesn't consume a slot.

Processing has a dedicated source and plays once, without forced looping. A request while active is ignored; after natural completion or explicit Stop it can start again. No current action has justified perceptible latency, so processing is available but unused: no fake delay was added.

Audio adds SFX volume (default 60%) and Mute SFX with separate PlayerPrefs keys. Effective SFX gain is Master × SFX volume, or zero when SFX is muted. Music and SFX mutes are independent. Master changes update both directors without restarting music. No campaign-save fields are involved.

Explicit LCARS navigation/action buttons request ButtonPress. Deliberate section changes request ScreenOpen; reopening Production requests it only when the panel was closed. Same-tab Refresh and responsive layout changes request no cue. Passive focus changes synchronize the remembered section silently, so a later deliberate reopening still cues. Human combat focus may request one opening cue; AI-only processing stays silent. Selecting a route destination requests Search when the authoritative preview succeeds, Error when invalid. Hover is silent.

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

Windows target reruns EditMode before building x64 IL2CPP. Inspect `artifacts/unity/editmode.xml`, `editmode.log` and `windows-build.log`. The user reported Unity **6000.6.5f1** local EditMode **50/50 passed** on the handoff after headless compatibility fixes, and a successful Windows x64 IL2CPP build. Music playback, soundtrack transition/crossfade by listening, LCARS SFX playback and Music/SFX volume/mute independence passed locally. Responsive LCARS UI was exercised in the Windows player; AI-only combat non-interruption and human combat/report flow were part of that accepted build.

Consolidation restores three accidentally deleted, headless-safe vector-elbow cases (20×20, 104×64, 180×90). Final authored count is **53**, including 8 SFX cases. The 50/50 report applies to the handoff before those cases were restored; no 53/53 Unity execution is claimed in cloud, which has no Editor. Headless tests do not use EditorWindow.Show or synthetic detached UI event dispatch. Runtime first-pointer/keyboard arming remains a local/manual check.

Manual integrated checklist:

- For future local reruns, verify all six catalog references and cues (Processing explicitly, since no current operation needs it).
- Startup silent until genuine pointer/keyboard input; first click works; rapid clicks remain bounded, processing cannot overlap and Stop ends it.
- Major navigation/Production opening cue once; repeated refresh, hover, resizing and AI-only combat silent. Successful commands/save/load/human combat acknowledge; rejected stale/invalid actions and failures use Error.
- Master affects music/SFX; both volumes and mutes independent; restart preserves preferences; music continues and crossfades without duplicate sources across saves/loads.
- Verify 1280×720, 1600×900, 1920×1080, 2560×1440, 3440×1440, 5120×1440 and live resize: map usable, no overlap, elbows and Production dock/drawer intact.
- Verify research, enqueue/cancel, governance, route preview/submit/partial movement, save/load, non-interrupting AI-only combat, human-only plans, buffered reports/history and explicit finalization.

Local integrated acceptance has been supplied by the user. Consolidation permits only a verified fast-forward to main after cloud gates and exact candidate CI pass. VSync, frame-rate caps and graphics options are deferred; none are implemented or claimed tested here.
