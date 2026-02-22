using DicomViewer.Core.Services;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Unit tests for DicomFileService.
/// </summary>
public class DicomFileServiceTests
{
    private static readonly string SampleDir = ResolveSampleDir();

    private static string ResolveSampleDir()
    {
        var baseDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "SampleFiles", "DICOM 20251125"));
        var dicomSubDir = Path.Combine(baseDir, "DICOM");
        return Directory.Exists(dicomSubDir) ? dicomSubDir : baseDir;
    }

    private static bool HasSampleFiles()
    {
        return Directory.Exists(SampleDir)
            && Directory.GetFiles(SampleDir).Any(f =>
                !Path.GetExtension(f).Equals(".roi", StringComparison.OrdinalIgnoreCase)
                && !Path.GetExtension(f).Equals(".json", StringComparison.OrdinalIgnoreCase)
                && !Path.GetExtension(f).Equals(".settings", StringComparison.OrdinalIgnoreCase));
    }

    private readonly DicomFileService _sut = new();

    [Fact]
    public void LoadFiles_WithValidDirectory_ReturnsFiles()
    {
        if (!HasSampleFiles()) return;

        // Arrange & Act
        var files = _sut.LoadFiles(SampleDir);

        // Assert
        Assert.NotEmpty(files);
        Assert.All(files, f => Assert.False(string.IsNullOrEmpty(f.FileName)));
    }

    [Fact]
    public void LoadFiles_ExcludesRoiAndSettingsFiles()
    {
        if (!HasSampleFiles()) return;

        var files = _sut.LoadFiles(SampleDir);

        Assert.DoesNotContain(files,
            f => f.FileName.EndsWith(".roi"));
        Assert.DoesNotContain(files,
            f => f.FileName.EndsWith(".settings"));
    }

    [Fact]
    public void LoadFiles_ParsesWidthAndHeight()
    {
        if (!HasSampleFiles()) return;

        var files = _sut.LoadFiles(SampleDir);
        var firstFile = files.First();

        Assert.True(firstFile.Width > 0, "Width should be > 0");
        Assert.True(firstFile.Height > 0, "Height should be > 0");
    }

    [Fact]
    public void LoadFiles_ParsesFrameCount()
    {
        if (!HasSampleFiles()) return;

        var files = _sut.LoadFiles(SampleDir);

        Assert.All(files, f =>
            Assert.True(f.FrameCount >= 1, "FrameCount should be >= 1"));
    }

    [Fact]
    public void LoadFiles_ParsesAcquisitionDateTime()
    {
        if (!HasSampleFiles()) return;

        var files = _sut.LoadFiles(SampleDir);
        var withTime = files.Where(f => f.AcquisitionDateTime.HasValue).ToList();

        Assert.NotEmpty(withTime);
    }

    [Fact]
    public void LoadFiles_ParsesPixelSpacing()
    {
        if (!HasSampleFiles()) return;

        var files = _sut.LoadFiles(SampleDir);
        var withSpacing = files.Where(f => f.PixelSpacing != null).ToList();

        Assert.NotEmpty(withSpacing);
        Assert.All(withSpacing, f =>
        {
            Assert.Equal(2, f.PixelSpacing!.Length);
            Assert.True(f.PixelSpacing[0] > 0);
            Assert.True(f.PixelSpacing[1] > 0);
        });
    }

    [Fact]
    public void LoadFiles_ThrowsForInvalidDirectory()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => _sut.LoadFiles(@"C:\Nonexistent\Path"));
    }

    [Theory]
    [InlineData(@"0.3\0.3", 0.3, 0.3)]
    [InlineData("[0.3, 0.3]", 0.3, 0.3)]
    [InlineData("0.5\\0.5", 0.5, 0.5)]
    [InlineData("1.0", 1.0, 1.0)]
    [InlineData("", null, null)]
    public void ParseSpacingString_ParsesVariousFormats(
        string input, double? expectedRow, double? expectedCol)
    {
        var result = DicomFileService.ParseSpacingString(input);

        if (expectedRow == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.Equal(expectedRow.Value, result![0], 6);
            Assert.Equal(expectedCol!.Value, result[1], 6);
        }
    }
}
