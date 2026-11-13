param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Seal', 'Verify', 'Launch')]
    [string] $Mode,

    [Parameter(Mandatory = $true)]
    [string] $PackagePath,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string] $ExpectedSourceSha
)

$ErrorActionPreference = 'Stop'
$task = 'PB-20261113'
$manifestName = 'PB-RUNTIME-MANIFEST.json'
$scriptName = 'Invoke-PBPackage.ps1'
$launcherName = 'Start-Verified-PB.cmd'
$package = [System.IO.Path]::GetFullPath($PackagePath).TrimEnd('\', '/')
if (-not (Test-Path -LiteralPath $package -PathType Container)) {
    throw "Package directory is missing: $package"
}

$identityPath = Join-Path $package 'BUILD-IDENTITY.json'
if (-not (Test-Path -LiteralPath $identityPath -PathType Leaf)) {
    throw 'BUILD-IDENTITY.json is missing.'
}
$identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
if ($identity.task -cne $task -or $identity.sourceSha -cne $ExpectedSourceSha) {
    throw 'Build identity does not match this task and exact source SHA.'
}
if ($identity.editor -cne '6000.6.5f1' -and $identity.editor -cne '6000.6.9f1') {
    throw 'Build identity names an unsupported Unity Editor.'
}
if (($identity.editor -ceq '6000.6.9f1') -ne [bool]$identity.editorPatchEvaluation) {
    throw 'Editor patch evaluation flag does not match the Editor version.'
}
$expectedDirectory = 'Windows-PB-20261113-' + $ExpectedSourceSha.Substring(0, 12) + '-' + $identity.editor
if ([System.IO.Path]::GetFileName($package) -cne $expectedDirectory) {
    throw "Package directory name does not match build identity: $expectedDirectory"
}

$exePath = Join-Path $package 'HobbyDS9GeneratedCampaign.exe'
$assemblyPath = Join-Path $package 'GameAssembly.dll'
$playerPath = Join-Path $package 'UnityPlayer.dll'
$dataPath = Join-Path $package 'HobbyDS9GeneratedCampaign_Data'
$bootstrapPath = Join-Path $package 'Start-PB-20261113.cmd'
foreach ($required in @($exePath, $assemblyPath, $playerPath, $bootstrapPath)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required PB runtime file is missing: $required"
    }
}
if (-not (Test-Path -LiteralPath $dataPath -PathType Container)) {
    throw 'Unity data directory is missing.'
}
$bootstrap = Get-Content -LiteralPath $bootstrapPath -Raw
if ($bootstrap -cnotmatch '--hobbyds9-singleplayer-demo' -or $bootstrap -cnotmatch '-screen-fullscreen 0') {
    throw 'PB bootstrap does not select the explicit playable profile and windowed mode.'
}

$manifestPath = Join-Path $package $manifestName
if ($Mode -ceq 'Seal') {
    if (Test-Path -LiteralPath $manifestPath) {
        throw 'The package is already sealed; preserve it and use a fresh build stage.'
    }
    $copyPath = Join-Path $package $scriptName
    if (-not [string]::Equals([System.IO.Path]::GetFullPath($PSCommandPath), $copyPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        if (Test-Path -LiteralPath $copyPath) { throw 'Existing verifier script in package; refusing to overwrite.' }
        Copy-Item -LiteralPath $PSCommandPath -Destination $copyPath
    }
    $launcherPath = Join-Path $package $launcherName
    if (Test-Path -LiteralPath $launcherPath) { throw 'Existing verified launcher; refusing to overwrite.' }
    $launcher = '@echo off' + "`r`n" +
        'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-PBPackage.ps1" -Mode Launch -PackagePath "%~dp0" -ExpectedSourceSha ' + $ExpectedSourceSha + "`r`n" +
        'exit /b %ERRORLEVEL%' + "`r`n"
    [System.IO.File]::WriteAllText($launcherPath, $launcher, [System.Text.Encoding]::ASCII)
}

$links = @(Get-ChildItem -LiteralPath $package -Recurse -Force -Attributes ReparsePoint -ErrorAction SilentlyContinue)
if ($links.Count -ne 0) { throw 'A linked file or directory exists inside the package.' }
$files = @(Get-ChildItem -LiteralPath $package -Recurse -Force -File | Where-Object { $_.FullName -cne $manifestPath })
$fileMap = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([System.StringComparer]::Ordinal)
foreach ($file in $files) {
    $relative = $file.FullName.Substring($package.Length + 1).Replace('\', '/')
    if ($fileMap.ContainsKey($relative)) { throw "Duplicate package path: $relative" }
    $fileMap.Add($relative, $file)
}
$paths = [string[]]@($fileMap.Keys)
[Array]::Sort($paths, [System.StringComparer]::Ordinal)

if ($Mode -ceq 'Seal') {
    $entries = @(
        foreach ($relative in $paths) {
            $file = $fileMap[$relative]
            [pscustomobject]@{
                path = $relative
                bytes = [int64]$file.Length
                sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }
    )
    $manifest = [pscustomobject]@{
        schemaVersion = 1
        task = $task
        sourceSha = $ExpectedSourceSha
        editor = $identity.editor
        launchProfile = 'singleplayer-demo'
        launchArgs = @('--hobbyds9-singleplayer-demo', '-screen-fullscreen', '0')
        files = $entries
    }
    $temporaryPath = $manifestPath + '.tmp'
    if (Test-Path -LiteralPath $temporaryPath) { throw 'Existing temporary manifest; refusing to overwrite.' }
    [System.IO.File]::WriteAllText($temporaryPath, ($manifest | ConvertTo-Json -Depth 6), (New-Object System.Text.UTF8Encoding($false)))
    Move-Item -LiteralPath $temporaryPath -Destination $manifestPath
    Write-Output "Sealed $($entries.Count) files for $ExpectedSourceSha"
    return
}

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'PB runtime manifest is missing.'
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1 -or $manifest.task -cne $task -or
    $manifest.sourceSha -cne $ExpectedSourceSha -or $manifest.editor -cne $identity.editor -or
    $manifest.launchProfile -cne 'singleplayer-demo' -or
    (@($manifest.launchArgs) -join '|') -cne '--hobbyds9-singleplayer-demo|-screen-fullscreen|0') {
    throw 'PB runtime manifest identity or launch profile is invalid.'
}
if (@($manifest.files).Count -ne $paths.Count) {
    throw 'Package file count differs from the sealed manifest.'
}
$seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach ($entry in $manifest.files) {
    $relative = [string]$entry.path
    if (-not $seen.Add($relative) -or -not $fileMap.ContainsKey($relative)) {
        throw "Missing or duplicate sealed file: $relative"
    }
    $file = $fileMap[$relative]
    if ([int64]$entry.bytes -ne [int64]$file.Length -or
        [string]$entry.sha256 -cne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()) {
        throw "Sealed package file differs: $relative"
    }
}
Write-Output "Verified $($paths.Count) PB runtime files for $ExpectedSourceSha"
if ($Mode -ceq 'Launch') {
    $player = Start-Process -FilePath $exePath -ArgumentList @('--hobbyds9-singleplayer-demo', '-screen-fullscreen', '0') -WorkingDirectory $package -Wait -PassThru
    exit $player.ExitCode
}
