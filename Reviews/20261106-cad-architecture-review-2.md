# 簡易 WPF CAD 設計書レビュー（要件一本化・仕様変更の反映後）

- レビュー日：2026-11-06
- 対象：[アーキテクチャー／クラス設計書](../Documents/20261105-cad-architecture.md)（状態：レビュー待ち。2026-11-06 の要件の一本化と、要件一本化後の仕様書レビュー指摘 1〜3 の反映後）
- 前提：[仕様書](../Documents/20261105-cad-specification.md)（2026-11-06 再承認）。前回のレビューは [20261106-cad-architecture-review.md](20261106-cad-architecture-review.md)
- 観点：承認済みの仕様書との整合（特に今回追加された座標の範囲 A-29、出力の上限 A-24、保存確認後の中止 AC-34）、責務の所在と依存の向き、契約の明確さ（Testable）
- 方法：`sustainable-code-jp` スキルの「設計の相談」の手順（object-design.md、quality-gates.md の「七箇条をレビューの観点にする」）。`.github/skills` と `.claude/skills` の複製は一致していることを確認した。
- 設計の意図（ひとこと）：UI に依存しない `Core` に、不変な図形と Undo / Redo 付きの `Drawing`、入力を解釈する `EditorController`、ファイル操作の流れを持つ `MainViewModel` を置き、WPF 側は表示と入力の変換と OS の機能だけを受け持つ。
- 結果：中程度 3 件、低程度 6 件。今回の仕様変更のうち、出力の上限（`ExportFrame`）、Paste の再検証、保存確認後の中止の規則（5.4）は反映されている。

## 指摘一覧

| # | 重要度 | 場所 | 指摘 |
|---|---|---|---|
| 1 | 中 | 5.1 `ShapeKindInfo.CanAppend`（214行） | 1 点目は「座標が有限」しか確かめず、座標の上限（±1,000,000）を超える 1 点目を受け付ける |
| 2 | 中 | 5.4 `DrawingViewModel`（406行）、6.4（572、582行）、5.4 のクラス図 | Open / New の後の「全体図表示・選択の解除・入力中の図形の取り消し」の持ち主と経路が決まっておらず、記述が食い違う |
| 3 | 中 | 5.3 `EditorController.Paste`（326行）、8.2 | Paste のオフセット後の制約違反を「エラーを通知する」とあるが、通知の手段（契約）がない |
| 4 | 低 | 6.4、6.5 のシーケンス図 | 保存確認で Yes の後の保存失敗・Save As のキャンセルで操作を中止する分岐（AC-34）が図にない |
| 5 | 低 | 7 章（637行）、5.6 の検証⑥（463行） | 仮定 A-29（座標の範囲）の対応がない。読み込み時の検証の列挙に「座標の範囲」がない |
| 6 | 低 | 5.1 `SatisfiesConstraints()`（205行） | 「導出される幾何値が有限」が、仕様 A-29 の改訂（座標の範囲から導かれる性質。防御としての検査）と表現が揃っていない |
| 7 | 低 | 3.3 の依存関係図（82–109行） | `Infrastructure`（`CompositionRoot`）と `Views` から `Core` の各層への依存が図にない |
| 8 | 低 | 9.4 の末尾（702行） | 「DD-15、DA-7 と変更点は再承認の対象とする」が、2026-11-06 の再承認後も残っている |
| 9 | 低（参考） | 5.4 `MainViewModel`（397–404行） | 責務の列挙が多く、変更理由が複数ある |

## 指摘の詳細

### 1. 1 点目の座標の上限を確かめない（中）

- `5.1 ShapeKindInfo.CanAppend`：仕様との食い違い（七箇条の外。整合）（1 点目は「座標が有限であることだけを確かめる」。仕様 5.2 は「座標上限を超える点のクリックは点として追加しない」とし、1 点目を除外していない）→ 1 点目も座標の範囲を確かめる。
- 影響：全体図の外縁付近を拡大して ±1,000,000 を超える位置をクリックすると、1 点目が受け付けられる。2 点目以降の `SatisfiesConstraints()` で候補全体が拒否され続け、入力が進まない（拒否の理由が「2 点目が不正」と誤って伝わる）。
- 改善案：座標の範囲の判定を `Point2`（または `Models` の 1 か所。例：`Point2.IsWithinLimits`）に置き、`CanAppend` の 1 点目と `SatisfiesConstraints()` の両方がそれを使う（Once And Only Once）。T-EDC-35 の手順に、1 点目が範囲外の場合を含める。

