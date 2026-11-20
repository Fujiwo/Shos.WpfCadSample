本レポジトリは、.github\prompts\create-wpf-cad.prompt.md で示されるように、WPF アプリケーションとして 簡易な CAD を作成することを目的としている。
ただし、.github\prompts\create-wpf-cad.prompt.md を実行する前に、計画書を作成したい。
また、sustainable-code-jp スキルを利用することにしたい。

そのための CLAUDE.md や .github/copilot-instructions.md を作成せよ。

## 要件

- MVC アーキテクチャー (Models, Views, Controllers の各 namespace を持つ)
- MainWindow も Views namespace に入れる
- Models, Views, Controllers のいずれにも属さないクラスも、それぞれ適切な namespace に入れることとし、プロジェクトの namespace の直下には何も置かない
- コードビハインド (*.xaml.cs) にはできるだけコードを書かない
- MVVM (Model - View - ViewModel) アーキテクチャーとする
―View は Model や ViewModel とデータバインドを行う
- View は UserControl として作成し、ItemsControl 要素を置き、そこで図形を描画する
- View には適宜 Dependency Property を用意する
- MainWindow のコードはなるべくシンプルにする
  - 必要に応じて、UserControl やカスタム コントロールを作成する
  - カスタム コントロールの XAML は、カスタム コントロールごとに適切な別ファイルとする
- ItemsControl のレイアウトパネルに Canvas を使用し、各アイテムの座標（X/Y軸など）をバインドして自由に配置
- 図形の種類
  - 線分
   - 開始点と終了点による入力
  - 円
   - 中心点と円周上の1点による入力
  - 矩形 (傾きのない長方形)
   - 1点と対角の点による入力
  - ポリライン
  - ポリゴン
  - 開いた自由線
  - 閉じた自由線
- 図形は属性を持つ
  - 図形の属性は、線の色、線の太さ (論理座標系)、線種など
- クリックによる図形追加
  - クリックで図形の端点などを入力
  - ポリラインとポリゴン、開いた自由線、閉じた自由線は、クリックによる端点追加とし、ダブルクリックで確定
- 自由線は、3次スプライン曲線とし、実装にはベジエ曲線を用いる
  - https://github.com/Fujiwo/Shos.BezierSamples を参考にする
- 図形の座標系は、論理座標系とする
  - 描画座標系とは別
- Json ファイルにシリアライズする
- 設定も Json ファイルで適切なフォルダーに保存する
- class より record や struct、record struct が適切な場合は、そちらを使用する
- C#14/.NET10を使用し、なるべく新たな C# の文法を利用する
- Undo / Redo をサポートする
  - Shos.UndoRedoList の UndoRedoObservableCollection<T> を利用
    - https://github.com/Fujiwo/Shos.UndoRedoList
    - https://www.nuget.org/packages/Shos.UndoRedoList/
- 図形の属性をモードレスに可視化する属性 (プロパティ) ウィンドウが表示・非表示できる
- 図形選択機能
  - クリックで選択
    - 選択された図形に対する処理
      - 端点をドラッグで移動可能
      - 端点以外をドラッグすることで一つの図形全体を移動可能
      - delete キーで削除可能
      - 図形をクリックすると、図形の属性が、属性 (プロパティ) ウィンドウにリアルタイムに表示され、その場で編集可能 (モードレスに)
- 画面拡大機能
  - Control + ホイールで拡大・縮小表示可能 (全体図から更なる縮小は不可)
  - 拡大表示中は、Control + 矢印キーでスクロール
- 編集機能
  - クリップボード
    - 独自形式でのコピー&ペースト
    - 拡張メタファイルへのコピー
    - 画像へのコピー
- 不要な機能
  - 印刷機能

## 補足

- sustainable-code-jp スキルを利用すること
