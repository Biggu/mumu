# Unity の画像 .meta を書く共通処理（他のスクリプトから . で読み込む）
#   Write-SheetMeta : 128x128 コマのスプライトシート（行ごとの名前の接頭辞を指定）
#   Write-SingleMeta: 1枚絵（必要なら 9-slice の border）
# どちらも既存の .meta があれば GUID を引き継ぐので、プレハブの参照は切れない。

$TemplateDir = Join-Path $PSScriptRoot 'templates'

function Get-MetaGuid([string]$metaPath) {
    if (Test-Path $metaPath) {
        $line = (Get-Content $metaPath -TotalCount 2)[1]
        return ($line -replace 'guid: ', '').Trim()
    }
    return [guid]::NewGuid().ToString('N')
}

# $rows: 上の行から順に名前の接頭辞（例: 'pug_001_cry_','pug_001_happy_'）。各行 4 コマ、名前は 接頭辞+0..3
function Write-SheetMeta([string]$pngPath, [string[]]$rows, [int]$startId = 1500000001, [int]$cols = 4, [int]$cell = 128) {
    $metaPath = "$pngPath.meta"
    $guid = Get-MetaGuid $metaPath
    $m = [IO.File]::ReadAllText((Join-Path $TemplateDir 'sprite_sheet.meta'))
    $nl = if ($m.Contains("`r`n")) { "`r`n" } else { "`n" }
    $m = [regex]::Replace($m, 'guid: [0-9a-f]{32}', "guid: $guid", 1)

    $sb = New-Object Text.StringBuilder; $tbl = New-Object Text.StringBuilder; $id = $startId
    for ($r = 0; $r -lt $rows.Count; $r++) {
        for ($f = 0; $f -lt $cols; $f++) {
            $name = "$($rows[$r])$f"; $sid = [guid]::NewGuid().ToString('N')
            [void]$sb.Append("    - serializedVersion: 2`n      name: $name`n      rect:`n        serializedVersion: 2`n        x: $($f * $cell)`n        y: $(($rows.Count - 1 - $r) * $cell)`n        width: $cell`n        height: $cell`n      alignment: 0`n      pivot: {x: 0.5, y: 0.5}`n      border: {x: 0, y: 0, z: 0, w: 0}`n      customData: `n      outline: []`n      physicsShape: []`n      tessellationDetail: -1`n      bones: []`n      spriteID: $sid`n      internalID: $id`n      vertices: []`n      indices: `n      edges: []`n      weights: []`n")
            [void]$tbl.Append("      ${name}: $id`n"); $id++
        }
    }
    $a = $m.IndexOf("    sprites:$nl") + ("    sprites:$nl").Length
    $b = $m.IndexOf("    outline: []$nl    customData: ")
    $m = $m.Substring(0, $a) + $sb.ToString().Replace("`n", $nl) + $m.Substring($b)
    $a = $m.IndexOf("    nameFileIdTable:$nl") + ("    nameFileIdTable:$nl").Length
    $b = $m.IndexOf("  mipmapLimitGroupName:")
    $m = $m.Substring(0, $a) + $tbl.ToString().Replace("`n", $nl) + $m.Substring($b)
    [IO.File]::WriteAllText($metaPath, $m)
}

# $border: 9-slice の幅（px、0 = なし）。既に .meta があれば何もしない（手で変えた設定を壊さない）
function Write-SingleMeta([string]$pngPath, [int]$border = 0) {
    $metaPath = "$pngPath.meta"
    if (Test-Path $metaPath) { return }
    $m = [IO.File]::ReadAllText((Join-Path $TemplateDir 'sprite_single.meta'))
    $m = [regex]::Replace($m, 'guid: [0-9a-f]{32}', 'guid: ' + [guid]::NewGuid().ToString('N'), 1)
    if ($border -gt 0) { $m = [regex]::Replace($m, 'spriteBorder: \{[^}]*\}', "spriteBorder: {x: $border, y: $border, z: $border, w: $border}") }
    [IO.File]::WriteAllText($metaPath, $m)
}

# フォルダの .meta（Unity 外で作ったフォルダ用）
function Write-FolderMeta([string]$dir) {
    if (Test-Path "$dir.meta") { return }
    [IO.File]::WriteAllText("$dir.meta", "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
}

# 9-slice の幅を辺ごとに指定（left, bottom, right, top の順、px）。既に .meta があれば border だけ書き換える
function Write-SingleMetaBorder([string]$pngPath, [int]$left, [int]$bottom, [int]$right, [int]$top) {
    $metaPath = "$pngPath.meta"
    if (-not (Test-Path $metaPath)) { Write-SingleMeta $pngPath 0 }
    $m = [IO.File]::ReadAllText($metaPath)
    $m = [regex]::Replace($m, 'spriteBorder: \{[^}]*\}', "spriteBorder: {x: $left, y: $bottom, z: $right, w: $top}")
    [IO.File]::WriteAllText($metaPath, $m)
}
# 名前と位置を指定したスライス（$rects は @{ Name; X; Y; W; H } の配列、Y は画像の下からの位置）
function Write-RectsMeta([string]$pngPath, $rects, [int]$startId = 1500000001) {
    $metaPath = "$pngPath.meta"
    $guid = Get-MetaGuid $metaPath
    $m = [IO.File]::ReadAllText((Join-Path $TemplateDir 'sprite_sheet.meta'))
    $nl = if ($m.Contains("`r`n")) { "`r`n" } else { "`n" }
    $m = [regex]::Replace($m, 'guid: [0-9a-f]{32}', "guid: $guid", 1)
    $sb = New-Object Text.StringBuilder; $tbl = New-Object Text.StringBuilder; $id = $startId
    foreach ($r in $rects) {
        $sid = [guid]::NewGuid().ToString('N')
        [void]$sb.Append("    - serializedVersion: 2`n      name: $($r.Name)`n      rect:`n        serializedVersion: 2`n        x: $($r.X)`n        y: $($r.Y)`n        width: $($r.W)`n        height: $($r.H)`n      alignment: 0`n      pivot: {x: 0.5, y: 0.5}`n      border: {x: 0, y: 0, z: 0, w: 0}`n      customData: `n      outline: []`n      physicsShape: []`n      tessellationDetail: -1`n      bones: []`n      spriteID: $sid`n      internalID: $id`n      vertices: []`n      indices: `n      edges: []`n      weights: []`n")
        [void]$tbl.Append("      $($r.Name): $id`n"); $id++
    }
    $a = $m.IndexOf("    sprites:$nl") + ("    sprites:$nl").Length
    $b = $m.IndexOf("    outline: []$nl    customData: ")
    $m = $m.Substring(0, $a) + $sb.ToString().Replace("`n", $nl) + $m.Substring($b)
    $a = $m.IndexOf("    nameFileIdTable:$nl") + ("    nameFileIdTable:$nl").Length
    $b = $m.IndexOf("  mipmapLimitGroupName:")
    $m = $m.Substring(0, $a) + $tbl.ToString().Replace("`n", $nl) + $m.Substring($b)
    [IO.File]::WriteAllText($metaPath, $m)
}