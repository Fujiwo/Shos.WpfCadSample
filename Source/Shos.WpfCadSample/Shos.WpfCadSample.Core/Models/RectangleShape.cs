using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// 辺が軸に平行な矩形。<see cref="Corner"/> と <see cref="OppositeCorner"/> は対角の 2 点。
/// </summary>
public sealed record RectangleShape(ShapeId Id, ShapeAttributes Attributes, Point2 Corner, Point2 OppositeCorner) : Shape(Id, Attributes)
{
    public override ShapeKind Kind => ShapeKind.Rectangle;

    /// <summary>
    /// 4 つの角を、<see cref="Corner"/> から一周する順に並べる。角 i の対角は角 (i + 2) % 4。
    /// </summary>
    public override IReadOnlyList<Point2> HandlePoints
        => [Corner, new(OppositeCorner.X, Corner.Y), OppositeCorner, new(Corner.X, OppositeCorner.Y)];

    public override Outline CreateOutline()
    {
        var corners = HandlePoints;
        return new([new OutlineFigure(corners[0], [.. corners.Skip(1).Select(corner => new LineSegment(corner))], IsClosed: true)]);
    }

    /// <summary>
    /// 動かした角が <paramref name="to"/> になり、対角は固定される（仕様 5.3）。動かした角の添字は変わらない。
    /// </summary>
    public override Shape MoveHandle(int index, Point2 to)
    {
        ThrowIfNotHandleIndex(index, 4);
        return index switch {
            0 => this with { Corner = to },
            1 => this with { Corner = new(Corner.X, to.Y), OppositeCorner = new(to.X, OppositeCorner.Y) },
            2 => this with { OppositeCorner = to },
            _ => this with { Corner = new(to.X, Corner.Y), OppositeCorner = new(OppositeCorner.X, to.Y) }
        };
    }

    public override Shape Translate(Vector2 delta) => this with { Corner = Corner + delta, OppositeCorner = OppositeCorner + delta };

    protected override IEnumerable<Point2> DefinitionPoints => [Corner, OppositeCorner];

    protected override bool SatisfiesKindConstraints() => Corner.X != OppositeCorner.X && Corner.Y != OppositeCorner.Y;
}
