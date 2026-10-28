$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot 'downloads/manifest.json'
$approvedArtifacts = @{}
if (Test-Path -LiteralPath $manifestPath) {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    foreach ($artifact in $manifest.artifacts) {
        if ($artifact.path -ne 'downloads/ROW-1.06-Setup.exe' -or $artifact.sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Unexpected release artifact.' }
        $artifactPath = Join-Path $repoRoot $artifact.path
        if ((Get-Item -LiteralPath $artifactPath).Length -ne $artifact.size -or (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $artifact.sha256) { throw 'Release artifact hash/size mismatch.' }
        $approvedArtifacts[[IO.Path]::GetFullPath($artifactPath)] = $true
    }
}
$files = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force | Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' })
$blocked = @($files | Where-Object {
    if ($approvedArtifacts.ContainsKey($_.FullName) -or $_.FullName -eq (Join-Path $repoRoot 'downloads\SHA256SUMS.txt')) { return $false }
    ($_.Extension -notin '.cs','.ps1','.md','.json','.png','.ico' -and $_.Name -notin '.gitignore','.gitattributes') -or $_.FullName -match '[\\/]private[\\/]' -or
    $_.Name -match '^(settings\.json|desktop-layout-backup\.json|data-check\.json|error-log.*|crash-log.*|sports-.*\.png|.*check\.txt|self-test\.txt|live-.*|preview.*)$' -or
    $_.Extension -match '^\.(exe|zip|lnk|pfx|p12|pem|key|log)$' -or $_.Name -match '^\.env'
})
if ($blocked.Count) { throw ('Private/build files found: ' + (($blocked | ForEach-Object { $_.Name }) -join ', ')) }
$textFiles = @($files | Where-Object { $_.Extension -in '.cs','.ps1','.md','.json','.yml','.yaml' })
foreach ($file in $textFiles) {
    if ($file.Name -eq 'Verify-Repository.ps1') { continue }
    $body = [IO.File]::ReadAllText($file.FullName)
    if ($body -match '(github_pat_[A-Za-z0-9_]{20,}|gh[pousr]_[A-Za-z0-9]{20,}|-----BEGIN .*PRIVATE KEY-----|C:\\Users\\|C:/Users/)') {
        throw ('Potential credential/local path in: ' + $file.Name)
    }
}
if (Test-Path -LiteralPath (Join-Path $repoRoot '.github/workflows')) { throw 'Review workflows before enabling automation; preparation repository should not have publishing workflows.' }
$ready = Get-Content -LiteralPath (Join-Path $repoRoot 'release-readiness.json') -Raw | ConvertFrom-Json
if ($ready.publicReleaseApproved) { throw 'Full provider clearance is not recorded for this preview.' }
if ($approvedArtifacts.Count -gt 0 -and (-not $ready.maintainerPublicationAuthorized -or $ready.releaseMode -ne 'public-preview')) { throw 'Explicit public-preview decision required.' }
Write-Output ('PASS: {0} source/docs/artifact files; only manifest-pinned installer allowed; no personal state, obvious credentials, local user paths or publishing workflows.' -f $files.Count)

