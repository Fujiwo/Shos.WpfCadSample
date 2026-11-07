using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Models.Geometry;
using Shos.WpfCadSample.Settings;

namespace Shos.WpfCadSample.Tests.Settings;

/// <summary>
/// T-SET-07：<see cref="WindowPlacement"/>、<see cref="AppSettings"/> の検証（テスト計画書 4.11）。
/// 仮想スクリーンの矩形は画面座標（Y 下向き）で、<see cref="Rect2.Bottom"/> が上端（Y の最小）、<see cref="Rect2.Top"/> が下端（Y の最大）。
/// </summary>
public class WindowPlacementTests
{
    static readonly Rect2 singleScreen = new(0, 0, 1920, 1080);
    static readonly Rect2 twoScreens = new(-1920, 0, 1920, 1080);   // 左側に 2 台目のモニター（負の座標）

    // T-SET-07
    [Theory]
    [InlineData(100.0, 100.0, 800.0, 600.0, true)]      // 完全に内側
    [InlineData(0.0, 0.0, 1920.0, 1080.0, true)]        // ちょうど全体
    [InlineData(1500.0, 100.0, 800.0, 600.0, false)]    // 右にはみ出す
    [InlineData(100.0, -10.0, 800.0, 600.0, false)]     // 上にはみ出す
    [InlineData(3000.0, 100.0, 800.0, 600.0, false)]    // 完全に外側
    [InlineData(-1500.0, 100.0, 800.0, 600.0, false)]   // 存在しない左のモニター
    public void IsWithin_SingleScreen(double left, double top, double width, double height, bool expected)
        => Assert.Equal(expected, new WindowPlacement(left, top, width, height, false).IsWithin(singleScreen));

    // T-SET-07（複数モニター）
    [Theory]
    [InlineData(-1500.0, 100.0, 800.0, 600.0, true)]    // 左のモニター
    [InlineData(-400.0, 100.0, 800.0, 600.0, true)]     // 2 台にまたがる
    [InlineData(-2000.0, 100.0, 800.0, 600.0, false)]   // 左端からはみ出す
    public void IsWithin_VirtualScreenWithNegativeCoordinates(double left, double top, double width, double height, bool expected)
        => Assert.Equal(expected, new WindowPlacement(left, top, width, height, false).IsWithin(twoScreens));

    // T-SET-06（位置・サイズの検証）
    [Theory]
    [InlineData(10.0, 10.0, 800.0, 600.0, true)]
    [InlineData(-500.0, -20.0, 1.0, 1.0, true)]         // 負の位置は有効（判定は IsWithin）
    [InlineData(10.0, 10.0, 0.0, 600.0, false)]
    [InlineData(10.0, 10.0, 800.0, -1.0, false)]
    [InlineData(double.NaN, 10.0, 800.0, 600.0, false)]
    [InlineData(10.0, double.PositiveInfinity, 800.0, 600.0, false)]
    [InlineData(10.0, 10.0, double.PositiveInfinity, 600.0, false)]
    public void IsValid_RequiresFinitePositionAndPositiveSize(double left, double top, double width, double height, bool expected)
        => Assert.Equal(expected, new WindowPlacement(left, top, width, height, true).IsValid());

    [Fact]
    public void Default_IsValidVersionOneWithDefaultAttributes()
    {
        var settings = AppSettings.Default;

        Assert.True(settings.IsValid());
        Assert.Equal(1, settings.Version);
        Assert.Equal(ShapeAttributes.Default, settings.DefaultAttributes);
    }

    [Fact]
    public void IsValid_RejectsInvalidPartsOrVersion()
    {
        var settings = AppSettings.Default;

        Assert.False((settings with { DefaultAttributes = ShapeAttributes.Default with { Thickness = 0.1 } }).IsValid());
        Assert.False((settings with { MainWindow = settings.MainWindow with { Width = 0 } }).IsValid());
        Assert.False((settings with { PropertiesWindow = settings.PropertiesWindow with { Placement = settings.MainWindow with { Height = double.NaN } } }).IsValid());
        Assert.False((settings with { Version = 0 }).IsValid());
        Assert.False((settings with { Version = 2 }).IsValid());
    }
}
