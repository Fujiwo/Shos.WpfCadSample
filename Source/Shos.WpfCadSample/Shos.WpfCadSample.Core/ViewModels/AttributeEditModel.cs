using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// 属性の編集部品（RGB のスライダー、16 進、太さ、線種）の状態（仕様 4.5、5.4、設計書 5.4）。
/// いずれかを編集すると他も連動し、有効な値になったときだけ <see cref="Attributes"/> を変えて通知する。
/// 不正な入力は反映せず、<see cref="RevertInvalidInput"/>（フォーカスを離れたときなど）で直前の有効値へ戻す。
/// </summary>
/// <param name="changed">有効な属性に変わったときに呼ばれる。</param>
public sealed class AttributeEditModel(Action<ShapeAttributes> changed) : ObservableObject
{
    ShapeAttributes attributes = ShapeAttributes.Default;
    string hex = ShapeAttributes.Default.Color.ToHex();
    string thicknessText = FormatThickness(ShapeAttributes.Default.Thickness);
    bool isHexValid = true;
    bool isThicknessValid = true;

    public static IReadOnlyList<LineStyle> LineStyles { get; } = Enum.GetValues<LineStyle>();

    /// <summary>
    /// 現在の有効な属性。
    /// </summary>
    public ShapeAttributes Attributes => attributes;

    public byte Red {
        get => attributes.Color.R;
        set => ApplyColor(attributes.Color with { R = value });
    }

    public byte Green {
        get => attributes.Color.G;
        set => ApplyColor(attributes.Color with { G = value });
    }

    public byte Blue {
        get => attributes.Color.B;
        set => ApplyColor(attributes.Color with { B = value });
    }

    /// <summary>
    /// <c>#RRGGBB</c> の入力。入力中の文字列はそのまま保ち、有効なときだけ反映する。
    /// </summary>
    public string Hex {
        get => hex;
        set {
            if (!SetProperty(ref hex, value))
                return;
            IsHexValid = RgbColor.TryParse(value, out var color);
            if (IsHexValid)
                ApplyColor(color);
        }
    }

    /// <summary>
    /// 太さの入力（論理単位。範囲は仕様 A-6）。入力中の文字列はそのまま保ち、有効なときだけ反映する。
    /// </summary>
    public string ThicknessText {
        get => thicknessText;
        set {
            if (!SetProperty(ref thicknessText, value))
                return;
            IsThicknessValid = TryParseThickness(value, out var thickness);
            if (IsThicknessValid)
                Apply(attributes with { Thickness = thickness });
        }
    }

    public LineStyle LineStyle {
        get => attributes.LineStyle;
        set => Apply(attributes with { LineStyle = value });
    }

    public bool IsHexValid {
        get => isHexValid;
        private set => SetProperty(ref isHexValid, value);
    }

    public bool IsThicknessValid {
        get => isThicknessValid;
        private set => SetProperty(ref isThicknessValid, value);
    }

    /// <summary>
    /// 表示を属性に合わせる（選択の変更、Undo / Redo、設定の読み込み）。変更の通知（changed）はしない。
    /// </summary>
    public void Load(ShapeAttributes newAttributes)
    {
        SetAttributes(newAttributes);
        ResetTexts();
    }

    /// <summary>
    /// 不正なままの入力を、直前の有効値へ戻す。
    /// </summary>
    public void RevertInvalidInput()
    {
        if (!IsHexValid || !IsThicknessValid)
            ResetTexts();
    }

    // 16 進の表示を、反映した色の表記にそろえる（不正な入力の後に RGB を編集した場合も、連動して有効な値になる）
    void ApplyColor(RgbColor color)
    {
        Apply(attributes with { Color = color });
        hex = color.ToHex();
        OnPropertyChanged(nameof(Hex));
        IsHexValid = true;
    }

    bool Apply(ShapeAttributes newAttributes)
    {
        if (newAttributes == attributes)
            return false;
        SetAttributes(newAttributes);
        changed(newAttributes);
        return true;
    }

    void SetAttributes(ShapeAttributes newAttributes)
    {
        attributes = newAttributes;
        OnPropertyChanged(nameof(Attributes));
        OnPropertyChanged(nameof(Red));
        OnPropertyChanged(nameof(Green));
        OnPropertyChanged(nameof(Blue));
        OnPropertyChanged(nameof(LineStyle));
    }

    void ResetTexts()
    {
        hex = attributes.Color.ToHex();
        thicknessText = FormatThickness(attributes.Thickness);
        OnPropertyChanged(nameof(Hex));
        OnPropertyChanged(nameof(ThicknessText));
        IsHexValid = true;
        IsThicknessValid = true;
    }

    static bool TryParseThickness(string text, out double thickness)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out thickness) &&
           (ShapeAttributes.Default with { Thickness = thickness }).IsValid();

    static string FormatThickness(double thickness) => thickness.ToString("0.0##", CultureInfo.InvariantCulture);
}
