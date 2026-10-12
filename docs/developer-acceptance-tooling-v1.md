# Developer acceptance tooling v1

This local tooling validates a candidate without switching the primary checkout. It invokes the repository's existing solution and `scripts/PrepareUnity.proj`; it does not change gameplay, UI, content, versions, Unity settings, CI or that preparation project.

The preserved checkpoint is `codex/dev-acceptance-tooling-v1` at `a68f7fdf75dc7622002b7caf2b8a58e4b63f1a1d`, originally based on `a0f750681438d26ded17fff38cb3df37c3da9eb0`. It remains unchanged. The current candidate is `codex/dev-acceptance-tooling-integration-v1`, replayed onto accepted Science & Engineering main `90edc7bff4fe9ecfd8a958417b5270a38ec2153a`. Keep it separate from the R&D and LCARS branches. Scripts can run from the tooling checkout with `-RepoPath` pointing to another checkout; candidates do not need to merge the tooling branch.

## Prerequisites

- Windows PowerShell 5.1 or PowerShell 7, Git, and the SDK selected by the candidate's `global.json` (currently 10.0.401).
- For Unity stages: licensed Unity 6000.6.5f1, Windows Build Support (IL2CPP), and the required Windows C++ toolchain.
- Close the Unity editor for the candidate project before invoking batch targets. Each worktree has its own Unity Library, import time and disk usage.
- The candidate must have `HobbyDS9.slnx`, `scripts/PrepareUnity.proj` and the standard Unity scaffold. The target implementation remains the authority for build behavior.

Scripts derive their default repository root from their location. Always pass the target root when validating another checkout. No tools are installed automatically. If local execution policy blocks a script, use a reviewed script with `powershell.exe -NoProfile -ExecutionPolicy Bypass -File <script> <parameters>`; this affects that process only.

## Worktree layout and creation

```text
E:\HobbyDS9          accepted/main workspace (after the current feature work is safely moved)
E:\HobbyDS9-rnd      Science & Engineering candidate
E:\HobbyDS9-lcars    LCARS prototype
<tooling checkout>  codex/dev-acceptance-tooling-integration-v1
```

Point each local ChatGPT/Codex chat at its relevant worktree. Never ask two Unity batch processes or the editor and a batch process to use the same project simultaneously. The acceptance runner serializes its own runs in a worktree, but cannot lock out external Unity processes.

In these examples, set `$Tooling` to the directory containing this document's tooling branch:

```powershell
$Tooling = 'E:\HobbyDS9\artifacts\worktrees\dev-acceptance-tooling-integration-v1'
$Tools = Join-Path $Tooling 'tools\acceptance'
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'

& "$Tools\New-HobbyDS9Worktree.ps1" -SourceRepo 'E:\HobbyDS9' `
    -Branch 'codex/science-engineering-foundation-v1' -Destination 'E:\HobbyDS9-rnd' -Fetch

& "$Tools\New-HobbyDS9Worktree.ps1" -SourceRepo 'E:\HobbyDS9' `
    -Branch 'codex/lcars-strategic-screen-prototype-v1' -Destination 'E:\HobbyDS9-lcars' -Fetch
```

`-Fetch` runs only `git fetch origin`. Local branches take precedence; a fetch never advances an existing local branch. If only `origin/<branch>` exists, the helper creates a local tracking branch. A missing branch, existing destination, or branch already checked out elsewhere fails without force. **Both feature branches are currently checked out elsewhere:** the primary checkout holds R&D and a separate worktree holds the LCARS prototype. These creation examples will refuse until their owners safely release those branches. Use the existing checkout in the meantime; the helper will not switch it, stash it or discard changes.

## Runner

Standard R&D acceptance:

```powershell
& "$Tools\Invoke-HobbyDS9Acceptance.ps1" -RepoPath 'E:\HobbyDS9-rnd' `
    -UnityEditor $Unity -Fetch `
    -ExpectedRef 'origin/codex/science-engineering-foundation-v1' `
    -RunEditMode -BuildWindows
