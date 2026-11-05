# エージェント指示ファイル Review

- レビュー日：2026-11-06
- 対象：`.github/copilot-instructions.md`、`CLAUDE.md`
- 範囲：指示の明確さ、指示間の矛盾、参照先・リポジトリ実態との整合、実行環境（Windows／WSL の Claude Code）での実行可能性
- 結果：中重要度 2 件、低重要度 4 件の指摘があります。

## 指摘事項

### 1. `CLAUDE.md` のスキル同期指示が、`copilot-instructions.md` の同期ルールと食い違う

- 重要度：中
- 場所：`CLAUDE.md` 3行、`.github/copilot-instructions.md` 44行
- 根拠：`CLAUDE.md` は「`.claude/skills/` と `.github/skills/` の対応ファイルを同期させておく」と無条件に指示しています。一方 `copilot-instructions.md` 44行は「差分があれば黙って同期せず報告する。同期は承認された変更が両方に当てはまる場合に行う」と条件を付けています。
- 影響：Claude Code は `CLAUDE.md` の短い指示を優先して読み、差分を見つけた際に報告せず片方へ揃える（どちらが正か判断せず上書きする）おそれがあります。
- 改善案：`CLAUDE.md` 3行を「`.claude/skills/` は `.github/skills/` の複製である。同期の扱いは `copilot-instructions.md` の Sustainable coding 節に従う」のように、ルール本体への参照だけにしてください。

### 2. ビルド／テストコマンドのパス区切りがシェル依存で、bash（WSL）では壊れる

- 重要度：中
- 場所：`.github/copilot-instructions.md` 58–59行（7、15、16–17行のパス表記も同様）
- 根拠：`dotnet build Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx` は bash ではバックスラッシュがエスケープとして解釈され、`SourceShos.WpfCadSampleShos.WpfCadSample.slnx` という存在しないパスになります。Claude Code は現在 WSL（bash）で動作しており、`CLAUDE.md` 側は `/` 区切りを使っているため表記も揃っていません。また WPF（`net10.0-windows`）のテスト実行は Linux 上の `dotnet` ではできません。
- 影響：エージェントがコマンドを文字どおり実行して失敗するか、WSL 上で検証不能なのに原因を誤って報告するおそれがあります。
- 改善案：コマンドは Windows／bash の両方で通る `/` 区切り（`dotnet build Source/Shos.WpfCadSample/Shos.WpfCadSample.slnx`）にしてください。あわせて「WPF のビルド・テストは Windows 上の `dotnet`（WSL からは `dotnet.exe` 等）で実行し、実行できない環境ではその旨を報告する」と一文追加することを推奨します。

### 3. 33行の「記載すべき項目」がどの文書に適用されるか不明確

- 重要度：低
- 場所：`.github/copilot-instructions.md` 33行
- 根拠：「goal, scope and exclusions, requirements, design, ordered steps and completion criteria, test approach, risks, and open questions を含める」が主語なしで、直前のファイル名規則（仕様書・設計書・計画書すべて）に続いています。仕様書に ordered steps や test approach まで求めるのか、`Plans\` の計画書だけなのかが読み取れません。
- 影響：仕様書・設計書に不要な節が追加されるか、逆に計画書で項目が漏れる解釈の揺れが起きます。
- 改善案：「Each plan in `Plans\` must include …」のように対象を明記してください（仕様書・設計書の必須項目は `create-cad-plan.prompt.md` に委ねる）。

### 4. `Plans\` の説明が実際の用途より狭い

- 重要度：低
- 場所：`.github/copilot-instructions.md` 11行
- 根拠：「Markdown implementation plans」とありますが、28行・31行のとおりテスト計画書や、CAD 以外の設計・スコープ変更の計画も `Plans\` に置きます（現に `Plans/20261105-cad-test-plan.md` が存在）。
- 影響：テスト計画書などの保存先をエージェントが迷う可能性があります（軽微）。
- 改善案：「Markdown implementation, test, and change plans」などに改めてください。

### 5. 「review pending」の表記が状態行の正式文字列と一致しない

- 重要度：低
- 場所：`.github/copilot-instructions.md` 30行
- 根拠：同じ行で状態行を `状態：レビュー待ち` と正式文字列で定義しながら、差し戻し時は英語の「review pending」と書いています。
- 影響：英語表記の状態行を書くエージェントが出ると、`Progress\` の状態の写しや状態行の検索と食い違います。
- 改善案：「Revert … to `状態：レビュー待ち`」と正式文字列で書いてください。

### 6. 複製スキルの所在が `copilot-instructions.md` 単体では分からない

- 重要度：低
- 場所：`.github/copilot-instructions.md` 17行・40行・44行
- 根拠：44行は「mirrored `sustainable-code-jp` or `genshijin` skills」と書きますが、複製先 `.claude/skills/` はこのファイルのどこにも出てこず、`CLAUDE.md` にしか書かれていません。Repository layout（9–17行）にも `.claude\skills\` がありません。また 40行のリンク `skills/sustainable-code-jp/SKILL.md` は `.github/` 基準の相対パスで、`CLAUDE.md` から取り込まれた場合にリポジトリルート基準と誤読される余地があります。
- 影響：GitHub Copilot が 44行の比較対象を特定できず、比較手順を実行できません。
- 改善案：Repository layout に「`.claude\skills\`: Claude Code 用の `.github\skills\` の複製」を追加し、44行で両パスを明記してください。

## 参考（指示ファイルの内容以外）

- 作業ツリー上の両ファイル（および他の 13 ファイル）の変更は、空白・改行コードを無視すると差分がなく、LF → CRLF の改行コード変換だけです。`.gitattributes` がないため、Windows 側と WSL 側のツールが交互に触ると同種の全行差分が今後も生じます。必要であれば `.gitattributes`（例：`* text=auto`）の追加を別途検討してください。

## 確認した事項

- 前回レビュー（`Reviews/20261105-copilot-instructions-review.md`）の指摘 2 件（レビュー時のスキル適用範囲、複製一致の状態依存記述）は、41行・44行で解消されています。
- 参照先 `.github/prompts/create-wpf-cad.prompt.md`、`.github/prompts/create-cad-plan.prompt.md`、`.github/skills/sustainable-code-jp/SKILL.md` は存在します。
- `.claude/skills/` と `.github/skills/` の `sustainable-code-jp`、`genshijin` は、今回の比較（`diff -r`）でファイル集合・内容とも一致していました。
- `App` のルート namespace 例外（48行）、Material Design の適用範囲（52行）は、承認済み設計書 `Documents/20261105-cad-architecture.md`（DD-9、DD-14）と整合しています。
- `CLAUDE.md` の `@` インポートと、インポート不可時のフォールバック指示（4行）は妥当です。
