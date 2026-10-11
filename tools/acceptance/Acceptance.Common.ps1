# Shared helpers; compatible with Windows PowerShell 5.1 and PowerShell 7.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Native nonzero exit codes are handled explicitly, including in PowerShell 7.
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-AcceptanceGit {
    param([string]$Repo, [string[]]$Arguments)
    # Windows PowerShell turns redirected native stderr into ErrorRecords.
    # Judge native failure by its exit code, preserving stderr in the diagnostic.
    $ErrorActionPreference = 'Continue'
    $output = @(& git -C $Repo @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed: $($output -join [Environment]::NewLine)" }
    return ($output -join "`n").TrimEnd([char[]]"`r`n")
}

function Get-AcceptanceRoot {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) { throw "Repository directory does not exist: $Path" }
    $root = Invoke-AcceptanceGit $Path @('rev-parse', '--show-toplevel')
    return [IO.Path]::GetFullPath($root)
}

function Get-AcceptanceHygiene {
    param([string]$Repo)
    $status = Invoke-AcceptanceGit $Repo @('-c', 'core.quotepath=false', 'status', '--porcelain=v1', '--untracked-files=all')
    $findings = @()
    foreach ($line in ($status -split "`n")) {
        if (-not $line) { continue }
        $code = $line.Substring(0, 2)
        $path = $line.Substring(3)
        $suspicious = $path -match '(^|/)(Library|Temp|Logs|artifacts|Backup|\.vs)(/|$)|(^|/)UpgradeLog\.htm$|\.user$|^client/HobbyDS9.Unity/(ProjectSettings|Assets/Settings)/'
        $classification = 'source change'
        if ($code -ne '??') { $classification = 'tracked modification' }
        elseif ($suspicious) { $classification = 'untracked suspicious' }
        $findings += [pscustomobject]@{ path = $path; status = $code; classification = $classification; suspicious = $suspicious }
    }
    $ignored = Invoke-AcceptanceGit $Repo @('-c', 'core.quotepath=false', 'ls-files', '--others', '--ignored', '--exclude-standard', '--directory')
    foreach ($path in ($ignored -split "`n")) {
        if ($path -match '(^|/)(Library|Temp|Logs|artifacts|Backup|\.vs|bin|obj|TestResults)(/|$)|UpgradeLog\.htm$|\.user$') {
            $findings += [pscustomobject]@{ path = $path; status = '!!'; classification = 'expected generated/ignored'; suspicious = $false }
        }
    }
    return [pscustomobject]@{ dirty = [bool]$status; status = $status; findings = @($findings) }
}

function Read-AcceptanceEditMode {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Unity EditMode XML is missing: $Path" }
    [xml]$xml = Get-Content -LiteralPath $Path -Raw
    $run = $xml.SelectSingleNode('/test-run')
    if ($null -eq $run) { throw "Invalid Unity EditMode XML: no test-run element in $Path" }
    $counts = @{}
    foreach ($name in @('total', 'passed', 'failed', 'skipped')) {
        $value = 0
        if (-not [int]::TryParse($run.GetAttribute($name), [ref]$value) -or $value -lt 0) { throw "Invalid EditMode count '$name' in $Path" }
        $counts[$name] = $value
    }
    $failures = @($xml.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object {
        $message = $_.SelectSingleNode('failure/message')
        [pscustomobject]@{ name = $_.GetAttribute('fullname'); message = $(if ($message) { $message.InnerText } else { '' }) }
    })
    return [pscustomobject]@{ total = $counts.total; passed = $counts.passed; failed = $counts.failed; skipped = $counts.skipped; result = $run.GetAttribute('result'); failures = $failures }
}
