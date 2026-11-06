# `Documents/20261105-cad-architecture.md` レビュー

- レビュー日：2026-11-06
- 対象：[Documents/20261105-cad-architecture.md](../Documents/20261105-cad-architecture.md)（状態：レビュー待ち。2026-11-06 の仕様書の変更により差し戻し中）
- 観点：要件（[create-wpf-cad.prompt.md](../.github/prompts/create-wpf-cad.prompt.md)）と仕様書（[20261105-cad-specification.md](../Documents/20261105-cad-specification.md)、2026-11-06 改訂版）との整合、責務の割り当てと依存の向き、七箇条（意図を表現、単一責務、的確な名前、Once And Only Once、ルールの統一、Testable）、正しさ（例外の境界、実行環境）
- 方法：`sustainable-code-jp` スキル（`.github\skills\` と `.claude\skills\` の複製は `diff -r` で一致を確認）の「設計の相談」「レビュー」の基準で評価した。指摘は `<場所>: <臭いの名前>（<根拠>）→ <技法>` の形で示し、七箇条の外の指摘はその性質の名前を添えた。
- 結果：中程度の指摘が 4 件、低程度の指摘が 7 件あります。全体の構成（Core／WPF／Tests の 3 分割、不変 record と置き換えによる Undo、`Outline` を描画・ヒットテスト・出力の共通の入力にする判断、インターフェイスを UI 境界の 2 つに絞る判断）は妥当です。

## 設計の意図（一言で）

UI に依存しない `Core` に図形・編集・永続化の規則を集め、WPF 側は表示と入力の変換と OS 機能（ダイアログ、クリップボード、EMF）だけを受け持つ構成にし、Undo / Redo を不変 record の置き換えで実現する。

## 指摘事項の一覧

| # | 重要度 | 場所（行番号は現行版） | 指摘 | 観点 |
|---|---|---|---|---|
| 1 | 中 | 7、395、454、260・323、456、266、314、408・293、441、445、645–648行 | 仕様書の 2026-11-06 改訂が未反映 | 七箇条の外（仕様との整合） |
| 2 | 中 | 274行（`PointerEvent`）、113行 | `Core` の型が WPF の `ModifierKeys` を使っている | 七箇条の外（実行環境・ビルドの正しさ） |
| 3 | 中 | 182、210行（`ShapeKindInfo`）、204行、DD-12（38行） | 図形の種類ごとの知識と制約の判定が、`Shape` と `ShapeKindInfo` に重複 | Once And Only Once |
| 4 | 中 | 6.4（554行）、8.2（604、606行）、5.6（443行） | 権限なし・読み取り専用の失敗（`UnauthorizedAccessException`）が、捕捉の対象から漏れる | 七箇条の外（例外の境界） |
| 5 | 低 | 166行（`Drawing.Shapes`）、348行（`DrawingViewModel.Shapes`） | 履歴付きの可変コレクションをそのまま公開し、`Drawing` の不変条件を迂回できる | 単一責務 |
| 6 | 低 | 315行（`EditorController.Paste`）、342行 | ペーストのずらし量の累積状態の持ち主と、Copy によるリセットの経路が未定義 | 単一責務 |
| 7 | 低 | 170、286、387、493–494行 | `Committed` イベントが `Drawing` と `EditorController` に二重にあり、購読先の記述が食い違う | Once And Only Once、意図を表現 |
| 8 | 低 | 412、446、458、340行 | 設定の組み立てと保存、Exit コマンドからウィンドウを閉じる経路の責務が未定義 | 単一責務、Testable |
| 9 | 低 | 260行（`ShapeHitTester`）、205行 | 太い線のヒットテストで、線の太さを許容範囲に含めるかが未定義 | Testable |
| 10 | 低 | 34行（DD-8）、204、208行 | 検証メソッドの名前が `Validate()` と `IsValid()` で揺れている | 的確な名前、ルールの統一 |
| 11 | 低 | 70行、231–244行（`Viewport`／`ViewTransform`） | ビュー変換を `Models` に置く判断が、要件 §2.1・§6 の表と異なるのに、理由が設計判断にない | 意図を表現（要件との整合） |

## 指摘の詳細

### 1. 仕様書の 2026-11-06 改訂が未反映（中）

- `<全体>: 仕様との不整合（承認済み仕様の改訂後に未更新）→ 仕様の各変更点を設計要素へ写す`
- 根拠：仕様書は 2026-11-06 に再レビューの指摘を反映して改訂されましたが、本書は改訂前の仕様に基づいています。次の箇所が新しい仕様と食い違います。

| 仕様の変更 | 本書の該当箇所 | 必要な更新 |
|---|---|---|
| 保存確認は Yes / No / Cancel（4.6、A-16） | 395行 `AskSaveChanges(name)`（Save / Don't Save / Cancel） | 戻り値と表記を Yes / No / Cancel に合わせる |
| 出力の上限（長辺 10,000 px への縮小、生成失敗の扱い。5.7、A-24） | DD-6（32行）、`MetafileRenderer`（399行）、8.2（607行） | 縮尺の算出と、生成失敗（メモリー不足等）を例外として `MainViewModel` へ伝える流れを追加 |
| 属性ウィンドウでの `Alt+F4` とショートカット（4.5、A-25） | `MainWindow` の `InputBindings`（453行）、`PropertiesWindow`（454行） | `PropertiesWindow` にも同じコマンドの `InputBindings` を置くか共有する方法と、`Alt+F4` を非表示に変える方法（`WindowClosingBehavior` など） |
| クリック判定の優先順位（ハンドルは最も近いもの。5.3、A-9） | `ShapeHitTester.HitHandle`（230、260行）、`SelectInteraction`（323行） | `HitHandle` が最も近いハンドルを返すことを明記 |
| ドラッグの途切れ（5.3、5.10、A-26） | `DrawingSurface`（456行）、`PointerAction`（275行） | マウスキャプチャを取り、キャプチャの喪失（`LostMouseCapture`）を `Cancel` として渡す |
| 拡大の上限の補正（5.6） | `Viewport.UpdateOverview`（266行） | `MaxScale` を超える場合の補正を追加 |
| ドラッグ中の Copy as Metafile / Image の禁止（5.10） | 314行 | 無効にするコマンドへ追加（入力中は実行可） |
| 独自形式の図形はちょうど 1 個（6.6、7 章） | `ClipboardShapes.FromText`（408行）、`Paste(IReadOnlyList<Shape>)`（293行） | 1 個の検証を追加し、`Paste` の引数を `Shape` 1 個にする |
| バージョン 1 未満は不正（7 章） | 441行 ② | 判定条件に追加 |
| 設定で不正とみなす条件（7 章、A-28） | `SettingsStorage.Load`（445行） | 新しいバージョン、属性の範囲外、幅・高さ 0 以下を不正として退避する |

- あわせて、7行の「前提とする承認済み文書（状態：承認済み）」と、9.4（645–648行）の U-3、U-4「承認により確定する」は、現在の状態と合いません（仕様書はレビュー待ち、U-3・U-4 は 2026-11-05 に確定）。
- 改善案：仕様書の再承認後に、上の表に沿って本書を更新してください。

### 2. `Core` の型が WPF の `ModifierKeys` を使っている（中）

- `274行 PointerEvent: 不適切な関係（UI に依存しない Core の record が、WPF の System.Windows.Input.ModifierKeys を型に持つ）→ Core に独自の列挙を定義し、DrawingSurface で変換する`
- 根拠：`PointerEvent` は `Core`（`net10.0`）の型ですが、`ModifierKeys Modifiers` を持っています。`ModifierKeys` は WPF（`PresentationCore`）の型で、`net10.0` の `Core` からは参照できず、ビルドが通りません。113行は「`Core` は `System.Windows.*` を参照できない」と書いていますが、`Core` で使う `ICommand`（`System.Windows.Input` 名前空間、`System.ObjectModel` アセンブリ）は参照できるため、この記述自体も不正確です。
- 改善案：`Core` に `PointerModifiers`（`[Flags]`、`None`、`Control`、`Shift`、`Alt`）などを定義し、`DrawingSurface` で変換してください。113行は「`Core` は WPF のアセンブリ（`PresentationCore` など）を参照できない」に改めてください。

### 3. 図形の種類ごとの知識と制約の判定が重複（中）

- `182・210行 ShapeKindInfo: 重複したコード／スイッチ文（CanAppend が、Shape.IsValid() と同じ制約（直前の点と一致、矩形の幅・高さ 0、閉じる図形の最後と最初の一致）を種類で分岐して判定する）→ 制約の判定を Shape.IsValid() の一箇所に確定させ、CanAppend は「候補の図形を作って検証する」形で導く`
- 根拠：DD-12 は「種類ごとの違いを `Shape` の多態に閉じ込め、`switch` で分岐しない」としています。しかし `ShapeKindInfo` は、`ShapeKind` を引数にとる静的メソッド（`MinPoints`、`IsFixedTwoPoint`、`Create`、`CanAppend`）で、種類ごとの規則を別に持っています。特に、制約の判定は `IsValid()`（204行）と `CanAppend`（210行）の 2 箇所にあり、仕様 A-27（完全一致での判定）のような変更が来ると、2 箇所を同じに直す必要があります。種類の列挙も `Tool`、`ShapeKind`、`Shape` の派生、JSON の判別子の 4 つが並行しています。
- 影響：入力時と読み込み時で制約の判定が食い違うと、入力できた図形が保存後に読み込めない（またはその逆）という不具合になります。
- 改善案：`CanAppend(kind, points, point)` は、「点を加えた候補（2 点の種類はプレビュー、点列の種類は入力中の点列）を `Create` で作り、点数の不足を除いた制約で `IsValid` を確かめる」形にし、制約の知識を `Shape` 側の一箇所に置いてください。`ShapeKindInfo` に残すのは、入力の規則（最小点数、2 点で確定するか）だけにしてください。`Tool` と `ShapeKind` の対応は、1 つの対応表から導くと、並行する列挙の食い違いを防げます。

### 4. 権限なし・読み取り専用の失敗が捕捉の対象から漏れる（中）

- `6.4（554行）・8.2: 例外の境界の漏れ（Open／Save の失敗を DocumentFormatException と IOException で受けるとしているが、権限なし・読み取り専用のファイルでは UnauthorizedAccessException が発生し、これは IOException の派生ではない）→ 捕捉する例外を列挙して明記する`
- 根拠：仕様 7 章と AC-24 は「読み取り専用、権限なし」の保存失敗でエラーを表示し、元のファイルを壊さないことを求めています。.NET では、これらは `UnauthorizedAccessException` になり、`IOException` だけを捕捉すると、未処理の例外でアプリケーションが停止します。`File.Replace` は、ほかに `PlatformNotSupportedException` などを投げることもあります。
- 改善案：8.2 に、`MainViewModel` が捕捉する例外（`DocumentFormatException`、`IOException`、`UnauthorizedAccessException`。設定の保存でも同じ）を列挙し、テスト計画書で読み取り専用の保存先のケースを自動テストに含めてください。

### 5. 履歴付きの可変コレクションをそのまま公開している（低）

- `166・348行 Drawing.Shapes／DrawingViewModel.Shapes: 不適切な関係（UndoRedoObservableCollection<Shape> を公開し、外部から Add・Undo などを直接呼べる。Drawing の IsModified と Committed を迂回できる）→ 読み取り専用の型（IReadOnlyList<Shape> かつ INotifyCollectionChanged）で公開する`
- 根拠：`Drawing` の責務は「図形の一覧と、その Undo / Redo、未保存状態を保つ」（211行）ですが、`Shapes` を可変のまま公開しているため、この不変条件（変更すれば `IsModified` と `Committed`）を `Drawing` の外から破れます。View がバインドするために必要なのは、変更の通知と読み取りだけです。
- 改善案：`Shapes` を `ReadOnlyObservableCollection<Shape>` などで公開し、変更は `Drawing` の操作だけにしてください。

### 6. ペーストのずらし量の累積状態の持ち主が未定義（低）

- `315行 EditorController.Paste: 責務の所在が不明（「同じ内容を続けて貼ると累積し、新たな Copy でリセット」の状態を誰が持ち、Copy（MainViewModel の CopyCommand）が起きたことをどう知るかが書かれていない）→ 累積の状態を、クリップボードの内容と一緒に持つ一つの持ち主へ割り当てる`
- 根拠：Copy は `MainViewModel` のコマンドで、Paste は `EditorController` の操作です。「同じクリップボードの内容」かどうかの判定（他アプリや別インスタンスでの Copy を含む）と、リセットの契機が、どちらの責務か決まっていません。
- 改善案：例えば、`MainViewModel` が「最後に貼った独自形式のテキストと回数」を持ち、貼るたびに `Paste(shape, offsetCount)` を呼ぶ、のように、状態の持ち主と受け渡しを明記してください（Expert：判定に必要な情報、すなわちクリップボードの内容を扱う側に置く）。

### 7. `Committed` イベントの二重化と、購読先の食い違い（低）

- `170・286行 Committed: 重複したコード／仲介人（Drawing.Committed を EditorController.Committed が中継し、387行は DrawingViewModel が Drawing.Committed を受けるとし、6.1 の図（493–494行）は EditorController 経由とする）→ 購読の経路を一つに決め、中継をやめるか、中継する理由を書く`
- 根拠：同じ名前のイベントが 2 つあり、`DrawingViewModel` がどちらを購読するのかが本文と図で異なります。`EditorController` も `Drawing.Committed` を受けて選択を補正する（316行）ため、`DrawingViewModel` が `Drawing.Committed` を直接受けると、選択の補正より前に `Handles` を再計算する順序の問題が起きえます。
- 改善案：「`Drawing.Committed` は `EditorController` だけが購読し、選択を補正した後に `EditorController.Committed` を発生させる。`DrawingViewModel` は `EditorController.Committed` だけを購読する」のように、順序を含めて一つに決めてください。

### 8. 設定の組み立てと保存、Exit の経路の責務が未定義（低）

- `412・446・458行: 責務の所在が不明（AppSettings を誰が組み立てて SettingsStorage.Save を呼ぶか、PropertiesWindowSettings の定義、ExitCommand から Window を閉じる方法が書かれていない）→ 終了時の流れを一つの持ち主に割り当て、シーケンス図を 1 つ足す`
- 根拠：設定の値は、`MainViewModel.MainPlacement`、属性ウィンドウの配置（`WindowPlacementBehavior`）、表示状態、`EditorController.DefaultAttributes` に分散しています。`Save` は「終了時にだけ呼ぶ」とありますが、呼び出し元がありません。`AppSettings.PropertiesWindow` の型 `PropertiesWindowSettings` は図に定義がありません。ExitCommand（340行）は ViewModel のため、`Window` を閉じる手段（閉じる要求のイベントやビヘイビアなど）が必要ですが、記載がありません。
- 改善案：「`MainViewModel` が `TryClose()` の成功後に `AppSettings` を組み立てて保存する。ExitCommand は閉じる要求を出し、`WindowClosingBehavior` がウィンドウを閉じる」のように、終了の流れを 6 章に追加し、`PropertiesWindowSettings` を定義してください。`MainViewModel` 単体のテストで、保存される設定を確かめられるようになります（Testable）。

### 9. 太い線のヒットテストで、線の太さを含めるかが未定義（低）

- `260行 ShapeHitTester: 前提条件の不足（判定は中心線からの距離と画面上 約 5 px の許容範囲だけで、線の太さ（最大 50 論理単位）を考慮しない）→ 判定の式を明記する`
- 根拠：拡大表示で太い線を描いたとき、目に見える線の上（中心線から太さの半分の位置）をクリックしても、許容範囲（約 5 px）より遠ければ選択されません。仕様の「線上のクリックで選択」をユーザーの見た目どおりに満たすには、「中心線からの距離 ≤ 太さ ÷ 2 + 許容範囲」とするのが自然です。
- 改善案：どちらの式で判定するかを決めて明記し、テスト計画書のヒットテストのケースに、太い線の境界を加えてください。

### 10. 検証メソッドの名前の揺れ（低）

- `34行（DD-8）・204・208行: 不適切な名前（同じ「制約を満たすか」の検証が、DD-8 では Validate()、Shape では IsValid()、ShapeAttributes では Validate() と呼ばれ、戻り値の型も書かれていない）→ 名前の変更で一つに揃える`
- 改善案：例外を投げない真偽の判定であれば `IsValid()` に統一し、DD-8 の記述も合わせてください（ルールの統一）。

### 11. ビュー変換を `Models` に置く判断の理由がない（低）

- `70行 Models.Geometry: 意図の欠落（要件 §2.1 は ViewModels の役割に「ビュー変換など」を挙げ、§6 は「拡大率とスクロール位置はビュー変換（ViewModel）が保持」とするが、本書は ViewTransform と Viewport を Models.Geometry に置いている）→ 設計判断（DD）として理由を記録する`
- 根拠：`Viewport` を UI に依存しない純粋な型として `Models` に置き、`DrawingViewModel` が保持する構成は、テスト容易性の点で妥当です。ただし、要件の表と配置が異なり、その理由が書かれていないため、レビューや実装で要件違反と読まれるおそれがあります。
- 改善案：「状態の保持は `DrawingViewModel`（要件 §6）、規則の計算は純粋な型として `Models.Geometry`（`Controllers` も座標変換に使うため）」という DD を追加してください。

## 確認した事項（問題なし）

- 要件 F-01〜F-19 と仕様の仮定 A-17〜A-23 は、7 章の対応表で設計要素に対応付けられています（2026-11-06 に追加された A-24〜A-28 は指摘 1 のとおり未対応）。
- 依存の向き（`Views` → `ViewModels` → `Controllers` → `Models`、`Infrastructure` → `Services`）は一方向で、逆向きの依存はありません。`Core` を `net10.0` にして WPF への依存をコンパイル時に防ぐ構成（DD-1）は、規約に頼らずに要件を強制でき、妥当です（指摘 2 の型を除く）。
- インターフェイスを、テストで UI を起動できない 2 つの境界（`IDialogService`、`IClipboardService`）に限り、ファイル I/O をパスを受け取る具象クラスにする判断（DD-13）は、実装が 1 つのインターフェイスを増やさない点で妥当です。
- 不変 record の置き換えと `DisabledUndoScope` によるドラッグ 1 回 = Undo 1 回（DD-2、DD-3）は、ライブラリの記録の仕組みだけで成立し、R-7 で添字の整合のリスクとテストも示されています。
- `Outline` を、画面の描画・ヒットテスト・EMF／画像の出力の共通の入力にする判断（DD-6、DD-7）は、見た目の食い違いを防ぎ、`SplineBezier` を純粋な関数として検証できます。
- コードビハインドを `InitializeComponent()` だけにする方針と、`App` の配置の例外（DD-14）は、リポジトリ指示と一致しています。

## 推奨する対応

1. 仕様書の再承認後に、指摘 1 の表に沿って本書を更新する。
2. 指摘 2〜4 は実装の不具合やビルドエラーに直結するため、同じ更新で対応する。
3. 指摘 5〜11 は同時に対応するか、実装計画書で扱う事項として記録するかを選ぶ。
4. 更新後も、ユーザーの承認までは `状態：レビュー待ち` のままとする。
