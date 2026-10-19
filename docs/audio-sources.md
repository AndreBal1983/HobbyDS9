# Audio sources — personal development prototype

The cloud attempts on 2026-10-11 to https://www.trekcore.com/audio/ and https://www.stdimension.org/MediaLib/computere.htm failed at the proxy with tunnel HTTP 403. The user subsequently supplied the six Federation WAVs locally, committed in accepted handoff `34c2c6660d2901de2257d808e1b852504bd38b50`. Consolidation preserves their exact bytes and metadata GUIDs; no downloads, substitutes or re-encoding occurred.

All six assets are under `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/`. Intended source site: **Star Trek Dimension**, page https://www.stdimension.org/MediaLib/computere.htm, Federation group. Displayed item names below come from the supplied milestone prompt; the page was not independently accessible. Exact direct download URLs and original retrieval dates were not supplied and remain unverified. Hashes identify the accepted user-supplied bytes, not an independently verified source download.

| Semantic ID | Displayed item | Local filename | SHA256 |
|---|---|---|---|
| `buttonPress` | Key beep 1 | `federation-key-beep-1.wav` | `2a1dedbe17e3d2a51ca33e7e3f90c4ff0eed62fdb4d4a9563cb4ff874b564bb6` |
| `success` | Input successful 1 | `federation-input-successful-1.wav` | `1663cb5681034096ca2bed9a51e4fe834d40677a150406f519a915dd3873f77e` |
| `error` | Access denied | `federation-access-denied.wav` | `95d8d7468eeff8937318308bb8a09b9afa7367cbc492898c9d4637f382ddcddb` |
| `processing` | Processing ... 1 | `federation-processing-1.wav` | `8608639986663f2d33e27b58e39de809ab0199cc284cc2b645a55577131d7c51` |
| `screenOpen` | Screen display 1 | `federation-screen-display-1.wav` | `0f09b73814439ba383a15c5831a1a5a7d65f1a31a418c7fd8969ae207d98c5f8` |
| `search` | LCARS search | `federation-lcars-search.wav` | `538dd9a507f6a278d37b8cc0d9ef013d4039288e9f43a4a332b1fc4ec4584331` |

Attribution/source statement supplied by the prompt: files originate from Star Trek series, movies and games; copyright remains with respective holders. Star Trek Dimension requests credit and a link for public use of enhanced versions. The site statement could not be independently rechecked. Redistribution permission is **unclear** for each item; no public-domain or royalty-free claim is made. These are used for personal development/prototyping. This records provenance, not a legal conclusion.

The accepted Unity importer metadata is retained unchanged: Decompress On Load (`loadType: 0`), Vorbis (`compressionFormat: 1`), preload disabled (`preloadAudioData: 0`), background loading disabled, channel preservation (`forceToMono: 0`), `normalize: 1` serialized, sample-rate preservation (`sampleRateSetting: 0`). Normalization is relevant to force-to-mono conversion, which is disabled; source WAV bytes are unchanged. The importer serializes `3D: 1`; playback sources explicitly use 2D `spatialBlend: 0`. No streaming. This documents the actual locally accepted settings rather than the earlier suggested defaults.

`Resources/LcarsSfxCatalog.asset` is the canonical catalog: all six references are populated and match tracked WAV metadata GUIDs. The accidental troubleshooting catalog was removed. Both Deep Station music files and their import settings remain byte-for-byte unchanged.


## Quiet Frontier — generated music resource

Added at the project owner's request on 2026-10-16.

- Asset: [`Quiet Frontier.mp3`](../client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/Music/Quiet%20Frontier.mp3).
- Intended use: instrumental exploration/diplomacy background music; warm French horns, gentle strings and woodwinds.
- Generated using ElevenLabs Music v2 (`eleven_music_v2`), with a prompt requesting an original melody, no vocals and restrained orchestral dynamics.
- Flow ID: `CDM77NowotSrhAcnopqr`; generation ID: `f40OkHx7Bl7Q1kJEBnk2`.
- Source: unmodified generated MP3, 4,338,322 bytes; stereo 48 kHz; encoded duration 180.024 seconds.
- SHA-256: `11f41a38b16b4a59b7e6c9c2919bd31a9d04384c22f79b4a62523c49cc9153ad`.
- Unity GUID: `c0e242ee197e4d9d9e2072837dc3186c`. Import settings match the existing music assets: Streaming, Vorbis quality 0.85, preserve sample rate/stereo, background loading, preload disabled.

