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
