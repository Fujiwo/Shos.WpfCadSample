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
│   │   ├─ Behaviors\           … Shos.WpfCadSample.Views.Behaviors
│   │   └─ Converters\          … Shos.WpfCadSample.Views.Converters
│   ├─ Themes\                  … Generic.xaml と DrawingSurface.xaml（カスタムコントロールの既定のスタイル）
│   └─ Infrastructure\          … Shos.WpfCadSample.Infrastructure
└─ Shos.WpfCadSample.Tests\     … net10.0。xUnit。Core を参照
    ├─ Architecture\            … Shos.WpfCadSample.Tests.Architecture
    ├─ Controllers\             … Shos.WpfCadSample.Tests.Controllers
    ├─ Models\                  … Shos.WpfCadSample.Tests.Models
    │   └─ Geometry\            … Shos.WpfCadSample.Tests.Models.Geometry
    ├─ Serialization\           … Shos.WpfCadSample.Tests.Serialization
    ├─ Settings\                … Shos.WpfCadSample.Tests.Settings
    ├─ ViewModels\              … Shos.WpfCadSample.Tests.ViewModels
    └─ Support\                 … Shos.WpfCadSample.Tests.Support（一時フォルダー、偽の IDialogService・IClipboardService）
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
| I-6 | ViewModels の組み立て | `MainViewModel` が、`Drawing`・`Viewport`・`EditorController`・`DrawingViewModel`・`PropertiesViewModel` を作る。外から受け取るのは、`IDialogService`・`IClipboardService`・`DocumentStorage`・`SettingsStorage` だけ | 1 つのドキュメントの部品の組を 1 か所で作り、I-7 の `CompositionRoot` は UI に依存するサービスとパスだけを渡せばよいようにするため |
| I-6 | `MainViewModel.LoadSettings()` | 起動時に呼ぶ公開メソッドとした（設計書 5.4 は `SaveSettings()` だけを挙げている）。読み込み自体の失敗（他のアプリが排他で開いているなど）も、警告と既定値で起動を続ける | 実装計画書 I-6 の「設定の読み込み」を、表示（警告）を伴うためコンストラクターではなく明示的な操作にした。I-4 からの申し送りの対応 |
| I-6 | `AttributeEditModel` の太さ | 太さの入力は文字列の `ThicknessText` とした（設計書 5.4 の図では `double Thickness`） | 数値でない入力を ViewModel で受けて「反映せず、直前の有効値へ戻す」（仕様 5.4）ため。有効な値は `Attributes.Thickness` で得る |
| I-6 | `DrawingViewModel` のコマンドの引数 | `SetViewSizeCommand` は `ViewSize`、`ZoomCommand` は `ZoomRequest`（カーソル位置とノッチ数）、`ScrollCommand` は `ScrollDirection` を受け取る。描画領域の外へ出たことを伝える `PointerLeaveCommand` を加えた | Core が WPF の型（`Size` など）に依存しないため。カーソル位置の表示を、描画領域の外で空欄にするため（仕様 4.4） |
| I-6 | 貼り付けの回数 | 貼り付けに成功したときだけ、回数と前回のテキストを更新する | 失敗した貼り付けを回数に含めると、次の貼り付けのずらし量が、実際に貼った回数とずれるため |
| I-6 | エラー・警告の文言 | 仕様 7 章の文言に、原因と対象のファイルを続ける（例：`The file is not a valid CAD document.` の後に理由と `File: <パス>`） | 仕様 4.6「操作名、原因、対象ファイルを示す英語の文」のため。タイトルは操作名（`Open`、`Save`、`Paste` など） |
| I-7 | 独自形式のクリップボードのデータ | 書式名 `Shos.WpfCadSample.Shapes` に、JSON テキストを UTF-8 のバイト列（`MemoryStream`）として置く | 文字列を独自の書式で渡すときの、型の直列化に頼らないため |
| I-7 | EMF・画像の座標の変換 | 論理座標から出力のピクセル座標への変換（Y の反転、範囲への平行移動、縮尺）を倍精度で計算してから GDI+ に渡し、GDI+ の変換行列は使わない。線の太さも同じ縮尺にする | 座標が大きいとき（±1,000,000）に、GDI+ の単精度の変換行列で生じる誤差を避けるため |
| I-7 | Win32 の呼び出し | `NativeClipboard` は `DllImport` を使う | `LibraryImport` は unsafe のコンパイル（`AllowUnsafeBlocks`）を要するため、プロジェクトの設定を変えずに済む方を選んだ |
| I-7 | ダイアログの親 | アクティブなウィンドウ（属性ウィンドウを含む）を親にし、なければメインウィンドウ。エラーのタイトルは `<操作名> - WPF CAD Sample` | 属性ウィンドウのショートカットから実行した操作でも、ダイアログがそのウィンドウの前に出るようにするため |
| I-8 | 論理領域の枠 | `DrawingViewModel.LogicalArea`（`Viewport.LogicalArea` をそのまま返す）を追加し、`Rect2ToRectConverter` で `RectangleGeometry` にする。ビュー変換は `Path` ではなくジオメトリに掛け、線の太さは 1 px | 論理領域の大きさを XAML に重複して書かないため。変換を `Path` に掛けると線も拡大されるため、拡大率に左右されない薄い線（仕様 4.3）にするには、ジオメトリに掛ける必要がある |
| I-8 | Material Design 3 のスタイル（R-3） | `MaterialDesign3.Defaults.xaml` の暗黙のスタイル（メニュー、ツールバー、ステータスバー、ボタン）に加えて、ウィンドウに `MaterialDesignWindow`、ツールボタンに `MaterialDesignToolRadioButton` を明示する。描画領域の自作の部分は、テーマのブラシ（枠は `MaterialDesign.Brush.Separator.Background`、ハンドルは `MaterialDesign.Brush.Primary` の線と `MaterialDesign.Brush.Background` の塗り）を `DynamicResource` で参照する | 5.4.0 のパッケージ（`MaterialDesignThemes.Wpf.dll`）に含まれるキーを、2026-11-07 に確認した。ツールボタンは、選択状態を Material Design の見た目で示すため（仕様 4.2） |
| I-8 | ツールバーのアイコン | `PackIcon` の `CursorDefault`（Select）、`VectorLine`、`CircleOutline`、`RectangleOutline`、`VectorPolyline`、`VectorPolygon`、`VectorCurve`（Open Curve）、`VectorEllipse`（Closed Curve）、`Undo`、`Redo` | 図形の形が分かるもの。5.4.0 の `PackIconKind` にあることを確認した |
| I-8 | 起動時の組み立て | `CompositionRoot` は、コンストラクターで `MainViewModel` を作り、`LoadSettings()` を呼ぶ。設定のパスは `%APPDATA%\Shos.WpfCadSample\settings.json` | `App.xaml` のリソースとして作られる時点（`MainWindow` が `DataContext` を得る前）に、設定を反映しておくため |
| I-8 | ショートカットの表示 | メニューの項目に、ショートカットを `InputGestureText` で表示する。キーの割り当て（`InputBindings`）は I-9 で行う | 実装計画書の分担（キー操作は I-9）に従うため |
| I-9 | `Themes` の場所 | `Themes\Generic.xaml` と `Themes\DrawingSurface.xaml` は、`Views\` の下ではなく、プロジェクトの直下に置く | WPF がカスタムコントロールの既定のスタイルを探す場所が、アセンブリの `themes/generic.xaml` に決まっているため（実装計画書 I-9 の成果物の `Themes` を、この場所に置いた） |
| I-9 | 描画領域の大きさの通知 | `DrawingSurface` は、読み取り専用の依存関係プロパティ `ViewSize` に加えて、大きさが変わるたびに `ViewSizeCommand`（`DrawingViewModel.SetViewSizeCommand` にバインド）を実行する | `DrawingViewModel` は大きさをコマンドで受け取るため（設計書 5.4）。マウスの入力（`PointerCommand`）と同じ形にそろえた |
| I-9 | マウスの入力 | 左ボタンだけを扱う。ホイールのノッチ数は、移動量 ÷ 120 を 0 に切り捨てずに最低 1 とする | 仕様の操作は左ボタンだけで、右ボタンの機能は要件にないため。細かい移動量を送るタッチパッドでも、1 回の操作で拡大縮小するため |
| I-9 | ショートカットの共有 | ショートカットを、`App.xaml` の `ShortcutCollection`（キー、修飾キー、コマンドへのパス）としてデータで定義し、添付プロパティ `Shortcuts.Source` が、ウィンドウごとに `KeyBinding` を作る。`Esc` と `Ctrl+矢印` は、メインウィンドウだけに直接定義する | `KeyBinding` は 1 つの要素にしか属せず、リソースとして 2 つのウィンドウで共有できないため。`Esc` と `Ctrl+矢印` は仕様 4.2 の表になく、属性ウィンドウでは働かない |
| I-9 | Paste の有効状態の更新の契機 | 添付プロパティ `CommandEvents.Activated`（ウィンドウのアクティブ化）と `CommandEvents.SubmenuOpened`（Edit メニューを開いたとき）で、`RefreshClipboardStateCommand` を実行する | 設計書 5.4 の契機（R-5）を、コードビハインドを使わずに、イベントからコマンドへつなぐため |
| I-9 | 属性ウィンドウの生成と所有者 | 属性ウィンドウは、メインウィンドウの添付プロパティ `WindowVisibilityBehavior.OwnedWindow` の値として XAML で作り、メインウィンドウの表示後に所有者にする。アプリケーションは `ShutdownMode="OnMainWindowClose"` | メインウィンドウの前面に表示するため（仕様 4.5）。非表示の属性ウィンドウが残っていても、メインウィンドウを閉じたらアプリケーションを終了させるため |
| I-9 | ウィンドウの配置の書き戻し | 表示の直前に 1 回だけ配置を適用し、その後は、位置・サイズ・状態が変わるたびに `RestoreBounds` で `MainPlacement` / `PropertiesPlacement` へ書き戻す（最小化中は書き戻さない） | 閉じるときの `TryClose()`（設定の保存）より前に、配置が確実に ViewModel にある状態にするため。`Closing` のハンドラーの順序に頼らずに済む |
| I-9 | 属性ウィンドウの編集部品 | 「Selected Shape」と「Defaults for New Shapes」は、同じ `DataTemplate` を使う。選択がないときは、区画を無効にして値の部品を隠す（空欄）。不正な入力は、テキストボックスを `MaterialDesign.Brush.ValidationError` の色で示す。線種の `DashDot` は `Dash-Dot` と表示する | 同じ編集部品を二重に書かないため。仕様 4.5 の「空欄で無効」と、仕様 5.4 の「無効状態の見た目」、仕様 4.5 の線種の表記に合わせた |
| I-9 | カーソルの形 | `PointerCursor.Move` は `Cursors.SizeAll`、`Cross` は `Cursors.Cross`、`Arrow` は `Cursors.Arrow` | 仕様 4.3 の「移動を示す形状」に当たる、標準のカーソル |
