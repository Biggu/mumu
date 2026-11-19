# MapGenerator の確認（19 パターン x 難易度 1/10/20/30/40/50 x seed 1〜5）
# 各セルに 大きさ・通れるマス数・有効マップ数・経路/障害物/空きマスの割合 を出し、同じ seed で同じマップになるかも確認する。
# MapGenerator.cs を変えたら実行して "invalid maps: 0/570" を確認する。
#
# 使い方: powershell -ExecutionPolicy Bypass -File run_gentest.ps1
Push-Location (Join-Path $PSScriptRoot '..\gentest')
try { dotnet run -c Release }
finally { Pop-Location }
