# テーマのアニメするデコ（_2・_3 の 4コマ・スプライトシート）を作ってプロジェクトに入れる
#   art\DecoSheetGen.cs の DecoSheetGen.Run を呼ぶ。_4・_5 の静止デコは変えない
#
# 使い方: powershell -ExecutionPolicy Bypass -File run_deco_sheet_gen.ps1 [-Project <プロジェクトのパス>]
param(
    [string]$Project = 'E:\testmodel\dev\mumu'
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '..\art\DecoSheetGen.cs') -ReferencedAssemblies System.Drawing
$themes = Join-Path $Project 'Assets\mumu\Resources\Sprites\Themes'
[DecoSheetGen]::Run($themes)
Write-Host "done."
