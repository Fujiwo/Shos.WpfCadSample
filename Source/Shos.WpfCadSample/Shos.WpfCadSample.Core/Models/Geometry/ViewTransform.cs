namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 論理座標（Y 上向き）と画面座標（Y 下向き、px）の変換。ビュー変換は、ここに集約する（仕様 3.4、設計書 DD-4）。
/// 画面座標 = (TranslateX + Scale·x, TranslateY − Scale·y)。
/// </summary>
public readonly record struct ViewTransform(double Scale, double TranslateX, double TranslateY)
{
    public Point2 ToScreen(Point2 logical)
        => new(TranslateX + Scale * logical.X, TranslateY - Scale * logical.Y);

    public Point2 ToLogical(Point2 screen)
        => new((screen.X - TranslateX) / Scale, (TranslateY - screen.Y) / Scale);
}
