# ソースコード全体レビュー

- 実施日：2026-11-07
- 対象：`Source\Shos.WpfCadSample` の全体（`Shos.WpfCadSample.Core`、WPF プロジェクト `Shos.WpfCadSample`、`Shos.WpfCadSample.Tests`）。コミット `195dc77` の時点。
- 観点：sustainable-code-jp の七箇条とコードの臭い。加えて、七箇条の外として、正しさ（例外の境界、状態遷移、後始末）と、仕様書・設計書との整合を見た。
  - 参照した文書：[仕様書](../Documents/20261105-cad-specification.md)、[アーキテクチャー／クラス設計書](../Documents/20261105-cad-architecture.md)。
  - 設計書の設計判断（DD-x）と、[Source/README.md](../Source/README.md) の「設計判断」の表で意図して選んだことは、その理由を確かめたうえで判断した。
- 方法：
  - 領域ごと（Models／Controllers・Serialization・Services・Settings／ViewModels／WPF プロジェクト）に分けて読んだ。
  - 各指摘はコードで確かめた。指摘 1 と 4 は、一時的なテストを書いて Windows の `dotnet.exe test` で再現を確かめた。一時的なテストは削除済みで、リポジトリには残っていない。
  - 実機（Windows のクリップボード）でしか確かめられない点は、「要確認」と書いた。
- 既知の指摘：[20261107-source-code-review.md](./20261107-source-code-review.md)（固定 2 点図形の終点のダブルクリック）は修正済みのため除いた。
- 対応状況：指摘 1、2、4 を修正済み（2026-11-07）。それぞれ、再現するテストを先に足して失敗を確かめてから直した。`dotnet build Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` 成功（警告 0、エラー 0）。`dotnet test Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` 成功（675 件）。ほかの指摘は未対応。

## 指摘の一覧

| # | 重要度 | 場所 | 性質 | 確認の方法 |
|---|---|---|---|---|
| 1 | 高 | `Core/Serialization/ShapesJson.cs` | 七箇条の外：正しさ（検証の漏れ） | テストで再現 |
| 2 | 中 | `Core/ViewModels/MainViewModel.cs` | 七箇条の外：正しさ（例外の境界） | コードで確認（実機での頻度は要確認） |
| 3 | 中 | `Core/ViewModels/*.cs` ほか 5 か所 | Once And Only Once（重複したコード） | コードで確認 |
| 4 | 低 | `Core/Controllers/DrawInteraction.cs` | 七箇条の外：正しさ（状態遷移の漏れ） | テストで再現 |
| 5 | 低 | `Core/ViewModels/MainViewModel.cs` | 七箇条の外：正しさ（状態の一貫性） | コードで確認 |
| 6 | 低 | `Core/Controllers/SelectInteraction.cs`、`AttributeEditor.cs` | Once And Only Once（変更の分散） | コードで確認 |
| 7 | 低 | `Tests/ViewModels/` | Testable | コードで確認 |
| 8 | 低 | `Documents/20261105-cad-architecture.md` 5.4 | 七箇条の外：設計との整合 | 文書とコードで確認 |
| 9 | 低 | `Infrastructure/NativeClipboard.cs` | 七箇条の外：正しさ（実行環境の差）、ルールの統一 | コードで確認（実機での頻度は要確認） |
| 10 | 低 | `Infrastructure/NativeClipboard.cs` | 七箇条の外：仕様との整合 | コードと仕様で確認 |
| 11 | 低 | `Core/Models/Geometry/Outline.cs` | Testable（前提条件が明示されていない） | コードで確認 |
| 12 | 低 | `Core/Models/LineStyleExtensions.cs` | ルールの統一 | コードで確認 |

## 指摘

### 1. 高：`shapes` に `null` の要素があると、Open と Paste でアプリが終了する

