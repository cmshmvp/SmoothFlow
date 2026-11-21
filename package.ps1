$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
& (Join-Path $root 'build.ps1')

$dist = Join-Path $root 'dist'
[void][IO.Directory]::CreateDirectory($dist)
$name = 'SmoothFlow-0.3.3-win-x64.zip'
$archive = Join-Path $dist $name
$files = @('SmoothFlow.exe', 'LICENSE', 'README.md', 'assets/SmoothFlow-icon.png', 'tests/legacy-0.2-trace.csv')
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Create($archive)
try {
    $zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $root $file), $file)
        }
    } finally { $zip.Dispose() }
} finally { $stream.Dispose() }
$digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$archive.sha256" -Value "$digest  $name" -Encoding Ascii
Write-Output $archive
