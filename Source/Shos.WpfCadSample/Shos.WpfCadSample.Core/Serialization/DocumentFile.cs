using Shos.WpfCadSample.Models;

namespace Shos.WpfCadSample.Serialization;

/// <summary>
/// 保存する JSON の全体：形式の識別子、形式のバージョン、図形の列（仕様 6.4、6.6、設計書 5.6）。
/// </summary>
public sealed record DocumentFile(string Format, int Version, IReadOnlyList<Shape> Shapes);
