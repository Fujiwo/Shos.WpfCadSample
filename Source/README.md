# Shos.WpfCadSample

WPF（C# 14 / .NET 10）で作る、MVC + MVVM 構成の簡易 2D CAD のサンプルです。要件は[仕様書](../Documents/20261105-cad-specification.md)（唯一の要件の文書）、設計と計画は [Documents](../Documents/) と [Plans](../Plans/) の承認済みの文書を正とします。

> この README は、実装計画書の段階 I-1 で作った骨子です。操作方法と設計判断は、段階 I-10 で完成させます。

## ビルドとテスト

リポジトリのルートから、Windows の `dotnet` で実行します（WSL からは `dotnet.exe`）。

```
dotnet build Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx
dotnet test Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx
```

## フォルダーと namespace の構成

```text
Source\Shos.WpfCadSample\
├─ Shos.WpfCadSample.slnx
├─ Directory.Build.props        … 共通の設定（Nullable、警告をエラーとして扱う、ルート namespace）
├─ Shos.WpfCadSample.Core\      … net10.0。UI に依存しない
│   ├─ Models\                  … Shos.WpfCadSample.Models
│   │   └─ Geometry\            … Shos.WpfCadSample.Models.Geometry
│   ├─ Controllers\             … Shos.WpfCadSample.Controllers
│   ├─ ViewModels\              … Shos.WpfCadSample.ViewModels
│   ├─ Serialization\           … Shos.WpfCadSample.Serialization
│   ├─ Settings\                … Shos.WpfCadSample.Settings
│   └─ Services\                … Shos.WpfCadSample.Services
├─ Shos.WpfCadSample\           … net10.0-windows（WPF）
│   ├─ App.xaml                  … Shos.WpfCadSample.App（ルート namespace 直下の唯一の型）
│   ├─ Views\                   … Shos.WpfCadSample.Views
│   └─ Infrastructure\          … Shos.WpfCadSample.Infrastructure
└─ Shos.WpfCadSample.Tests\     … net10.0。xUnit。Core を参照
    ├─ Architecture\            … Shos.WpfCadSample.Tests.Architecture
    ├─ Controllers\             … Shos.WpfCadSample.Tests.Controllers
    ├─ Models\                  … Shos.WpfCadSample.Tests.Models
    │   └─ Geometry\            … Shos.WpfCadSample.Tests.Models.Geometry
    ├─ Serialization\           … Shos.WpfCadSample.Tests.Serialization
    ├─ Settings\                … Shos.WpfCadSample.Tests.Settings
    └─ Support\                 … Shos.WpfCadSample.Tests.Support（一時フォルダーなどのテストの補助）
```

## 使用するパッケージと版

| パッケージ | 版 | 使用するプロジェクト | 用途 |
|---|---|---|---|
| Shos.UndoRedoList | 1.0.8 | Core | Undo / Redo（`UndoRedoObservableCollection<T>`） |
| CommunityToolkit.Mvvm | 8.4.2 | Core | `ObservableObject`、`RelayCommand` |
| MaterialDesignThemes | 5.4.0 | WPF | UI の外観（Material Design 3、Light） |
| xunit | 2.9.3 | Tests | テスト |
| xunit.runner.visualstudio | 4.0.1 | Tests | `dotnet test`（VSTest）からの実行 |
| Microsoft.NET.Test.Sdk | 18.11.0 | Tests | `dotnet test` の実行環境 |

- 版は、導入時点で NuGet の最新の安定版であることを確認して固定します（実装計画書 P-1）。上表の版は、2026-11-06 に NuGet の最新の安定版であることを確認しました。確認と動作の状況は、[進捗記録](../Progress/20261105-cad-progress.md)を参照してください。
- `xunit.runner.visualstudio` と `Microsoft.NET.Test.Sdk` は、要件（仕様書 3.2）の xUnit のテストを `dotnet test`（VSTest）で実行するための付随パッケージです。xUnit の本体（`xunit`）だけでは `dotnet test` からテストを見つけて実行できないため、追加しています。
- `xunit` は v2 系（2.9.3）を使います。実績のある v2 系の組み合わせで、`slnx` 全体の `dotnet test` を確実に動かすためです（実装計画書 4 章。v3 への移行は要件外）。
- EMF・画像の出力に使う GDI+（`System.Drawing`）は、WPF のフレームワーク参照（`Microsoft.WindowsDesktop.App.WPF`）に `System.Drawing.Common` が含まれるため、パッケージを追加せずに使えます（2026-11-06 の試作 R-2 で確認）。

