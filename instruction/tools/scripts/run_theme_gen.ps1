# テーマ画像（33 テーマ: 昼 1〜14 / 新 15〜19 / 夜 20〜33）を作ってプロジェクトに入れる（Sprites/Themes/theme_XXXXXX）
#   XXXXXX_0 未通過 / _1 通過済み / _2・_3 コマ送りのデコ（4コマのシート）/ _4・_5 デコ（静止、透明、169x281 下揃え）/ _6 水 / _7 通れないブロックの土台 / _8 橋
#   _bg 背景タイルの4コマシート（きらめき）。Unity 側は ThemeDecoSheetImporter が分割する
# テーマの中身（色・デコ）は art\ThemeGen.cs の ThemeGen.All() を編集する。
#
# 使い方: powershell -ExecutionPolicy Bypass -File run_theme_gen.ps1 [-NoInstall]
#   出力フォルダに sheet_*.png（一覧プレビュー）も作る
param(
    [switch]$NoInstall,
    [string]$Project = 'E:\testmodel\dev\mumu'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpriteMeta.ps1')

Add-Type -Path (Join-Path $PSScriptRoot '..\art\ThemeGen.cs') -ReferencedAssemblies System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot '..\art\DecoSheetGen.cs') -ReferencedAssemblies System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot '..\art\NightGen.cs') -ReferencedAssemblies System.Drawing
$out = Join-Path $env:TEMP 'mumu_gen\themes'
New-Item -ItemType Directory -Force $out | Out-Null
Write-Host ([ThemeGen]::Run($out))
# 昼のデコ（_2・_3 のシート）を作り直してから、夜のテーマ（20〜33）を昼の画像から作る
[DecoSheetGen]::Run($out)
[NightGen]::Run($out)
if ($NoInstall) { Write-Host "done (not installed): $out"; return }

$themes = Join-Path $Project 'Assets\mumu\Resources\Sprites\Themes'
foreach ($dir in Get-ChildItem $out -Directory -Filter 'theme_*') {
    $target = Join-Path $themes $dir.Name
    if (-not (Test-Path $target)) { New-Item -ItemType Directory $target | Out-Null; Write-FolderMeta $target }
    foreach ($png in Get-ChildItem $dir.FullName -Filter '*.png') {
        Copy-Item $png.FullName "$target\$($png.Name)" -Force
        Write-SingleMeta "$target\$($png.Name)"
    }
    Write-Host "  $($dir.Name)"
}
Write-Host "done."
