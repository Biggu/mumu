# ギミックのアイコン（やじるし・まがる・まっすぐ・ほね）→ Sprites/Common/gimmick_*.png
# 使い方: powershell -ExecutionPolicy Bypass -File run_gimmick_gen.ps1 [-NoInstall]
param(
    [switch]$NoInstall,
    [string]$Project = 'D:\test1\mumu'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpriteMeta.ps1')

Add-Type -Path (Join-Path $PSScriptRoot '..\art\GimmickGen.cs') -ReferencedAssemblies System.Drawing
$out = Join-Path $env:TEMP 'mumu_gen\gimmicks'
New-Item -ItemType Directory -Force $out | Out-Null
[GimmickGen]::Run($out) | Out-Null
if ($NoInstall) { Write-Host "done (not installed): $out"; return }

$common = Join-Path $Project 'Assets\mumu\Resources\Sprites\Common'
foreach ($png in Get-ChildItem $out -Filter 'gimmick_*.png') {
    Copy-Item $png.FullName "$common\$($png.Name)" -Force
    Write-SingleMeta "$common\$($png.Name)"
    Write-Host "  $($png.Name)"
}
Write-Host "done."
