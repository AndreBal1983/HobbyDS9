# Audio output listener defect fix

Base: remote main `c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4`. Isolated branch: `codex/unity-audio-listener-fix`. 14 October 2026.

## Reproduction and cause

The owner reported no music or LCARS SFX in the review-built Windows executable launched into the ordinary strategic client without review flags, despite an unmuted/nonzero audio panel displaying Deep Station. The [review builder](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Editor/ReviewBuild.cs) includes the empty PhysicalReview scene. The only authored AudioListener is in SampleScene. Ordinary ClientEntry startup constructs both audio services, but the original services created sources without providing a listener.

Actual Unity `6000.6.5f1` regression execution on the unchanged original directors confirmed **zero active listeners after successful service initialization** in an empty scene, in both music-first and SFX-first orders. The two cases failed specifically on expected listener count 1 / actual 0 (`artifacts/audio-listener/baseline-empty-red.xml`). Two earlier regression harness runs failed on Editor scene-creation restrictions and are not claimed as defect reproduction. This proves a missing engine output prerequisite; it does not prove that it is the only possible cause of silence on the owner's hardware.

## Narrow change

[AudioOutputListener](../client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/AudioOutputListener.cs) is a shared service-owned fallback, separate from both source pools. Each director acquires it idempotently and releases its ownership on destruction. In play it persists across scene replacement. It enables its one listener only when no other active listener exists, yields to an existing/late scene listener, and takes over again after that listener disables/disappears. Existing scene listeners and global listener volume/pause are never changed. The fallback is disabled/released when the last service exits; a deferred-destroy instance is not reused during immediate re-entry. Authored scenes with multiple existing listeners remain their owners' configuration; this helper does not rewrite them.

Music still owns exactly two sources; SFX owns four bounded one-shot sources plus processing. SFX's factory now initializes explicitly even when EditMode does not invoke Awake, and repeated acquisition does not add sources or reset preferences. There is no new AudioSource, gain/mute policy, cue timing, catalog, clip, import setting, Windows setting, save field, simulation rule, RNG draw or gameplay activation.

Only AudioDirector, SfxDirector, this narrowly scoped helper and metadata, focused tests and this report change. ClientEntry, ReviewEntry, shared scripts and project audio/settings assets are not part of the commit. Unity import-generated files/settings remain uncommitted in the isolated checkout; no user work, previous build or save was reset/discarded.

## Executed acceptance

- Both default and opt-in review Prepare targets passed in the fresh checkout.
- Portable MusicPolicyTests/SfxPolicyTests: **42 passed**, no failures/skips.
- Full graphics-enabled actual Unity EditMode: **128/128 passed**, no failures/skips, including the seven new cases in [AudioOutputTests](../client/HobbyDS9.Unity/Assets/HobbyDS9/Tests/Editor/AudioOutputTests.cs). A final execution after removing new deprecated lookup calls is recorded as `artifacts/audio-listener/editmode-final.xml` / `.log`.
- Real play-mode entry/exit inside the EditMode suite verifies persistent services/listener across scene replacement; automatic handover to/removal of a scene listener across actual frames; last-owner release in both destruction orders; and subsequent re-entry.
- Empty-scene initialization in both orders, repeated calls, exact source counts, ordinary authored SampleScene listener preservation, disabled/late scene listeners and preferences/global volume/pause preservation are covered.
- All six actual imported SFX clips load/decode successfully and contain nonzero PCM samples. Existing catalog tests verify both full music cues, references and streaming import configuration. This is asset/signal data evidence, **not hardware audibility**.
- Default Unity static integration checks passed, including original clip hashes, metadata, copied DLL boundaries and unchanged legacy scenario SHA-256 `3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`. No golden baseline was updated.

Older experimental test results are retained in ignored artifacts and superseded by final results. Existing Unity source API deprecation and ICU/URP warnings remain possible; no zero-warning, full GPU, audio-device or international-text claim is made.

## Remaining coordinated acceptance

No Windows player was launched, no desktop input/sound settings were controlled, and no new IL2CPP build was produced for this isolated fix. PM will combine the independently delivered UI work and this audio fix for one build. The owner must confirm physical music/SFX audibility, independent controls and music transition/save-load continuity on that combined executable. Save/content/rules/compiler identities and default-v1 campaign behavior are unchanged; gameplay determinism was not re-baselined or independently rerun for this presentation-only change. Draft delivery only; no merge or default activation.
