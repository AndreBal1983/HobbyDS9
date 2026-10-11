[CmdletBinding()]
param(
    [string]$RepoPath = (Join-Path $PSScriptRoot '../..'),
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe',
    [string]$ExpectedRef,
    [string]$ExpectedSha,
    [switch]$Fetch,
    [switch]$AllowDirty,
    [switch]$RunEditMode,
    [switch]$BuildWindows,
    [switch]$LaunchPlayer,
    [ValidateRange(1, 16384)][int]$PlayerWidth = 1920,
    [ValidateRange(1, 16384)][int]$PlayerHeight = 1080,
    [switch]$Windowed,
    [switch]$ContinueOnFailure
)
. (Join-Path $PSScriptRoot 'Acceptance.Common.ps1')

$stages = [ordered]@{}
foreach ($name in @('preflight', 'restore', 'build', 'test', 'prepareUnity', 'editMode', 'windows', 'player')) {
    $requested = $name -in @('preflight', 'restore', 'build', 'test')
    if ($name -eq 'prepareUnity') { $requested = [bool]($RunEditMode -or $BuildWindows) }
    if ($name -eq 'editMode') { $requested = [bool]($RunEditMode -or $BuildWindows) }
    if ($name -eq 'windows') { $requested = [bool]$BuildWindows }
    if ($name -eq 'player') { $requested = [bool]$LaunchPlayer }
    $stages[$name] = [ordered]@{ requested = $requested; executed = $false; status = 'skipped'; exitCode = $null; durationSeconds = $null; command = $null; logPath = $null }
}
$report = [ordered]@{
    schemaVersion = 1; timestampUtc = [DateTime]::UtcNow.ToString('o'); repositoryPath = $RepoPath
    branch = $null; detachedHead = $null; headSha = $null; expectedRef = $ExpectedRef; expectedSha = $ExpectedSha
    resolvedExpectedRefSha = $null; dirty = $null; hygieneBefore = $null; hygieneAfter = $null
    originComparison = $null; dotnetVersion = $null; pinnedSdk = $null; installedSdks = @()
    unityPath = $UnityEditor; unityVersion = $null; unityProjectVersion = $null
    stages = $stages; dotnetTests = $null; editModeTests = $null
    executablePath = $null; executableExists = $false; playerProcessId = $null; screenshotDirectory = $null
    failures = @(); warnings = @(); overallStatus = 'failed'; reportDirectory = $null
    manualAcceptance = 'Required: verify gameplay, visuals, interaction, audio and screenshots in the launched player. Launch alone is not acceptance.'
}
$root = $null
$output = $null
$runLock = $null
$preflightTimer = [Diagnostics.Stopwatch]::StartNew()

function Invoke-Gate {
    param([string]$Name, [string[]]$Arguments, [switch]$DeferFailure)
    $stage = $stages[$Name]
    $stage.executed = $true
    $stage.command = 'dotnet ' + (($Arguments | ForEach-Object { '"' + $_ + '"' }) -join ' ')
    $stage.logPath = Join-Path $output ($Name + '.log')
    $timer = [Diagnostics.Stopwatch]::StartNew()
    Write-Host "Running $($stage.command)"
    Push-Location $root
    $savedPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & dotnet @Arguments *> $stage.logPath
        $stage.exitCode = $LASTEXITCODE
    } catch {
        $stage.exitCode = 1
        $_ | Out-String | Add-Content -LiteralPath $stage.logPath
    } finally {
        $ErrorActionPreference = $savedPreference
        Pop-Location
        $timer.Stop()
        $stage.durationSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 3)
    }
    $stage.status = $(if ($stage.exitCode -eq 0) { 'passed' } else { 'failed' })
    # Preserve the original summary lines instead of inventing aggregate counts.
    $stage['diagnostics'] = @(Select-String -LiteralPath $stage.logPath -Pattern '\b(warning|error)\s+[A-Z]+\d+|\d+ Warning\(s\)|\d+ Error\(s\)' | ForEach-Object { $_.Line } | Select-Object -Unique)
    if ($stage.exitCode -ne 0) {
        $report.failures += "Command failed (exit $($stage.exitCode)): $($stage.command). Log: $($stage.logPath)"
        if (-not $ContinueOnFailure -and -not $DeferFailure) { throw $report.failures[-1] }
    }
    return ($stage.exitCode -eq 0)
}

