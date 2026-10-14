# Local YuE2 music generation — art director procedure

## Status and scope

**Available and verified, 14 October 2026:** local YuE2 generation through HOT-Step CPP 1.4.0, Windows x64 Vulkan, on the owner's AMD Radeon RX 9070 XT (16 GB VRAM). No Suno subscription or cloud API key is required for music generation. AI lyric assistance is optional and requires a separately configured text model; handwritten lyrics work now.

**Proposed:** use this resource to produce and compare original music candidates for HobbyDS9. Prompt examples below are starting briefs, not approved faction themes. **Open:** final musical direction, approved cues, distribution clearance and Unity integration. Installation verification does not approve the test song as game content.

This is an art production workflow. It changes no gameplay, campaign saves, Unity settings or audio runtime. Read the [art direction index](README.md), [visual direction](visual-direction.md), [audio foundation](../audio-foundation-v1.md) and [audio source record](../audio-sources.md) before proposing integration. The audio foundation describes its own historical checkpoint, not the current test baseline.

## Installed resource

| Resource | Local location / value |
| --- | --- |
| Installation root | `C:\Users\ambal\AI\YuE2` |
| Launcher | `C:\Users\ambal\AI\YuE2\Start-YuE2.ps1` |
| Desktop shortcut | `YuE2 Music` |
| Browser interface | <http://127.0.0.1:3001> |
| Native engine | `http://127.0.0.1:8085` |
| Models | `C:\Users\ambal\AI\YuE2\app\models\yue2` |
| Generated audio | `C:\Users\ambal\AI\YuE2\app\data\audio` |
| Settings / local library | `C:\Users\ambal\AI\YuE2\app\data\hotstep.db` |
| Startup logs | `C:\Users\ambal\AI\YuE2\logs\server.log` and `server-error.log` |

Only the YuE2 model pack is installed, although the app lists other backends. Installed files are `yue2-lm-q8_0.gguf`, `yue2-vae-standard-f32.gguf` and the required `nar_lora_joint_v9.safetensors` decoder companion, about 4.6 GB total. Each has a `.verification.json` receipt recording its source revision and SHA-256. The companion is applied automatically by the engine; do not manually select it as a creative style adapter. Use the standard VAE; the legacy VAE is not installed.

The app and engine bind to `127.0.0.1`. These addresses are usable on this PC only. Models, runtime and generated audio stay outside Git; this guide travels with the repository. A different PC or cloud agent needs its own installation, not merely this localhost URL.

## Start and generate in the browser

1. Double-click **YuE2 Music**, or run the command below. The launcher reuses a running app, otherwise starts it hidden and opens the browser. Closing the browser does not stop the server.
2. Confirm the backend picker says **YuE2**, the LM is **Q8_0** and VAE is **standard**. Keep style adapters empty initially.
3. Open **Create**. Enter a musical brief in the style description: intended context, instrumentation, mood, energy, approximate tempo, development and ending. Put lyrics in the separate lyrics field, with sections such as `[verse]` and `[chorus]`.
4. For game background cues, enable **Instrumental** and leave lyrics empty. Start with **Chain of Thought: Full**, **one song**, **one variation**, **32 render steps**, and the stock solver/scheduler. Instrumental handling works best with the planned score; CoT Off can produce wordless vocals.
5. Generate one candidate at a time. Wait for completion, listen to the entire result and save the original WAV. Use the library/download control or find the file in the audio directory above. Save its matching `.score.abc` when present.
6. Produce several seeds for the same brief, then change one prompt or setting at a time. Record each seed and all changed settings. Matching seeds aid comparison; exact reproducibility across builds/drivers is not guaranteed.

```powershell
& 'C:\Users\ambal\AI\YuE2\Start-YuE2.ps1'
```

YuE2 chooses its own song ending, with an engine ceiling of roughly six minutes. It has no exact duration target. Mentioning duration or a loop in the prompt does not guarantee either. Review and edit an exported copy if a fixed length or seamless loop is needed; preserve the raw original. Start with full cues, consistent with the existing audio foundation.

### Proposed instrumental briefs

**Strategic map / exploration**

> Original instrumental science-fiction strategy underscore. Spacious warm strings, restrained French horns, soft analog pads and delicate bell textures. Calm curiosity with a little uncertainty, around 80 BPM. A clear but understated motif develops slowly over a gentle pulse. Broad dynamics without sudden loud impacts. No vocals, no choir, no spoken words. Resolve gently at the end.

