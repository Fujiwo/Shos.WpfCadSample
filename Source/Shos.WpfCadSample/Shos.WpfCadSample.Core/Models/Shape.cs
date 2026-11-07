using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Models;

/// <summary>
/// 図形（不変）。変更は、新しい図形への置き換えで表す（設計書 DD-2）。種類ごとの違いは、派生の多態で表す（DD-12）。
/// コンストラクターは例外を投げず、正しさは <see cref="IsValid"/> で判定する（読み込み・貼り付け・編集で共通）。
/// </summary>
public abstract record Shape(ShapeId Id, ShapeAttributes Attributes)
{
    public abstract ShapeKind Kind { get; }

    /// <summary>
    /// 選択ハンドルを置く点（矩形は 4 つの角、円は中心と円周上の点、ほかは定義点）。
    /// </summary>
    public abstract IReadOnlyList<Point2> HandlePoints { get; }

    /// <summary>
    /// 外接矩形（線の太さは含めない）。
    /// </summary>
    public Rect2 Bounds => CreateOutline().Bounds();

    /// <summary>
    /// 描画・ヒットテスト・出力に共通の中間表現。
    /// </summary>
    public abstract Outline CreateOutline();

    /// <summary>
    /// <paramref name="index"/> 番目のハンドルを <paramref name="to"/> へ動かした結果の図形（仕様 5.3）。制約の判定はしない。
    /// </summary>
    public abstract Shape MoveHandle(int index, Point2 to);

    public abstract Shape Translate(Vector2 delta);

    /// <summary>
    /// 最小点数を除く制約（仕様 6.3）を満たすか：すべての定義点が座標の範囲内で、種類ごとの制約（点の一致は完全一致。仕様 A-27）を満たす。
    /// 座標の範囲内なら中間表現は有限になる（仕様 A-29）が、防御としてそれも確かめる。制約の判定は、ここだけに置く。
    /// </summary>
    public bool SatisfiesConstraints()
        => DefinitionPoints.All(point => point.IsWithinLimits()) && SatisfiesKindConstraints() && CreateOutline().IsFinite();

    public bool IsValid() => HasMinimumPoints && SatisfiesConstraints() && Attributes.IsValid();

    /// <summary>
    /// 図形を定める点（保存する点）。
    /// </summary>
    protected abstract IEnumerable<Point2> DefinitionPoints { get; }

    /// <summary>
    /// 種類ごとの制約（点の一致、幅・高さ、半径）。
    /// </summary>
    protected abstract bool SatisfiesKindConstraints();

    protected virtual bool HasMinimumPoints => true;

    protected static void ThrowIfNotHandleIndex(int index, int handleCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, handleCount);
    }
}
