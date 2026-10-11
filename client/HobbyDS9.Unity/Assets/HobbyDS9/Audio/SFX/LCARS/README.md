# Missing LCARS source clips

Both authorized sites returned proxy HTTP 403 on 2026-10-11. No sound bytes were imported or synthesized. See `docs/audio-sources.md` for the six requested Federation items. The catalog intentionally has null references and playback is silent.

Supply original files, preserving their actual codec/extension, in this folder. Assign each AudioClip to its semantic entry in Resources/LcarsSfxCatalog. Import as Decompress On Load, preload enabled, background loading disabled, preserve channels, normalization disabled; do not stream these short clips. Commit Unity-generated metadata and record actual filenames, direct URLs and SHA256 in the provenance document. Intended WAV filenames are provisional until original formats are verified.