$LASTEXITCODE
```

Use `-ExpectedSha <full-40-character-SHA>` for an immutable candidate. `-ExpectedRef` also enforces exact HEAD equality with the resolved commit. With no fetch, remote refs are the locally cached refs. Ref movement during an ongoing run does not change the recorded candidate SHA. Detached HEAD is supported and recorded. Origin comparison reports `aheadBehind` as local-only count followed by remote-only count.

All runs check source identity, dirty status, SDK selection, then execute locked restore, Release build and the full .NET test suite. Unity preparation is requested for either Unity gate. The existing Windows target includes EditMode, so `-BuildWindows` implies EditMode and does not add a duplicate independent test invocation. Native targets may repeat preparation as part of their own dependencies. The runner never reimplements preparation. Current main deliberately runs EditMode with graphics enabled (`-batchmode`, without `-nographics`) because the accepted R&D test attaches a displayed editor window. The runner passes the editor path to the existing target and adds no direct Unity flags; the Windows build retains its own accepted `-nographics` invocation. Do not change these target behaviors in acceptance tooling.

Dirty source is rejected before tool/build work. `-AllowDirty` explicitly permits it, records files and warns that the result covers uncommitted state. Use it only after reviewing those changes. Never infer that a dirty acceptance run validates the committed SHA alone.

`-ContinueOnFailure` continues only safe later work: a failed restore blocks build/tests/preparation; a failed build blocks tests/preparation. A failed .NET test gate can still permit Unity gates. The existing Windows target stops on its own failed EditMode dependency. Any failure or skipped requested gate keeps the process exit code nonzero. Preflight failures always stop. Player launch is skipped after gate failures.

## Resolution and screenshot workflow

Build and launch LCARS at 1920x1080:

```powershell
& "$Tools\Invoke-HobbyDS9Acceptance.ps1" -RepoPath 'E:\HobbyDS9-lcars' `
    -UnityEditor $Unity -ExpectedRef 'origin/codex/lcars-strategic-screen-prototype-v1' `
    -BuildWindows -LaunchPlayer -PlayerWidth 1920 -PlayerHeight 1080 -Windowed
```

Close that player, then launch the existing executable at 3440x1440:

```powershell
& "$Tools\Invoke-HobbyDS9Acceptance.ps1" -RepoPath 'E:\HobbyDS9-lcars' `
    -ExpectedRef 'origin/codex/lcars-strategic-screen-prototype-v1' `
    -LaunchPlayer -PlayerWidth 3440 -PlayerHeight 1440 -Windowed
```

The second command still runs .NET gates but skips Unity/build. The report confirms only existence of the existing executable, not that it was built from current HEAD. Build first when accepting a changed candidate. Launch requires an explicit `-LaunchPlayer`; building alone does not launch.

The launcher uses Unity's documented `-screen-width`, `-screen-height`, and `-screen-fullscreen 0` (windowed) or `1` (fullscreen) [standalone player arguments](https://docs.unity3d.com/6000.0/Documentation/Manual/PlayerCommandLineArguments.html). Width/height are requested values. Supported display resolutions, OS scaling, window decorations and application overrides can affect the actual dimensions; verify the captured image size manually. Use a display supporting the requested resolution, or a suitable window on that display. No graphics settings are changed to force sizing.

Each launch creates and prints `screenshots/<width>x<height>/` inside the run's report directory. Capture screenshots manually and save as `mars-1920x1080.png` or `mars-3440x1440.png`. There is no GUI capture, OCR or image comparison. A started process is recorded as **launched**, with no invented pass or process exit code; it does not establish visual or gameplay acceptance.

## Evidence

Every invocation writes an ignored `artifacts/acceptance/<UTC timestamp>-<unique id>/` folder. Invalid repository paths fall back to a named directory in the OS temporary folder. Reports and logs are not committed.

- `acceptance.json`: schema version, source identity, expected refs, dirty status, before/after hygiene, SDK/editor versions where available, requested/executed stages, status, command, command exit code, elapsed seconds, diagnostic summary lines, .NET TRX counts/paths, EditMode counts/failures/XML/log paths, executable existence, player PID, screenshot folder, warnings and failures.
- `acceptance.md`: source, .NET, Unity, Windows, launch, failure/warning and hygiene summaries, plus explicit manual acceptance still required.
- Per-command logs, per-project .NET TRX files, copies of Unity logs and current verified XML. Incomplete/stale XML is retained as `editmode-unverified.xml` with a warning.

Null means unavailable, not success. Unrequested or blocked stages are `skipped`. Missing, malformed, stale, failed or zero-count Unity XML fails acceptance. Windows includes EditMode's elapsed time; the dependency has no separate duration or exit code. Stage exit codes are from the invoked `dotnet`/MSBuild commands: the existing MSBuild entry point does not expose raw nested Unity process exit codes. Original Unity logs are retained for diagnosis. A successful Windows gate also requires the expected `artifacts/unity/Windows/HobbyDS9.exe` to exist; build freshness is governed by the native target rather than inferred from file existence.

Hygiene classifies ignored generated folders, tracked modifications, suspicious untracked paths (Library, Temp, Logs, artifacts, Backup, UpgradeLog.htm, .vs, *.user, Unity ProjectSettings/Assets/Settings), and other source changes. It does not delete or clean anything. Before/after reports allow Unity-produced source modifications to be reviewed; post-run dirtiness produces a warning. Git supplies ignored-directory summaries so the check does not recursively enumerate huge Library caches. Rename paths use Git's human-readable porcelain representation.

## Safe worktree inspection/removal and comparison

```powershell
git -C E:\HobbyDS9 worktree list
git -C E:\HobbyDS9-rnd status --short
# Close Unity and the player, review/preserve source and ignored evidence first.
# Execute only once the worktree's work is no longer needed and it is clean:
git -C E:\HobbyDS9 worktree remove E:\HobbyDS9-rnd

