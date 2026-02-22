using DicomViewer.Core.Models;
using DicomViewer.Core.Services;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Unit tests for TimeSeriesGroupingService.
/// </summary>
public class TimeSeriesGroupingServiceTests
{
    private readonly TimeSeriesGroupingService _sut = new();

    [Fact]
    public void GroupFiles_EmptyList_ReturnsEmpty()
    {
        var result = _sut.GroupFiles([]);
        Assert.Empty(result);
    }

    [Fact]
    public void GroupFiles_SingleFile_OnlyOneGroup()
    {
        var files = new List<DicomFileEntry>
        {
            MakeEntry("f1", DateTime.Now)
        };
        var result = _sut.GroupFiles(files);

        Assert.Single(result);
        Assert.Single(result[0].Files);
    }

    [Fact]
    public void GroupFiles_TwoFilesWithinWindow_OneGroup()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        var files = new List<DicomFileEntry>
        {
            MakeEntry("f1", t),
            MakeEntry("f2", t.AddSeconds(30))
        };
        var result = _sut.GroupFiles(files, 60);

        Assert.Single(result);
        Assert.Equal(2, result[0].Files.Count);
    }

    [Fact]
    public void GroupFiles_TwoFilesExceedingWindow_TwoGroups()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        var files = new List<DicomFileEntry>
        {
            MakeEntry("f1", t),
            MakeEntry("f2", t.AddSeconds(120))
        };
        var result = _sut.GroupFiles(files, 60);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GroupFiles_MixedTimesAndNoTimes_SeparatesUnknown()
    {
        var t = new DateTime(2025, 1, 1, 12, 0, 0);
        var files = new List<DicomFileEntry>
        {
            MakeEntry("f1", t),
            MakeEntry("f2", null),
            MakeEntry("f3", null)
        };
        var result = _sut.GroupFiles(files);

        Assert.Equal(2, result.Count);
        Assert.Equal("Unknown Time", result[^1].Label);
        Assert.Equal(2, result[^1].Files.Count);
    }

    [Fact]
    public void GroupFiles_AllNoTimes_SingleUnknownGroup()
    {
        var files = new List<DicomFileEntry>
        {
            MakeEntry("f1", null),
            MakeEntry("f2", null)
        };
        var result = _sut.GroupFiles(files);

        Assert.Single(result);
        Assert.Equal("group_unknown", result[0].GroupId);
    }

    [Fact]
    public void GroupFiles_GroupLabels_ShowTimeRange()
    {
        var t = new DateTime(2025, 1, 1, 14, 30, 15);
        var files = new List<DicomFileEntry>
        {
            MakeEntry("f1", t),
            MakeEntry("f2", t.AddSeconds(10)),
            MakeEntry("f3", t.AddSeconds(20))
        };
        var result = _sut.GroupFiles(files, 60);

        Assert.Single(result);
        Assert.Contains("14:30:15", result[0].Label);
        Assert.Contains("14:30:35", result[0].Label);
    }

    [Fact]
    public void GroupFiles_StableGroupIds()
    {
        var t = new DateTime(2025, 6, 15, 9, 0, 0);
        var files = new List<DicomFileEntry>
        {
            MakeEntry("f1", t),
            MakeEntry("f2", t.AddSeconds(5))
        };

        var result1 = _sut.GroupFiles(files, 60);
        var result2 = _sut.GroupFiles(files, 60);

        Assert.Equal(result1[0].GroupId, result2[0].GroupId);
    }

    [Fact]
    public void GroupFiles_SampleDirectory_60s()
    {
        var sampleDir = ResolveSampleDir();
        if (!HasSampleFiles(sampleDir)) return;

        var fileService = new DicomFileService();
        var files = fileService.LoadFiles(sampleDir);
        var groups = _sut.GroupFiles(files, 60);

        Assert.True(groups.Count > 1,
            $"Expected multiple groups at 60s window, got {groups.Count}");
    }

    [Fact]
    public void GroupFiles_SampleDirectory_600s()
    {
        var sampleDir = ResolveSampleDir();
        if (!HasSampleFiles(sampleDir)) return;

        var fileService = new DicomFileService();
        var files = fileService.LoadFiles(sampleDir);
        var groups60 = _sut.GroupFiles(files, 60);
        var groups600 = _sut.GroupFiles(files, 600);

        Assert.True(groups600.Count <= groups60.Count,
            "600s window should produce same or fewer groups than 60s");
    }

    private static DicomFileEntry MakeEntry(string name, DateTime? dt) =>
        new() { FileName = name, FilePath = name, AcquisitionDateTime = dt };

    private static string ResolveSampleDir()
    {
        var baseDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "SampleFiles", "DICOM 20251125"));
        var dicomSubDir = Path.Combine(baseDir, "DICOM");
        return Directory.Exists(dicomSubDir) ? dicomSubDir : baseDir;
    }

    private static bool HasSampleFiles(string dir)
    {
        return Directory.Exists(dir)
            && Directory.GetFiles(dir).Any(f =>
                !Path.GetExtension(f).Equals(".roi", StringComparison.OrdinalIgnoreCase)
                && !Path.GetExtension(f).Equals(".json", StringComparison.OrdinalIgnoreCase)
                && !Path.GetExtension(f).Equals(".settings", StringComparison.OrdinalIgnoreCase));
    }
}
