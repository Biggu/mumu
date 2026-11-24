# 冒険のマップ 1〜50（map_list.csv と maps/map_XXXXXX.csv）をまとめて作り直す
# 中身は tools\mapgen\Program.cs（テーマは 3 ステージごと、最初の 3 つはとても簡単、50 で難易度 20）。
# 実行すると今のマップは上書きされる。先にバックアップを取る（-Backup で自動）。
# 同じ設定なら毎回同じマップになる（seed 固定）。
#
# 使い方: powershell -ExecutionPolicy Bypass -File run_mapgen.ps1 [-Backup]
param(
    [switch]$Backup,
    [string]$Project = 'D:\test1\mumu'
)
$ErrorActionPreference = 'Stop'
$mapData = Join-Path $Project 'Assets\mumu\Resources\MapData'
if ($Backup) {
    $bk = Join-Path $env:TEMP ("mumu_mapdata_" + (Get-Date -Format 'yyyyMMddHHmm'))
    Copy-Item $mapData $bk -Recurse
    Write-Host "backup: $bk"
}
Push-Location (Join-Path $PSScriptRoot '..\mapgen')
try { dotnet run -c Release -- $mapData }
finally { Pop-Location }
