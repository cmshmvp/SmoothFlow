$ErrorActionPreference = 'Stop'
$appRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4 compiler was not found.' }
$sources = Get-ChildItem -LiteralPath (Join-Path $appRoot 'source') -Filter '*.cs' | ForEach-Object { $_.FullName }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll /reference:System.Core.dll "/win32icon:$appRoot\assets\SmoothFlow.ico" "/resource:$appRoot\assets\SmoothFlow.ico,SmoothFlow.AppIcon" "/resource:$appRoot\assets\Languages.xml,SmoothFlow.Languages.xml" "/resource:$appRoot\assets\SmoothFlow-icon.png,SmoothFlow.Brand.png" "/out:$appRoot\SmoothFlow.exe" $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
$test = Start-Process -FilePath (Join-Path $appRoot 'SmoothFlow.exe') -ArgumentList '--self-test' -PassThru -Wait -WindowStyle Hidden
Get-Content -LiteralPath (Join-Path $appRoot 'self-test-results.txt')
if ($test.ExitCode -ne 0) { throw 'Self-tests failed.' }
