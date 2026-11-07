namespace Shos.WpfCadSample.Models;

/// <summary>
/// 不透明な RGB の色。ファイルと画面の入力では <c>#RRGGBB</c> で表す（仕様 6.2）。
/// </summary>
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    const int hexTextLength = 7;    // "#RRGGBB"

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>
    /// <c>#RRGGBB</c>（16 進。大文字・小文字を問わない）を読む。空白・符号などを含む文字列は受け付けない。
    /// </summary>
    public static bool TryParse(string? text, out RgbColor color)
    {
        color = default;
        if (text is not { Length: hexTextLength } || text[0] != '#' || !text[1..].All(char.IsAsciiHexDigit))
            return false;

        color = new(Component(text, 1), Component(text, 3), Component(text, 5));
        return true;
    }

    static byte Component(string text, int start) => Convert.ToByte(text.Substring(start, 2), 16);
}
