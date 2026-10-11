# Audio sources — personal development prototype

Existing Deep Station music provenance remains in the client README; its original bytes/import settings are unchanged.

On 2026-10-11 HTTPS access to both https://www.trekcore.com/audio/ and https://www.stdimension.org/MediaLib/computere.htm failed at the cloud proxy: tunnel HTTP 403. No LCARS audio was retrieved. The following coherent Federation set is requested from **Star Trek Dimension**, page https://www.stdimension.org/MediaLib/computere.htm. These are displayed item names supplied by the milestone prompt, not independently verified page text.

| Semantic ID | Displayed Federation item | Intended local filename |
|---|---|---|
| `buttonPress` | Key beep 1 | `federation-key-beep-1.wav` |
| `success` | Input successful 1 | `federation-input-successful-1.wav` |
| `error` | Access denied | `federation-access-denied.wav` |
| `processing` | Processing ... 1 | `federation-processing-1.wav` |
| `screenOpen` | Screen display 1 | `federation-screen-display-1.wav` |
| `search` | LCARS search | `federation-lcars-search.wav` |

All intended files belong under `client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/`. Direct download URLs, actual codec, retrieval dates and SHA256: **unavailable for all six**, because no files were retrieved. WAV filenames are provisional; preserve original bytes and extensions when supplying the originals. The six catalog entries are present with null clip references, which safely produce silence.

Attribution/source statement supplied by the prompt: these files originate from Star Trek series, movies and games; copyright remains with respective holders. Star Trek Dimension requests credit and a link for public use of enhanced versions. The site statement could not be independently checked here. Redistribution permission is **unclear** for every requested item; no public-domain or royalty-free status is claimed. This records provenance for personal development/prototyping, not a legal conclusion. No voice prompts or substitute/generated sounds are included.

For local import, use Decompress On Load, preload enabled, background loading disabled, preserve mono/stereo, no normalization, no streaming. Playback is 2D. Actual audio importer settings cannot be verified before originals are supplied. Existing music settings are untouched.
