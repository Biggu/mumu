# 雰囲気の粒の画像（四角・丸）→ Sprites/Common/ambient_*.png
# 使い方: powershell -ExecutionPolicy Bypass -File run_ambient_gen.ps1 [-NoInstall]
param(
    [switch]$NoInstall,
    [string]$Project = 'D:\test1\mumu'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpriteMeta.ps1')

Add-Type -Path (Join-Path $PSScriptRoot '..\art\AmbientGen.cs') -ReferencedAssemblies System.Drawing
$out = Join-Path $env:TEMP 'mumu_gen\ambient'
New-Item -ItemType Directory -Force $out | Out-Null
[AmbientGen]::Run($out) | Out-Null
if ($NoInstall) { Write-Host "done (not installed): $out"; return }

$common = Join-Path $Project 'Assets\mumu\Resources\Sprites\Common'
foreach ($png in Get-ChildItem $out -Filter 'ambient_*.png' | Where-Object { $_.Name -notlike '*_preview.png' }) {
    Copy-Item $png.FullName "$common\$($png.Name)" -Force
    Write-SingleMeta "$common\$($png.Name)"
    Write-Host "  $($png.Name)"
}
Write-Host "done."
