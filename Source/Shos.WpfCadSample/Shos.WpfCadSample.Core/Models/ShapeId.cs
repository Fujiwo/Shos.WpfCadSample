namespace Shos.WpfCadSample.Models;

/// <summary>
/// 図形の識別子（保存しない）。置き換え（<c>with</c>）で引き継がれ、選択の維持の判定に使う（設計書 DD-11）。
/// </summary>
public readonly record struct ShapeId(Guid Value)
{
    public static ShapeId New() => new(Guid.NewGuid());
}
