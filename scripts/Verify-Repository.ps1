$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$files = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force | Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' })
$blocked = @($files | Where-Object {
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
if ($ready.publicReleaseApproved) { throw 'This preparation audit expects public release to remain disabled.' }
Write-Output ('PASS: {0} allowlisted source/docs files; no generated binaries, personal state, obvious credentials, local user paths or publishing workflows.' -f $files.Count)

