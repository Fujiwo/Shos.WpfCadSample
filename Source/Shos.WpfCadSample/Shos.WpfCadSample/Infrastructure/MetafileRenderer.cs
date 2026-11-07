using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;
using DrawingColor = System.Drawing.Color;

namespace Shos.WpfCadSample.Infrastructure;

/// <summary>
/// 図形を GDI+ で EMF と PNG 画像に描く（設計書 DD-6、5.5）。範囲と縮尺は <see cref="ExportFrame"/> の結果だけを使う。
/// 図形は <see cref="Shape.CreateOutline"/>（直線と 3 次ベジェ）から描くため、画面の描画と同じ形になる。
/// </summary>
public static class MetafileRenderer
{
    const float dpi = 96;   // 1 論理単位 = 1 px（仕様 A-10）

    /// <summary>
    /// EMF（EMF+ と EMF の両方の記録。背景は透明）を作る。呼び出し側が破棄する。
    /// </summary>
    public static Metafile CreateMetafile(IReadOnlyList<Shape> shapes)
    {
        var frame = ExportFrame.For(shapes);
        using var reference = Graphics.FromHwnd(IntPtr.Zero);
        var referenceHdc = reference.GetHdc();
        Metafile metafile;
        try {
            metafile = new Metafile(referenceHdc, new RectangleF(0, 0, frame.PixelWidth, frame.PixelHeight), MetafileFrameUnit.Pixel, EmfType.EmfPlusDual);
        } finally {
            reference.ReleaseHdc(referenceHdc);
        }
        using (var graphics = Graphics.FromImage(metafile))
            Draw(graphics, shapes, frame);
        return metafile;
    }

    /// <summary>
    /// PNG 画像（32 bit ARGB、背景は白、96 DPI。設計書 DA-6）のバイト列を作る。
    /// </summary>
    public static byte[] CreatePng(IReadOnlyList<Shape> shapes)
    {
        var frame = ExportFrame.For(shapes);
        using var bitmap = new Bitmap(frame.PixelWidth, frame.PixelHeight, PixelFormat.Format32bppArgb);
        bitmap.SetResolution(dpi, dpi);
        using (var graphics = Graphics.FromImage(bitmap)) {
            graphics.Clear(DrawingColor.White);
            Draw(graphics, shapes, frame);
        }
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    static void Draw(Graphics graphics, IReadOnlyList<Shape> shapes, ExportFrame frame)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PageUnit = GraphicsUnit.Pixel;
        foreach (var shape in shapes) {
            using var path = CreatePath(shape.CreateOutline(), frame);
            using var pen = CreatePen(shape.Attributes, frame.Scale);
            graphics.DrawPath(pen, path);
        }
    }

    // 論理座標（Y 上向き）を、出力範囲の左上を原点とするピクセル座標（Y 下向き）へ変換して、パスを作る。
    // 変換は倍精度で行ってから単精度にする（座標が大きいときの、GDI+ の変換行列の単精度の誤差を避ける）
    static GraphicsPath CreatePath(Outline outline, ExportFrame frame)
    {
        PointF ToPixel(Point2 point)
            => new((float)((point.X - frame.Area.Left) * frame.Scale), (float)((frame.Area.Top - point.Y) * frame.Scale));

        var path = new GraphicsPath();
        foreach (var figure in outline.Figures) {
            path.StartFigure();
            var current = figure.Start;
            foreach (var segment in figure.Segments) {
                switch (segment) {
                    case CubicBezierSegment bezier:
                        path.AddBezier(ToPixel(current), ToPixel(bezier.Control1), ToPixel(bezier.Control2), ToPixel(bezier.End));
                        break;
                    default:
                        path.AddLine(ToPixel(current), ToPixel(segment.End));
                        break;
                }
                current = segment.End;
            }
            if (figure.IsClosed)
                path.CloseFigure();
        }
        return path;
    }

    // 線の太さも同じ縮尺にする（仕様 A-24）。破線のパターンは太さに対する倍率で、画面と同じ（設計書 DA-5）
    static Pen CreatePen(ShapeAttributes attributes, double scale)
    {
        var color = attributes.Color;
        var pen = new Pen(DrawingColor.FromArgb(color.R, color.G, color.B), (float)(attributes.Thickness * scale)) {
            LineJoin = LineJoin.Round
        };
        var pattern = attributes.LineStyle.DashPattern();
        if (pattern.Count > 0)
            pen.DashPattern = [.. pattern.Select(length => (float)length)];
        return pen;
    }
}