- `Shos.WpfCadSample.Core/Serialization/ShapesJson.cs:27`：検証の漏れ（`null` の要素が逆シリアル化を通り抜け、`shape.IsValid()` で `NullReferenceException` になる）→ `Read` で `null` の要素を `DocumentFormatException`（`Invalid`）として拒否する。T-SER-07 と T-SER-17 に `[null]` のケースを足す。
- 七箇条の外：正しさ、仕様 7 章との整合。
- 根拠：
  - `JsonRules` の `RespectNullableAnnotations` が見るのは、プロパティとコンストラクターの引数だけである。コレクションの要素は見ない。
  - 一時的なテストで、`{"format":"Shos.WpfCadSample.Document","version":1,"shapes":[null]}` を `DocumentSerializer.Deserialize` に渡した。結果は `System.NullReferenceException` だった。
  - `MainViewModel.Open`（`MainViewModel.cs:168-187`）と `Paste`（`:226-252`）が捕捉するのは、`DocumentFormatException` と I/O の例外だけである。`DispatcherUnhandledException` の処理もないため、未処理例外でアプリが終了する。
  - 仕様 7 章は、不正なファイルや貼り付けのデータに対して「エラーダイアログを表示し、ドキュメントは変更しない」と定める。手で編集したファイルや、他のアプリがクリップボードに置いたデータで起こりうる。
  - 既存のテスト（`DocumentSerializerTests`、`ClipboardShapesTests`）は、`"shapes": null` を確かめているが、`[null]` は確かめていない。

### 2. 中：クリップボードを調べるときの失敗を、誰も受け止めない

- `Shos.WpfCadSample.Core/ViewModels/MainViewModel.cs:271-275`（`RefreshClipboardState`）：例外の境界の漏れ（`clipboard.ContainsShapes` が投げる `ClipboardOperationException` を捕捉していない）→ `RefreshClipboardState` で `ClipboardOperationException` を捕捉し、Paste を無効として扱う。`FakeClipboardService` で `ContainsShapes` の失敗を再現できるようにし、テストを足す。
- 七箇条の外：正しさ（例外の境界）。テストで失敗を再現できない点は、Testable にも当たる。
- 根拠：
  - `ClipboardService.ContainsShapes`（`Infrastructure/ClipboardService.cs:23`）は、`Clipboard.ContainsData` の `ExternalException` を `ClipboardOperationException` に変えて投げる。`IClipboardService` の契約も、「失敗はすべて `ClipboardOperationException`」と定めている。
  - ほかのクリップボードの操作（Copy、Paste、Copy as Metafile、Copy as Image）は、`TryClipboard` か `catch` で受けている。`RefreshClipboardState` だけが受けていない。
  - このコマンドは、ウィンドウのアクティブ化と Edit メニューを開いたときに、`CommandEvents` から呼ばれる。そのため、他のアプリがクリップボードを開いたままのときにウィンドウを切り替えるだけで、未処理例外でアプリが終了しうる。
  - 要確認：WPF の `Clipboard` は内部で再試行してから例外を出すため、実機で起きる頻度は低い。

### 3. 中：「選択中の図形を得る」処理が 5 か所にある

- 次の 5 か所：重複したコード（`SelectedId is { } id ? drawing.IndexOf(id) : -1` と `Shapes[index]` を、それぞれ書いている）→ `EditorController` に `Shape? SelectedShape`（または添字）を公開し、ViewModels はそれを使う。Controllers の内部の 2 か所も、1 つの補助にそろえる。
  - `Core/ViewModels/MainViewModel.cs:285-289`
  - `Core/ViewModels/DrawingViewModel.cs:199`
  - `Core/ViewModels/PropertiesViewModel.cs:74`
  - `Core/Controllers/EditorController.cs:226`
  - `Core/Controllers/SelectInteraction.cs:72`
- 七箇条：Once And Only Once（変更の分散）。
- 根拠：
  - 「選択は ID で持ち、添字はその都度探す」という規則（設計書 DD-11）が、Controller の外の 3 つの ViewModel に漏れている。
  - 選択した図形が消えたときの扱いなど、この規則を変えるときは 5 か所を直す必要がある。
  - `EditorController` に公開メンバーを足すと、設計書 5.3 のクラス図が変わる。そのため、設計書の改訂と承認が要る。

### 4. 低：複数点の図形を確定した直後の 3 回目のクリックで、誤った「点数不足」のメッセージが出る

- `Shos.WpfCadSample.Core/Controllers/DrawInteraction.cs:24-28`：状態遷移の漏れ（`ClickCount >= 2` の Down を、入力中でないときも `Confirm()` に回している）→ 入力中でない（`!IsActive`）ときの `ClickCount >= 2` の Down は無視する。この条件は、固定 2 点図形の分岐（前回のレビューの修正）も含むため、2 つの分岐を 1 つにまとめられる。`clickCount: 3` の回帰テストを足す。
- 七箇条の外：正しさ（状態表示が実際の操作を表さない）。
- 根拠：
  - 一時的なテストで、Polyline を (10, 20)、(110, 70) とクリックし、(110, 70) でダブルクリックして確定した。続けて `ClickCount = 3` の Down を送った。
  - 結果は、図形が 1 個追加されたうえで、状態表示が `A polyline needs at least 2 points. The input was canceled.` になった。
  - WPF は、素早い 3 回目のクリックを `ClickCount = 3` として通知する。`DrawingSurface` は、それをそのまま渡す。
  - 前回のレビューで直した固定 2 点図形の問題と、同じ種類の漏れである。