### 2. Open / New の後の表示の初期化の持ち主が決まっていない（中）

- `5.4 DrawingViewModel` と `6.4`：責務の所在が不明（デメテルの法則違反の兆候。6.4 で `MainViewModel` が `Viewport.Reset` と選択の解除を直接行うが、`MainViewModel` は `Viewport` も `EditorController` も保持していない（5.4 のクラス図は `MainViewModel --> DrawingViewModel` だけ。`Drawing` への依存も図にない））→ 処理を、状態の持ち主へ委譲する公開操作にまとめる（Expert）。
- 食い違い：5.4（406行）は「ファイルの読み込み…で `Viewport.UpdateOverview` を呼ぶ」とする。`UpdateOverview` は拡大中なら縮尺を維持するため、仕様 5.8 の「Open・New の後は全体図表示」と合わない。6.4 は `Viewport.Reset` としている。
- 抜け：`Drawing.Load` は `Committed` を発生させない（5.1）ため、`EditorController` の選択の補正が走らず、`SelectedId` と `Handles` が古いまま残る。入力中の図形の取り消し（6.4 の 572行）を誰が行うかも書かれていない。
- 改善案（単一責務・的確な名前）：例えば `DrawingViewModel.ResetForNewDocument()`（入力中の図形の取り消し、選択の解除、`Viewport.Reset`、`Handles` の更新）と、`EditorController.Reset()`（入力の取り消しと選択の解除）を設け、`MainViewModel` は Open・New の成功後にこれだけを呼ぶ。入力の取り消しは保存確認の前に行う（6.4、`TryClose` と同じ順序）。5.4 のクラス図に `MainViewModel --> Drawing`（`Load`、`MarkSaved`）を加えるか、`Load` も `DrawingViewModel` 経由にする。

### 3. Paste の失敗の通知手段がない（中）

- `5.3 EditorController.Paste`：契約の不完全性（七箇条の外。Testable に関わる）（「追加せずエラーを通知する」が、`EditorController` の公開する通知は `StateChanged`・`Committed`・`StatusMessage` だけで、エラーダイアログを出す `MainViewModel` へ失敗を伝える手段がない。8.2 は `MainViewModel` が捕捉する例外を 4 種に限っており、新しい例外も想定していない）→ 戻り値で結果を返す。
- 改善案：`Paste(shape, repeat)` を `bool`（成功したか）を返す操作にし、`MainViewModel` が偽なら `IDialogService.ShowError` で英語のメッセージを表示する（仕様 7 章「Paste：オフセット適用後の図形が制約または座標上限に違反する」）。T-VM（Paste の失敗の表示）と T-EDC（追加されない）の両方で確かめられる。例外にしないのは、予期できる入力の不正であり、8.2 の「`Models` は例外を投げず、不正は `IsValid()` で表す」方針と揃えるため。

### 4. シーケンス図に AC-34 の中止の分岐がない（低）

- `6.4`：意図を表現（572行は「未保存なら AskSaveChanges」の後、無条件に `ShowOpenFileDialog` へ進む。Cancel、保存の失敗、Save As のキャンセルで中止する分岐がない）→ 図に `alt` を加える。
- `6.5`：605行の分岐が「Cancel または保存の失敗」で、Save As のキャンセルを含まない。
- 改善案：6.4 に「Cancel・保存失敗・Save As キャンセル → 中止（ドキュメントと変更あり状態を維持）」の分岐を、6.5 の条件に「Save As のキャンセル」を加える。文章（5.4 の 403行）は正しいので、図だけの修正。

### 5. A-29 の対応表と検証の列挙の抜け（低）

- `7 章`（637行）：仕様との対応の抜け（七箇条の外。整合）（「A-17〜A-28」で止まっており、A-29 がない）→ A-29：`Shape.SatisfiesConstraints`、`ShapeKindInfo.CanAppend`、`EditorController.Paste`、`DocumentSerializer`、`ClipboardShapes` を加える。
- `5.6` の検証⑥（463行）：「座標の有限性、制約、点数、太さの範囲」に「座標の範囲」を加える（`IsValid()` には含まれているので、記述だけの修正）。

