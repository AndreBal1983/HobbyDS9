[CmdletBinding()]
param(
    [string]$SourceRepo = (Join-Path $PSScriptRoot '../..'),
    [Parameter(Mandatory = $true)][string]$Branch,
    [Parameter(Mandatory = $true)][string]$Destination,
    [switch]$Fetch
)
. (Join-Path $PSScriptRoot 'Acceptance.Common.ps1')
try {
    $root = Get-AcceptanceRoot $SourceRepo
    Invoke-AcceptanceGit $root @('check-ref-format', '--branch', $Branch) | Out-Null
    if ($Branch.StartsWith('-') -or $Branch -eq 'HEAD' -or $Branch.StartsWith('refs/') -or $Branch.StartsWith('origin/')) {
        throw 'Branch must be a branch name, without refs/ or origin/ prefixes.'
    }
    $destinationPath = [IO.Path]::GetFullPath($Destination)
    if (Test-Path -LiteralPath $destinationPath) { throw "Destination already exists; no files will be overwritten: $destinationPath" }
    if ($Fetch) { Invoke-AcceptanceGit $root @('fetch', 'origin') | Write-Host }
    $null = & git -C $root show-ref --verify --quiet "refs/heads/$Branch"
    $local = $LASTEXITCODE -eq 0
    $null = & git -C $root show-ref --verify --quiet "refs/remotes/origin/$Branch"
    $remote = $LASTEXITCODE -eq 0
    if ($local) {
        Write-Host "Using existing local branch $Branch. Fetch does not advance it."
        $arguments = @('worktree', 'add', '--', $destinationPath, $Branch)
    } elseif ($remote) {
        Write-Host "Creating local branch $Branch from origin/$Branch."
        $arguments = @('worktree', 'add', '--track', '-b', $Branch, '--', $destinationPath, "refs/remotes/origin/$Branch")
    } else { throw "No local branch or origin/$Branch exists. Use -Fetch explicitly if needed." }
    try { Invoke-AcceptanceGit $root $arguments | Write-Host }
    catch { throw "$($_.Exception.Message)`nIf the branch is already checked out, use that worktree or choose a different branch. No --force is used." }
    [pscustomobject]@{
        path = Get-AcceptanceRoot $destinationPath
        branch = Invoke-AcceptanceGit $destinationPath @('rev-parse', '--abbrev-ref', 'HEAD')
        sha = Invoke-AcceptanceGit $destinationPath @('rev-parse', 'HEAD')
    } | Format-List | Out-Host
    exit 0
} catch {
    Write-Warning $_.Exception.Message
    exit 1
}
