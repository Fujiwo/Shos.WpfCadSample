# CAD 開発 進捗記録

- 最終更新：2026-11-07（I-2 を完了。次は I-3）
- 対象：簡易 WPF CAD（要件は[仕様書](../Documents/20261105-cad-specification.md)。2026-11-06 に唯一の要件の文書とした）
- 更新ルール：文書の作成・承認、実装段階の開始・完了、方針の変更があるたびに、このファイルを更新する。承認状態の正本は各文書の先頭の状態行であり、この表はその写しである。

## 1. 現在の状況

2026-11-06 に、[変更計画](../Plans/20261106-requirements-consolidation-plan.md)に沿って、`create-wpf-cad.prompt.md` の要件を仕様書に取り込み、仕様書を唯一の要件の文書にした。同プロンプトと `create-cad-plan.prompt.md` は削除した。その後、[要件一本化後の仕様書レビュー](../Reviews/20261106-cad-specification-review-post-consolidation.md)の指摘 1〜3 を仕様書、アーキテクチャー／クラス設計書、実装計画書、テスト計画書へ反映した。その後、ユーザーが仕様書を再承認した。続いて設計書をレビューして指摘を反映し、ユーザーが設計書を再承認した。さらに実装計画書をレビューして指摘を反映し、ユーザーが実装計画書を再承認した。その後、テスト計画書のレビュー指摘を反映した際に実装計画書の I-3 も補完したため、その後、ユーザーが実装計画書とテスト計画書を承認し、4 文書がすべて承認済みになった。2026-11-06 に I-1（骨組みと試作）を完了した。プロジェクト、`Directory.Build.props`、T-ARC、README（付随パッケージの理由を含む）、パッケージの版の確認（P-1、P-2）、試作 R-1・R-2・R-6・R-8 がすべて済んだ。2026-11-07 に I-2（幾何とスプライン）を完了した。次は I-3（図形・属性・Drawing）である。

## 2. 計画文書の進捗

| 段階 | 文書 | 保存先 | 状態 | 備考 |
|---|---|---|---|---|
| 1 | 仕様書 | [Documents\20261105-cad-specification.md](../Documents/20261105-cad-specification.md) | 承認済み | 2026-11-05 に承認。2026-11-06 に再レビュー指摘 1〜12 を反映して変更し、同日ユーザーが再承認。要件の一本化で変更後、レビュー待ちに戻した。要件一本化後のレビュー指摘 1〜3 と、その確認の指摘を反映（保存確認の失敗時動作、出力画素数上限、座標範囲）。2026-11-06 にユーザーが再承認 |
| 2 | アーキテクチャー／クラス設計書（UML を含む） | [Documents\20261105-cad-architecture.md](../Documents/20261105-cad-architecture.md) | 承認済み | 2026-11-05 に承認。2026-11-06 に仕様書の改訂とレビュー指摘 1〜11 を反映し再承認。図形の `DataTemplate` を追記して再承認。要件の一本化で変更後、レビュー待ちに戻した。今回の仕様変更（座標検証、Paste の再検証、出力上限）を反映。再承認待ち。設計書のレビュー（要件一本化・仕様変更の反映後）の指摘 1〜8 を反映し、2026-11-06 にユーザーが再承認 |
| 3 | 実装計画書 | [Plans\20261105-cad-implementation-plan.md](../Plans/20261105-cad-implementation-plan.md) | 承認済み | 2026-11-05 に承認。レビュー指摘への修正と仕様・設計更新のたびに再承認・レビュー待ちを経た。要件の一本化で変更後、今回 AC-35 までを実装対象に反映。実装計画書のレビュー（要件一本化・仕様と設計の再承認後）の指摘 1〜7 を反映し、2026-11-06 にユーザーが再承認。テスト計画書のレビュー指摘 2 で I-3 のテスト群に T-EXP を加え、レビュー待ちに戻した。2026-11-06 にユーザーが実装計画書と一組で承認 |
| 3 | テスト計画書 | [Plans\20261105-cad-test-plan.md](../Plans/20261105-cad-test-plan.md) | 承認済み | 2026-11-05 に承認。テスト計画レビュー・初期プロジェクト確認で修正後、要件一本化で変更しレビュー待ち。今回、保存失敗時動作、座標上限、出力画素数のテストと AC 対応を追加。2026-11-06 にユーザーが実装計画書と一組で承認 |

