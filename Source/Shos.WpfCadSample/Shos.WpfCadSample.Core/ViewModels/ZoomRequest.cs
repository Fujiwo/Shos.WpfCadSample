using Shos.WpfCadSample.Models.Geometry;

namespace Shos.WpfCadSample.ViewModels;

/// <summary>
/// Ctrl+ホイールによる拡大縮小。<see cref="DrawingViewModel.ZoomCommand"/> の引数。
/// </summary>
/// <param name="ScreenPoint">カーソルの位置（画面座標）。この位置の論理座標が動かない。</param>
/// <param name="Notches">ホイールのノッチ数（拡大は正、縮小は負）。</param>
public readonly record struct ZoomRequest(Point2 ScreenPoint, int Notches);