Available as an audio resource; not yet registered in `Resources/MusicCatalog.asset`. The integration agent can register it with stable ID `music.quiet-frontier`, display name `Quiet Frontier`, Strategic category and FullCue policy. It has not been edited or verified as a seamless loop. Audition transitions and choose crossfade/quiet-tail values during local Unity acceptance. Existing playlist behavior is unchanged.


## Across the Stars — generated music resource

Added at the project owner's request on 2026-10-16.

- Asset: [`Across the Stars.mp3`](../client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/Music/Across%20the%20Stars.mp3).
- Intended use: Original late-Romantic piano concerto with acoustic orchestra; major discoveries and sweeping journeys.
- Generated using ElevenLabs Music v2 (`eleven_music_v2`) from the owner's musical brief, with original melody and instrumental-only settings. Named musical references were expressed as descriptive musical qualities.
- Flow ID: `iSfmjLNsF2OAP7vPuw5E`; generation ID: `evDZiC9YyGkCk4ER0tI4`.
- Source: unmodified generated MP3, 8,658,322 bytes; stereo 48 kHz; encoded duration 360.024 seconds.
- SHA-256: `8449a257fe7322f959e5a24ebb4f1b42a05fa95ac38a7b6d1f4a0cad39d1b32a`.
- Unity GUID: `22b9873a967243259ce547af180f0da1`. Import settings match the existing music assets: Streaming, Vorbis quality 0.85, preserve sample rate/stereo, background loading, preload disabled.

Available as an audio resource; not yet registered in `Resources/MusicCatalog.asset`. Suggested stable ID: `music.across-the-stars`; display name: `Across the Stars`; FullCue policy. The integration agent should choose playback context and audition crossfade/quiet-tail values locally. The source is not verified as a seamless loop. Existing playlist behavior is unchanged.


## Secrets in the Courtyard — generated music resource

Added at the project owner's request on 2026-10-16.

- Asset: [`Secrets in the Courtyard.mp3`](../client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/Music/Secrets%20in%20the%20Courtyard.mp3).
- Intended use: Restrained Cardassian courtyard instrumental; low strings, bass clarinet, bassoon and understated intrigue.
- Generated using ElevenLabs Music v2 (`eleven_music_v2`) from the owner's musical brief, with original melody and instrumental-only settings. Named musical references were expressed as descriptive musical qualities.
- Flow ID: `gT2vsBXYKuf9MzJ3WTFk`; generation ID: `Z7zf7hZdyn4usOUsPMkN`.
- Source: unmodified generated MP3, 1,938,706 bytes; stereo 48 kHz; encoded duration 80.040 seconds.
- SHA-256: `c6b4ed4cfd02a527fa8f49c2cf759cd7d3052ce9188a0552a3f86487bbec2128`.
- Unity GUID: `106b49eb6ae14ac1af9aa7cfc8b4c852`. Import settings match the existing music assets: Streaming, Vorbis quality 0.85, preserve sample rate/stereo, background loading, preload disabled.

Available as an audio resource; not yet registered in `Resources/MusicCatalog.asset`. Suggested stable ID: `music.secrets-in-the-courtyard`; display name: `Secrets in the Courtyard`; FullCue policy. The integration agent should choose playback context and audition crossfade/quiet-tail values locally. The source is not verified as a seamless loop. Existing playlist behavior is unchanged.

## 19 October catalog integration candidate

The three owner-delivered MP3s and importer GUIDs above are preserved byte for byte. `MusicCatalog.asset` now references all five music files so Unity includes them in a reproducible player. The original Deep Station entries, order and timing hints remain unchanged. Quiet Frontier follows them in the Strategic playlist. Across the Stars and Secrets in the Courtyard are registered in the existing Event category, reserved for explicit future event presentation; this change does not invent discoveries, diplomacy or faction-specific triggers. The current strategic flow does not switch to that category.

All new entries use FullCue, a provisional six-second crossfade and zero assumed quiet tail. No seamless loop or measured silence claim is made. These are reversible presentation settings pending local transition audition. Original Deep Station playback is still first, and the current frozen PR45 executable is unchanged.

Static source hashes, importer settings and catalog GUID/context references are checked for all five cues. The existing receiving Editor catalog test also checks playable imported clips, category order and streaming settings; its updated case has not yet run on this candidate. No Unity import, native playback or human listening acceptance is claimed while native control is stopped.
