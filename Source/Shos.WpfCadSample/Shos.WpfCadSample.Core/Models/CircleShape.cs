using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

public sealed record CircleShape(ShapeId Id, ShapeAttributes Attributes, Point2 Center, Point2 PointOnCircle) : Shape(Id, Attributes)
{
    public override ShapeKind Kind => ShapeKind.Circle;

    public double Radius => Center.DistanceTo(PointOnCircle);

    public override IReadOnlyList<Point2> HandlePoints => [Center, PointOnCircle];

    public override Outline CreateOutline() => new([OutlineFigure.Circle(Center, Radius)]);

    /// <summary>
    /// 中心（0）を動かすと円全体を平行移動し、円周上の点（1）を動かすと半径が変わる（仕様 5.3）。
    /// </summary>
    public override Shape MoveHandle(int index, Point2 to)
    {
        ThrowIfNotHandleIndex(index, 2);
        return index == 0 ? Translate(to - Center) : this with { PointOnCircle = to };
    }

    public override Shape Translate(Vector2 delta) => this with { Center = Center + delta, PointOnCircle = PointOnCircle + delta };

    protected override IEnumerable<Point2> DefinitionPoints => [Center, PointOnCircle];

    protected override bool SatisfiesKindConstraints() => Radius > 0;
}
