---
description: WPF (C# 14 / .NET 10) で、MVC + MVVM アーキテクチャーの簡易 CAD アプリケーションを作成する
agent: agent
---

# WPF 簡易 CAD アプリケーションの作成

WPF アプリケーションとして、簡易な 2D CAD を既存の `Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx` ソリューション内に作成せよ。アプリケーションとテストの各プロジェクトは `Source\Shos.WpfCadSample\` 配下に配置してこのソリューションに追加し、別のソリューションは作成しない。

## 0. 作業方針

- 実装を始める前に、`Documents\` にある CAD 仕様書とアーキテクチャー／クラス設計書、および `Plans\` にある実装計画書とテスト計画書を確認する。各文書の先頭行が `状態：承認済み` であり、相互に整合している場合に限り実装を開始する。不足・未承認・矛盾があれば実装せず、該当文書の作成または更新へ戻る。
- 承認済み仕様書と設計書を唯一の詳細仕様として扱い、段階・順序・完了条件・テストは承認済みの実装計画書とテスト計画書に従う。このプロンプトの要件を勝手に変更しない。要件との矛盾や計画外の変更が必要な場合は、影響する文書を更新して再承認を得てから実装する。
- 最初に `.github\skills\sustainable-code-jp\SKILL.md` と、必要な `references\*.md` を読み（読む前に、リポジトリ指示に従って `.github\skills\` と `.claude\skills\` の複製を比較し、差があれば報告する）、その判断基準（命名、Testable 設計、Think Simple、ガード節など）に従ってコードを書くこと。
- 要件に書かれていない機能は追加しない（YAGNI）。承認済み文書で決まっていない実装の詳細は、実装計画書の「設計・計画から逸脱する際の承認手順」で承認を要しないとされる範囲（非公開のメソッド・補助クラスの追加、定数の調整など）に限り、合理的な既定値を選んで `Source\README.md` の「設計判断」節に理由を添えて記録する。それ以外（仕様の動作、主要なクラスの責務、保存形式、方式の変更など）は、同じ手順で影響する文書を更新し、ユーザーの承認を得てから実装する。
- 各段階の開始時と完了時に、`Progress\` の進捗記録（例：`Progress\YYYYMMDD-cad-progress.md`）を更新し、実行したビルド・テストのコマンドと結果、未解決の課題を記録する。
- 段階的に進め、**各段階の終わりにリポジトリルートから `dotnet build Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` と `dotnet test Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` が成功すること**を確認してから次へ進む。コマンドは Windows の `dotnet` で実行する（WSL からは `dotnet.exe`。WPF のプロジェクトは Linux の `dotnet` ではビルド・テストできない）。実行できない場合は、確認できなかったことと理由を報告し、その段階を完了としない。各段階の開始時にテスト対象プロジェクトがまだない場合は、まずテスト可能な骨組みを用意する。
- 段階の大枠は次のとおり。実際の段階の分割・順序・完了条件は、承認済みの実装計画書（I-1〜I-10）に従う。
  1. 参照資料の調査、ソリューションの骨組み
  2. Models（図形・属性・シリアライズ・ベジェ変換）とそのテスト
  3. ViewModels / Controllers（図形追加・選択・編集・Undo/Redo）とそのテスト
  4. Views（描画・操作・プロパティウィンドウ・ズーム）
  5. クリップボード・設定の保存・仕上げ
- 完了後、実際にアプリを起動してスモークテスト（図形追加→選択→移動→Undo/Redo→保存→読込）を行い、結果を報告する。エージェントがアプリの画面を操作できない場合は、テスト計画書の手動テストの手順をユーザーに示して実施を依頼し、結果を進捗記録に記録する。実施していない項目を合格と報告しない。

## 1. 技術要件

- C# 14 / .NET 10 (`net10.0-windows`)、WPF。新しい C# の文法（primary constructor、collection expression、`field` キーワード、拡張メンバー、パターンマッチングなど）を積極的に使う。
- 不変データは `class` ではなく `record` / `struct` / `record struct` を使う（例：論理座標の点、サイズ、図形の属性）。
- Nullable 参照型を有効にし、警告はゼロにする。
- 使用してよい外部パッケージ：
  - `Shos.UndoRedoList`（`UndoRedoObservableCollection<T>`）
    - https://github.com/Fujiwo/Shos.UndoRedoList
    - https://www.nuget.org/packages/Shos.UndoRedoList/
  - `CommunityToolkit.Mvvm`（`ObservableObject`、`RelayCommand` 等。使用は任意）
  - `MaterialDesignThemes`（Material Design in XAML Toolkit。UI の外観に使う。アプリ本体の WPF プロジェクトだけが参照する。導入時点の最新の安定版（プレリリース／CI 版は除く）を NuGet で確認し、`net10.0-windows` に対応していることを確かめて使う）
    - https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit
    - https://www.nuget.org/packages/MaterialDesignThemes/
  - テストプロジェクトでは xUnit
  - 上記以外を追加する場合は、理由を README に記録する。
- API の使い方は、推測せず、NuGet / GitHub の README とソースで確認すること。

## 2. アーキテクチャー

### 2.1 namespace とフォルダー構成

- ルート namespace は `Shos.WpfCadSample` とし、**ルート直下には型を置かない**。ただし、WPF アプリケーションクラス `App`（`App.xaml` / `App.xaml.cs`）だけは例外として、WPF プロジェクトのルート namespace 直下（`Shos.WpfCadSample.App`）に置く。`App.xaml` の `x:Class` はこの名前にする。
- フォルダー構成と namespace を一致させる。
- 役割は次のとおり。

| namespace | 役割 |
|---|---|
| `Models` | 図形・属性・ドキュメント・論理座標系など、UI に依存しないデータとルール。ベジェ曲線への変換もここ |
| `ViewModels` | View にバインドする状態とコマンド（図形・選択・属性・ビュー変換など） |
| `Views` | `MainWindow`、`UserControl`、カスタムコントロール、プロパティウィンドウ、コンバーター |
| `Controllers` | 入力操作の解釈と編集の実行（描画ツールの状態遷移、選択・ドラッグ移動・削除、クリップボード操作など） |

- 上記のいずれにも当てはまらないクラスは、役割に合った namespace（例：`Services`、`Settings`、`Clipboard`）を新設して入れる。
- `MainWindow` は `Views` に置く。
- `Models` は WPF の UI 型に依存させない（`System.Windows.Media` 等を参照しない）。単体テスト可能にする。

### 2.2 MVVM とコードビハインド

- View は Model・ViewModel とデータバインドで結ぶ。
- コードビハインド（`*.xaml.cs`）には、`InitializeComponent()` 以外のロジックを書かない。入力処理は、コマンド、添付プロパティ、ビヘイビア、カスタムコントロールのルーテッドイベント／コマンドで実現する。
- `MainWindow` の XAML / コードはできるだけシンプルにする。必要に応じて `UserControl` やカスタムコントロールを作って分割する。
- カスタムコントロールの XAML（ControlTemplate）は、コントロールごとに別ファイルの `ResourceDictionary` とし、`Themes\Generic.xaml` から `MergedDictionaries` で取り込む。
- View には、バインド対象として適宜 Dependency Property を用意する。

### 2.3 UI スタイル（Material Design）

- UI の外観には Material Design in XAML Toolkit（`MaterialDesignThemes`）を採用する。API の使い方は、推測せず、公式の README とソースで確認する。
- `App.xaml` で `materialDesign:BundledTheme`（`BaseTheme="Light"`、プライマリー色・セカンダリー色は設計書で決める）と、**Material Design 3**（最新の版）の既定スタイルである `MaterialDesign3.Defaults.xaml` の `ResourceDictionary` を `MergedDictionaries` に取り込む（Material Design 2 の `MaterialDesign2.Defaults.xaml` は使わない）。`MainWindow` と属性ウィンドウの `Window` には `MaterialDesignWindow` スタイルを適用する。導入時に公式の README／Wiki（Getting Started）で、使用する版の手順と資源名を確認する。
- メニュー、ツールバー、ボタン、テキストボックス、スライダー、コンボボックス、ステータスバー、各ウィンドウの外観は、Material Design のスタイルとアイコン（`PackIcon`）を使う。自作のカスタムコントロールの `ControlTemplate` では、テーマのブラシを `DynamicResource` で参照する。
- 適用範囲は、アプリケーションのクローム（メニュー、ツールバー、属性ウィンドウなど）に限る。描画領域の図形の色と線種は、ユーザーが設定する図形属性で決まり、テーマの色や外観に左右されない。描画領域の背景は白とする。
- `Models`、`ViewModels`、`Controllers` は Material Design に依存させない（`MaterialDesignThemes` の型を参照しない）。Material Design の型やスタイルは、`App.xaml` と `Views` の XAML／カスタムコントロールにとどめる。
- テーマの切り替え（ダークテーマ等）、`DialogHost` や `Snackbar` など、要件にない Material Design 固有の機能は実装しない（YAGNI）。ファイルダイアログとエラー表示のメッセージボックスは標準のものを使う。

## 3. 描画

- 描画領域は `UserControl` とし、その中に `ItemsControl` を置いて図形を描画する。
- `ItemsControl` の `ItemsPanel` は `Canvas` とし、各アイテムの座標（X / Y など）を `ItemContainerStyle` でバインドして自由に配置する。
- 図形の種類ごとに `DataTemplate`（`Line`、`Ellipse`、`Rectangle`、`Polyline`、`Polygon`、`Path` など）を用意する。
- **論理座標系と描画（デバイス）座標系を分ける。**
  - 図形は論理座標で保持する。論理座標から描画座標への変換（拡大率・平行移動）は 1 か所（ビュー変換）に集約する。
  - 論理座標の Y 軸の向きは CAD の慣例に合わせ「上向き」を既定とする（変更する場合は README に理由を記録する）。
  - 線の太さは論理座標系の値とし、拡大縮小に応じて変わる。
  - 選択ハンドル（端点のつまみ）とヒットテストの許容範囲は画面上の一定サイズ（ピクセル）とし、拡大率に左右されないようにする。

## 4. 図形

### 4.1 種類と入力方法

| 図形 | 入力方法 |
|---|---|
| 線分 | 開始点と終了点（クリック 2 回） |
| 円 | 中心点と円周上の 1 点（クリック 2 回） |
| 矩形（傾きのない長方形） | 1 点と対角の点（クリック 2 回） |
| ポリライン | クリックで端点を追加し、ダブルクリックで確定 |
| ポリゴン | クリックで端点を追加し、ダブルクリックで確定 |
| 開いた自由線 | クリックで通過点を追加し、ダブルクリックで確定 |
| 閉じた自由線 | クリックで通過点を追加し、ダブルクリックで確定 |

- ツールバー（またはメニュー）で図形の種類（と選択ツール）を切り替える。
- 入力中は、確定前の図形をプレビュー表示する（ゴムひも表示）。`Esc` で入力を取り消す。
- ダブルクリックで確定するとき、ダブルクリックの 2 回目のクリックで余分な点が増えないようにする。点数が不足する図形（例：ポリラインで 2 点未満）は追加しない。

### 4.2 自由線（スプライン曲線）

- 自由線は、通過点を滑らかに結ぶ **3 次スプライン曲線**とし、実装には**ベジェ曲線**（3 次ベジェの区間列）を用いる。
  - 開いた自由線は端点条件付き（自然スプライン等）、閉じた自由線は周期スプラインとする。
  - 参考実装：https://github.com/Fujiwo/Shos.BezierSamples （まず内容を確認し、使える考え方・アルゴリズムを取り込む。コードをコピーする場合は出典を明記する）。
- 通過点 → ベジェ制御点の変換は `Models` 内の純粋な関数とし、単体テストで検証する（通過点を必ず通ること、閉じた曲線で始点と終点が滑らかにつながることなど）。

### 4.3 属性

- 図形は属性を持つ。属性は `record` 等の不変型で表し、少なくとも次を含む。
  - 線の色
  - 線の太さ（論理座標系）
  - 線種（実線、破線、点線、一点鎖線など）
- 新規図形には「現在の既定属性」を適用する（既定属性は設定ファイルに保存する）。

## 5. 編集機能

### 5.1 選択と変形

- ツールが選択モードのとき、図形をクリックして選択する。何もない場所をクリックすると選択を解除する。複数選択は不要。
- 選択された図形には、端点（制御点）に選択ハンドルを表示する。
  - 端点をドラッグ：その端点を移動する。
  - 端点以外をドラッグ：図形全体を移動する。
  - `Delete` キー：図形を削除する。
- ドラッグ操作は、ドラッグ中はリアルタイムに表示し、**マウスを離した時点で 1 回の Undo 単位**として記録する。

### 5.2 属性（プロパティ）ウィンドウ

- 図形の属性をモードレスに表示・編集する属性ウィンドウを、メニュー（またはツールバー／ショートカット）で表示・非表示を切り替えられるようにする。
- 図形を選択すると、その属性がウィンドウに**リアルタイムに**表示され、その場で編集できる。選択がないときは編集不可の状態にする。
- 属性ウィンドウでの編集も Undo / Redo の対象とする。
- ウィンドウの表示状態と位置は設定として保存し、次回起動時に復元する。

### 5.3 Undo / Redo

- 図形コレクションに `Shos.UndoRedoList` の `UndoRedoObservableCollection<T>` を利用する。
- 図形の追加・削除・移動（全体・端点）・属性変更を、Undo / Redo の対象とする。
- メニュー、ツールバー、ショートカット（`Ctrl+Z` / `Ctrl+Y`）から操作できる。
- Undo / Redo 後も、選択状態と属性ウィンドウの表示が矛盾しないこと。

## 6. 画面の拡大・縮小・スクロール

- `Ctrl + マウスホイール`で、マウスカーソル位置を中心に拡大・縮小する。
- **全体図（全体が収まる表示）より小さくは縮小できない**。
- 拡大表示中は、`Ctrl + 矢印キー`でスクロールする。
- 拡大率とスクロール位置はビュー変換（ViewModel）が保持し、描画領域の View にバインドする。

## 7. クリップボード

| 操作 | 内容 |
|---|---|
| 独自形式のコピー＆ペースト | 選択した図形を独自のクリップボード形式（JSON）でコピーし、ペーストで図形を追加する。ペースト時は少しずらした位置に配置する。`Ctrl+C` / `Ctrl+V` |
| 拡張メタファイル (EMF) へのコピー | 描画内容を EMF としてクリップボードに格納する（Word 等に貼り付け可能なこと） |
| 画像へのコピー | 描画内容を画像（ビットマップ）としてクリップボードに格納する |

- コピー対象は、選択中の図形がなければドキュメント全体とする（独自形式は選択中の図形のみ）。
- EMF は WPF に標準機能がないため、実現方法（`System.Drawing.Imaging.Metafile` の利用、Win32 の `CF_ENHMETAFILE` など）を調査して選び、選択理由を README に記録する。
- ペーストは Undo / Redo の対象とする。

## 8. 保存と設定

- ドキュメントは JSON ファイルにシリアライズする（`System.Text.Json`）。
  - 図形の多態性は `[JsonPolymorphic]` / `[JsonDerivedType]` 等で扱う。
  - ファイルにバージョン番号を持たせる。
  - メニューに「新規」「開く」「名前を付けて保存」「上書き保存」を用意する。変更があるときは、閉じる前に保存を確認する。
- 設定も JSON ファイルで、`%APPDATA%\Shos.WpfCadSample\settings.json` に保存する。設定項目の例：ウィンドウの位置・サイズ、属性ウィンドウの表示状態と位置、既定属性、最近使ったファイル。
- ファイルの読み書きは、UI から切り離したサービスとして実装し、不正・破損ファイルや書き込み失敗は握りつぶさず、ユーザーに分かるエラーとして扱う。

## 9. テスト

- テストプロジェクト（xUnit）を作成し、UI に依存しない部分（Models、ViewModels、Controllers）を単体テストする。
- 最低限、次をテストする。
  - ベジェ変換（通過点・閉曲線）
  - 図形のシリアライズ／デシリアライズの往復
  - 図形の移動・端点移動・ヒットテスト
  - 図形追加の状態遷移（点の追加、ダブルクリックでの確定、`Esc` での取り消し）
  - Undo / Redo（ドラッグ 1 回が 1 単位であること、属性変更）
  - 拡大率の下限（全体図より縮小できないこと）

## 10. 不要な機能（実装しない）

- 印刷機能
- 複数選択、グループ化、スナップ、レイヤー、寸法線、テキスト図形、図形の回転・拡大縮小

## 11. 成果物と完了条件

- ソリューション：既存の `Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx`
  - アプリ本体プロジェクト、テストプロジェクト
- `Source\README.md`：概要、フォルダー／namespace の構成、操作方法（ショートカット一覧を含む）、設計判断（座標系、EMF の方式、Material Design の版・配色と適用範囲、参照した資料と出典）
- 完了条件（すべて満たすこと。満たした根拠として、実行したコマンドと結果を報告する）
  - `dotnet build Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` が警告・エラーなしで成功する（Windows の `dotnet` で実行する）
  - `dotnet test Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx` がすべて成功する
  - コードビハインドに `InitializeComponent()` 以外のロジックがない（`MainWindow` を含む）
  - ルート namespace 直下に型がない（例外は WPF アプリケーションクラス `App` のみ）
  - 要件のすべての図形・編集・拡大縮小・クリップボード・保存機能が、実際の起動で動作する
