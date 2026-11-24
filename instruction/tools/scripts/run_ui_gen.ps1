# UI 画像を作ってプロジェクトに入れる（Sprites/UI と Sprites/Common/paw）
#   UiGen.cs  : ボタン・パネル・看板・丸ボタン・トグル・歯車/閉じる/SE/BGM アイコン
#   UiGen3.cs : ★(on/off)・ゲージ(back/fill)・メニュー/やりなおし/ホーム アイコン
#   UiGen4.cs : 王冠(on/off)・ハート・肉球
# 既存の .meta はそのまま（9-slice などの設定を壊さない）。新しい画像だけ .meta を作る。
#
# 使い方: powershell -ExecutionPolicy Bypass -File run_ui_gen.ps1 [-NoInstall]
param(
    [switch]$NoInstall,
    [string]$Project = 'D:\test1\mumu'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpriteMeta.ps1')

$art = Join-Path $PSScriptRoot '..\art'
Add-Type -Path (Join-Path $art 'UiGen.cs') -ReferencedAssemblies System.Drawing
Add-Type -Path (Join-Path $art 'UiGen3.cs') -ReferencedAssemblies System.Drawing
Add-Type -Path (Join-Path $art 'UiGen4.cs') -ReferencedAssemblies System.Drawing

$out = Join-Path $env:TEMP 'mumu_gen\ui'
New-Item -ItemType Directory -Force $out | Out-Null
[UiGen]::Run($out) | Out-Null
[UiGen3]::Run($out) | Out-Null
[UiGen4]::Run($out) | Out-Null
if ($NoInstall) { Write-Host "done (not installed): $out"; return }

$sprites = Join-Path $Project 'Assets\mumu\Resources\Sprites'
# 9-slice の幅（px）。ここにない画像は border なし
$Borders = @{ ui_button = 16; ui_panel = 16; ui_plate = 16; ui_gauge_back = 16; ui_gauge_fill = 12 }
foreach ($png in Get-ChildItem $out -Filter '*.png') {
    $name = [IO.Path]::GetFileNameWithoutExtension($png.Name)
    if ($name -like '*preview*') { continue }
    $dir = if ($name -eq 'paw') { "$sprites\Common" } else { "$sprites\UI" }
    Copy-Item $png.FullName "$dir\$($png.Name)" -Force
    $border = if ($Borders.ContainsKey($name)) { $Borders[$name] } else { 0 }
    Write-SingleMeta "$dir\$($png.Name)" $border
    Write-Host "  $name"
}
Write-Host "done."
