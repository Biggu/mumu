# パグの画像を作ってプロジェクトに入れる（すべて等倍 = 1コマ 32x32。ゲームでは Point フィルターで拡大表示）
#   - Sprites/Common/pug.png              : 主人公の男の子・いつもの女の子（6 行 x 4 コマ）
#   - Sprites/Pugs/{000,100,...}/pug_XXX.png : character.csv の各キャラ（泣く/喜ぶ/座る/歩く の 4 行 x 4 コマ）。100 匹ずつのフォルダ
#   - Sprites/PugIcons/pug_icons_N.png    : ずかん用のアイコン集（座りの1コマ目、16 x 16 = 256 匹で1枚、icon_XXX）
# character.csv の fur（毛色）・outfit（衣装）・marking（模様）列から作る。新しいキャラは CSV に 1 行足してこれを実行。
# atlas にしないのは、表示中の子の分だけ読み込むため（ずかんだけはアイコン集を使う）。
#
# 使い方（PowerShell）:
#   powershell -ExecutionPolicy Bypass -File run_pug_gen.ps1              # 全部
#   powershell -ExecutionPolicy Bypass -File run_pug_gen.ps1 -Ids "51,52" # 指定キャラだけ（アイコン集は毎回全部作り直す）
#   -NoInstall を付けると出力フォルダに作るだけ（プレビュー確認用）
param(
    [string]$Ids = '',
    [switch]$NoInstall,
    [string]$Project = 'E:\testbranch\testdev\mumu'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpriteMeta.ps1')
# "1,46" のような指定を数字の一覧にする（-File で渡すと配列にならないため文字列で受ける）
$IdList = @($Ids -split '[,\s]+' | Where-Object { $_ -ne '' } | ForEach-Object { [int]$_ })

$art = Join-Path $PSScriptRoot '..\art'
Add-Type -Path (Join-Path $art 'PugGen5.cs') -ReferencedAssemblies System.Drawing
[PugGen5]::SetScale(1)
$Cell = 32

$out = Join-Path $env:TEMP 'mumu_gen\pugs'
New-Item -ItemType Directory -Force $out | Out-Null
$sprites = Join-Path $Project 'Assets\mumu\Resources\Sprites'

# 毛色（fur, furLight, furDark, mask, maskLight）。新しい毛色はここに足す
$Fur = @{
    fawn      = @('E8C08A', 'F8DEB6', 'C89A64', '4A3A34', '6A564C')
    black     = @('5A504C', '7A6E68', '3E3634', '262020', '3A302C')
    cream     = @('F4E6CC', 'FFF8EC', 'D8C4A0', '6A564C', '8A746A')
    apricot   = @('E8A060', 'F8C48A', 'C07A40', '5A3A2A', '7A5440')
    silver    = @('C8C4C0', 'E6E2DE', 'A09A96', '3A3634', '56504C')
    mocha     = @('A88468', 'C8A88C', '86644C', '3E2C22', '584034')
    peach     = @('FFC8A8', 'FFE4D0', 'E8A080', '7A4A3A', '9A6450')
    smoky     = @('98A4B8', 'C0CADA', '74809A', '2E3444', '464E62')
    caramel   = @('D89A58', 'F0C088', 'B07438', '5A3418', '7A4C28')
    honey     = @('FFD890', 'FFF0C8', 'E0B060', '6A4A2A', '8A6440')
    ash       = @('B0A8A4', 'D0CAC6', '8A8480', '3A3434', '524A48')
    ginger    = @('E88C48', 'F8B070', 'C06A30', '5A2E1A', '7A4428')
    sand      = @('E8D4A8', 'F8EAC8', 'C8B080', '5A4A3A', '7A6650')
    cocoa     = @('9A6A4A', 'B8886A', '7A4E34', '3A261A', '50382A')
    rose      = @('F0B0A8', 'FFD4CC', 'D08880', '7A4448', '9A5E60')
    mint      = @('B8E8D0', 'E0F8EC', '90C8B0', '3A5A50', '52786A')
    lilac     = @('C8B8E8', 'E8E0F8', 'A898C8', '4A4060', '625880')
    golden    = @('FFD050', 'FFF0A0', 'D8A020', '8A5A20', 'A87A30')
    pink      = @('FFB0C8', 'FFD8E4', 'E088A8', '7A4058', '9A5A74')
    sky       = @('A8D8F8', 'D8F0FF', '78B0D8', '3A5878', '557494')
    chocolate = @('8A5A3A', 'A87650', '6A4228', '3A2418', '4E3426')
    white     = @('FFFFFF', 'FFFFFF', 'E0E0E8', '8A8A96', 'A8A8B4')
    violet    = @('B8A0E0', 'DCCCF8', '8A70C0', '4A3A6A', '62528A')
    bubblegum = @('FFC8E0', 'FFE8F4', 'F0A0C8', '8A5070', 'A86A8A')
}
# 衣装は PugGen5.cs の OutfitFront / OutfitSide にある名前。模様は none / white / spots

function Get-PugFolder([int]$id) { '{0:000}' -f ([math]::Floor($id / 100) * 100) }

# 1) 主人公・いつもの女の子のシート（Ids 指定時は作らない）
if ($IdList.Count -eq 0) {
    [PugGen5]::Run("$out\pug.png", "$out\pug_preview.png", "$out\point_arrow.png", "$out\point_arrow_preview.png") | Out-Null
    if (-not $NoInstall) {
        Copy-Item "$out\pug.png" "$sprites\Common\pug.png" -Force
        Write-SheetMeta "$sprites\Common\pug.png" @('pug_girl_cry_', 'pug_girl_happy_', 'pug_boy_sit_', 'pug_boy_run_', 'pug_boy_happy_', 'pug_girl_sit_') 1500000001 4 $Cell
    }
}

# 2) キャラクター
$csv = Import-Csv (Join-Path $Project 'Assets\mumu\Resources\MasterData\character.csv') -Encoding UTF8
$pugRoot = "$sprites\Pugs"
New-Item -ItemType Directory -Force $pugRoot | Out-Null
Write-FolderMeta $pugRoot
foreach ($row in $csv) {
    $id = [int]$row.id
    if ($IdList.Count -gt 0 -and $IdList -notcontains $id) { continue }
    $p = $Fur[$row.fur]
    if ($p -eq $null) { Write-Warning "unknown fur '$($row.fur)' (id $id)"; continue }
    $outfit = if ($row.outfit -eq 'none') { '' } else { $row.outfit }
    $marking = if ($row.marking) { $row.marking } else { 'none' }
    $name = $row.sprite
    [PugGen5]::RunCharacter("$out\$name.png", "$out\${name}_preview.png", $p[0], $p[1], $p[2], $p[3], $p[4], $outfit, $marking) | Out-Null
    if (-not $NoInstall) {
        $folder = Join-Path $pugRoot (Get-PugFolder $id)
        if (-not (Test-Path $folder)) { New-Item -ItemType Directory $folder | Out-Null; Write-FolderMeta $folder }
        $target = "$folder\$name.png"
        # 前の版（フォルダ分け前）にあった画像は GUID を引き継ぐため .meta ごと移す
        $legacy = "$pugRoot\$name.png"
        if ((Test-Path $legacy) -and -not (Test-Path $target)) { Move-Item $legacy $target; if (Test-Path "$legacy.meta") { Move-Item "$legacy.meta" "$target.meta" } }
        Copy-Item "$out\$name.png" $target -Force
        Write-SheetMeta $target @("${name}_cry_", "${name}_happy_", "${name}_sit_", "${name}_walk_") 1500000001 4 $Cell
    }
}

# 3) ずかん用のアイコン集（全キャラ、座りの1コマ目）
Add-Type -AssemblyName System.Drawing
$perRow = 16; $perPage = $perRow * $perRow
$pages = [math]::Ceiling($csv.Count / $perPage)
$iconRoot = "$sprites\PugIcons"
if (-not $NoInstall) { New-Item -ItemType Directory -Force $iconRoot | Out-Null; Write-FolderMeta $iconRoot }
for ($page = 0; $page -lt $pages; $page++) {
    $bmp = New-Object Drawing.Bitmap ($perRow * $Cell), ($perRow * $Cell), ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $rects = New-Object Collections.Generic.List[object]
    for ($i = 0; $i -lt $perPage; $i++) {
        $index = $page * $perPage + $i
        if ($index -ge $csv.Count) { break }
        $row = $csv[$index]
        $sheet = "$out\$($row.sprite).png"
        if (-not (Test-Path $sheet)) { $sheet = Join-Path (Join-Path $pugRoot (Get-PugFolder ([int]$row.id))) "$($row.sprite).png" }
        if (-not (Test-Path $sheet)) { continue }
        $img = [Drawing.Image]::FromFile($sheet)
        $x = ($i % $perRow) * $Cell; $y = [math]::Floor($i / $perRow) * $Cell
        # 座りの1コマ目は上から3行目（4 行 x 4 コマ）
        $g.DrawImage($img, (New-Object Drawing.Rectangle $x, $y, $Cell, $Cell), (New-Object Drawing.Rectangle 0, ($Cell * 2), $Cell, $Cell), [Drawing.GraphicsUnit]::Pixel)
        $img.Dispose()
        $rects.Add(@{ Name = ('icon_{0:000}' -f [int]$row.id); X = $x; Y = ($perRow * $Cell) - $y - $Cell; W = $Cell; H = $Cell })
    }
    $g.Dispose()
    $iconPng = "$out\pug_icons_$page.png"
    $bmp.Save($iconPng); $bmp.Dispose()
    if (-not $NoInstall) {
        Copy-Item $iconPng "$iconRoot\pug_icons_$page.png" -Force
        Write-RectsMeta "$iconRoot\pug_icons_$page.png" $rects
    }
}
Write-Host "done. $($csv.Count) characters, $pages icon pages. preview: $out"