### 5. 低：確定していない属性の編集が、そのまま保存される

- `Shos.WpfCadSample.Core/ViewModels/MainViewModel.cs:190`（`Save`）、`:329-341`（`SaveDocumentTo`）：状態の一貫性の問題（Save は、属性の編集を確定も取り消しもせずに、`document.Shapes` を保存する）→ 保存の前に `editor.CommitAttributes()` で確定する。New・Open・終了が `CancelInput` で取り消すのと対になる。
- 七箇条の外：正しさ（状態の一貫性）。
- 根拠：
  - 属性ウィンドウの 16 進の欄は `UpdateSourceTrigger=PropertyChanged` で、有効な値を打った時点で `PreviewReplace`（記録しない置き換え）が走る（`AttributeEditModel.cs:50-58` → `EditorController.PreviewAttributes`）。
  - 属性ウィンドウでもショートカットは働く（仕様 A-25）。そこで `Ctrl+S` を押すと、確定していない色が保存され、`MarkSaved` でタイトルの `*` が消える。
  - その後に `Enter` で確定すると、`Replace` が記録される。その結果、内容がファイルと同じなのに「変更あり」になる。
  - 確定せずに終了すると、`TryClose` → `CancelInput` で、メモリー上は元の色に戻る。しかし `IsModified` は偽のままなので、保存確認なしに終わる。ファイルには、確定していない色が残る。
  - メニューをマウスで選んだ場合は、`EditCommit` がフォーカスの移動で先に確定するため、起きない。

### 6. 低：「元へ戻してから 1 回だけ記録する」手順が、2 か所に書かれている

- `Shos.WpfCadSample.Core/Controllers/SelectInteraction.cs:93-101`（`Finish`）、`AttributeEditor.cs:29-39`（`Commit`）：重複したコード、変更の分散（`RestoreOriginal` → `Replace` の順序は `Drawing` の記録の規則だが、その順序を呼び出し側がそれぞれ守っている）→ `Drawing` に、確定の操作を 1 つのメソッド（例：`CommitPreview(index, original, final)`）として置き、呼び出し側は「確定する」とだけ書く。
- 七箇条：Once And Only Once。
- 根拠：
  - 「ドラッグやスライダーの操作 1 回 = Undo 1 回」は、設計書 DD-3 と R-7 で `Drawing` の規則として定めたものである。
  - プレビューを使う編集が 3 つ目にできたら、同じ順序を 3 か所目に書くことになる。順序を誤ると、Undo の単位が壊れる。
  - 重要度を低とした理由：設計書（DD-3、5.1）は、この 3 つのメソッドを `Drawing` の公開 API として定めている。変えるには、設計書の改訂が要る。

### 7. 低：コマンドの有効状態やタイトルの通知が漏れても、テストで気づけない

- `Shos.WpfCadSample.Tests/ViewModels/MainViewModelTests.cs`（例：72 行の `CanExecute` の補助）ほか：Testable の弱さ（有効状態は `CanExecute(null)` を直接呼んで確かめていて、`CanExecuteChanged` や `PropertyChanged(Title)` が発生したかは確かめていない）→ 状態を変える代表的な操作に、通知が発生したことのアサーションを足す。例：Copy の後の `PasteCommand.CanExecuteChanged`、Save の後の `Title` の `PropertyChanged`。
- 七箇条：Testable。
- 根拠：
  - WPF の画面は、`CanExecute` を自分からは問い合わせ直さない。通知を受けたときだけ問い合わせる。
  - そのため、`MainViewModel.Copy` の `RefreshCommands()` や、`SaveDocumentTo` の `OnPropertyChanged(nameof(Title))` を消しても、テストはすべて通る。しかし、実際の画面では、Paste のボタンやタイトルが更新されなくなる。
  - `Tests/ViewModels/` で通知を購読しているのは、`PropertiesViewModelTests.cs:125` の 1 か所だけである。

### 8. 低：設計書 5.4 の記述が、設計書 5.3 とコードの両方と食い違っている

