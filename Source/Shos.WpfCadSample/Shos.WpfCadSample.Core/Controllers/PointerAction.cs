namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// マウス操作の種類（設計書 5.3）。CaptureLost は、他のウィンドウへの切り替えなどで操作が途切れたことを表す（仕様 A-26）。
/// </summary>
public enum PointerAction
{
    Down,
    Move,
    Up,
    CaptureLost
}
