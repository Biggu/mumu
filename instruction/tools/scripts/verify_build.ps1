# Unity を使わずにスクリプトのコンパイルを確認する
# tools\verify の csproj（Unity が作ったものの写し）の Compile 一覧を Assets/mumu/Scripts の今のファイルで作り直してビルドする。
# 0 エラーなら OK（MSB3245 の「参照を解決できません」警告は無視してよい）。
#
# 前提: .NET SDK（dotnet）、Unity 6000.0.58f2 が既定の場所にインストール済み、一度 Unity でプロジェクトを開いて Library がある
# 使い方: powershell -ExecutionPolicy Bypass -File verify_build.ps1
param([string]$Project = 'D:\test1\mumu')
$ErrorActionPreference = 'Stop'

$verify = Join-Path $PSScriptRoot '..\verify'
$abs = $Project.TrimEnd('\')
# Unity が作った csproj（相対パス）を絶対パスにして verify に写す
foreach ($name in 'Assembly-CSharp.csproj', 'Assembly-CSharp-Editor.csproj') {
    $text = [IO.File]::ReadAllText((Join-Path $Project $name))
    $text = $text.Replace('Include="Assets\', 'Include="' + $abs + '\Assets\')
    $text = $text.Replace('HintPath>Library\', 'HintPath>' + $abs + '\Library\')
    # Assembly-CSharp の参照は verify 側（今の .cs を入れた csproj）を使うため、UniTask 側だけ絶対パスにする
    $text = $text.Replace('ProjectReference Include="UniTask', 'ProjectReference Include="' + $abs + '\UniTask')
    [IO.File]::WriteAllText((Join-Path $verify $name), $text)
}
$scripts = Join-Path $Project 'Assets\mumu\Scripts'
$files = Get-ChildItem $scripts -Recurse -Filter *.cs
$sets = @(
    @('Assembly-CSharp.csproj', ($files | Where-Object { $_.FullName -notmatch '\\Editor\\' })),
    @('Assembly-CSharp-Editor.csproj', ($files | Where-Object { $_.FullName -match '\\Editor\\' }))
)
$escaped = [regex]::Escape((Join-Path $Project 'Assets\mumu\'))
foreach ($set in $sets) {
    $path = Join-Path $verify $set[0]
    $lines = [IO.File]::ReadAllLines($path)
    $first = -1; $out = New-Object Collections.Generic.List[string]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "Compile Include=`"$escaped") { if ($first -lt 0) { $first = $out.Count }; continue }
        $out.Add($lines[$i])
    }
    $out.InsertRange($first, [string[]]($set[1] | ForEach-Object { "    <Compile Include=`"$($_.FullName)`" />" }))
    [IO.File]::WriteAllLines($path, $out)
    Write-Host "$($set[0]): $($set[1].Count) files"
}

Push-Location $verify
try {
    dotnet build Assembly-CSharp-Editor.csproj -nologo -v q 2>&1 | Where-Object { $_ -match 'error|エラー' -and $_ -notmatch 'MSB3245' }
}
finally { Pop-Location }