- `Documents/20261105-cad-architecture.md:417`：設計書の中で記述が食い違っている。この行は「表示とコントローラーの状態（入力、選択、拡大縮小）には、`DrawingViewModel` の操作を通してだけ触れる」としている。しかし、`MainViewModel` は `EditorController` を直接持ち、選択と状態を読んでいる（`MainViewModel.cs:35, 59, 199-205, 281-288`）→ 設計書のほうを実態に合わせる。5.4 のクラス図に `MainViewModel --> EditorController` を加え、417 行の記述を「Undo / Redo / Delete / Paste の実行と、コマンドの有効状態の判定には、`EditorController` を直接使う」と改める。
- 七箇条の外：設計との整合。
- 根拠：
  - 設計書 5.4 の 419 行は「`EditorController.Paste(shape, 回数)` を呼ぶ」と書いている。設計書 5.3 も、ViewModels が `IsDragging` を `CanExecute` に使うと書いている。
  - したがって、ずれているのは 417 行の記述とクラス図で、コードではない。
  - 読み手が、依存の向きを誤解するおそれがある。

### 9. 低：Copy as Metafile だけ、クリップボードを開く再試行がない

- `Shos.WpfCadSample/Infrastructure/NativeClipboard.cs:19-22`：実行環境の差（`OpenClipboard` を 1 回だけ試し、失敗するとすぐに例外にする）→ WPF の `Clipboard` と同じく、短い間隔で数回再試行してから `Win32Exception` にする。
- 七箇条の外：正しさ（実行環境の差）。同じ種類の操作で振る舞いがそろっていない点は、ルールの統一にも当たる。
- 根拠：
  - 独自形式の Copy と Copy as Image は WPF の `Clipboard` を使い、内部で再試行する。
  - そのため、クリップボードの履歴やリモートデスクトップなど、他のプロセスが一瞬クリップボードを開いているとき、Copy as Metafile だけが失敗しやすい。
  - 要確認：実機での発生の頻度。

### 10. 低：EMF の書き込みに失敗すると、クリップボードが空になる場合がある

- `Shos.WpfCadSample/Infrastructure/NativeClipboard.cs:24-28`：仕様との不一致（`EmptyClipboard` が成功した後に `SetClipboardData` が失敗すると、元の内容は消えたままになる）→ コードはこのままとし、この稀な失敗の扱いを、仕様 7 章の例外（仮定）として記録する。
- 七箇条の外：仕様との整合。
- 根拠：
  - 仕様 7 章（仕様書 488 行）は、クリップボードへの書き込みの失敗について「ドキュメントとクリップボードの内容は変更しない」と定める。
  - Win32 のクリップボードの API では、空にした後に元の内容を戻す手段がない。そのため、コードを直すより、仕様に例外として書く方が現実的である。
  - 発生はきわめて稀（`SetClipboardData` の失敗はメモリー不足などに限られる）。

### 11. 低：`Outline.DistanceTo` の前提条件（区間が 1 つ以上）が書かれていない

- `Shos.WpfCadSample.Core/Models/Geometry/Outline.cs:28-29`：前提条件が明示されていない（`Flatten(tolerance).Min(...)` は、区間がないと LINQ の `InvalidOperationException` を投げる）→ summary に前提条件を書くか、ガード節で明示的な例外を投げる。
- 七箇条：Testable（前提条件の明示）。同じ層の `Rect2.Bounding`（`Rect2.cs:24-31`）は、前提条件を summary に書いて明示的な例外を投げているので、ルールの統一にも当たる。
- 根拠：
  - 1 点だけの `PointListShape` の `PolylineOutline()`（`PointListShape.cs:45-46`）は、区間のない `Outline` を作る。
  - ただし、現在の呼び出し元（`ShapeHitTester` は有効な図形だけ、`CanAppend` とプレビューは 2 点以上）からは到達しない。そのため、いま起きる不具合ではない。

### 12. 低：`LineStyleExtensions` の summary は「拡張メンバー」だが、従来の拡張メソッドである

- `Shos.WpfCadSample.Core/Models/LineStyleExtensions.cs:3-6, 18`：記述とコードの不一致（summary は「拡張メンバー」と書いているが、実装は `this` 引数の従来の拡張メソッド）→ C# 14 の `extension(LineStyle style) { ... }` ブロックに書き換えるか、summary を「拡張メソッド」に直す。
- 七箇条：ルールの統一。
- 根拠：
  - リポジトリの規約（`.github/copilot-instructions.md` の CAD conventions）は、拡張メンバーを含む新しい C# の構文を積極的に使うよう求めている。しかし、Core に `extension(` の使用はない。
  - 振る舞いへの影響はない。