### 6. 導出値の有限性の位置づけ（低）

- `5.1 SatisfiesConstraints()`：意図を表現（「導出される幾何値が有限」が独立した制約のように読める。仕様 6.3・A-29 は、2026-11-06 の改訂で「座標の範囲から導かれる性質で、実装は防御として検査してよい」とした）→ 表現を仕様に揃える。
- 改善案：「座標の範囲内では導出値は有限になる（仕様 A-29）。防御として、`CreateOutline()` の結果の有限性も確かめる」とする。検査する場合、`SatisfiesConstraints()` は `CanAppend` とドラッグの `Move` のたびに呼ばれ、曲線ではスプラインの連立方程式を解くことになる。点数が仕様の想定（A-14）の範囲なら問題はないが、性能の計測（T-SPL-10）でこの経路も確認するとよい。

### 7. 依存関係図の抜け（低）

- `3.3`：意図を表現（`CompositionRoot` は `Drawing`、`EditorController`、`Viewport`、各 ViewModel を作る（5.7）が、図の `Infra` からの矢印は `Services` と `Models` だけ。`Views` も `DataTemplate` の `DataType`（`LineShape` など）とコンバーターで `Models` に依存するが、図は `ViewModels` だけ）→ `Infra --> ViewModels / Controllers / Serialization / Settings`、`Views --> Models` を加える。
- 依存の向きはいずれも WPF → Core で、逆向きの依存は生じない。図から依存の全体を読み取れるようにするための修正。

### 8. 9.4 の古い記述（低）

- `9.4` の末尾（702行）：「2026-11-06 に追加した DD-15、DA-7 と変更点は、再承認の対象とする」は、同日の再承認（改訂履歴 712行）で済んでいる。「DD-15、DA-7 は 2026-11-06 に承認を得た」に改める。

### 9. `MainViewModel` の責務の幅（低、参考）

- `5.4 MainViewModel`：単一責務（変更理由の列挙：ファイル操作の流れ、タイトル、クリップボードのコマンドとペーストの回数、属性ウィンドウの表示、ウィンドウの配置と設定の保存。少なくとも 4 つの独立した仕様の変更がここに集まる）→ 現時点では変更しない。
- 判断：アプリケーション全体のコマンドの入口として 1 か所にまとまっていることには、View からの見通しの利点がある。指摘 2 の委譲（表示の初期化を `DrawingViewModel` へ）で増えるのを防げば十分。実装中に 1 つのクラスが大きくなった場合（例：ファイル操作の流れのテストが `MainViewModel` の他の状態の準備を多く必要とする場合）は、ファイル操作の流れ（保存確認と中止の規則）を別の型へ抽出する（クラスの抽出）。

## 確認した事項（問題なし）

- 出力の上限：DD-6、`ExportFrame`（270行）、`MetafileRenderer`（418行）が、仕様 A-24（各辺 10,000 px、4,000,000 画素、EMF も同じ縮尺）と一致する。
- 保存確認後の中止：5.4（403行）が仕様 5.8・AC-34 と一致する。
- Paste の再検証：5.3（326行）で `IsValid()` による再検証を行う（通知の手段は指摘 3）。
- ドラッグ中の座標の範囲：`SelectInteraction` の `Move` が候補を `IsValid()` で確かめ、違反なら直前の有効な状態を保つため、仕様 5.3 の全体移動・端点移動の規定を満たす。
- 依存の向き（`Views` → `ViewModels` → `Controllers` → `Models`、`Controllers` から `ViewModels` への依存なし、`Core` から WPF への参照なし）と、DD-15 によるビュー変換の規則の置き場所は妥当。
- 要件の参照は、要件の一本化に伴い、仕様書 3 章へ置き換えられている（冒頭、DD-1、DD-4、DD-5、DD-15、5.2、5.7、8.1）。

## 推奨する対応

1. 指摘 1〜3 は、実装の開始前に設計書を修正する。指摘 1 と 3 はテスト計画書（T-EDC-35、T-VM）に、指摘 2 は実装計画書の I-6 とテスト計画書の T-VM に影響する。
2. 指摘 4〜8 は、同じ改訂で記述を直す。
3. 指摘 9 は記録にとどめ、実装中に判断する。
