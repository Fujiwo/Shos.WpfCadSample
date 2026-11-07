namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 拡大縮小とスクロールの規則（仕様 5.6、設計書 5.2）。表示の縮尺と中心を保ち、ビュー変換 <see cref="Transform"/> を与える。
/// 状態の持ち主は DrawingViewModel で、ここは規則だけを持つ（設計書 DD-15）。
/// </summary>
public sealed class Viewport
{
    const double zoomStepRatio = 1.2;   // ホイール 1 ノッチの倍率（仕様 A-7）
    const double maxZoomRatio = 32;     // 拡大の上限（全体図の 32 倍。仕様 A-7）

    // 拡大・縮小を往復したときの丸め誤差で、全体図の表示を拡大中と取り違えないための相対的な許容差
    const double fitTolerance = 1e-9;

    // 論理領域は固定（仕様 A-1。設定やファイルでは変更しない）
    static readonly Rect2 logicalArea = new(0, 0, 1000, 700);

    // ビューの大きさ（px）。SetViewSize が呼ばれるまでは、論理領域を等倍で表示する大きさとする
    double viewWidth = logicalArea.Width;
    double viewHeight = logicalArea.Height;

    public Viewport() => Reset();

    public Rect2 LogicalArea => logicalArea;

    /// <summary>
    /// 全体図：論理領域と、すべての図形の外接矩形との和集合。
    /// </summary>
    public Rect2 Overview { get; private set; } = logicalArea;

    /// <summary>
    /// 1 論理単位あたりの画面上の大きさ（px）。<see cref="FitScale"/> 以上、その 32 倍以下。
    /// </summary>
    public double Scale { get; private set; }

    /// <summary>
    /// 画面の中央に表示する論理座標。
    /// </summary>
    public Point2 Center { get; private set; }

    /// <summary>
    /// 全体図がビューに収まる縮尺（拡大率 100%）。
    /// </summary>
    public double FitScale => Math.Min(viewWidth / Overview.Width, viewHeight / Overview.Height);

    public double ZoomPercent => Scale / FitScale * 100;

    // Center が画面の中央（viewWidth / 2, viewHeight / 2）に来る平行移動。Y は上下が反転するため符号が逆になる
    public ViewTransform Transform => new(Scale, viewWidth / 2 - Scale * Center.X, viewHeight / 2 + Scale * Center.Y);

    double MaxScale => FitScale * maxZoomRatio;

    bool IsShowingOverview => Scale <= FitScale * (1 + fitTolerance);

    /// <summary>
    /// ビューの大きさ（px）を変える。全体図の表示中は、全体図に合わせ直す。
    /// 大きさが正の有限値でないとき（最小化など）は、直前の表示を保つ。
    /// </summary>
    public void SetViewSize(double width, double height)
    {
        if (!IsPositiveFinite(width) || !IsPositiveFinite(height))
            return;

        var wasShowingOverview = IsShowingOverview;
        (viewWidth, viewHeight) = (width, height);
        Readjust(wasShowingOverview);
    }

    /// <summary>
    /// 図形の外接矩形から全体図を求め直す。図形がなければ <paramref name="shapesBounds"/> は null。
    /// 全体図の表示中は新しい全体図に合わせ直し、拡大中は縮尺と中心を保つ（範囲を外れる分だけ補正する）。
    /// </summary>
    public void UpdateOverview(Rect2? shapesBounds)
    {
        var wasShowingOverview = IsShowingOverview;
        Overview = shapesBounds is { } bounds ? logicalArea.Union(bounds) : logicalArea;
        Readjust(wasShowingOverview);
    }

    /// <summary>
    /// 画面上の <paramref name="screenPoint"/> にある論理座標が動かないように、ノッチ数だけ拡大（正）・縮小（負）する。
    /// </summary>
    public void ZoomAt(Point2 screenPoint, int notches)
    {
        var anchor = Transform.ToLogical(screenPoint);
        Scale = Math.Clamp(Scale * Math.Pow(zoomStepRatio, notches), FitScale, MaxScale);
        // 新しい縮尺で anchor が同じ画面位置に来るよう、中心を逆算する（ToScreen(anchor) = screenPoint を Center について解く）
        Center = new(anchor.X - (screenPoint.X - viewWidth / 2) / Scale,
                     anchor.Y + (screenPoint.Y - viewHeight / 2) / Scale);
        KeepVisibleRangeInsideOverview();
    }

    /// <summary>
    /// 表示範囲を、画面上の移動量（px。右向き・下向きが正）だけ動かす。全体図の外へは出ない。
    /// </summary>
    public void Scroll(double dxPixels, double dyPixels)
    {
        // 画面の下向きは論理座標の Y の負の向きのため、Y だけ符号を反転する
        Center = new(Center.X + dxPixels / Scale, Center.Y - dyPixels / Scale);
        KeepVisibleRangeInsideOverview();
    }

    /// <summary>
    /// 全体図の表示（拡大率 100%、全体図を中央に表示）に戻す。
    /// </summary>
    public void Reset()
    {
        Scale = FitScale;
        Center = Overview.Center;
    }

    void Readjust(bool wasShowingOverview)
    {
        if (wasShowingOverview) {
            Reset();
            return;
        }
        Scale = Math.Clamp(Scale, FitScale, MaxScale);
        KeepVisibleRangeInsideOverview();
    }

    // 表示範囲が全体図より小さい向きは全体図の内側に寄せ、全体図以上の向き（縦横比の違う余白）は中央に置く
    void KeepVisibleRangeInsideOverview()
        => Center = new(CenterWithin(Center.X, viewWidth / Scale, Overview.Left, Overview.Right),
                        CenterWithin(Center.Y, viewHeight / Scale, Overview.Bottom, Overview.Top));

    static double CenterWithin(double center, double visibleLength, double min, double max)
        => visibleLength >= max - min
               ? (min + max) / 2
               : Math.Clamp(center, min + visibleLength / 2, max - visibleLength / 2);

    static bool IsPositiveFinite(double value) => double.IsFinite(value) && value > 0;
}
