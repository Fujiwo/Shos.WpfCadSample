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
    └─ Architecture\            … Shos.WpfCadSample.Tests.Architecture
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

## 操作方法

（段階 I-10 で記載する：ショートカットの一覧を含む）

## 設計判断

（段階 I-10 で記載する：座標系、EMF の方式、Material Design の版・配色と適用範囲、参照した資料と出典）