## 出典

- 自由線のスプライン変換（`Models\Geometry\SplineBezier.cs`）は、[Shos.BezierSamples](https://github.com/Fujiwo/Shos.BezierSamples) の `BezierWpf/Geometry.cs`（MIT License）の考え方（接線の三重対角の連立方程式をトーマス法で解く。閉じた曲線は、巡回三重対角の連立方程式を Sherman-Morrison の公式で解く）に基づいて実装しました。コードは複製していません。

## 操作方法

（段階 I-10 で記載する：ショートカットの一覧を含む）

## 設計判断

（段階 I-10 で記載する：座標系、EMF の方式、Material Design の版・配色と適用範囲、参照した資料と出典）

### 承認済みの文書で決まっていない実装の詳細（実装計画書 9 章）

| 段階 | 箇所 | 選んだ既定 | 理由 |
|---|---|---|---|
| I-2 | `OutlineFigure.Circle(center, radius)` | 円の `Outline`（4 本の 3 次ベジェ。設計書 DA-3）を作る処理を、`Models.Geometry` に置く。I-3 の `CircleShape.CreateOutline()` は、これを使う | テスト計画書の T-OUT-04（円の `Outline` の誤差）を、I-2 の `Outline` のテストとして検証するため |
| I-2 | `Viewport.UpdateOverview(Rect2?)` | 図形がないときは `null` を渡す | 図形のないドキュメントの全体図（論理領域だけ）を、外接矩形の代わりの値を作らずに表すため |
| I-2 | `Viewport.SetViewSize` | 幅・高さが正の有限値でないとき（最小化など）は、直前の表示を保つ。最初の `SetViewSize` までは、論理領域を等倍で表示する大きさとする | 大きさ 0 で縮尺が 0 になり、座標変換が無限大・NaN になるのを防ぐため |
| I-2 | `Viewport` の全体図の判定 | `Scale` が `FitScale` の相対 1e-9 以内なら、全体図の表示中とみなす | 拡大と縮小を往復したときの丸め誤差で、全体図の表示を拡大中と取り違えないため |
| I-2 | `CubicBezierSegment` の折れ線化 | 半分ずつの分割を、最大 16 段で打ち切る | 許容誤差が座標の浮動小数点の精度を下回っても終わるようにするため。1 段ごとに誤差は約 1/4 になる。曲線は全体図に収まるため、最大の拡大（全体図の 32 倍）でも、打ち切ったときの画面上の誤差は 0.001 px 未満で、設計書 DA-2（画面上 0.25 px 相当）を満たす |
| I-2 | `Point2.DistanceTo` | テスト計画書の「`Distance`」を、`DistanceTo` という名前にした | `Outline.DistanceTo`、`Segment2.DistanceTo` と、同じ意味の操作に同じ名前を付けるため |
| I-3 | 点数が足りない曲線の `CreateOutline()` | 開いた自由線は 2 点未満、閉じた自由線は 3 点未満のとき、点を直線で結んだ折れ線で表す（閉じた自由線は閉じる） | 入力の途中の図形（プレビュー）と、`CanAppend` が 2 点目で作る候補を、スプラインを作れない点数でも表示・判定できるようにするため |
| I-3 | 矩形のハンドルの順と `MoveHandle` | ハンドルは `Corner` から一周する順の 4 つの角とし、角 i の対角は角 (i + 2) % 4。動かした角の添字は、動かした後も変わらない | ドラッグ中に同じ添字のハンドルを動かし続けられるようにするため |
| I-3 | `ShapeKindInfo.FromTool(Tool.Select)` | `null` を返す | Select は図形を描かないツールで、描画ツールかどうかの判定にも使えるため |
| I-3 | `Drawing.Undo` / `Redo` | 取り消せる（やり直せる）操作がないときは `InvalidOperationException` | 呼び出し側は `CanUndo` / `CanRedo` で無効にするため、呼ばれたら誤りとして早く気づけるようにする |
| I-3 | `ShapeHitTester` の曲線の近似 | 許容範囲の 1/20（約 5 px に対して 0.25 px）で折れ線に近似して距離を測る | 設計書 DA-2 の許容誤差（画面上 0.25 px 相当）を、許容範囲から求めるため |
| I-3 | `ExportFrame` の画素数の上限 | 切り上げで各辺が 1 px 未満増えることを見込み、(幅·縮尺 + 1)(高さ·縮尺 + 1) ≤ 4,000,000 から縮尺を求める | 切り上げ後も必ず上限を満たすため。上限から決まる縮尺との差は 1 px 程度で、テストで 99% 以上を確認した |
| I-3 | `RgbColor.ToHex()` | 16 進は大文字（`#1A2B3C`） | 読み込み（`TryParse`）は大文字・小文字を問わないため、出力は 1 つの形にそろえる |
| I-4 | JSON の必須項目と導出値 | 各型に `[JsonRequired]` を付ける代わりに、`Serialization` の `JsonRules`（ドキュメント・独自形式・設定に共通の規則）で、読み込む項目（設定できるプロパティ）をすべて必須にし、導出値（`HandlePoints`、`Bounds` など設定できないプロパティ）を読み書きの対象から除く | 「すべての項目が必須」「導出値は保存しない」という規則を 1 か所に置くため。`IgnoreReadOnlyProperties` は、コレクション型の読み取り専用プロパティ（`HandlePoints`）を除かない |
| I-4 | 図形の判別子の位置 | `type` が図形の最初の項目でなくても読み込む（`AllowOutOfOrderMetadataProperties`） | 手で編集したファイルの項目の順序で、読み込みの成否が変わらないようにするため |
| I-4 | 線種の数値 | 線種は名前の文字列だけを受け付け、数値（`1` など）は不正とする | 仕様 6.4 の形式（文字列）以外の表記を受け付けないため |
| I-4 | 設定の既定値 | メインウィンドウは (80, 60) に 1000 × 700、属性ウィンドウは表示し (760, 120) に 300 × 520 | 仕様に具体値がない。1366 × 768 の画面にも収まる大きさにした |
| I-4 | `WindowPlacement.IsWithin` | ウィンドウ全体が仮想スクリーンに含まれるときだけ `true`（一部がはみ出すと `false`） | 仕様 5.9 の「含まれる」を、ウィンドウ全体が含まれることと解釈した。仮想スクリーンの矩形は、画面座標（Y 下向き）の `Rect2` で、`Bottom` が上端、`Top` が下端 |
| I-4 | 設定の退避 | 不正な設定ファイルは `settings.json.bak` へ移動する（既存の退避ファイルは置き換える） | 仕様 7 章。移動に失敗したときは、元のファイルを残し、終了時の保存で置き換わる（仮定 A-22） |
| I-5 | 入力中・ドラッグ中に無効なコマンド | `EditorController` の `Undo`・`Redo`・`DeleteSelected`・`SetTool` は何もせずに戻り、`Paste` は何も変えずに `false` を返す | 無効化は ViewModels の `CanExecute` が行う（仕様 5.10）。呼ばれても状態を壊さないようにした |
| I-5 | ハンドルのドラッグ | ハンドルは、つかんだ位置とのずれを保って動かす（カーソルの位置へ飛ばない） | ハンドルの許容範囲（約 6 px）の端をつかんだときに、図形が跳ねないようにするため |
| I-5 | 状態表示の文言 | ツールごとの操作案内を表示し、点を受け付けなかったときや点数が足りないときは、その理由を次のクリックまで表示する（英語） | 仕様 A-12 の操作案内と、仕様 5.2 の「状態表示にその旨を表示する」のため |
| I-5 | `IInteraction` などの補助の型 | `DrawInteraction`、`SelectInteraction`、`AttributeEditor`、`IInteraction` は `Controllers` の内部の型とし、外部へは `EditorController` だけを公開する | ViewModels が使うのは `EditorController` だけのため（設計書 5.3、5.4） |