## 3. 実装の進捗

| 項目 | 状況 |
|---|---|
| 段階 | I-1（骨組みと試作）を 2026-11-06 に完了。I-2（幾何とスプライン）を 2026-11-07 に完了。次は I-3（図形・属性・Drawing） |
| ソリューション | `Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx` に 3 プロジェクトを登録済み（ユーザーが追加。2026-11-06） |
| アプリ／テストプロジェクト | 初期プロジェクトを、[確認結果](../Reviews/20261106-initial-projects-review.md)に沿って修正済み（`Directory.Build.props`、`MainWindow` の namespace、`CommunityToolkit.Mvvm` の参照先、フォルダー、仮のクラス・テストの削除、`coverlet.collector` の削除）。`Source\README.md` の骨子を作成 |
| ビルド・テスト | 2026-11-07、I-2 の完了時にエージェントが WSL から Windows の `dotnet.exe`（SDK 10.0.403）で実行：`dotnet build Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` は成功（警告 0、エラー 0）。`dotnet test Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` は 163 件すべて成功（失敗 0、スキップ 0）。以前の記録：2026-11-06、ユーザーが Windows で実行：`dotnet build Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` は成功（警告・エラーなし）。`dotnet test Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` は、T-ARC-04 の修正後の再実行で 3 件すべて成功（失敗 0、スキップ 0） |
| パッケージの版（P-1） | 2026-11-06 に NuGet で確認。Shos.UndoRedoList 1.0.8、CommunityToolkit.Mvvm 8.4.2、MaterialDesignThemes 5.4.0、xunit 2.9.3（v2 系の最新）は最新の安定版。xunit.runner.visualstudio（3.1.4 → 4.0.1）と Microsoft.NET.Test.Sdk（17.14.1 → 18.11.0）は、ユーザーの判断（「最新に更新」）で最新の安定版へ更新した。更新後、Windows でビルド・テストが成功した（R-8 を再確認。P-2 はこの組み合わせで確定）。`System.Drawing.Common` はパッケージ不要（R-2） |
| 試作（R-1、R-2、R-6、R-8） | 試作のコードは成果物に残さず、セッションのスクラッチパッドに置いた。**R-2 成立**：`net10.0-windows`・`UseWPF` のプロジェクトで、パッケージ参照なし（`project.assets.json` のライブラリが空）に `System.Drawing.Imaging.Metafile` などを使うコードが、警告・エラーなしでビルドできた。`System.Drawing.Common` は `Microsoft.WindowsDesktop.App.WPF` の参照（10.0.14）に含まれる（Linux の .NET SDK 10.0.114 と `EnableWindowsTargeting` でのビルドで確認。実行時の動作は R-1 で確認する）。**R-6 成立**：`CommunityToolkit.Mvvm` 8.4.2 の部分プロパティ形式の `[ObservableProperty]`、`[NotifyPropertyChangedFor]`、`[NotifyCanExecuteChangedFor]`、`[RelayCommand(CanExecute = ...)]` と、手書きのプロパティの `field` キーワードが、C# 14 / .NET 10（`net10.0`、`Nullable`、`TreatWarningsAsErrors`）で警告なしにビルドでき、実行して通知とコマンドの 5 項目がすべて期待どおりだった（.NET SDK 10.0.114）。**R-1 成立**（2026-11-06、ユーザーが Windows で確認）：試作（`Metafile`（`EmfPlusDual`）に Y 反転の変換をかけて破線の直線とベジェを描き、`GetHenhmetafile()` のハンドルを、所有者ウィンドウを付けた `OpenClipboard` → `EmptyClipboard` → `SetClipboardData(CF_ENHMETAFILE)` で渡す）を Windows で実行し、Word への貼り付けで直線とベジェが表示された。**R-8 成立**：3.1.4 / 17.14.1 に続き、更新後の xunit.runner.visualstudio 4.0.1 / Microsoft.NET.Test.Sdk 18.11.0 でも、`slnx` 全体の `dotnet test` で xunit 2.9.3 のテスト 3 件が実行・成功した（2026-11-06、Windows） |

### テスト群の実装状況（実装計画書 C-3）

