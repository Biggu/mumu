# UI キット（色違いの枠・ボタン・タブ・リボン・吹き出し・線・ゲージ、白いアイコン、バッジ、チェック/ラジオ）
# → Sprites/UI/Kit/kit_*.png。9-slice の幅は kit_border.txt から自動で入る。
# 一覧のプレビュー画像も作る（%TEMP%\mumu_gen\uikit\kit_preview.png）
# 使い方: powershell -ExecutionPolicy Bypass -File run_uikit_gen.ps1 [-NoInstall]
param(
    [switch]$NoInstall,
    [string]$Project = 'E:\testbranch\testdev\mumu'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpriteMeta.ps1')

Add-Type -Path (Join-Path $PSScriptRoot '..\art\UiKitGen.cs') -ReferencedAssemblies System.Drawing
$out = Join-Path $env:TEMP 'mumu_gen\uikit'
if (Test-Path $out) { Get-ChildItem $out -Filter '*.png' | Remove-Item -Force }
New-Item -ItemType Directory -Force $out | Out-Null
[UiKitGen]::Run($out) | Out-Null

# プレビュー（緑の背景に並べる）
Add-Type -AssemblyName System.Drawing
$pngs = Get-ChildItem $out -Filter 'kit_*.png' | Sort-Object Name
$cols = 8; $cell = 112
$rows = [math]::Ceiling($pngs.Count / $cols)
$sheet = New-Object Drawing.Bitmap ($cols * $cell), ($rows * ($cell + 14))
$g = [Drawing.Graphics]::FromImage($sheet); $g.Clear([Drawing.Color]::FromArgb(110, 160, 110)); $g.InterpolationMode = 'NearestNeighbor'
$font = New-Object Drawing.Font('Segoe UI', 7)
for ($i = 0; $i -lt $pngs.Count; $i++) {
    $img = [Drawing.Image]::FromFile($pngs[$i].FullName)
    $scale = [math]::Min(1.0, [math]::Min(($cell - 8) / $img.Width, ($cell - 8) / $img.Height))
    $x = ($i % $cols) * $cell + 4; $y = [math]::Floor($i / $cols) * ($cell + 14) + 4
    $g.DrawImage($img, $x, $y, [int]($img.Width * $scale), [int]($img.Height * $scale))
    $g.DrawString(($pngs[$i].BaseName -replace '^kit_', ''), $font, [Drawing.Brushes]::White, $x, $y + $cell - 6)
    $img.Dispose()
}
$sheet.Save("$out\kit_preview.png"); $g.Dispose(); $sheet.Dispose()
if ($NoInstall) { Write-Host "done (not installed): $out"; return }

$kit = Join-Path $Project 'Assets\mumu\Resources\Sprites\UI\Kit'
if (-not (Test-Path $kit)) { New-Item -ItemType Directory $kit | Out-Null; Write-FolderMeta $kit }
$borders = @{}
foreach ($line in Get-Content "$out\kit_border.txt") { $p = $line.Split(','); $borders[$p[0]] = $p[1..4] }
foreach ($png in $pngs) {
    Copy-Item $png.FullName "$kit\$($png.Name)" -Force
    Write-SingleMeta "$kit\$($png.Name)"
    if ($borders.ContainsKey($png.BaseName)) {
        $b = $borders[$png.BaseName]
        Write-SingleMetaBorder "$kit\$($png.Name)" $b[0] $b[1] $b[2] $b[3]
    }
}
Write-Host "done. $($pngs.Count) images -> $kit"