# Optional file-level branch comparison (no merge):
git -C E:\HobbyDS9-rnd diff --name-status a0f750681438d26ded17fff38cb3df37c3da9eb0...HEAD
```

No removal helper or force cleanup is provided. Git removal can discard ignored caches/evidence even when source is clean: copy needed reports/screenshots elsewhere first. Inspect comparison paths by `src/HobbyDS9.Contracts`, `Simulation`, `Application`, Unity Runtime, Unity Tests, scripts/tools, docs, and scenarios/assets; this is a file review, not semantic merge analysis.

## Tooling verification

```powershell
& "$Tools\Test-HobbyDS9Acceptance.ps1"
& "$Tools\Test-HobbyDS9Acceptance.ps1" -PowerShellExecutable `
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
```

The integration fixture uses disposable temporary Git repositories and a fake dotnet CLI. It covers clean/dirty input, explicit dirty allowance, SHA/ref failure, conditional Unity discovery, missing/failed XML, Windows dependency evidence, nonzero gates, prerequisite handling, local/remote worktrees, collision/checked-out-branch refusal, and generated junk detection. Temporary fixtures/logs are retained for inspection. Fake Unity/player files are never executed. This tests orchestration and evidence, not Unity licensing, IL2CPP or visual correctness. The real .NET acceptance run is separate. CI and README are unchanged to avoid feature-branch overlap.

The preserved pre-Science/Engineering checkpoint passed 824 .NET tests. That is historical evidence, not the integration baseline. The current-main integration suite contains 930 tests. See the integration validation result below for this candidate.


For large Unity validations, place the worktree and temporary directory on a volume with sufficient free space. The integration validation uses E: after C: ran out of space. Temporary output can be redirected per session without changing machine-wide settings:

```powershell
$env:TEMP = 'E:\HobbyDS9\artifacts\acceptance-temp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
```


## Integration validation result

Validated locally on 11 October 2026 against current main 90edc7bff4fe9ecfd8a958417b5270a38ec2153a: all 23 tooling fixture checks passed using PowerShell 7.6.5 and Windows PowerShell 5.1.26100.9444 child processes. Locked restore, Release build (zero warnings/errors), and all 930 .NET tests passed. The real accepted Unity 6000.6.5f1 EditMode target passed 61/61 tests, including the displayed-window R&D case, without any target or runner changes. JSON and Markdown evidence were produced, including post-import hygiene findings. Unity-generated settings state and .vsconfig were preserved as ignored evidence and restored/relocated only in this task-owned fresh worktree; none is part of the candidate. IL2CPP/player visuals were not rerun for this integration.

The helper also safely refused the actual occupied R&D and LCARS branches and explained their existing worktree locations. Recommended E: sibling layouts remain valid after the relevant owners release their branches. No runner behavior adjustment was required by Science & Engineering; documentation updates record the new base, current test count, graphics-enabled EditMode behavior, occupied worktrees and E: validation location.
