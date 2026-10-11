# Disposable integration fixtures; does not run Unity or modify the caller's repository.
[CmdletBinding()]
param([string]$PowerShellExecutable = (Get-Process -Id $PID).Path)
. (Join-Path $PSScriptRoot 'Acceptance.Common.ps1')
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('HobbyDS9-tooling-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
$originalPath = $env:PATH
$originalMode = $env:HOBBYDS9_FIXTURE_MODE
$failures = @(); $count = 0
function Assert-Case {
    param([bool]$Condition, [string]$Name)
    $script:count++
    if (-not $Condition) { $script:failures += $Name; Write-Warning "FAIL: $Name" }
    else { Write-Host "PASS: $Name" }
}
function Run-Runner {
    param([string[]]$Extra = @())
    & $PowerShellExecutable -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Invoke-HobbyDS9Acceptance.ps1') -RepoPath $script:repo @Extra *> (Join-Path $fixture 'last-run.log')
    $code = $LASTEXITCODE
    $latest = Get-ChildItem (Join-Path $script:repo 'artifacts/acceptance') -Directory | Sort-Object Name | Select-Object -Last 1
    $json = Get-Content (Join-Path $latest.FullName 'acceptance.json') -Raw | ConvertFrom-Json
    return [pscustomobject]@{ code = $code; report = $json; markdown = (Test-Path (Join-Path $latest.FullName 'acceptance.md')) }
}
function Run-Worktree {
    param([string]$Branch, [string]$Destination)
    & $PowerShellExecutable -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'New-HobbyDS9Worktree.ps1') -SourceRepo $repo -Branch $Branch -Destination $Destination *> (Join-Path $fixture 'worktree.log')
    return $LASTEXITCODE
}
try {
    $repo = Join-Path $fixture 'repo with spaces'; $bin = Join-Path $fixture 'bin'
    New-Item -ItemType Directory -Path $repo, $bin | Out-Null
    Invoke-AcceptanceGit $repo @('init', '-b', 'main') | Out-Null
    Invoke-AcceptanceGit $repo @('config', 'user.email', 'tooling-test@example.invalid') | Out-Null
    Invoke-AcceptanceGit $repo @('config', 'user.name', 'Acceptance fixture') | Out-Null
    '/artifacts/' | Set-Content (Join-Path $repo '.gitignore')
    '<Solution />' | Set-Content (Join-Path $repo 'HobbyDS9.slnx')
    '{"sdk":{"version":"10.0.401"}}' | Set-Content (Join-Path $repo 'global.json')
    New-Item -ItemType Directory (Join-Path $repo 'scripts') | Out-Null
    '<Project />' | Set-Content (Join-Path $repo 'scripts/PrepareUnity.proj')
    'source' | Set-Content (Join-Path $repo 'source.txt')
    Invoke-AcceptanceGit $repo @('add', '.') | Out-Null
    Invoke-AcceptanceGit $repo @('commit', '-m', 'Disposable tooling fixture') | Out-Null
    $sha = Invoke-AcceptanceGit $repo @('rev-parse', 'HEAD')
    @'
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0fake-dotnet.ps1" %*
exit /b %errorlevel%
'@ | Set-Content (Join-Path $bin 'dotnet.cmd') -Encoding ascii
    @'
$arguments = $args
if ($arguments[0] -eq '--version') { '10.0.401'; exit 0 }
if ($arguments[0] -eq '--list-sdks') { '10.0.401 [C:\fake\sdk]'; exit 0 }
if ($env:HOBBYDS9_FIXTURE_MODE -eq 'restore-fails' -and $arguments[0] -eq 'restore') { [Console]::Error.WriteLine('error FIX001: fixture failure'); exit 13 }
if ($arguments[0] -eq 'test') {
    $index = [Array]::IndexOf($arguments, '--results-directory'); $dir = $arguments[$index + 1]
    New-Item -ItemType Directory -Force $dir | Out-Null
    '<TestRun><ResultSummary><Counters total="3" passed="3" failed="0" notExecuted="0" /></ResultSummary></TestRun>' | Set-Content (Join-Path $dir 'fixture.trx')
}
if ($arguments[0] -eq 'msbuild' -and ((($arguments -contains '-t:EditMode') -or ($arguments -contains 'EditMode')) -or (($arguments -contains '-t:Windows') -or ($arguments -contains 'Windows')))) {
    $dir = Join-Path (Get-Location) 'artifacts/unity'; New-Item -ItemType Directory -Force $dir | Out-Null
    'fixture Unity log' | Set-Content (Join-Path $dir 'editmode.log')
    if ($env:HOBBYDS9_FIXTURE_MODE -eq 'missing-xml') { exit 0 }
    if ($env:HOBBYDS9_FIXTURE_MODE -eq 'failed-xml') {
        '<test-run result="Failed" total="2" passed="1" failed="1" skipped="0"><test-case fullname="Fixture.Fails" result="Failed"><failure><message>fixture failure</message></failure></test-case></test-run>' | Set-Content (Join-Path $dir 'editmode.xml')
        exit 17
    }
    '<test-run result="Passed" total="2" passed="2" failed="0" skipped="0" />' | Set-Content (Join-Path $dir 'editmode.xml')
    if (($arguments -contains '-t:Windows') -or ($arguments -contains 'Windows')) {
        New-Item -ItemType Directory -Force (Join-Path $dir 'Windows') | Out-Null
        'fake player; never launched' | Set-Content (Join-Path $dir 'Windows/HobbyDS9.exe')
    }
}
'fixture succeeded'; exit 0
'@ | Set-Content (Join-Path $bin 'fake-dotnet.ps1') -Encoding ascii
    $env:PATH = "$bin;$originalPath"; $env:HOBBYDS9_FIXTURE_MODE = ''
    $r = Run-Runner @('-ExpectedSha', $sha, '-ExpectedRef', 'main')
    Assert-Case ($r.code -eq 0 -and $r.report.overallStatus -eq 'passed') 'clean repository accepted'
    Assert-Case ($r.markdown -and $r.report.dotnetTests.total -eq 3) 'JSON, Markdown and TRX count produced'
    Assert-Case (-not $r.report.stages.editMode.executed -and $null -eq $r.report.stages.editMode.exitCode) 'skipped Unity stage has no invented success'
    'changed' | Add-Content (Join-Path $repo 'source.txt')
    $r = Run-Runner
    Assert-Case ($r.code -ne 0 -and $r.report.dirty -and -not $r.report.stages.restore.executed) 'dirty tracked repository rejected before restore'
    Assert-Case ($r.report.hygieneBefore.findings[0].path -eq 'source.txt') 'porcelain leading spaces retained'
    $r = Run-Runner @('-AllowDirty')
    Assert-Case ($r.code -eq 0 -and $r.report.dirty) 'AllowDirty records and accepts dirty state'
    'source' | Set-Content (Join-Path $repo 'source.txt')
    $r = Run-Runner @('-ExpectedSha', ('0' * 40))
    Assert-Case ($r.code -ne 0 -and ($r.report.failures -join ' ') -match 'Expected SHA mismatch') 'wrong SHA rejected'
    $r = Run-Runner @('-ExpectedRef', 'missing-branch')
    Assert-Case ($r.code -ne 0) 'unknown expected ref rejected'
    $r = Run-Runner @('-RunEditMode', '-UnityEditor', (Join-Path $fixture 'missing.exe'))
    Assert-Case ($r.code -ne 0 -and ($r.report.failures -join ' ') -match 'Unity executable does not exist') 'missing Unity rejected when requested'
    $r = Run-Runner @('-UnityEditor', (Join-Path $fixture 'missing.exe'))
    Assert-Case ($r.code -eq 0) 'missing Unity irrelevant to NET-only acceptance'
    $unity = Join-Path $fixture 'fake-unity.exe'; 'fixture' | Set-Content $unity
    $env:HOBBYDS9_FIXTURE_MODE = 'missing-xml'
    $r = Run-Runner @('-RunEditMode', '-UnityEditor', $unity)
    Assert-Case ($r.code -ne 0 -and ($r.report.failures -join ' ') -match 'XML is missing') 'missing EditMode XML fails clearly'
    $env:HOBBYDS9_FIXTURE_MODE = 'failed-xml'
    $r = Run-Runner @('-RunEditMode', '-UnityEditor', $unity)
    Assert-Case ($r.code -ne 0 -and $r.report.editModeTests.failed -eq 1 -and $r.report.editModeTests.failures[0].name -eq 'Fixture.Fails') 'failed Unity results and command exit retained'
    $env:HOBBYDS9_FIXTURE_MODE = ''
    $r = Run-Runner @('-BuildWindows', '-UnityEditor', $unity)
    Assert-Case ($r.code -eq 0 -and $r.report.stages.editMode.status -eq 'passed' -and $r.report.executableExists -and $null -eq $r.report.stages.editMode.exitCode) 'Windows dependency results recorded without fabricated exit'
    $xmlPath = Join-Path $repo 'artifacts/unity/editmode.xml'
    (Get-Item $xmlPath).LastWriteTimeUtc = [DateTime]::UtcNow.AddMinutes(-10)
    $env:HOBBYDS9_FIXTURE_MODE = 'missing-xml'
    $r = Run-Runner @('-RunEditMode', '-UnityEditor', $unity)
    Assert-Case ($r.code -ne 0 -and ($r.report.failures -join ' ') -match 'XML is stale') 'old XML cannot pass a new run'
    $env:HOBBYDS9_FIXTURE_MODE = ''
    $lockPath = Join-Path $repo 'artifacts/acceptance/run.lock'
    $heldLock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try { $r = Run-Runner } finally { $heldLock.Dispose() }
    Assert-Case ($r.code -ne 0 -and -not $r.report.stages.restore.executed -and ($r.report.failures -join ' ') -match 'worktree lock') 'concurrent acceptance refused'
    Invoke-AcceptanceGit $repo @('checkout', '--detach', $sha) | Out-Null
    $r = Run-Runner
    Assert-Case ($r.code -eq 0 -and $r.report.detachedHead) 'detached HEAD accepted and reported'
    Invoke-AcceptanceGit $repo @('checkout', 'main') | Out-Null
    $env:HOBBYDS9_FIXTURE_MODE = 'restore-fails'
    $r = Run-Runner
    Assert-Case ($r.code -ne 0 -and $r.report.stages.restore.exitCode -eq 13 -and -not $r.report.stages.build.executed) 'failed gate returns nonzero and stops dependent build'
    $r = Run-Runner @('-ContinueOnFailure')
    Assert-Case ($r.code -ne 0 -and -not $r.report.stages.build.executed) 'ContinueOnFailure respects restore dependency'
    $env:HOBBYDS9_FIXTURE_MODE = ''
    Invoke-AcceptanceGit $repo @('branch', 'candidate') | Out-Null
    $dest = Join-Path $fixture 'local worktree'
    $code = Run-Worktree candidate $dest
    Assert-Case ($code -eq 0 -and (Invoke-AcceptanceGit $dest @('rev-parse', 'HEAD')) -eq $sha) 'local worktree resolves branch correctly'
    Assert-Case ((Run-Worktree candidate $dest) -ne 0) 'existing destination refused'
    Assert-Case ((Run-Worktree candidate (Join-Path $fixture 'second-candidate')) -ne 0) 'already checked-out branch refused without force'
    Invoke-AcceptanceGit $repo @('remote', 'add', 'origin', $repo) | Out-Null
    Invoke-AcceptanceGit $repo @('update-ref', 'refs/remotes/origin/remote-candidate', $sha) | Out-Null
    $remoteDest = Join-Path $fixture 'remote worktree'
    $code = Run-Worktree remote-candidate $remoteDest
    Assert-Case ($code -eq 0 -and (Invoke-AcceptanceGit $remoteDest @('rev-parse', 'HEAD')) -eq $sha) 'remote-only branch creates tracking worktree'
    New-Item -ItemType Directory -Force (Join-Path $repo 'client/HobbyDS9.Unity/Backup') | Out-Null
    'junk' | Set-Content (Join-Path $repo 'client/HobbyDS9.Unity/Backup/junk.txt')
    $r = Run-Runner
    Assert-Case ($r.code -ne 0 -and @($r.report.hygieneBefore.findings | Where-Object { $_.classification -eq 'untracked suspicious' }).Count -eq 1) 'generated untracked junk classified and rejected'
} catch { $failures += $_.Exception.Message; Write-Warning ($_ | Out-String) }
finally {
    $env:PATH = $originalPath; $env:HOBBYDS9_FIXTURE_MODE = $originalMode
    Write-Host "Fixture retained for inspection: $fixture"
}
Write-Host "$count checks; $($failures.Count) failures; PowerShell $($PSVersionTable.PSVersion); child: $PowerShellExecutable"
if ($failures.Count -gt 0) { exit 1 }
exit 0
