[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ArtifactDirectory,
    # Optional read-only reuse of an already acquired official ZIP; never an arbitrary DLL.
    [string]$SourceArchive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$archiveName = 'sqlite-dll-win-x64-3540100.zip'
$sourceUrl = 'https://sqlite.org/2026/sqlite-dll-win-x64-3540100.zip'
$expectedArchiveHash = 'c2d09c8babb9399277055c057c97a53f1ffd7c5f11d0c1f2118ffdfa39f2daa4'
$expectedNativeHash = '4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea'
$maximumBytes = 32MB

function Get-SafeAbsolutePath([string]$Path) {
    if (-not [IO.Path]::IsPathFullyQualified($Path)) { throw 'An explicit absolute artifact/source path is required.' }
    $fullPath = [IO.Path]::GetFullPath($Path)
    if ($fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) -eq [IO.Path]::GetPathRoot($fullPath).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) {
        throw 'A filesystem root is not an artifact/source path.'
    }
    $candidate = $fullPath
    while ($candidate) {
        if (Test-Path -LiteralPath $candidate) {
            $item = Get-Item -LiteralPath $candidate -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Reparse paths are not allowed: $candidate" }
        }
        $candidate = [IO.Path]::GetDirectoryName($candidate)
    }
    return $fullPath
}

function Assert-Hash([string]$Path, [string]$Expected, [switch]$Sha3) {
    $null = Get-SafeAbsolutePath $Path
    $item = Get-Item -LiteralPath $Path -Force
    if ($item.PSIsContainer -or $item.Length -le 0 -or $item.Length -gt $maximumBytes) { throw "Unexpected artifact type/size: $Path" }
    if ($Sha3) {
        # Python's standard library provides SHA3 even when the Windows .NET provider does not.
        $actual = & python -c 'import hashlib, pathlib, sys; print(hashlib.sha3_256(pathlib.Path(sys.argv[1]).read_bytes()).hexdigest())' $Path
        if ($LASTEXITCODE -ne 0) { throw 'Python SHA3-256 verification failed.' }
    } else {
        $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    }
    if ($actual -ne $Expected) { throw "Hash mismatch; existing bytes are retained: $Path" }
}

$artifactRoot = Get-SafeAbsolutePath $ArtifactDirectory
# This helper must never stage into or change a global package cache.
$packageCaches = @($env:NUGET_PACKAGES, (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages'))
foreach ($cache in $packageCaches) {
    if ([string]::IsNullOrWhiteSpace($cache)) { continue }
    $cacheRoot = [IO.Path]::GetFullPath($cache).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if ($artifactRoot.Equals($cacheRoot, [StringComparison]::OrdinalIgnoreCase) -or $artifactRoot.StartsWith($cacheRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The artifact directory must be outside the global NuGet cache.'
    }
}
if (Test-Path -LiteralPath $artifactRoot) {
    if (-not (Get-Item -LiteralPath $artifactRoot).PSIsContainer) { throw 'The artifact path must be a directory.' }
} else {
    $null = New-Item -ItemType Directory -Path $artifactRoot
}
$null = Get-SafeAbsolutePath $artifactRoot
$archivePath = Get-SafeAbsolutePath (Join-Path $artifactRoot $archiveName)
$nativePath = Get-SafeAbsolutePath (Join-Path $artifactRoot 'e_sqlite3.dll')
# Refuse mismatched existing files before downloading or staging anything else.
if (Test-Path -LiteralPath $archivePath) { Assert-Hash $archivePath $expectedArchiveHash -Sha3 }
if (Test-Path -LiteralPath $nativePath) { Assert-Hash $nativePath $expectedNativeHash }

if ($SourceArchive) {
    $verifiedSource = Get-SafeAbsolutePath $SourceArchive
    Assert-Hash $verifiedSource $expectedArchiveHash -Sha3
    if (-not (Test-Path -LiteralPath $archivePath)) { [IO.File]::Copy($verifiedSource, $archivePath, $false) }
} elseif (-not (Test-Path -LiteralPath $archivePath)) {
    $downloadPath = Join-Path $artifactRoot (([Guid]::NewGuid().ToString('N')) + '.download')
    # Normal system TLS verification; no global configuration or certificate bypass.
    Invoke-WebRequest -Uri $sourceUrl -OutFile $downloadPath -MaximumRedirection 0 -TimeoutSec 120
    Assert-Hash $downloadPath $expectedArchiveHash -Sha3
    [IO.File]::Move($downloadPath, $archivePath)
}
Assert-Hash $archivePath $expectedArchiveHash -Sha3

if (-not (Test-Path -LiteralPath $nativePath)) {
    $zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $entries = @($zip.Entries | Where-Object { $_.FullName -ceq 'sqlite3.dll' })
        if ($entries.Count -ne 1 -or $entries[0].Length -le 0 -or $entries[0].Length -gt $maximumBytes) { throw 'Expected exactly one bounded sqlite3.dll ZIP entry.' }
        # Read only the named entry. No archive-controlled paths are extracted.
        $stagedPath = Join-Path $artifactRoot (([Guid]::NewGuid().ToString('N')) + '.verified-dll')
        $inputStream = $entries[0].Open()
        try {
            $outputStream = [IO.File]::Open($stagedPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose() }
        } finally { $inputStream.Dispose() }
        Assert-Hash $stagedPath $expectedNativeHash
        [IO.File]::Move($stagedPath, $nativePath)
    } finally { $zip.Dispose() }
}
Assert-Hash $nativePath $expectedNativeHash

# Return data to the caller; do not change environment variables or build outputs here.
[pscustomobject]@{
    Recipe = 'Windows x64 SQLite R2 managed-host trial only'
    SourceUrl = $sourceUrl
    ArchivePath = $archivePath
    ArchiveSha3_256 = $expectedArchiveHash
    NativeSqliteOverride = $nativePath
    NativeSha256 = $expectedNativeHash
    NativeVersion = '3.54.1'
    NativePlayerAcceptance = $false
}
