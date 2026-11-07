namespace Shos.WpfCadSample.Models;

/// <summary>
/// 図形の属性：線の色、線の太さ（論理単位）、線種（仕様 6.2）。
/// </summary>
public sealed record ShapeAttributes(RgbColor Color, double Thickness, LineStyle LineStyle)
{
    public const double MinThickness = 0.5; // 仕様 A-6
    public const double MaxThickness = 50;  // 仕様 A-6

    /// <summary>
    /// 既定属性の初期値：黒、太さ 1.0、実線（仕様 A-21）。
    /// </summary>
    public static ShapeAttributes Default { get; } = new(new RgbColor(0, 0, 0), 1.0, LineStyle.Solid);

    /// <summary>
    /// 線の太さが範囲内で、線種が定義済みの値か。列挙型の変数には未定義の数値も入りうるため、防御として線種も確かめる。
    /// </summary>
    public bool IsValid()
        => Thickness is >= MinThickness and <= MaxThickness && Enum.IsDefined(LineStyle);
}
