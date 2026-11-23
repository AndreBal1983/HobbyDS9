param([Parameter(Mandatory = $true)][string]$Path)

$ErrorActionPreference = 'Stop'
[xml]$trx = Get-Content -LiteralPath $Path -Raw
$c = $trx.TestRun.ResultSummary.Counters
$observed = @($trx.TestRun.Results.UnitTestResult | ForEach-Object { [string]$_.testName })
$required = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'physical-travel-required-tests.txt') | Where-Object { $_ })

if ($required.Count -eq 0 -or @($required | Sort-Object -Unique).Count -ne $required.Count) {
    throw 'Physical-travel required-test inventory is empty or contains duplicates.'
}
if ([int]$c.total -ne $observed.Count -or [int]$c.total -lt $required.Count -or
    [int]$c.passed -ne [int]$c.total -or [int]$c.failed -ne 0 -or
    [int]$c.error -ne 0 -or [int]$c.notExecuted -ne 0 -or
    @($observed | Sort-Object -Unique).Count -ne $observed.Count) {
    throw "Unexpected physical-travel evidence: $($c.OuterXml)"
}
$missing = @($required | Where-Object { $_ -cnotin $observed })
if ($missing.Count -ne 0) {
    throw "Missing required physical-travel tests: $($missing -join ', ')"
}
Write-Host "physical: $($c.passed) passed, 0 failed, 0 skipped; all $($required.Count) required identities present"
