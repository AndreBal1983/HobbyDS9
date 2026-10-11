# Audio Foundation v1 candidate

This is a separate stacked branch, `codex/audio-foundation-v1`, based exactly on
`79009a3ceea831e0c772846eb6211d6243a6ccee` as requested. It neither moves nor merges
`codex/population-politics-strategic-geography-v1` or `main`. The later combat-control
candidate `f5a8293c9113b4d2f2f5a8f7a3e6794dec4c4fa2` is intentionally not part of this
base; accept/integrate that branch independently before combining milestones.

## Files and ownership

- `Assets/HobbyDS9/Audio/Music/Deep Station.mp3` and `Deep Station track 1.mp3`:
  user-supplied original Suno full cues, copied verbatim. No normalization,
  re-encoding, trimming or generated replacement of source files was performed.
- `Resources/MusicCatalog.asset`: serialized clip references, stable IDs
  `music.deep-station` and `music.deep-station-1`, names, category, loop policy,
  sequential order, per-cue crossfade and quiet-tail hints.
- `Runtime/MusicCatalog.cs`: presentation-only ScriptableObject/entry types.
- `Runtime/MusicPolicy.cs`: pure playlist, catalog validation, cue timing/fade
  envelopes and client settings-store abstraction, also linked into .NET tests.
- `Runtime/AudioDirector.cs`: persistent singleton, two reusable AudioSources,
  loading/fallback, DSP scheduling, gain envelopes and PlayerPrefs adapter.
- `Runtime/AudioPanel.cs`: compact Audio header tab, Master/Music sliders and Mute.
- `Tests/Editor/AudioTests.cs` and .NET `MusicPolicyTests.cs`: configuration,
  lifetime, playlist/timing/preferences and campaign-save isolation coverage.
- `scripts/check-unity-integration.py`: original-file hashes, catalog references,
  importer settings, metadata GUIDs and audio/campaign dependency separation.

Audio owns no campaign IDs/state, consumes no campaign RNG and calls no Application
API. No portable library changed. There is no music field in a save or snapshot.
Scene/view/session rebuilds reuse the director; it lives on a separate
DontDestroyOnLoad object with exactly two owned sources. Managed Editor state
rebuilds reuse existing sources instead of adding another pair. Destroying the
owner stops/releases its sources; imported clip assets remain Unity-owned.

## Sources and import

| File | Bytes | Encoded duration | SHA-256 |
| --- | ---: | ---: | --- |
| Deep Station.mp3 | 4251486 | 189.648 s | `94bd236d2f1c1e6916cae87a240e4d3987bec0bae0f9857a8c502f6b7454a512` |
| Deep Station track 1.mp3 | 4662365 | 201.888 s | `91c3e99e255c6dc5590000e204747ac16de55b342c84fbd2542e9b78d51ce91a` |

Both are stereo 48 kHz sources. Unity import uses **Streaming**, Vorbis quality
0.85, Preserve Sample Rate, no force-to-mono/normalization, background loading,
and **Preload Audio Data disabled in defaultSampleSettings**. No platform override
is introduced. Unity may encode its imported player asset as usual; the committed
MP3 source bytes remain intact. Streaming avoids holding two fully decompressed
three-minute cues in persistent buffers. The Resources catalog directly references
both clips, ensuring they are included in the Windows player.

Source files were accessible in the cloud and are already committed with their
metadata; there is no local copy step. Static CI validates their SHA-256 values.

## Playback and settings

Both entries are Strategic / FullCue. Menu, Combat and Event categories exist for
future content. Ordering is sequential (Deep Station first), with no immediate
repeat when multiple playable cues remain. Missing references, failed clips and
invalid/duplicate metadata are skipped; an empty playlist is silent and leaves
the campaign usable. Playback failures are warnings, not gameplay exceptions.

The director initially schedules the first cue approximately one DSP second ahead
and fades in over two seconds. It prepares the other streaming source two seconds
before the planned transition, uses PlayScheduled, and applies complementary
sine/cosine gain envelopes for the **six-second** overlap. Track metadata may adjust
the overlap. Neither supplied clip loops at its sample boundary. Full cues play
through most of their composed form; the departing source stops after the overlap
before its quiet tail. Explicit StopMusic fades out; mute changes gain only and
leaves the current source clocks running.

