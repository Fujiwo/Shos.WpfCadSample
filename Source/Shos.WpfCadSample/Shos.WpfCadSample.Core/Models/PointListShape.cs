using System.Collections.Immutable;
using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// 点列で定まる図形（折れ線・多角形・自由線）。ハンドルは各定義点。
/// </summary>
/// <remarks>
/// <see cref="ImmutableArray{T}"/> の record の等価性は参照で比較される。点列の比較は SequenceEqual で行う（設計書 5.1）。
/// </remarks>
public abstract record PointListShape(ShapeId Id, ShapeAttributes Attributes, ImmutableArray<Point2> Points) : Shape(Id, Attributes)
{
    public override IReadOnlyList<Point2> HandlePoints => Points;

    public override Shape MoveHandle(int index, Point2 to)
    {
        ThrowIfNotHandleIndex(index, Points.Length);
        return this with { Points = Points.SetItem(index, to) };
    }

    public override Shape Translate(Vector2 delta) => this with { Points = [.. Points.Select(point => point + delta)] };

    /// <summary>
    /// 最後の点と最初の点を結んで閉じる図形か（多角形・閉じた自由線）。
    /// </summary>
    protected abstract bool IsClosed { get; }

    protected override IEnumerable<Point2> DefinitionPoints => Points;

    protected override bool HasMinimumPoints => Points.Length >= ShapeKindInfo.MinPoints(Kind);

    // 隣り合う点が一致しない。閉じる図形は、最後と最初も隣り合う（2 点以下では、最初の対と同じ対になるため除く）
    protected override bool SatisfiesKindConstraints() => AdjacentPairs().All(pair => pair.First != pair.Second);

    IEnumerable<(Point2 First, Point2 Second)> AdjacentPairs()
    {
        var pairs = Points.Zip(Points.Skip(1));
        return IsClosed && Points.Length > 2 ? pairs.Append((Points[^1], Points[0])) : pairs;
    }

    /// <summary>
    /// 点を直線で結んだ中間表現（閉じる図形は閉じる）。
    /// </summary>
    protected Outline PolylineOutline()
        => new([new OutlineFigure(Points[0], [.. Points.Skip(1).Select(point => new LineSegment(point))], IsClosed)]);
}
