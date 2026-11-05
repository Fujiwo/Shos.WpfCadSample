---
description: AI エージェント向けのリポジトリ指示ファイル (.github/copilot-instructions.md と CLAUDE.md) を作成する
agent: agent
---

# AI エージェント向け指示ファイルの作成

このリポジトリで作業する AI エージェント（GitHub Copilot、Claude Code）向けの指示ファイルを作成せよ。

## 1. 背景

- 本リポジトリの目的は、WPF アプリケーションとして簡易な CAD を作成することである。作成手順は [.github/prompts/create-wpf-cad.prompt.md](./create-wpf-cad.prompt.md) に定義されている。
- 既存のソリューション `Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx` を使用する。アプリケーションとテストのプロジェクトは `Source\Shos.WpfCadSample\` 配下に作成してこのソリューションに追加し、別のソリューションを作成しない。
- ただし、そのプロンプトを実行する**前に、計画書を作成する**運用にしたい。
- コードを書く・変える・評価する作業では、**sustainable-code-jp スキル**を利用する。
  - スキルの実体：`.github\skills\sustainable-code-jp\SKILL.md`（Claude Code 用の同一内容が `.claude\skills\sustainable-code-jp\` にある）。

指示ファイルは、これらの運用をエージェントが毎回の作業で自動的に守るためのものである。

## 2. 事前調査（作成前に必ず行う）

1. 次を確認し、指示ファイルに書く内容の根拠とする。推測で書かない。
   - `.github\prompts\*.prompt.md`（既存プロンプトの内容と、そこで定めた規約）
   - `.github\skills\` と `.claude\skills\` の構成（`sustainable-code-jp`、`genshijin`）と、各 `SKILL.md` の適用範囲（`description`）
   - リポジトリ直下の構成（`Plans\`、`Reviews\`、`Source\`、`.gitignore`）と、既存ソリューション内のプロジェクトの有無
   - `Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx` が存在すること。これを使い、プロジェクトを追加する場合は同じディレクトリ配下に作成してソリューションに登録する。
2. `.github\copilot-instructions.md` と `CLAUDE.md` が**すでに存在するか**を確認する。存在する場合は上書きせず、内容を読んで統合・更新する。
3. プロジェクトの有無を確認してから検証手順を書く。プロジェクトがまだソリューションにない場合は、ビルド／テストを実行できない旨を明記し、実在しないプロジェクトやコマンドを仮定しない。

## 3. 作成するファイル

### 3.1 `.github\copilot-instructions.md`（正本）

GitHub Copilot と、他のエージェントも参照する**正本**とする。次の内容を、簡潔に（目安は 100 行以内）書く。

1. **プロジェクト概要**：何を作るリポジトリか（WPF の簡易 CAD、C# 14 / .NET 10、MVC + MVVM）。詳細な要件は `create-wpf-cad.prompt.md` を参照させ、**要件を複製しない**（二重管理を避ける）。ソリューションとプロジェクトの配置先は `Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx` とその配下であることを明記する。
2. **フォルダー構成と役割**
   - `Plans\`：計画書
   - `Reviews\`：レビュー結果
   - `Progress\`：日付入りの Markdown の進捗記録。現状、文書の承認状態、確定方針、未決事項、履歴、次の作業を記す。変更のたびに新規作成せず既存の記録を更新する。承認の根拠にはしない
   - `Source\Shos.WpfCadSample\`：ソリューションと、その配下のアプリケーション／テストプロジェクト
   - `.github\prompts\`：再利用するプロンプト
   - `.github\skills\`：スキル
3. **作業の進め方（計画書ファースト）**
   - CAD の初期実装および設計・スコープに影響する複数段階の変更では、着手前に `Plans\` に MarkDown の計画書を作成する。
   - 計画書には、目的、スコープ／非スコープ、要件、設計案、段階的な作業と完了条件、テスト方針、リスク、未決事項を含める。日付と内容が分かるファイル名を付ける（例：`Plans\YYYYMMDD-内容.md`）。
   - 計画をユーザーに提示し、承認後に実装する。CAD の初期実装では、承認前に `Source\Shos.WpfCadSample\` のアプリケーションコードを変更しない。
   - `create-wpf-cad.prompt.md` を実行するときは、対応する承認済み計画書があることを確認し、なければ先に計画書を作成して承認を得る。
   - 単純で限定された修正（例：明確なバグ修正、誤字修正、ドキュメントの小変更）には、計画書と事前承認を要求しない。計画から外れる変更が必要になった場合は、計画を更新して再承認を得る。
4. **sustainable-code-jp スキルの利用**
   - コードを書く・変える・評価する作業（設計・実装・命名・テスト・リファクタリング・コードレビュー・バグ修正・性能改善）では、`sustainable-code-jp` スキルを使う。
   - 小さな変更は軽く、判断を伴う作業は `references\` の該当ファイルを読んで適用する、というスキル自身の軽重の基準に従う。
   - 適用しない作業（文書の誤字修正、git 操作のみ、環境構築のみ、設定値の変更のみ、CI/ビルド設定のみ、コードの説明のみ）も、スキルの記述に合わせて書く。
5. **コーディング規約の要点**（`create-wpf-cad.prompt.md` と矛盾させない）
   - 名前空間はルート直下に型を置かない。`Models` / `ViewModels` / `Views` / `Controllers` ほか。唯一の例外は WPF アプリケーションクラス `App`（`App.xaml` / `App.xaml.cs`）で、WPF プロジェクトのルート namespace（`Shos.WpfCadSample`）直下に置く。
   - コードビハインドには `InitializeComponent()` 以外のロジックを書かない。
   - `record` / `struct` / `record struct` を適切に使い、新しい C# の文法を利用する。
   - Undo / Redo は `Shos.UndoRedoList` の `UndoRedoObservableCollection<T>` を使う。
   - UI の外観には Material Design in XAML Toolkit（`MaterialDesignThemes`）を使う。適用は `App.xaml` と `Views` に限り、`Models` / `ViewModels` / `Controllers` は依存させない。描画領域の図形の色はテーマではなく図形属性で決める。
6. **ビルド・テスト・検証**：ソリューションにアプリ／テストプロジェクトが追加された後は、リポジトリルートから `dotnet build Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx` と `dotnet test Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx` を実行する。プロジェクト未追加の間は実行可能と記載せず、未実施の理由を報告する。
7. **やってはいけないこと**：承認済み計画なしに CAD の初期実装や計画対象の大きな変更を始めない。要件にない機能を追加しない（YAGNI）。秘密情報をコミットしない。

8. **プロンプトとレビュー成果物**
   - 再利用するプロンプトを新規作成するときは `.github\prompts\` に新しい Markdown ファイル（`.prompt.md`）を作成し、既存のプロンプトを上書きしない。必要に応じて関連文書の参照も更新する。
   - 依頼されたレビューを実施したときは、結果を `Reviews\YYYYMMDD-<対象>-review.md` という新規 Markdown ファイルに記録する。同名ファイルがある場合は上書きせず、識別可能な接尾辞を付ける。
   - レビュー結果には対象範囲、指摘事項（指摘がなければその旨）、関連ファイルと位置を記載する。ユーザーへの報告にはレビュー記録へのリンクを含める。

### 3.2 `CLAUDE.md`（リポジトリ直下）

- Claude Code 用。**内容は複製せず**、正本を取り込む形にする。

  ```markdown
  @.github/copilot-instructions.md
  ```

- そのうえで、Claude Code 固有の補足だけを短く書く（例：スキルは `.claude\skills\` から使われること、`.github\skills\` との内容を同期させること）。補足がなければ取り込みのみでよい。
- 取り込み構文（`@パス`）が使えない場合に備え、正本へのリンクと「必ず読むこと」の一文を書く。

### 3.3 スキルの二重管理への対応

- `.github\skills\` と `.claude\skills\` の `sustainable-code-jp` と `genshijin` を、それぞれディレクトリ全体で比較する。相対パスごとのファイル集合（不足・余分なファイル）と各ファイルのハッシュを照合する。
- 差分があれば内容を同期せず、差分を報告する。ファイル集合とハッシュが一致する場合のみ、両者を同一内容に保つ旨を指示ファイルに書く。

## 4. 記述の方針

- エージェントが毎回読むため、**短く、具体的で、検証可能**に書く。一般論（「きれいなコードを書く」等）は書かない。
- 命令形・箇条書きを使い、1 項目 1 事実にする。
- 他のファイルに書いてある内容は、複製せずリンクで参照する。
- 指示同士が矛盾しないことを確認する（特に `create-wpf-cad.prompt.md` との整合）。
- 新しい規約を勝手に発明しない。ユーザーが述べた運用（計画書ファースト、sustainable-code-jp の利用）と、リポジトリで確認できた事実だけを書く。計画書の命名規則など、必要な既定値を補う場合は、完了報告で「補った点」として明示する。

## 5. 検証と報告

1. 作成したファイルの内容を読み返し、次を確認する。
   - すべてのリンク先・パスが実在し、ソリューションの場所とプロジェクト配置の指示が一致する。
   - `create-wpf-cad.prompt.md` の要件と矛盾していない。
   - ビルド／テストコマンドが既存のソリューションとプロジェクト構成に対応し、プロジェクトがない場合は実行可能と誤記していない。
   - `CLAUDE.md` と `.github\copilot-instructions.md` に重複がない。
2. 次を報告する。
   - 作成・更新したファイル
   - 事前調査で分かった事実（既存ファイルの有無、スキルの差分など）
   - 補った規約と、その理由
   - 未決事項（ユーザーの判断が要るもの）
3. **今回はアプリケーションのコードは書かない**。計画書の作成も行わない（それは次の作業）。