Read-only silence analysis at -50 dB found terminal quiet sections around 1.63 s
and 0.41 s. Catalog hints are **1.65 s** and **0.45 s**, respectively. With the encoded
durations, next-cue starts fall around 181.998 s and 195.438 s into playback. Unity
uses actual imported clip.length rather than hardcoded duration; audible local
acceptance may fine-tune these presentation-only hints if needed. Source audio is
never destructively edited. Short cues clamp overlaps/tail hints conservatively.
Only a future explicitly SeamlessLoop entry may set AudioSource.loop.

For v1, strategic music continues through selection, production/research/politics
panels, End Turn, save/load and AwaitingCombat. Those paths never restart it. Empty
future categories fall back to Strategic without restarting the current playlist.
A nonempty context change affects the next natural cue, avoiding rapid switching;
this is a small playlist service, not an adaptive score system.

PlayerPrefs keys `hobbyds9.audio.v1.master`, `.music`, `.muted` store bounded client
preferences. Defaults: Master 1, Music 0.65, unmuted. Master and Music multiply each
music source's fade gain; Master currently applies to the implemented music bus
(no SFX bus exists yet). Muting preserves volumes. Changing settings never restarts
or reallocates a track/source. Preference read/write failures leave in-memory
settings and gameplay functional. Playlist position and playback timestamps are
not saved, including in PlayerPrefs.

## Cloud verification

Locked restore, Release build with zero warnings/errors, **747 .NET tests**,
portable Unity preparation and static checks pass. The canonical/reverse 25-turn
runs still produce:

`10009857d0396dea4bd0a81001cadfb6b5ebfad2c8772223d8288bd70d3559e4`

Turn 26 / revision 187, 17 encounters, 9 ships/fleets, next entity 97 and campaign
RNG `d8919406d6e01cf1` are unchanged. Save 12, content 8, rules
`population-politics-geography-v1`, phased combat resolver v1 and scenario bytes
are unchanged. This branch adds 15 device-independent .NET cases and three Unity
EditMode cases, bringing its expected EditMode total to **24** (21 from the base).

Cloud has no Unity Editor/audio device/Windows IL2CPP toolchain. Static checks and
policy tests are not Unity compilation, imported-audio quality, audible playback,
EditMode execution or player-build verification. Those gates remain local.

## Exact local acceptance

In a clean Windows checkout, preserve any current local work before switching:

```powershell
git fetch origin
git switch --track origin/codex/audio-foundation-v1
# If the local branch already exists instead:
# git switch codex/audio-foundation-v1
# git pull --ff-only

dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
```

Open `client/HobbyDS9.Unity` with **Unity 6000.6.5f1**, allow audio import, and require
zero compilation errors. Inspect both MP3 import settings and the Resources catalog.
Close the Editor for batch verification:

```powershell
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
```

Require **24/24 EditMode passes**, then Windows x64 IL2CPP build/launch. Windows
Build Support (IL2CPP), Visual Studio Desktop development with C++ and a compatible
Windows SDK are required. Logs/XML/player go under ignored `artifacts/unity`.
Launch `artifacts/unity/Windows/HobbyDS9.exe` and check:

1. The real scenario loads and Deep Station begins after a short initial fade-in.
2. Listen through at least two transitions (about 6½ minutes): both full cues play,
   rotation is smooth, with no boundary cut or obvious silent-tail gap. Check at
   normal and low music volume; metadata tuning remains subject to listening.
3. Audio tab Master/Music sliders and Mute work without restarting the cue. Unmute
   continues its current timeline. Quit/relaunch and confirm preferences persist.
4. Selection, panels, End Turn and combat continue strategic music. Save/load and
   repeated session reloads neither restart it unnecessarily nor layer sources.
   In Play Mode inspect HobbyDS9 Audio: one director, exactly two 2D sources.
5. Verify politics/geography/production UI and all four display sizes as on the
   base candidate. Audio changes must not alter campaign turn/revision/RNG/save.
6. To check missing-content fallback safely, use a temporary local catalog override
   with one then both clip references cleared; require the remaining cue or silence
   with a usable game, then restore the catalog before committing anything.
7. Return EditMode XML, IL2CPP launch and audible transition feedback. Keep Library,
   logs, imported caches and builds out of Git. Do not merge either candidate until
   its own acceptance is complete.