| テスト群 | 段階 | 実装 | 実行結果 |
|---|---|---|---|
| T-ARC | I-1、I-10 | T-ARC-01、02、04 を実装（`Tests\Architecture\ArchitectureTests.cs`）。T-ARC-03 は `dotnet test` の実行そのもので確認する。2026-11-07、T-ARC-04 の除外の判定を修正（下の履歴） | 3 件成功（2026-11-07、Windows の `dotnet.exe`）。T-ARC-03 も確認済み |
| T-GEO、T-SPL、T-OUT、T-VPT | I-2 | 全ケースを実装（`Tests\Models\Geometry\*Tests.cs`。T-GEO-01〜05、T-SPL-01〜10、T-OUT-01〜04、T-VPT-01〜15 の 34 ケースを、境界値などを含む 160 件のテストで確認） | 160 件成功（2026-11-07、Windows の `dotnet.exe`）。T-SPL-10 の 10,000 点の変換（開いた曲線と閉じた曲線）は 21.5 ms |
| T-SHP、T-ATT、T-HIT、T-DRW、T-EXP | I-3 | 未着手 | — |
| T-SER、T-STO、T-SET | I-4 | 未着手 | — |
| T-EDC | I-5 | 未着手 | — |
| T-VM | I-6 | 未着手 | — |

## 4. 確定した方針

| ID | 内容 | 決定日 |
|---|---|---|
| D-1 | 「全体図」は固定の論理領域を基本とし、はみ出した図形があれば外接矩形まで広げる | 2026-11-05 |
| D-2 | UI の表示言語は英語のみ | 2026-11-05 |
| D-3 | ドキュメントの拡張子は `.json` | 2026-11-05 |
| D-4 | 線の色は RGB スライダーと 16 進入力で編集する | 2026-11-05 |
| D-5 | UI の外観に Material Design in XAML Toolkit（`MaterialDesignThemes`）を採用し、最新の版（Material Design 3、最新の安定版パッケージ）を使う。Light テーマのみ | 2026-11-05 |
| D-6 | ルート namespace 直下に型を置かない規則の例外として、WPF アプリケーションクラス `App` だけをプロジェクトのルート namespace 直下（`Shos.WpfCadSample.App`）に置く（設計書 DD-14） | 2026-11-05 |

## 5. 未決事項

| ID | 内容 | 状況 |
|---|---|---|
| U-1 | 論理領域の大きさ、太さの範囲、拡大の上限の具体値 | 仕様書の仮定（A-1、A-6、A-7）のまま。実装時に定数として調整可能 |
| U-2 | 属性ウィンドウを「選択図形」と「新規図形の既定属性」の 2 区画にする案（A-2） | 仕様書の承認により確定（2026-11-06 の変更で、仕様書 9.2 にも確定を記録） |
| U-3 | 画像・EMF の出力解像度と範囲、EMF の方式 | 設計書の承認により確定（GDI+ の `Metafile` と `CF_ENHMETAFILE`）。実現性は実装の最初（I-1）の試作で確認（R-1） |
| U-4 | Material Design 3 の具体的な配色 | 設計書の承認により確定（Primary: `Indigo`、Secondary: `Amber`） |
| U-5 | 図形の内部をクリックしても選択できるようにするか | 仕様書の承認により、現行案（線上のみ。A-17）で確定 |

設計書のリスク：R-1（EMF の方式）、R-2（`System.Drawing.Common`）、R-3（Material Design 3 のスタイル名）、R-4（最大化時の位置）、R-5（クリップボードの変更通知）、R-6（`CommunityToolkit.Mvvm` と C# 14）、R-7（`DisabledUndoScope` と履歴の整合）。実装計画書では、R-8（`xunit` のランナー）、R-9（UI の手動確認）、R-10（性能）を追加した。

実装計画書の未決事項：P-1（パッケージの版。導入時に確認）、P-2（`xunit` のランナーの版。I-1 で確認）。

## 6. 履歴

