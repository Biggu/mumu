# 待ち時間に画面を横切る生き物（ちょうちょ・かもめ・うさぎ・キャンディ・紙ひこうき）
# → Sprites/Common/critters.png（2 コマ x 5 行、48x48）。名前は critter_{name}_{0|1}
# 使い方: powershell -ExecutionPolicy Bypass -File run_critter_gen.ps1 [-NoInstall]
param(
    [switch]$NoInstall,
    [string]$Project = 'E:\testbranch\testdev\mumu'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpriteMeta.ps1')

Add-Type -Path (Join-Path $PSScriptRoot '..\art\CritterGen.cs') -ReferencedAssemblies System.Drawing
$out = Join-Path $env:TEMP 'mumu_gen\critters'
New-Item -ItemType Directory -Force $out | Out-Null
[CritterGen]::Run("$out\critters.png") | Out-Null
if ($NoInstall) { Write-Host "done (not installed): $out"; return }

$target = Join-Path $Project 'Assets\mumu\Resources\Sprites\Common\critters.png'
Copy-Item "$out\critters.png" $target -Force
Write-SheetMeta $target @([CritterGen]::Names | ForEach-Object { "critter_${_}_" }) 1500000001 2 48
Write-Host "done."
