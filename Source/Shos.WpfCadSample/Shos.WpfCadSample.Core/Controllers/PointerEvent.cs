using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// View（DrawingSurface）がマウス操作から作る、WPF に依存しない入力（設計書 DD-5）。座標は画面座標（px）。
/// </summary>
/// <param name="ClickCount">Down の連続クリック数。2 以上はダブルクリック（点の入力の確定）。</param>
public sealed record PointerEvent(PointerAction Action, Point2 ScreenPoint, int ClickCount = 1, PointerModifiers Modifiers = PointerModifiers.None);
