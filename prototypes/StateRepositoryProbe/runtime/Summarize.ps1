param([string]$EvidenceDirectory = '../../../docs/evidence/state-repository-20261122')
$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture
function Median($values) {
    $sorted = @($values | Sort-Object)
    if ($sorted.Count % 2) { return $sorted[[int][Math]::Floor($sorted.Count / 2)] }
    return ($sorted[$sorted.Count / 2 - 1] + $sorted[$sorted.Count / 2]) / 2
}
$reports = @('runtime-30.json', 'runtime-100.json', 'runtime-1000.json', 'runtime-1000-worker.json')
$rows = foreach ($file in $reports) {
    $report = Get-Content -LiteralPath (Join-Path $EvidenceDirectory $file) -Raw | ConvertFrom-Json
    foreach ($measure in $report.measurements) {
        [pscustomobject]@{
            report = $file; systems = $report.environment.systemCount; operation = $measure.name
            n = $measure.n; warmup = $measure.warmup; medianMs = $measure.medianMs
            observedP95Ms = $measure.p95Ms; observedP99Ms = $measure.p99Ms; maxMs = $measure.maxMs
            medianAllocatedBytes = Median $measure.samples.allocatedBytes
            gc0Total = ($measure.samples | ForEach-Object { $_.gcCollections[0] } | Measure-Object -Sum).Sum
            gc1Total = ($measure.samples | ForEach-Object { $_.gcCollections[1] } | Measure-Object -Sum).Sum
            gc2Total = ($measure.samples | ForEach-Object { $_.gcCollections[2] } | Measure-Object -Sum).Sum
            gcPauseTotalMs = ($measure.samples.gcPauseMs | Measure-Object -Sum).Sum
            gcPauseMaxMs = ($measure.samples.gcPauseMs | Measure-Object -Maximum).Maximum
            processCpuTotalMs = ($measure.samples.processCpuMs | Measure-Object -Sum).Sum
        }
    }
}
$rows | Export-Csv -LiteralPath (Join-Path $EvidenceDirectory 'runtime-summary.csv') -NoTypeInformation -Encoding utf8
