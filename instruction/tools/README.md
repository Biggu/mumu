# つなパグ ツール集

Unity の外で画像・マップを作るためのツールと、コンパイル確認用のプロジェクト。
すべて Windows の PowerShell 5.1 と .NET SDK（`dotnet`）で動く。画像は System.Drawing で描いている。

```
tools/
  art/        画像ジェネレーター（C#、PowerShell の Add-Type で読み込んで実行）
  scripts/    実行用スクリプト（ここを使う）
    templates/  .meta のひな形（1枚絵・スプライトシート）
  mapgen/     冒険マップ 1〜50 をまとめて作る（dotnet run）
  gentest/    MapGenerator の確認（dotnet run）
  verify/     Unity なしでスクリプトをコンパイルする csproj
```

実行は `powershell -ExecutionPolicy Bypass -File <スクリプト>`。
生成した画像はいったん `%TEMP%\mumu_gen\` に出て、`-NoInstall` を付けなければプロジェクトへコピーされる。
`.meta` は既存があれば GUID を引き継ぐので、プレハブからの参照は切れない。

## scripts/

| スクリプト | 内容 |
|---|---|
| `run_pug_gen.ps1` | パグの画像（等倍 32px のコマ、`Sprites/Pugs/{000,100,…}/` に 100 匹ずつ、ずかん用アイコン集 `Sprites/PugIcons/` も作る）。character.csv の fur / outfit / marking 列から。`Sprites/Common/pug.png`（主人公・いつもの女の子）と `Sprites/Pugs/pug_XXX.png`（character.csv の全キャラ）。`-Ids "51,52"` で指定キャラだけ。毛色の表はスクリプト内 `$Fur`、衣装の名前は `art/PugGen5.cs` |
| `run_ui_gen.ps1` | UI 画像（ボタン・パネル・看板・トグル・アイコン・★・ゲージ・王冠・ハート・肉球）→ `Sprites/UI`、`Sprites/Common/paw.png`。新しい画像の 9-slice 幅はスクリプト内 `$Borders` |
| `run_critter_gen.ps1` | 画面を横切る生き物（ちょうちょ・かもめ・うさぎ・キャンディ・紙ひこうき）→ `Sprites/Common/critters.png`（48x48 x 2 コマ x 5 行） |
| `run_gimmick_gen.ps1` | ギミックのアイコン（やじるし・まがる・まっすぐ・宝箱 木/銀/金・カギ・扉・ワープ入口/出口・こわれる橋、通貨アイコンのほね。カギ・扉・ワープは白で描いてゲームで色をつける）→ `Sprites/Common/gimmick_*.png` |
| `run_ambient_gen.ps1` | 雰囲気の粒の画像（四角 `ambient_square`・丸 `ambient_round`、ドット絵のまま）→ `Sprites/Common/ambient_*.png`。`art/AmbientGen.cs`。プレビューは `%TEMP%\mumu_gen\ambient\*_preview.png` |
| `run_uikit_gen.ps1` | UI キット 100 枚 → `Sprites/UI/Kit/kit_*.png`（9-slice 自動）。一覧は `instruction/ui_kit_preview.png` |
| `run_theme_gen.ps1` | **古い生成（14 テーマ・ブロック・デコ・水・橋・背景）**。今の 1〜33 のテーマ画像（葉の色、特別なテーマの中身、デコの大きさ 静止画は幅 50 units 以内）はこの生成では作っていない。現在の画像は作業用スクリプト（保存していない）で作った。テーマを変える時は `Sprites/Themes/theme_XXXXXX` を直接直す、または作業用スクリプトを `art/` に保存してから使う |
| `run_mapgen.ps1` | 冒険マップ 1〜50 を作り直す（上書き。`-Backup` 推奨）。設定は `mapgen/Program.cs` |
| `run_gentest.ps1` | MapGenerator を 570 マップで確認（ギミックありも）。`invalid maps: 0/570`・`gimmick maps invalid: 0/570`・`play check fail: 0`（PuzzlePath で設計の経路をたどってクリアできるか）・`rule tests failed: 0`（扉・こわれる橋・ワープの決まりを小さいマップで確認）・`plain maps treasure wrong: 0`（ギミックなしでも宝箱が1つ）・`wrongly allowed: 0`（やじるしを横から出られないか）が出れば OK |
| `verify_build.ps1` | プロジェクト（既定 `D:\test\mumu`、`-Project` で変更）の Assets/mumu/Scripts の全 .cs をコンパイル。プロジェクト直下の `Assembly-CSharp*.csproj` から `verify` 用の csproj を毎回作り直す。`0 エラー` と出れば OK |
| `SpriteMeta.ps1` | .meta を書く共通関数（`Write-SheetMeta` / `Write-SingleMeta` / `Write-FolderMeta`） |

## art/（画像ジェネレーター）

| ファイル | クラス | 主な関数 |
|---|---|---|
| `PugGen5.cs` | `PugGen5` | `Run(sheet, preview, arrow, arrowPreview)` 主人公シート＋下向き矢印、`RunCharacter(sheet, preview, fur, furLight, furDark, mask, maskLight, outfit)` キャラシート |
| `UiGen.cs` | `UiGen` | `Run(outDir)` 基本 UI |
| `UiGen3.cs` | `UiGen3` | `Run(outDir)` ★・ゲージ・メニュー/やりなおし/ホーム |
| `UiGen4.cs` | `UiGen4` | `Run(outDir)` 王冠・ハート・肉球 |
| `ThemeGen.cs` | `ThemeGen` | `Run(outRoot)` 古い 14 テーマの生成（今の 1〜33 の画像は作っていない。`run_theme_gen.ps1` の注意を見る） |
| `GimmickGen.cs` | `GimmickGen` | `Run(outDir)` ギミックのアイコン |
| `UiKitGen.cs` | `UiKitGen` | `Run(outDir)` UI キット（色は `Palette`、アイコンは `Icons` の文字の絵、9-slice 幅は `kit_border.txt` に出す） |
| `CritterGen.cs` | `CritterGen` | `Run(sheetPath)` 横切る生き物のシート（`Names` の順に行） |

- 1コマは 32x32 px（等倍で保存、`PugGen5.SetScale(1)`。ほかの画像は 4 倍）。パグは頭を大きめ（ちびキャラ）。
- 主人公シート `pug.png`（6行x4コマ）: girl_cry / girl_happy / boy_sit / boy_run（横向き・右） / boy_happy / girl_sit
- キャラシート `pug_XXX.png`（4行x4コマ）: `{sprite}_cry_N` / `_happy_N` / `_sit_N` / `_walk_N`
- 衣装: none, flower_crown, straw_hat, scarf, beret, sailor_hat, chef_hat, knit_cap, bandana, party_hat, sunglasses, bow_tie, cat_hood, sprout, bell_collar（ノーマル）/ crown, sakura_pin, halo, witch_hat, earmuffs（tier4）/ star_band, sparkles（tier6）。halo と sparkles は輪郭の外に描く（`OutfitAfter`）
- 同じ型名は 1 つの PowerShell では 1 回しか Add-Type できない。ジェネレーターを直したらクラス名を変えるか、新しい PowerShell で実行する（スクリプトは毎回新しいプロセスなので問題なし）。
- 色やパーツを変えたい時は `-NoInstall` で出して `%TEMP%\mumu_gen\...\*_preview.png` を確認してから入れる。

## mapgen / gentest

- どちらも `mapgen\mapgen.csproj` / `gentest\gentest.csproj` が、ゲーム側のソース（`D:\test\mumu\Assets\mumu\Scripts` の `Data\**\*.cs`・`Define\MumuEnum.cs`・`Game\Gimmick` の一部・`Game\Map\PuzzlePath.cs`）を直接コンパイルする。Unity の型は `Stubs.cs` の最小限の代用品。`CharacterMaster.cs` は Unity 専用のため除外、`MapCsv.cs` は gentest だけ除外、`gentest` は `Core\Save\SaveCrypto.cs` も入れる。
- `GimmickMaster.cs` は `Sprite` / `ResourceManager` を使うため入れていない。ゲーム側で生成に必要な型を増やしたら csproj の `Compile Include` も足す。
- ゲーム側でファイルを移動したら csproj のパスも直す（`..\..\..\mumu\...` は tools の 3 つ上が D:\test）。
- 動作確認済み: `run_gentest.ps1` → `invalid maps: 0/570`、`run_mapgen.ps1` は `play check: 0 fail / 500 stages`（ただし実行すると今の MapData を上書きする）。
- mapgen の決まり（`Program.cs`）: テスト用 500 ステージ。テーマは 10 ステージごとに 1〜14 を繰り返し、難易度は 10 ステージごとに +1（1〜50）。新しいギミックの順は `Schedule`（11, 21, … 131 に一本道のチュートリアル）、名前は `TutorialNames`。
  - ステージ1（手で作った一本道、宝箱なし）はそのまま使い、2 以降は消して作り直す
  - チュートリアルは 5x5 のくねくねの一本道（`buildTutorial`）に新しいギミックを並べ、木の宝箱を1つ
  - ふつう・むずかしいは `MapGenerator.Generate` に覚えたギミックを全部渡し、新しいギミックが入る seed を探す（同じ設定なら毎回同じ）
  - 前の 50 ステージ版は `Program_50stages.cs.txt`（参考用）

## verify

- `verify_build.ps1` の実行のたびに、プロジェクト直下の `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj`（Unity が作る）を読んで、`verify` の csproj に書き出す。書き出す時に `Assets\`・`Library\`・`ProjectReference` の相対パスをプロジェクトの絶対パスに直す（Compile 一覧は今の .cs ファイルに置き換える）。
- verify の csproj はスクリプトが作るので直接編集しない。Unity でパッケージを追加して参照が足りなくなったら、Unity にプロジェクトの csproj を作り直させる（Unity 側の参照を直す）。
- Unity が書き出す csproj の場所が変わったら、`verify_build.ps1` の `-Project` を指定する。
