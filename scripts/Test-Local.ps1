$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$exePath = Join-Path $repoRoot 'ROW.exe'
if (-not (Test-Path -LiteralPath $exePath)) { throw 'Run build.ps1 first.' }
foreach ($mode in @('--self-test','--selection-test','--interaction-test','--sports-layout-test')) {
    $process = Start-Process -FilePath $exePath -ArgumentList $mode -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw ('Local test failed: ' + $mode + '; see error-log.txt locally.') }
}
foreach ($report in @('self-test.txt','selection-check.txt','interaction-check.txt','sports-layout-check.txt')) {
    $text = Get-Content -LiteralPath (Join-Path $repoRoot $report) -Raw
    if ($text -notmatch 'PASS') { throw ('Missing PASS in ' + $report) }
    Write-Output $text.Trim()
}
Write-Output 'PASS: offline tests only; no feed requests or desktop icon movement.'
