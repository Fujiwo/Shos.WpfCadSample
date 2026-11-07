using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

public sealed record LineShape(ShapeId Id, ShapeAttributes Attributes, Point2 Start, Point2 End) : Shape(Id, Attributes)
{
    public override ShapeKind Kind => ShapeKind.Line;

    public override IReadOnlyList<Point2> HandlePoints => [Start, End];

    public override Outline CreateOutline() => new([new OutlineFigure(Start, [new LineSegment(End)], IsClosed: false)]);

    public override Shape MoveHandle(int index, Point2 to)
    {
        ThrowIfNotHandleIndex(index, 2);
        return index == 0 ? this with { Start = to } : this with { End = to };
    }

    public override Shape Translate(Vector2 delta) => this with { Start = Start + delta, End = End + delta };

    protected override IEnumerable<Point2> DefinitionPoints => [Start, End];

    protected override bool SatisfiesKindConstraints() => Start != End;
}
