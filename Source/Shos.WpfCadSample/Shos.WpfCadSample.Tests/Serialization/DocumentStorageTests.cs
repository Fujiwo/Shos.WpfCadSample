using Shos.WpfCadSample.Models;
using Shos.WpfCadSample.Serialization;
using Shos.WpfCadSample.Tests.Support;

namespace Shos.WpfCadSample.Tests.Serialization;

/// <summary>
/// T-STO：<see cref="DocumentStorage"/>（テスト計画書 4.10。一時フォルダーを使う）
/// </summary>
public sealed class DocumentStorageTests : IDisposable
{
    readonly TempFolder folder = new();
    readonly DocumentStorage storage = new();

    public void Dispose() => folder.Dispose();

    static Shape[] OneLine() => [new LineShape(ShapeId.New(), ShapeAttributes.Default, new(0, 0), new(10, 10))];

    // T-STO-01：保存は一時ファイルへ書いてから置き換えるため、一時ファイルが残らないことも確かめる
    [Fact]
    public void SaveThenLoad_RoundTripsAndLeavesNoTemporaryFile()
    {
        var path = folder.Combine("drawing.json");
        var shapes = DocumentSerializerTests.AllKinds();

        storage.Save(path, shapes);

        DocumentSerializerTests.AssertSameShapes(shapes, storage.Load(path));
        Assert.Equal(["drawing.json"], folder.FileNames());
    }

    // T-STO-02
    [Fact]
    public void Save_OverExistingFile_ReplacesContent()
    {
        var path = folder.Combine("drawing.json");
        storage.Save(path, DocumentSerializerTests.AllKinds());

        storage.Save(path, OneLine());

        DocumentSerializerTests.AssertSameShapes(OneLine(), storage.Load(path));
        Assert.Equal(["drawing.json"], folder.FileNames());
    }

    // T-STO-03
    [Fact]
    public void Save_WhenFileIsLockedExclusively_ThrowsAndKeepsOriginal()
    {
        var path = folder.Combine("drawing.json");
        storage.Save(path, DocumentSerializerTests.AllKinds());
        var original = File.ReadAllText(path);

        // 共有なしで開き、ほかのアプリケーションがファイルを排他で開いている状態を作る
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.ThrowsAny<IOException>(() => storage.Save(path, OneLine()));

        Assert.Equal(original, File.ReadAllText(path));
        Assert.Equal(["drawing.json"], folder.FileNames());
    }

    // T-STO-04
    [Fact]
    public void Save_IntoMissingFolder_Throws()
        => Assert.ThrowsAny<IOException>(() => storage.Save(folder.Combine("missing", "drawing.json"), OneLine()));

    // T-STO-05
    [Fact]
    public void Load_MissingFile_ThrowsFileNotFound()
        => Assert.Throws<FileNotFoundException>(() => storage.Load(folder.Combine("missing.json")));

    // T-STO-06
    [Theory]
    [InlineData("{ \"format\": \"Other\" }", DocumentFormatError.Invalid)]
    [InlineData("{ \"format\": \"Shos.WpfCadSample.Document\", \"version\": 2, \"shapes\": [] }", DocumentFormatError.NewerVersion)]
    public void Load_InvalidOrNewerFile_ThrowsDocumentFormatException(string content, DocumentFormatError expected)
    {
        var path = folder.Combine("drawing.json");
        File.WriteAllText(path, content);

        Assert.Equal(expected, Assert.Throws<DocumentFormatException>(() => storage.Load(path)).Error);
    }

    // T-STO-07：読み取り専用のファイルへの書き込みは IOException の派生ではない例外になるため、T-STO-03 とは別に確かめる
    [Fact]
    public void Save_OverReadOnlyFile_ThrowsUnauthorizedAccessAndKeepsOriginal()
    {
        var path = folder.Combine("drawing.json");
        storage.Save(path, DocumentSerializerTests.AllKinds());
        var original = File.ReadAllText(path);
        File.SetAttributes(path, FileAttributes.ReadOnly);

        Assert.Throws<UnauthorizedAccessException>(() => storage.Save(path, OneLine()));

        Assert.Equal(original, File.ReadAllText(path));
        Assert.Equal(["drawing.json"], folder.FileNames());
    }
}
