$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$ready = Get-Content -LiteralPath (Join-Path $repoRoot 'release-readiness.json') -Raw | ConvertFrom-Json
$version = [string]$ready.version
if ($version -notmatch '^\d+\.\d+(\.\d+)?$') { throw 'Invalid version.' }
if (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'ROW.exe'))) { throw 'Build and test ROW.exe first.' }
# Local maintainer candidate only. Does not change release approval or upload anything.
$candidateRoot = Join-Path $repoRoot ('dist\candidate-' + $version + '-' + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $candidateRoot 'payload'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
foreach ($name in @('ROW.exe','README.md','PRIVACY.md','THIRD_PARTY_NOTICES.md','LICENSE-STATUS.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination $payload
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs\INSTALLER_CANDIDATE.md') -Destination (Join-Path $payload 'USER_GUIDE.md')
$status = 'ROW ' + $version + ' maintainer-only candidate. Public download not approved. Tencent, CoinLore, ESPN and team-logo use remains pending. This is not evidence of provider permission.'
[IO.File]::WriteAllText((Join-Path $payload 'RELEASE_STATUS.md'), $status, [Text.Encoding]::UTF8)
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath (Join-Path $candidateRoot 'payload.zip')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$setup = Join-Path $candidateRoot ('ROW-' + $version + '-Setup-Candidate.exe')
$icon = Join-Path $repoRoot 'assets\ROW.ico'
& $compiler /nologo /target:winexe /optimize+ "/out:$setup" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.Core.dll /reference:Microsoft.CSharp.dll "/win32icon:$icon" "/resource:$icon,ROW.ico" "/resource:$candidateRoot\payload.zip,payload.zip" (Join-Path $repoRoot 'installer\Installer.cs')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $candidateRoot 'SHA256SUMS.txt'), $hash + '  ' + [IO.Path]::GetFileName($setup) + "`n", [Text.Encoding]::UTF8)
Write-Output 'LOCAL CANDIDATE ONLY: not a public release; provider approvals remain pending.'
Write-Output $setup