**Station / diplomacy**

> Original instrumental space-station ambience. Warm electric piano, low strings, soft synthesizer textures and restrained percussion, around 70 BPM. Reflective, inhabited and hopeful, with subtle tension below the surface. Slow harmonic development and a quiet ending. No vocals, singing, choir or speech.

**Combat candidate**

> Original instrumental tactical science-fiction underscore. Tight string ostinatos, low brass, controlled electronic bass and measured percussion, around 120 BPM. Urgent and disciplined, leaving room for interface sounds. Develop a short original motif, build gradually and finish decisively. No vocals, choir or speech.

Describe musical qualities rather than asking to reproduce a franchise recording. Compare candidates against the intended game context; an impressive standalone song may be distracting under a strategy interface.

## PowerShell procedure for the art director agent

The installed local REST API was used for the verification render. It permits local automatic authentication; tokens are transient and must not be committed. Start the app first. Check the active backend before submitting: switching backends affects other users of this same server. If another generation is underway, coordinate and wait before changing selections.

This example submits a normal instrumental candidate and writes provenance/status into the repository's already ignored `artifacts` directory. It leaves the engine's natural song-ending defaults intact. It does not install models, import Unity assets or modify application code.

```powershell
$ErrorActionPreference = 'Stop'
$base = 'http://127.0.0.1:3001'
$repoRoot = 'E:\HobbyDS9' # adjust for your checkout
$out = Join-Path $repoRoot ('artifacts\music\yue2\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $out -Force | Out-Null

$backend = Invoke-RestMethod "$base/api/backends"
if ($backend.activeId -ne 'yue2') {
    throw 'Select YuE2 in the UI after coordinating with any other current user.'
}
$health = Invoke-RestMethod "$base/api/models/health"
if ($health.aceServer -ne 'connected') { throw 'Native engine is not ready; inspect startup logs.' }
$auth = Invoke-RestMethod "$base/api/auth/auto"
$headers = @{ Authorization = 'Bearer ' + $auth.token }
$request = @{
    backend = 'yue2'
    prompt = 'Original instrumental science-fiction strategy underscore, warm strings, restrained horns, soft analog pads, calm curiosity, 80 BPM, gradual development, gentle ending, no vocals or choir.'
    instrumental = $true
    lyrics = ''
    seed = 42
    randomSeed = $false
    yue2Pick = @{ lm = 'q8_0'; vae = 'standard' }
    yue2Cot = 'full'
    yue2OdeSteps = 32
    yue2BatchSize = 1
    yue2Variations = 1
}
$json = $request | ConvertTo-Json -Depth 5
$json | Set-Content (Join-Path $out 'request.json') -Encoding UTF8
$job = Invoke-RestMethod "$base/api/generate" -Method Post -Headers $headers -ContentType 'application/json' -Body $json
$job | ConvertTo-Json | Set-Content (Join-Path $out 'job.json') -Encoding UTF8
Write-Output "Job $($job.jobId); evidence directory: $out"
```

Poll in separate calls so the agent remains responsive; keep the job ID/output directory from the preceding call. A server restart invalidates auth tokens; obtain a new token if needed for subsequent authenticated requests.

```powershell
$status = Invoke-RestMethod "$base/api/generate/status/$($job.jobId)"
$status | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $out 'status.json') -Encoding UTF8
$status | Select-Object status, stage, progress, error
if ($status.status -eq 'failed') { throw $status.error }
if ($status.status -eq 'succeeded') {
    for ($i = 0; $i -lt $status.result.audioUrls.Count; $i++) {
        $audioUrl = $status.result.audioUrls[$i]
        if (-not $audioUrl.StartsWith('/audio/')) { throw 'Unexpected output URL; inspect result before downloading.' }
        $destination = Join-Path $out ("candidate-$i.wav")
        Invoke-WebRequest ($base + $audioUrl) -OutFile $destination
        Get-FileHash -LiteralPath $destination -Algorithm SHA256
    }
}
```

The API saves a song in the local library. Copying its returned local audio is an export, not a Unity import. The engine name `ace-server` and the health property `aceServer` are shared application internals; a connected health response alone does not identify the active model. Confirm `/api/backends` as above.

## Evidence and candidate review

