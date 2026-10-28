$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$ready = Get-Content -LiteralPath (Join-Path $repoRoot 'release-readiness.json') -Raw | ConvertFrom-Json
if (-not $ready.publicReleaseApproved) { throw 'Full release clearance is not recorded. An explicitly authorized public preview uses Build-Candidate.ps1 -PublicPreview; source approvals remain pending.' }
if ($ready.softwareLicense.status -ne 'approved' -or [string]::IsNullOrWhiteSpace($ready.softwareLicense.evidence)) { throw 'RELEASE BLOCKED: software distribution license is not recorded.' }
if (@($ready.sources).Count -ne 4) { throw 'RELEASE BLOCKED: expected all four source/logo reviews.' }
foreach ($source in $ready.sources) {
    if ($source.status -notin @('approved','removed') -or [string]::IsNullOrWhiteSpace($source.evidence)) { throw ('RELEASE BLOCKED: ' + $source.name) }
}
# An approved record is a maintainer review, not a substitute for permission.
# "removed" means its code and network/image requests were actually removed and tested.
& (Join-Path $repoRoot 'build.ps1')
& (Join-Path $PSScriptRoot 'Test-Local.ps1')
$version = [string]$ready.version
if ($version -notmatch '^\d+\.\d+(\.\d+)?$') { throw 'Invalid release version.' }
$dist = Join-Path $repoRoot 'dist'
$stage = Join-Path $dist ('ROW-' + $version + '-Windows')
if (Test-Path -LiteralPath $stage) { throw 'Staging directory already exists. Review it before preparing another package.' }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
# Explicit allowlist: no settings, logs, snapshots, desktop identifiers, source or test helpers.
Copy-Item -LiteralPath (Join-Path $repoRoot 'ROW.exe') -Destination $stage
foreach ($name in @('README.md','PRIVACY.md','THIRD_PARTY_NOTICES.md','LICENSE-STATUS.md')) { Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination $stage }
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/USER_GUIDE.md') -Destination (Join-Path $stage 'USER_GUIDE.md')
$archive = Join-Path $dist ('ROW-' + $version + '-Windows.zip')
if (Test-Path -LiteralPath $archive) { throw 'Archive already exists; refusing to overwrite.' }
Compress-Archive -LiteralPath $stage -DestinationPath $archive
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS.txt'), ($hash + '  ' + [IO.Path]::GetFileName($archive) + "`n"), [Text.UTF8Encoding]::new($false))
Write-Output 'Prepared locally only; nothing was uploaded to GitHub.'
Write-Output $archive
