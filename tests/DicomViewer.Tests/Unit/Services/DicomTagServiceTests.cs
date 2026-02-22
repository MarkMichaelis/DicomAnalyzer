using DicomViewer.Core.Services;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Unit tests for DicomTagService.
/// </summary>
public class DicomTagServiceTests
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

    private readonly DicomTagService _sut = new();

    [Fact]
    public void GetTags_InvalidFile_ReturnsErrorMessage()
    {
        var tags = _sut.GetTags(@"C:\nonexistent\file");
        Assert.Single(tags);
        Assert.StartsWith("Error reading tags:", tags[0]);
    }

    [Fact]
    public void GetTags_ValidFile_ReturnsFormattedTags()
    {
        var filePath = Path.Combine(SampleDir, "IM_0001");
        if (!File.Exists(filePath)) return;

        var tags = _sut.GetTags(filePath);

        Assert.NotEmpty(tags);
        // Tags should be formatted as "GGGG,EEEE Name: Value"
        Assert.All(tags, t =>
            Assert.Matches(@"^[0-9A-Fa-f]{4},[0-9A-Fa-f]{4}", t));
    }

    [Fact]
    public void GetTags_ExcludesPixelData()
    {
        var filePath = Path.Combine(SampleDir, "IM_0001");
        if (!File.Exists(filePath)) return;

        var tags = _sut.GetTags(filePath);

        Assert.DoesNotContain(tags,
            t => t.StartsWith("7FE0,0010"));
    }
}