For every retained candidate, save the request, seed, job ID, application version, exact model filenames/source receipts, WAV SHA-256, duration, channel/sample format and review notes. Record any later trim, fade, level adjustment or loop edit as a derivative with its own filename/hash. A useful tracked review note is:

```text
Candidate ID: music.strategic-exploration.yue2.001
Status: proposed / reviewed / owner-approved / rejected
Purpose and musical brief:
Generation date, HOT-Step version, GPU/backend:
Model files and source revisions:
Seed, CoT, render steps, adapters, other overrides:
Evidence folder, raw WAV path and SHA-256:
Duration, sample rate, channels:
Full-cue or edited-loop proposal:
Listening findings and comparison with other seeds:
Edits, derivative path and SHA-256:
Source/licence record and attribution:
Approval and implementation handoff:
```

Listen for unwanted vocals, distortion/clipping, clicks, excessive repetition, intrusive dynamics and abrupt endings. Test at realistic background volume alongside interface sounds. A loop proposal needs a checked seam; a prompt is not loop validation. Do not mark a cue approved based only on API success, waveform statistics or a short preview.

Keep working WAVs and logs in `artifacts/music/yue2` or the external installation directory. Commit small Markdown briefs/review records as appropriate. Approved audio import is a separate scoped handoff: preserve source bytes, document provenance and let the client owner handle catalog IDs, Unity metadata/import settings and playback acceptance. Do not replace the accepted Deep Station tracks or edit audio runtime merely to add this resource.

## Installation verification and troubleshooting

The 14 October verification generated **29.9987 seconds**, stereo **48 kHz / 16-bit PCM**, in **28.7 seconds**, with the engine reporting **YuE2 backend: Vulkan0**. Its WAV contained finite non-silent samples (RMS 0.12855, peak 0.69836). This verifies generation, not artistic quality or normal full-song performance. The installation test deliberately capped semantic generation at 750 frames; do not carry that cap into normal candidate requests.

Local evidence is at `C:\Users\ambal\AI\YuE2\test-request.json`, `test-result.json`, `test-audio-verification.json` and the model checksum receipts. The test WAV is `C:\Users\ambal\AI\YuE2\app\data\audio\7615ebb3-65dc-4297-9d07-faa4eda8e612.wav`.

- **Browser unavailable:** run the launcher and inspect the two startup logs. It waits up to about a minute for the UI. Do not start duplicate engine/server processes on the same ports.
- **Missing model:** confirm the three files above exist and `/api/backends/models?backend=yue2` lists `q8_0`. Do not select an uninstalled backend or legacy VAE.
- **Failed render / VRAM pressure:** record the error, use one song/variation and close competing GPU workloads such as Blender renders before retrying. Verify the log still reports Vulkan. Do not install CUDA for this AMD setup.
- **Instrumental contains vocals:** keep Full CoT, Instrumental enabled, lyrics empty and explicit no-vocal wording; try another seed and review the whole result. No prompt guarantees compliance.
- **Unexpected truncation:** inspect request overrides for `yue2SemMaxTokens`; the 750-frame installation test is intentionally capped.
- **Shutdown/restart:** closing the browser leaves services running. Before stopping anything, confirm there are no active jobs and identify this installation's Node/engine processes by their executable/command paths. Never kill all Node processes on the PC.

## Sources and provenance

- [HOT-Step CPP 1.4.0 Windows Vulkan release](https://github.com/scragnog/HOT-Step-CPP/releases/tag/v1.4.0) — installed application; archive verified against its published SHA-256.
- [HOT-Step backend procedures](https://github.com/scragnog/HOT-Step-CPP/blob/master/docs/user/backends.md) — upstream documentation can change after this installation.
- [YuE2 GGUF pack](https://huggingface.co/scragnog/YuE2-GGUF) and [required decoder companion](https://huggingface.co/Mothersuperior/yue2-mothersuperior-realaudio-tokenizer-v4) — exact downloaded revisions are in the local verification receipts.
- [Original YuE2 project](https://map-yue2.github.io/) — research/model context.

The installed model-weight `LICENSE` states **CC BY-NC 4.0** for the YuE2 checkpoint weights and provides source attribution; it distinguishes weights from separately licensed bundled material. Preserve this notice and the source/model information in provenance. This guide does not determine generated-output rights or approve redistribution/commercial use. Carry the applicable notices and any unresolved distribution questions into the asset handoff rather than labelling a generated candidate royalty-free.
