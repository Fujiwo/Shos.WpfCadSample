namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// <see cref="OutlineFigure"/> の 1 区間。始点は直前の区間の終点（最初の区間は図形の始点）。
/// </summary>
public abstract record OutlineSegment(Point2 End)
{
    /// <summary>
    /// 区間の外接矩形を決める点（始点を除く）。
    /// </summary>
    internal abstract IEnumerable<Point2> ExtremePoints(Point2 start);

    /// <summary>
    /// 区間を、どの点も区間から <paramref name="tolerance"/> 以内にある線分の列で近似する。
    /// </summary>
    internal abstract IEnumerable<Segment2> Flatten(Point2 start, double tolerance);
}
