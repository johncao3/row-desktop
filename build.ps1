$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
& $compiler /nologo /target:exe "/out:$PSScriptRoot\assets\IconBuilder.exe" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "$PSScriptRoot\source\Brand.cs" "$PSScriptRoot\assets\IconBuilder.cs" "$PSScriptRoot\source\DesktopDock.cs"
if ($LASTEXITCODE -ne 0) { throw 'Icon compilation failed.' }
& "$PSScriptRoot\assets\IconBuilder.exe" "$PSScriptRoot\assets"
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }
& $compiler /nologo /target:winexe /optimize+ "/out:$PSScriptRoot\ROW.exe" "/win32icon:$PSScriptRoot\assets\ROW.ico" "/resource:$PSScriptRoot\assets\ROW.ico,ROW.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll "$PSScriptRoot\source\*.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output "Built ROW 1.02: $PSScriptRoot\ROW.exe"


