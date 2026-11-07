namespace Shos.WpfCadSample.Controllers;

/// <summary>
/// 修飾キー。WPF の ModifierKeys に依存しないための型（設計書 3.3）。
/// </summary>
[Flags]
public enum PointerModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4
}
