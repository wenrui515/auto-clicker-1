$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$cscCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw '找不到 .NET Framework 4.x 的 C# 编译器（csc.exe）。'
}

$sourceFiles = Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName }
if (-not $sourceFiles) {
    throw 'src 目录中没有 C# 源文件。'
}

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$output = Join-Path $dist '自然连点器.exe'
$icon = Join-Path $root 'assets\app.ico'
$manifest = Join-Path $root 'app.manifest'

$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/warn:4',
    '/langversion:5',
    ('/out:' + $output),
    ('/win32icon:' + $icon),
    ('/win32manifest:' + $manifest),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    '/reference:System.Xml.dll'
) + $sourceFiles

& $csc $arguments
if ($LASTEXITCODE -ne 0) {
    throw "编译失败，csc 退出码：$LASTEXITCODE"
}

$info = Get-Item $output
Write-Host ("Build OK: {0} ({1:N0} bytes)" -f $info.FullName, $info.Length)
