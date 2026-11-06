# 初期プロジェクトの確認

- レビュー日：2026-11-06
- 対象：ユーザーが追加した初期プロジェクト（コミット `93d63bf`）。`Source/Shos.WpfCadSample/` の `Shos.WpfCadSample.slnx`、`Shos.WpfCadSample.Core`、`Shos.WpfCadSample.Tests`、`Shos.WpfCadSample`（WPF）
- 観点：承認済みの[設計書](../Documents/20261105-cad-architecture.md)（3.1、3.2、DD-10、DD-14）、[実装計画書](../Plans/20261105-cad-implementation-plan.md)の I-1、[テスト計画書](../Plans/20261105-cad-test-plan.md)、リポジトリ指示（[copilot-instructions.md](../.github/copilot-instructions.md)）との整合
- 方法：ファイルの内容の確認。プロジェクトとビルドの設定が中心のため、`sustainable-code-jp` スキルは使っていない。
- 検証：未実施。このセッションでは NuGet（api.nuget.org）への接続が権限で拒否され、Windows の `dotnet.exe` もサンドボックスから起動できないため、復元・ビルド・テストを実行できなかった。パッケージの版が最新の安定版かどうかも確認できていない。
- 結果：中程度の指摘が 4 件、低程度の指摘が 5 件あります。3 プロジェクトの構成、参照関係（WPF → Core、Tests → Core）、ターゲット（Core と Tests は `net10.0`、WPF は `net10.0-windows`）、`App` のルート namespace 直下への配置、`.vs` と `*.user` の除外（`.gitignore`）は、設計どおりです。

## 指摘事項

| # | 重要度 | 場所 | 指摘 | 根拠 | 改善案 |
|---|---|---|---|---|---|
| 1 | 中 | `Shos.WpfCadSample/Views/MainWindow.xaml`（`x:Class`）、`MainWindow.xaml.cs`（namespace） | `MainWindow` がルート namespace `Shos.WpfCadSample` 直下にある | ルート直下に置けるのは `App` だけ（リポジトリ指示、設計書 DD-14、テスト計画書 S-01）。`MainWindow` は `Views` に置く（設計書 3.2） | `x:Class` を `Shos.WpfCadSample.Views.MainWindow` に、コードビハインドの namespace を `Shos.WpfCadSample.Views` にする |
| 2 | 中 | 3 プロジェクト（`Directory.Build.props` がない） | 共通設定が未作成で、`Core` の既定のルート namespace が `Shos.WpfCadSample.Core` になる | 実装計画書 I-1 は `Directory.Build.props` に `Nullable=enable`、`TreatWarningsAsErrors=true`、`RootNamespace=Shos.WpfCadSample` を置くとしている。設計書 3.2 の namespace は `Shos.WpfCadSample.Models` などで、`.Core` を含まない | `Source/Shos.WpfCadSample/Directory.Build.props` を追加し、各プロジェクトの重複する設定（`Nullable`）を削る |
| 3 | 中 | `Shos.WpfCadSample.csproj`、`Shos.WpfCadSample.Core.csproj` | `CommunityToolkit.Mvvm` が WPF プロジェクトにあり、`Core` にない | 設計書 DD-10、3.1：`CommunityToolkit.Mvvm` は `Core`（`ViewModels`）で使う。WPF 側に置くと `Core` の ViewModel が使えない | `PackageReference` を `Core` へ移す（WPF は `Core` 経由で参照できる） |
| 4 | 中 | `Shos.WpfCadSample.Core.csproj` の `<Folder Include="Geometry\" />` | `Geometry` が `Models` の外にある | 設計書 3.2：namespace は `Shos.WpfCadSample.Models.Geometry`。フォルダーと namespace を一致させる（設計書 3.1） | `Models\Geometry\` にする |
| 5 | 低 | `Shos.WpfCadSample.Core/Class1.cs` | 仮のクラスが `Shos.WpfCadSample.Core` namespace にある | 設計書 3.2 にない namespace で、テスト計画書 T-ARC-01（`Core` の型が設計書の namespace に属する）で失敗する | I-1 の T-ARC の実装時に削除する |
| 6 | 低 | `Shos.WpfCadSample.Tests/UnitTest1.cs` | 中身のない仮のテスト | テスト計画書 T-ARC-03 は「1 件以上のテストが実行・成功する」だが、空のテストは何も確かめていない | T-ARC-01〜04 を実装したら削除する |
| 7 | 低 | `Shos.WpfCadSample.Tests.csproj` の `coverlet.collector` | 計画にないパッケージ | テスト計画書 1 章：コードカバレッジの計測ツールは対象外。実装計画書 4 章：計画外のパッケージは理由を README に記録する | 削除する（使う場合は README に理由を記録する） |
| 8 | 低 | `MainWindow.xaml.cs` の `using` | 使っていない `using` が 10 行ある | 既定では警告にならないが、コードビハインドを `InitializeComponent()` だけにする方針（リポジトリ指示）の確認（S-02）で読みにくい | 不要な `using` を削る（`ImplicitUsings` で足りる） |
| 9 | 低 | `Shos.WpfCadSample.slnx` の `<Folder Name="/Documents/" />` | 中身のないソリューションフォルダー | 計画にない。害はない | 意図がなければ削除する |

## テスト計画書への影響（参考）

- `AssemblyInfo.cs` は namespace 宣言を持たない（アセンブリ属性だけで、型はない）。テスト計画書 付録 A の補足スクリプト（namespace を宣言していない `.cs` ファイルの一覧が空であること）は、このファイルを誤って検出する。付録 A に `AssemblyInfo.cs` の除外を加える必要がある（テスト計画書の変更のため、再承認が必要）。

## I-1 の残りの作業（実装計画書）

- `Directory.Build.props`（指摘 2）と、`Views`・`Infrastructure` などのフォルダー
- `Source/README.md` の骨子
- 進捗記録の「実装の進捗」への、テスト群ごとの実装状況の表
- パッケージの版の確認（最新の安定版。P-1）
- 試作：R-1（EMF）、R-2（`System.Drawing.Common`）、R-6（`CommunityToolkit.Mvvm` と C# 14）、R-8（`slnx` 全体の `dotnet test`）
- T-ARC-01〜04 の実装と、Windows の `dotnet` でのビルド・テスト
