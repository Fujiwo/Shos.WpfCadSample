namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// EMF・画像の出力範囲（論理座標）と縮尺（1 論理単位あたりのピクセル数）、ピクセル寸法（仕様 5.7、A-10、A-24、設計書 DD-6）。
/// MetafileRenderer は、この結果だけを使って描く。
/// </summary>
public sealed record ExportFrame(Rect2 Area, double Scale, int PixelWidth, int PixelHeight)
{
    public const int MaxPixelSide = 10_000;     // 各辺のピクセル数の上限（仕様 A-24）
    public const int MaxPixelCount = 4_000_000; // 画素数の上限（仕様 A-24）

    // 浮動小数点の丸めで切り上げが上限を超えたときに、縮尺をわずかに下げる比
    const double roundingMargin = 1 - 1e-12;

    /// <summary>
    /// 範囲は図形の外接矩形に線の太さの半分を加えた範囲の和。縮尺は 1（1 論理単位 = 1 px）から始め、
    /// 切り上げたピクセル寸法の各辺が <see cref="MaxPixelSide"/> 以下、画素数が <see cref="MaxPixelCount"/> 以下になるまで、縦横比を保って縮小する。
    /// </summary>
    public static ExportFrame For(IEnumerable<Shape> shapes)
    {
        var areas = shapes.Select(shape => shape.Bounds.Inflate(shape.Attributes.Thickness / 2)).ToList();
        if (areas.Count == 0)
            throw new ArgumentException("At least one shape is required.", nameof(shapes));

        var area = areas.Aggregate((union, next) => union.Union(next));
        var scale = FittingScale(area.Width, area.Height);
        return new(area, scale, Pixels(area.Width * scale), Pixels(area.Height * scale));
    }

    static double FittingScale(double width, double height)
    {
        if (Fits(width, height, 1))
            return 1;

        // 2 つの上限から見積もった縮尺から始め、切り上げの誤差で収まらない場合だけ、わずかずつ下げる
        var scale = Math.Min(MaxPixelSide / Math.Max(width, height), PixelCountScale(width, height));
        while (!Fits(width, height, scale))
            scale *= roundingMargin;
        return scale;
    }

    // 切り上げで各辺が 1 px 未満増えることを見込み、(w·s + 1)(h·s + 1) ≤ MaxPixelCount を満たす最大の s
    static double PixelCountScale(double width, double height)
    {
        // 展開すると w·h·s² + (w + h)·s + 1 − MaxPixelCount ≤ 0。2 次方程式の正の根を解の公式で求める
        double a = width * height, b = width + height, c = 1.0 - MaxPixelCount;
        return (-b + Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
    }

    static bool Fits(double width, double height, double scale)
    {
        int pixelWidth = Pixels(width * scale), pixelHeight = Pixels(height * scale);
        return pixelWidth <= MaxPixelSide && pixelHeight <= MaxPixelSide && (long)pixelWidth * pixelHeight <= MaxPixelCount;
    }

    static int Pixels(double length) => (int)Math.Ceiling(length);
}