## 指摘にしなかった点（確かめた結果、問題なし）

- **`ShapeKindInfo` の種類ごとの `switch`**（`FromTool`、`MinPoints`、`IsFixedTwoPoint`、`Create`）：形の上では「スイッチ文」の臭いである。ただし、1 つのクラスに集まっていて、どの `switch` も既定の分岐で例外を投げる。図形の種類は仕様で 7 種に固定されている。振る舞いの違いは `Shape` の多態に置いており（DD-12）、ここにあるのは図形の生成と入力の規則だけである。このため、直す必要はないと判断した。
- **`MainViewModel` の大きさ**（約 370 行）：ファイル操作、クリップボード、設定、ウィンドウの状態を担う。ただし、設計書 5.4 の責務の割り当てどおりで、各メソッドも短い。現時点では「巨大なクラス」として指摘しない。クリップボードの処理が増えたら、貼り付けの回数の管理などの切り出しを検討する。
- **数値計算の境界**：次の点を読んで確かめ、いずれも正しかった。
  - `CubicBezierSegment` の 2 次方程式の根：a = 0、b = 0、q = 0 のそれぞれの場合。
  - 長さ 0 の弦に対する平坦さの判定。
  - `SplineBezier` の n = 3 の巡回三重対角。
  - `ExportFrame` のループが終わることと、最小 1 px。
  - `Viewport` の、全体図が縮んだときの再クランプ。
  - `ShapeAttributes.IsValid` が太さ NaN を不正とすること。
- **GDI+ と Win32 の資源**：
  - `Metafile`、`Graphics`、`Bitmap`、`Pen`、`GraphicsPath` は `using` で解放している。
  - `GetHdc` と `ReleaseHdc` は、`finally` で対になっている。
  - `HENHMETAFILE` は、成功したときはクリップボードに所有が移り、失敗したときは削除している。
  - `CloseClipboard` は `finally` で呼んでいる。
- **WPF の規約**：
  - コードビハインドは、`InitializeComponent()` だけである。
  - Material Design への依存は、`App.xaml` と `Views` の XAML に閉じている。
  - 添付プロパティのイベント購読は 1 回だけで、対象はアプリの生存期間と同じなので、リークはない。
  - コンバーターは、型が違えば null を返し、作ったブラシやジオメトリを凍結している。

## 良かった点

- **責務の分け方**：
  - Core と WPF をプロジェクトで分け、その分離を構造のテスト（T-ARC）で確かめている。
  - 入力の解釈を、ツールごとに `IInteraction` の実装へ分けている。そのため、各クラスの仕事を「ひとこと」で言える。
  - `DrawingSurface` は入力の変換だけを担い、判断は Core に任せている。
- **規則の置き場所が 1 か所**：
  - 図形の制約は `Shape.SatisfiesConstraints`、座標の範囲は `Point2.IsWithinLimits` にある。
  - JSON の規則は `JsonRules` にある。
  - 描画・判定・出力は、すべて `Outline` から作る。
- **失敗の扱い**：
  - `SafeFile` は、一時ファイルに書いてから置き換える。後始末も確実で、後始末の失敗が保存の失敗を隠さない。
  - 捕捉する例外を 4 種に限り、ほかの例外を握りつぶしていない。指摘 1 と 2 は、この方針の中の漏れである。
- **Why のコメント**：数値計算の工夫（桁落ちを避ける根の公式、分割の打ち切り、Sherman-Morrison の公式）や、WPF の制約への対処に、理由が書かれている。

## 対応の提案

- **指摘 1、2、4**：仕様が決まっている不具合で、直し方も明らかである。それぞれ再現するテストを先に足し（Red）、直して Green にする。小さな修正として進められる。
- **指摘 5**：保存の前に確定するか、取り消すかを決める必要がある。New・Open・終了は取り消しているので、仕様の解釈を確認してから直す。
- **指摘 3、6、8**：`EditorController` と `Drawing` の公開 API、または設計書の記述が変わる。設計書を改訂し、承認を得てから直す。
- **指摘 7、9〜12**：低い優先度で、上記の対応のついでに扱える。指摘 10 は、仕様書の改訂（仮定の追加）になる。