| 日付 | 内容 |
|---|---|
| 2026-11-05 | 運用整備：リポジトリ指示（`.github\copilot-instructions.md`、`CLAUDE.md`）と、各プロンプト（`create-wpf-cad`、`create-agent-instructions`、`create-cad-plan`）を作成・更新。`.gitignore` を整備 |
| 2026-11-05 | 運用ルールの追加：プロンプトは `.github\prompts\`、レビュー結果は `Reviews\` に新規 Markdown として保存。[指示ファイルのレビュー](../Reviews/20261105-copilot-instructions-review.md)と、その指摘への修正 |
| 2026-11-05 | 仕様書（段階1）を作成し、レビュー待ちとして提示 |
| 2026-11-05 | Material Design の採用を、指示・プロンプト・仕様書に反映 |
| 2026-11-05 | 進捗記録を `Progress\` に作成 |
| 2026-11-05 | 仕様書をレビューし、[結果](../Reviews/20261105-cad-specification-review.md)を `Reviews\` に記録（中 6 件、低 6 件。要件の網羅に欠落なし） |
| 2026-11-05 | レビュー指摘 1〜12 を仕様書に反映（仮定 A-17〜A-23、未決事項 U-5、受け入れ条件 AC-24〜AC-31、5.10 を追加）。状態は `レビュー待ち` のまま |
| 2026-11-05 | ユーザーが仕様書を承認（「すべて承認」）。仕様書の状態を `承認済み` に更新 |
| 2026-11-05 | 設計書（段階2）を作成し、レビュー待ちとして提示。外部資料（Shos.UndoRedoList、Shos.BezierSamples、Material Design in XAML Toolkit）を確認し、設計判断 DD-1〜DD-13、仮定 DA-1〜DA-6、リスク R-1〜R-7 を記載。Mermaid の図 10 点を構文検証 |
| 2026-11-05 | ユーザーが設計書を承認（「すべてお勧め通りで承認」）。設計書の状態を `承認済み` に更新 |
| 2026-11-05 | 実装計画書（10 段階）とテスト計画書（自動 155 ケース、手動 22 項目）を作成し、レビュー待ちとして提示。開発環境（.NET SDK 10.0.403、WindowsDesktop 10.0.14）とパッケージの最新の安定版を確認。AC-01〜AC-31 がすべてテストに対応していること、文書間の ID の参照が整合していること、Mermaid の図の構文を検証 |
| 2026-11-05 | ユーザーの指示により、`App` クラスだけをルート namespace 直下に置く規則の変更（D-6）を、`copilot-instructions.md`、`create-wpf-cad`、`create-agent-instructions` の各プロンプト、設計書（3.1、3.2、DD-14）、実装計画書（I-1、I-8、I-10）、テスト計画書（S-01、付録 A）に反映。ユーザーが、この変更を指示すると同時に明示的に承認したため、設計書の承認状態は維持した |
| 2026-11-05 | ユーザーが実装計画書とテスト計画書を承認（「他は承認」）。4 文書がすべて承認済みになり、計画の作成は完了 |
| 2026-11-06 | 承認済みの仕様書を再レビューし、[結果](../Reviews/20261106-cad-specification-review.md)を `Reviews\` に記録（中 3 件、低 9 件。要件の網羅に欠落なし、前回指摘は反映済み）。仕様書は未変更で、承認状態は維持 |
| 2026-11-06 | 再レビューの指摘 1〜12 を仕様書に反映（4.3、4.5、4.6、5.3、5.6、5.7、5.10、6.3、6.6、7 章、仮定 A-24〜A-28、AC-32・AC-33 の追加と既存 AC の更新、9.2 の確定状況）。仕様書をレビュー待ちに戻し、影響を受ける設計書・実装計画書・テスト計画書もレビュー待ちに戻した |
| 2026-11-06 | 設計書をレビューし、[結果](../Reviews/20261106-cad-architecture-review.md)を `Reviews\` に記録（中 4 件、低 7 件。仕様書の 2026-11-06 改訂の未反映を含む）。設計書は未変更 |
| 2026-11-06 | ユーザーの指示により、設計書のレビュー指摘 1〜11 を設計書に反映（仕様の改訂の反映、`PointerModifiers`、制約の判定の一本化、捕捉する例外、`Shapes` の読み取り専用化、ペーストの回数、`Committed` の経路、終了の流れ、ヒットテストへの線の太さ、名前の統一、DD-15）。状態は `レビュー待ち` のまま。仕様書の再承認より前の更新のため、仕様書の承認時に変更があれば設計書も追従させる |
| 2026-11-06 | ユーザーが仕様書を再承認（「仕様書を再承認」）。仕様書の状態を `承認済み` に更新。再承認での変更はなく、設計書の前提の記述だけを更新 |
| 2026-11-06 | ユーザーが設計書を再承認（「設計書を再承認」）。設計書の状態を `承認済み` に更新 |
| 2026-11-06 | 実装計画書をレビューし、[結果](../Reviews/20261106-cad-implementation-plan-review.md)を `Reviews\` に記録（中 2 件、低 5 件。仕様書・設計書の改訂の未反映を含む）。実装計画書は未変更 |
| 2026-11-06 | ユーザーの指示により、実装計画書のレビュー指摘 1〜7 を実装計画書に反映。状態は `レビュー待ち` のまま。段階と手動テストの対応の変更（M-12〜M-14 と M-20 を I-9 へ）は、テスト計画書の段階の対応表にも反映が必要 |
| 2026-11-06 | テスト計画書をレビューし、[結果](../Reviews/20261106-cad-test-plan-review.md)を `Reviews\` に記録（中 2 件、低 4 件。仕様書・設計書・実装計画書の改訂の未反映を含む）。テスト計画書は未変更 |
| 2026-11-06 | ユーザーの指示により、テスト計画書のレビュー指摘 1〜6 をテスト計画書に反映（自動テストを 155 から 172 ケースへ。AC-32・AC-33 を含め、AC-01〜AC-33 をすべてテストに対応付けた）。状態は `レビュー待ち` のまま。実装計画書とテスト計画書は、一組で再承認を待つ |
| 2026-11-06 | ユーザーが実装計画書とテスト計画書を一組で再承認（「実装計画書とテスト計画書を承認」）。両計画書の状態を `承認済み` に更新。4 文書がすべて承認済みになり、計画の作成は再び完了 |
| 2026-11-06 | `create-wpf-cad.prompt.md` をレビューし、[結果](../Reviews/20261106-create-wpf-cad-prompt-review.md)を `Reviews\` に記録（中 4 件、低 4 件。段階の扱い、曖昧な点の扱い、コマンドの表記、`DataTemplate` の要件の未対応など）。プロンプトは未変更 |
| 2026-11-06 | ユーザーの指示（「指摘4 は (a) で。既存のプロンプトを直して。」）により、`create-wpf-cad.prompt.md` の指摘 1〜3、5〜8 を既存のプロンプトに反映（段階・完了条件は承認済みの計画書に従う、曖昧な点の扱いを実装計画書 9 章に合わせる、コマンドの `/` 区切りと Windows の `dotnet`、GUI を操作できない場合の手動テストの依頼、コードビハインドの規則、C# の文法、スキルの複製の比較）。指摘 4 は (a)：設計書 5.7・7 章と実装計画書 I-8 に `DataTemplate` を追記し、両文書をレビュー待ちに戻した。テスト計画書は変更不要のため承認を維持 |
| 2026-11-06 | ユーザーの指示により、C# の新しい文法を「積極的に使う」方針に統一：`.github\copilot-instructions.md` の CAD conventions を「actively use new C# syntax」に改め、`create-wpf-cad.prompt.md` §1 を「積極的に使う」に戻した（[プロンプトのレビュー](../Reviews/20261106-create-wpf-cad-prompt-review.md)の指摘 7 は、この決定により取り下げ） |
| 2026-11-06 | ユーザーが設計書と実装計画書を再承認（「設計書と実装計画書を再承認」）。両文書の状態を `承認済み` に更新。4 文書がすべて承認済みになった |
| 2026-11-06 | ユーザーが初期の 3 プロジェクトを追加（コミット `93d63bf`）。内容を確認し、[結果](../Reviews/20261106-initial-projects-review.md)を記録（中 4 件、低 5 件：`MainWindow` の namespace、`Directory.Build.props` の未作成、`CommunityToolkit.Mvvm` の参照先、`Geometry` フォルダーの位置など）。NuGet への接続と Windows の `dotnet.exe` が使えないため、復元・ビルド・テストは未実施 |
| 2026-11-06 | ユーザーの指示（「修正せよ。こちらで Windows でビルド・テストする。」）により、初期プロジェクトの確認の指摘 1〜9 を修正：`Directory.Build.props`（`Nullable`、`ImplicitUsings`、`TreatWarningsAsErrors`、`RootNamespace=Shos.WpfCadSample`）を追加、`MainWindow` を `Shos.WpfCadSample.Views` へ、`CommunityToolkit.Mvvm` を Core へ移動、`Models\Geometry` へ変更、`Class1`・`UnitTest1`・`coverlet.collector`・不要な `using`・空のソリューションフォルダーを削除。T-ARC-01、02、04 を実装し、`Source\README.md` の骨子を作成。テスト計画書の付録 A を修正し、テスト計画書をレビュー待ちに戻した。ビルド・テストは未実施（ユーザーが Windows で実行する） |
| 2026-11-06 | ユーザーが Windows でビルド・テスト：ビルドは成功（警告・エラーなし）。テストは T-ARC-04 が失敗。原因は、`Microsoft.NET.Test.Sdk` がテストプロジェクトのグローバル名前空間に生成するエントリポイント `AutoGeneratedProgram` が、生成コードを示す属性（想定した `TestSDKAutoGeneratedCodeAttribute`）を持たず、除外されなかったこと。テストを修正し（型名で除外し、効いていなかった属性名の判定を削除）、再実行を依頼。R-8（`slnx` 全体の `dotnet test`）は、テストが実行されたことで動作を確認 |
| 2026-11-06 | ユーザーが Windows でテストを再実行：3 件すべて成功（失敗 0、スキップ 0）。I-1 の完了条件のうち C-1、C-2 を満たした。残り：R-1、R-2、R-6 の試作、パッケージの版の確認（P-1）、テスト計画書の再承認 |
| 2026-11-06 | `create-wpf-cad.prompt.md` と 2 つの計画書の整合を確認し、実行の主体を評価。[結果](../Reviews/20261106-create-wpf-cad-prompt-vs-plans-review.md)を記録（内容の矛盾なし。運用上の注意 1 件：テスト計画書がレビュー待ちのため、プロンプトの開始条件を満たさない。低 3 件：大枠の順序、付随パッケージの理由の記録、WordPad）。評価：プロンプト全体の再実行ではなく、実装計画書を段階ごとに実行し、プロンプトは要件の出典と作業ルールとして使う |
| 2026-11-06 | ユーザーの指示（「要件を仕様書に吸収して、仕様書を唯一の要件の文書にする」）により、変更計画 [Plans\20261106-requirements-consolidation-plan.md](../Plans/20261106-requirements-consolidation-plan.md) を作成し、レビュー待ちとして提示。承認後に実行する |
| 2026-11-06 | ユーザーが変更計画を承認（「変更計画を承認」。Q-1 推奨どおり、Q-2 案 B、Q-3 推奨どおり）。変更計画の状態を `承認済み` に更新し、実行を開始 |
| 2026-11-06 | 変更計画を実行：仕様書の 3 章を「要件」に再構成し、`create-wpf-cad.prompt.md` の §1〜§11 を取り込み（3.1 要件の一覧、3.2〜3.7）、仕様書を唯一の要件の文書にした。§0 の作業方針の未記載分を実装計画書 3.1、3.2、9 章に追記。設計書・両計画書・`copilot-instructions.md`・`create-agent-instructions.prompt.md`・`Source\README.md` の参照を置き換え、`create-wpf-cad.prompt.md` と `create-cad-plan.prompt.md` を削除。対応表を変更計画の付録に記録。仕様書・設計書・実装計画書をレビュー待ちに戻した（テスト計画書はもともとレビュー待ち）。`Reviews\` と本記録の過去の履歴は変更していない |
| 2026-11-06 | ユーザーが[要件一本化後の仕様書レビュー](../Reviews/20261106-cad-specification-review-post-consolidation.md)を行い、指摘 1〜3（保存確認後の保存失敗・キャンセル時の中止、出力の画素数の上限、座標の範囲）を仕様書・設計書・実装計画書・テスト計画書へ反映。4 文書はレビュー待ちのまま |
| 2026-11-06 | ユーザーの依頼で、上記の対応を確認し、[確認結果](../Reviews/20261106-cad-specification-fix-check-review.md)を保存（中 1 件、低 5 件）。指摘 1〜6 を修正：実装計画書の AC-34・AC-35 の段階への割り当てと `ExportFrame` の説明、テスト計画書の I-10 の完了条件と T-SHP-18・T-SER-18、仕様書の導出値の有限性の明確化（6.3、7 章、A-29、AC-35）と改訂履歴の順序、本記録の履歴。4 文書はレビュー待ちのまま |
| 2026-11-06 | ユーザーが仕様書を再承認（「仕様書を再承認」）。仕様書の状態を `承認済み` に更新。設計書・実装計画書・テスト計画書はレビュー待ちのまま |
| 2026-11-06 | ユーザーの依頼で設計書をレビューし、[レビュー結果](../Reviews/20261106-cad-architecture-review-2.md)を保存（中 3 件、低 6 件）。指摘 1〜8 を設計書に反映し（座標の範囲の判定の一本化と 1 点目への適用、Open・New の後の表示の初期化の持ち主、`Paste` の成否の返し方、シーケンス図の中止の分岐など）、実装計画書（I-5、I-6）とテスト計画書（T-EDC-35〜37、T-VM-04、T-VM-16、T-VM-27、T-VM-28）を更新。指摘 9 は記録のみ。3 文書はレビュー待ちのまま |
| 2026-11-06 | ユーザーが設計書を再承認（「設計書を再承認」）。設計書の状態を `承認済み` に更新。実装計画書・テスト計画書はレビュー待ちのまま |
| 2026-11-06 | ユーザーの依頼で実装計画書をレビューし、[レビュー結果](../Reviews/20261106-cad-implementation-plan-review-2.md)を保存（中 1 件、低 6 件）。指摘 1〜7 を実装計画書に反映（1.3 の現状、R-1 の確認先、I-1 の README と R-8、I-2 の `Point2.IsWithinLimits()`、I-3 の `Drawing` の操作、AC-35 の段階、File メニューのバインドの段階）。テスト計画書の M-13 と 6 章から WordPad を外した。2 文書はレビュー待ちのまま |
| 2026-11-06 | ユーザーが実装計画書を再承認（「実装計画書を再承認」）。実装計画書の状態を `承認済み` に更新。テスト計画書はレビュー待ちのまま |
| 2026-11-06 | ユーザーの依頼でテスト計画書をレビューし、[レビュー結果](../Reviews/20261106-cad-test-plan-review-2.md)を保存（中 1 件、低 3 件）。指摘 1、3、4 をテスト計画書に反映（T-GEO-05、M-07 の座標の上限、T-VM の並び）。指摘 2 で実装計画書の I-3 に T-EXP を加え、実装計画書をレビュー待ちに戻した |
| 2026-11-06 | ユーザーが実装計画書とテスト計画書を承認（「実装計画書とテスト計画書を承認」）。両文書の状態を `承認済み` に更新。仕様書・設計書・実装計画書・テスト計画書の 4 文書がすべて承認済みになった |
| 2026-11-06 | I-1 の残りを再開（ユーザーの指示「I-1 の残りを進めよ」）。P-1：NuGet で版を確認し、ユーザーの判断でテスト用の付随パッケージ 2 つを最新の安定版へ更新（xunit.runner.visualstudio 4.0.1、Microsoft.NET.Test.Sdk 18.11.0）。README に付随パッケージの理由、xunit v2 を使う理由、`System.Drawing` がパッケージ不要であることを追記。試作：R-2・R-6 は成立（3 章）。R-1 は試作を作成し、Windows での実行と Word への貼り付けの確認をユーザーに依頼。Windows の `dotnet.exe` はエージェントの環境から起動できず、リポジトリのビルド・テストは未実施 |
| 2026-11-06 | ユーザーが Windows で確認：R-1 の試作は成功（Word への EMF の貼り付けを確認）。`dotnet build` は成功。`dotnet test` は、Tests のビルド中に `Microsoft.CodeCoverage` 18.11.0 の targets が `bin\Debug\net10.0\.msCoverageSourceRootsMapping_Shos.WpfCadSample.Tests` へ書き込めず（MSB3491、Access to the path is denied）失敗。ファイルは直前の `dotnet build` で書かれており、Dropbox の同期などによる一時的なロックを疑い、再実行を依頼 |
| 2026-11-06 | ユーザーが Windows で `dotnet test Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` を再実行：合計 3、成功 3、失敗 0、スキップ 0。前回の MSB3491 は再発せず、一時的なロックだったと判断。R-8 を更新後の版で再確認し、I-1 の完了条件（C-1、C-2、C-6、R-1・R-2・R-6・R-8 の記録）を満たしたため、I-1 を完了とした |
| 2026-11-07 | I-2 を開始（ユーザーの指示「I-2 を開始」）。前提の確認：仕様書・設計書・実装計画書・テスト計画書の先頭行がすべて `状態：承認済み`。`.github\skills\` と `.claude\skills\` の `genshijin`、`sustainable-code-jp` を比較し、差なし（改行コードを除き一致）。エージェントの環境から Windows の `dotnet.exe`（SDK 10.0.403）を実行できるようになり、開始時の `dotnet test Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` は 3 件成功 |
| 2026-11-07 | I-2 の実装（テストファースト。型ごとに Red（コンパイルエラー）を確認してから実装）：`Models\Geometry` に `Point2`（`IsWithinLimits()`、`IsFinite`、`DistanceTo`）、`Vector2`、`Rect2`（`Union`、`Contains`、`Bounding`）、`ViewTransform`、`Outline`・`OutlineFigure`（`Circle` を含む）・`OutlineSegment`・`LineSegment`・`CubicBezierSegment`、`Segment2`、`SplineBezier`（`Open`、`Closed`。出典をコメントと README に記載）、`Viewport` を追加。文書で決まっていない実装の詳細（`OutlineFigure.Circle` の置き場所、`UpdateOverview` の `null`、大きさ 0 のビューの扱い、全体図の判定の許容差、折れ線化の分割の上限、`DistanceTo` の名前）は、実装計画書 9 章に従い、`Source\README.md` の「設計判断」節に理由を記録した |
| 2026-11-07 | T-ARC-04 の失敗と修正：`SplineBezierTests` でコレクション式を使ったところ、コンパイラーが生成した型の入れ子の型（`<>z__ReadOnlySingleElementList`1+Enumerator`、`<PrivateImplementationDetails>+__StaticArrayInitTypeSize=…`）を、T-ARC-04 がグローバル名前空間の型として検出した。入れ子の型は、外側の型が持つ生成コードの属性を持たないため。テストの判定を、外側の型が生成された型なら除くように修正した（製品コードの変更なし） |
| 2026-11-07 | I-2 を完了。C-1：`dotnet build` 成功（警告 0、エラー 0）。C-2：`dotnet test` 163 件成功（失敗 0、スキップ 0）。C-3：T-GEO、T-SPL、T-OUT、T-VPT の全ケースを実装し成功。C-4：セルフチェック（下記）。C-5：要件にない機能は追加していない（使っていなかった `Vector2` の単項マイナスは削除した）。C-6：本記録。セルフチェック：各型の仕事はひとことで言える（点・変位・矩形・ビュー変換・中間表現・スプライン変換・拡大縮小の規則）。座標の範囲の判定は `Point2.IsWithinLimits()` だけ、ビュー変換は `ViewTransform` だけに置いた。区間ごとの違い（外接矩形の点、折れ線化）は `OutlineSegment` の多態で表した。範囲外の気づき：`Source\Shos.WpfCadSample\Shos.WpfCadSample.Core\Geometry\` に、I-1 のフォルダーの移動（`Models\Geometry` へ）の後に残った空のフォルダーがある（git の管理外。未変更）。WSL の git では、作業ツリーの 25 ファイルが改行コードの違いだけで変更扱いになっている（内容の差なし。未変更） |

## 7. 次の作業

1. 4 文書は 2026-11-06 にすべて承認済み（実装の前提を満たす）。
2. I-1 は 2026-11-06、I-2 は 2026-11-07 に完了。次は I-3（図形・属性・Drawing：T-SHP、T-ATT、T-HIT、T-DRW、T-EXP をテストファーストで実装）。以降は、実装計画書の段階（I-4〜I-10）を 1 段階ずつ実行する。
   - I-3 での注意：`SplineBezier.Closed` は通過点が 3 つ未満だと `ArgumentException` を投げる。設計書 5.1 の `ShapeKindInfo.CanAppend` は、2 点目の候補を `Create` で作って `SatisfiesConstraints()`（防御として `CreateOutline()` の有限性を確かめる）で判定するため、閉じた自由線の 2 点の候補で `CreateOutline()` を呼ばない、または 2 点の扱いを決める必要がある。
3. 各段階の開始時と完了時に、この記録を更新する（ビルド・テストのコマンドと結果、未解決の課題を含む）。