function Collect-EditMode {
    param([datetime]$Started)
    $xmlPath = Join-Path $root 'artifacts/unity/editmode.xml'
    if (-not (Test-Path -LiteralPath $xmlPath)) { throw "Unity EditMode XML is missing: $xmlPath" }
    if ((Get-Item -LiteralPath $xmlPath).LastWriteTimeUtc -lt $Started.AddSeconds(-2)) { throw "Unity EditMode XML is stale: $xmlPath" }
    $report.editModeTests = Read-AcceptanceEditMode $xmlPath
    $copy = Join-Path $output 'editmode.xml'
    Copy-Item -LiteralPath $xmlPath -Destination $copy
    $stages.editMode['resultPath'] = $copy
    if ($report.editModeTests.total -eq 0 -or $report.editModeTests.failed -gt 0 -or $report.editModeTests.result -ne 'Passed') {
        throw "Unity EditMode tests did not pass with a nonzero count. See $copy"
    }
}

try {
    $stages.preflight.executed = $true
    $root = Get-AcceptanceRoot $RepoPath
    $report.repositoryPath = $root
    $output = Join-Path $root ('artifacts/acceptance/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $report.reportDirectory = $output
    # Unity targets share fixed XML/log/player paths; serialize runs in this worktree.
    $lockPath = Join-Path $root 'artifacts/acceptance/run.lock'
    try { $runLock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch { throw "Another acceptance run may hold the worktree lock: $lockPath. $($_.Exception.Message)" }
    $report.headSha = Invoke-AcceptanceGit $root @('rev-parse', 'HEAD')
    $branch = Invoke-AcceptanceGit $root @('rev-parse', '--abbrev-ref', 'HEAD')
    $report.detachedHead = $branch -eq 'HEAD'
    $report.branch = $(if ($report.detachedHead) { '(detached HEAD)' } else { $branch })
    Write-Host "Repository: $root`nBranch: $($report.branch)`nHEAD: $($report.headSha)"
    $report.hygieneBefore = Get-AcceptanceHygiene $root
    $report.dirty = $report.hygieneBefore.dirty
    if ($report.dirty) {
        Write-Host $report.hygieneBefore.status
        if (-not $AllowDirty) { throw 'Working tree is dirty. Review the listed files; use -AllowDirty explicitly to proceed.' }
        $report.warnings += 'AllowDirty enabled: evidence includes uncommitted source state.'
    }
    if ($Fetch) {
        $stages.preflight.command = 'git fetch origin'
        Invoke-AcceptanceGit $root @('fetch', 'origin') | Write-Host
    }
    if ($ExpectedSha) {
        if ($ExpectedSha -notmatch '^[0-9a-fA-F]{40}$') { throw 'ExpectedSha must be a full 40-character commit SHA.' }
        if ($report.headSha -ne $ExpectedSha) { throw "Expected SHA mismatch: HEAD $($report.headSha), expected $ExpectedSha" }
    }
    if ($ExpectedRef) {
        $report.resolvedExpectedRefSha = Invoke-AcceptanceGit $root @('rev-parse', '--verify', "$ExpectedRef^{commit}")
        if ($report.headSha -ne $report.resolvedExpectedRefSha) { throw "Expected ref mismatch: HEAD $($report.headSha), $ExpectedRef resolves to $($report.resolvedExpectedRefSha)" }
    }
    if (-not $report.detachedHead) {
        $null = & git -C $root show-ref --verify --quiet "refs/remotes/origin/$branch"
        if ($LASTEXITCODE -eq 0) {
            $remote = Invoke-AcceptanceGit $root @('rev-parse', '--verify', "refs/remotes/origin/$branch^{commit}")
            $report.originComparison = [ordered]@{ ref = "origin/$branch"; sha = $remote; aheadBehind = (Invoke-AcceptanceGit $root @('rev-list', '--left-right', '--count', "HEAD...refs/remotes/origin/$branch")) }
        }
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet is unavailable. Install the repository-pinned SDK before running acceptance.' }
    Push-Location $root
    $savedPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $version = @(& dotnet --version 2>&1)
        if ($LASTEXITCODE -ne 0) { throw "dotnet --version failed: $($version -join ' ')" }
        $report.dotnetVersion = ($version -join '').Trim()
        $report.installedSdks = @(& dotnet --list-sdks)
        if ($LASTEXITCODE -ne 0) { throw 'dotnet --list-sdks failed.' }
    } finally { $ErrorActionPreference = $savedPreference; Pop-Location }
    $globalJson = Join-Path $root 'global.json'
    if (Test-Path -LiteralPath $globalJson) {
        $report.pinnedSdk = (Get-Content -LiteralPath $globalJson -Raw | ConvertFrom-Json).sdk.version
        if (-not ($report.installedSdks | Where-Object { $_.StartsWith($report.pinnedSdk + ' [') })) {
            $report.warnings += "Exact pinned SDK $($report.pinnedSdk) is not listed; dotnet selected $($report.dotnetVersion) under global.json roll-forward policy."
        }
    }
    if ($RunEditMode -or $BuildWindows) {
        if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity executable does not exist: $UnityEditor" }
        $report.unityVersion = (Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion
        $projectVersion = Join-Path $root 'client/HobbyDS9.Unity/ProjectSettings/ProjectVersion.txt'
        if (Test-Path -LiteralPath $projectVersion) { $report.unityProjectVersion = (Get-Content -LiteralPath $projectVersion) -join '; ' }
    }
    $solution = Join-Path $root 'HobbyDS9.slnx'
    if (-not (Test-Path -LiteralPath $solution)) { throw "Repository solution is missing: $solution" }
    $stages.preflight.status = 'passed'
    $stages.preflight.exitCode = 0
    $preflightTimer.Stop()
    $stages.preflight.durationSeconds = [Math]::Round($preflightTimer.Elapsed.TotalSeconds, 3)
    $restored = Invoke-Gate restore @('restore', $solution, '--locked-mode')
    $built = $false
    if ($restored) { $built = Invoke-Gate build @('build', $solution, '-c', 'Release', '--no-restore') }
    if ($built) {
        $testDirectory = Join-Path $output 'dotnet-tests'
        $null = Invoke-Gate test @('test', $solution, '-c', 'Release', '--no-build', '--no-restore', '--logger', 'trx', '--results-directory', $testDirectory) -DeferFailure
        $total = 0; $passed = 0; $failed = 0; $skipped = 0
        $results = @(Get-ChildItem -LiteralPath $testDirectory -Filter '*.trx' -Recurse -ErrorAction SilentlyContinue)
        foreach ($file in $results) {
            [xml]$trx = Get-Content -LiteralPath $file.FullName -Raw
            $counter = $trx.SelectSingleNode('//*[local-name()="Counters"]')
            if ($counter) {
                $total += [int]$counter.GetAttribute('total'); $passed += [int]$counter.GetAttribute('passed')
                $failed += [int]$counter.GetAttribute('failed'); $skipped += [int]$counter.GetAttribute('notExecuted')
            }
        }
        if ($results.Count -gt 0) { $report.dotnetTests = [ordered]@{ total = $total; passed = $passed; failed = $failed; skipped = $skipped; resultPaths = @($results.FullName) } }
        else { $report.warnings += 'No .NET TRX results were produced; test count is unavailable.' }
        if ($failed -gt 0 -and $stages.test.status -eq 'passed') { $stages.test.status = 'failed'; $report.failures += '.NET TRX reports failed tests despite a zero command exit code.' }
        if ($report.failures.Count -gt 0 -and -not $ContinueOnFailure) { throw $report.failures[-1] }
    }
    $prepared = $false
    if ($stages.prepareUnity.requested -and $restored -and $built) {
        $prepared = Invoke-Gate prepareUnity @('msbuild', 'scripts/PrepareUnity.proj', '-t:Prepare', "-p:UnityEditor=$UnityEditor")
    }
    # Windows depends on EditMode in the repository target: avoid a separate duplicate EditMode run.
    if ($prepared -and $BuildWindows) {
        $started = [DateTime]::UtcNow
        $null = Invoke-Gate windows @('msbuild', 'scripts/PrepareUnity.proj', '-t:Windows', "-p:UnityEditor=$UnityEditor") -DeferFailure
    } elseif ($prepared -and $RunEditMode) {
        $started = [DateTime]::UtcNow
        $null = Invoke-Gate editMode @('msbuild', 'scripts/PrepareUnity.proj', '-t:EditMode', "-p:UnityEditor=$UnityEditor") -DeferFailure
    }
    if ($prepared -and ($RunEditMode -or $BuildWindows)) {
        if ($BuildWindows) {
            $stages.editMode.executed = $true
            $stages.editMode.command = 'Dependency of ' + $stages.windows.command
            $stages.editMode['dependencyExitCode'] = $stages.windows.exitCode
            $stages.editMode['durationNote'] = 'Included in Windows target duration; no separate exit code is available.'
        }
        try {
            Collect-EditMode $started
            $stages.editMode.status = $(if ($BuildWindows -or $stages.editMode.exitCode -eq 0) { 'passed' } else { 'failed' })
        } catch {
            $stages.editMode.status = 'failed'
            $report.failures += $_.Exception.Message
            if (-not $ContinueOnFailure) { throw }
        }
    }
    $report.executablePath = Join-Path $root 'artifacts/unity/Windows/HobbyDS9.exe'
    $report.executableExists = Test-Path -LiteralPath $report.executablePath -PathType Leaf
    if ($stages.windows.status -eq 'passed' -and -not $report.executableExists) {
        $stages.windows.status = 'failed'; throw "Windows executable is missing: $($report.executablePath)"
    }
    if ($report.failures.Count -gt 0 -and -not $ContinueOnFailure) { throw $report.failures[-1] }
    if ($LaunchPlayer) {
        if ($report.failures.Count -gt 0) { $report.warnings += 'Player launch skipped because an acceptance gate failed.' }
        else {
            $stages.player.executed = $true
            $timer = [Diagnostics.Stopwatch]::StartNew()
            if (-not $report.executableExists) { throw "Player executable does not exist: $($report.executablePath)" }
            $fullscreen = $(if ($Windowed) { '0' } else { '1' })
            $playerArguments = @('-screen-width', "$PlayerWidth", '-screen-height', "$PlayerHeight", '-screen-fullscreen', $fullscreen)
            $stages.player.command = '"' + $report.executablePath + '" ' + ($playerArguments -join ' ')
            $report.screenshotDirectory = Join-Path $output "screenshots/${PlayerWidth}x${PlayerHeight}"
            New-Item -ItemType Directory -Path $report.screenshotDirectory -Force | Out-Null
            Write-Host "Screenshot folder: $($report.screenshotDirectory). Suggested name: mars-${PlayerWidth}x${PlayerHeight}.png"
            $process = Start-Process -FilePath $report.executablePath -ArgumentList $playerArguments -WorkingDirectory (Split-Path $report.executablePath) -PassThru
            $report.playerProcessId = $process.Id
            $stages.player.status = 'launched'
            $timer.Stop(); $stages.player.durationSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 3)
            $report.warnings += 'Player process started; resolution and manual acceptance remain unverified.'
        }
    }
} catch {
    $message = $_.Exception.Message
    if ($report.failures -notcontains $message) { $report.failures += $message }
    if ($stages.preflight.status -ne 'passed') {
        $stages.preflight.status = 'failed'; $stages.preflight.exitCode = 1
        $stages.preflight.durationSeconds = [Math]::Round($preflightTimer.Elapsed.TotalSeconds, 3)
    } elseif ($stages.player.executed -and $stages.player.status -eq 'skipped') { $stages.player.status = 'failed'; $stages.player.exitCode = 1 }
    Write-Warning $message
} finally {
    if ($null -eq $output) {
        # Invalid repositories still get evidence, without writing into the supplied unrelated directory.
        $output = Join-Path ([IO.Path]::GetTempPath()) ('HobbyDS9-acceptance-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $output -Force | Out-Null
        $report.reportDirectory = $output
    }
    if ($root) {
        try {
            $report.hygieneAfter = Get-AcceptanceHygiene $root
            if ($report.hygieneAfter.dirty) { $report.warnings += 'Working tree is dirty after acceptance. Review hygieneAfter before committing or accepting the candidate.' }
        } catch { $report.warnings += "Final hygiene check failed: $($_.Exception.Message)" }
        $report.executablePath = Join-Path $root 'artifacts/unity/Windows/HobbyDS9.exe'
        $report.executableExists = Test-Path -LiteralPath $report.executablePath -PathType Leaf
        foreach ($name in @('editmode.log', 'windows-build.log')) {
            $path = Join-Path $root "artifacts/unity/$name"
            $key = $(if ($name -eq 'editmode.log') { 'editMode' } else { 'windows' })
            if ($stages[$key].executed -and (Test-Path -LiteralPath $path)) {
                $copy = Join-Path $output $name
                Copy-Item -LiteralPath $path -Destination $copy
                $stages[$key]['unityLogPath'] = $copy
                $stages[$key]['unityLogLastWriteUtc'] = (Get-Item -LiteralPath $path).LastWriteTimeUtc.ToString('o')
            }
        }
        # Retain XML even when a target fails before normal collection.
        $xmlPath = Join-Path $root 'artifacts/unity/editmode.xml'
        if ($stages.editMode.requested -and (Test-Path -LiteralPath $xmlPath) -and -not $report.editModeTests) {
            Copy-Item -LiteralPath $xmlPath -Destination (Join-Path $output 'editmode-unverified.xml')
            $report.warnings += 'Retained unverified XML; it may be stale or come from an incomplete target.'
        }
    }
    foreach ($name in $stages.Keys) {
        if ($stages[$name].requested -and $stages[$name].status -eq 'skipped') { $report.warnings += "Requested stage '$name' skipped because prerequisites did not succeed." }
    }
    $incomplete = @($stages.Values | Where-Object { $_.requested -and $_.status -notin @('passed', 'launched') })
    $report.overallStatus = $(if ($report.failures.Count -eq 0 -and $incomplete.Count -eq 0) { 'passed' } else { 'failed' })
    $report | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $output 'acceptance.json') -Encoding UTF8
    $md = @('# HobbyDS9 acceptance', '', "Automated result: **$($report.overallStatus)**", '', '## Source state', '', "Repository: $($report.repositoryPath)", "Branch: $($report.branch); detached: $($report.detachedHead)", "HEAD: $($report.headSha)", "Expected SHA: $ExpectedSha; ref: $ExpectedRef; resolved: $($report.resolvedExpectedRefSha)", "Dirty before: $($report.dirty)", "UTC: $($report.timestampUtc)", '', '## .NET', '', "SDK: $($report.dotnetVersion); pinned: $($report.pinnedSdk)")
    foreach ($section in @(@('.NET stages', @('preflight', 'restore', 'build', 'test')), @('Unity EditMode', @('prepareUnity', 'editMode')), @('Windows build', @('windows')), @('Player launch', @('player')))) {
        $md += @('', "## $($section[0])", '', '| Stage | Requested | Executed | Result | Exit | Seconds | Log |', '|---|---|---|---|---|---|---|')
        foreach ($name in $section[1]) { $s = $stages[$name]; $md += "| $name | $($s.requested) | $($s.executed) | $($s.status) | $($s.exitCode) | $($s.durationSeconds) | $($s.logPath) |" }
    }
    $md += @('', "Executable: $($report.executablePath); exists: $($report.executableExists)", "Screenshots: $($report.screenshotDirectory)", '', '## Test counts', '', "NET: $($report.dotnetTests | ConvertTo-Json -Compress -Depth 5)", "EditMode: $($report.editModeTests | ConvertTo-Json -Compress -Depth 5)", '', '## Failures / warnings', '')
    foreach ($failure in $report.failures) { $md += "- FAILURE: $failure" }
    foreach ($warning in $report.warnings) { $md += "- WARNING: $warning" }
    $md += @('', '## Repository hygiene', '')
    foreach ($phase in @('hygieneBefore', 'hygieneAfter')) {
        $md += "### $phase"
        $md += ''
        if ($report[$phase]) { foreach ($finding in $report[$phase].findings) { $md += "- $($finding.classification): $($finding.status) $($finding.path)" } }
        $md += ''
    }
    $md += @('## Manual acceptance still required', '', $report.manualAcceptance)
    $md | Set-Content -LiteralPath (Join-Path $output 'acceptance.md') -Encoding UTF8
    Write-Host "Evidence: $output"
    if ($runLock) { $runLock.Dispose() }
}
if ($report.overallStatus -eq 'passed') { exit 0 }
exit 1
