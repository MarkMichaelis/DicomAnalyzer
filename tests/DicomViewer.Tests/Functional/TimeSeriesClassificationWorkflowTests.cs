using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests.Functional;

/// <summary>
/// Functional tests validating the time-series grouping and classification
/// workflow: load files, classify, group, and verify group composition.
/// </summary>
public class TimeSeriesClassificationWorkflowTests
{
    [Fact]
    public void Workflow_GroupAndClassify_SyntheticData()
    {
        // Arrange: Create synthetic DICOM file entries
        var baseTime = new DateTime(2025, 6, 15, 14, 0, 0);
        var files = new List<DicomFileEntry>
        {
            // Group 1: 3 files within 30s, CEUS spacing
            new() { FileName = "IM_0001", FilePath = "IM_0001",
                AcquisitionDateTime = baseTime,
                PixelSpacing = [0.5, 0.5] },
            new() { FileName = "IM_0002", FilePath = "IM_0002",
                AcquisitionDateTime = baseTime.AddSeconds(10),
                PixelSpacing = [0.5, 0.5] },
            new() { FileName = "IM_0003", FilePath = "IM_0003",
                AcquisitionDateTime = baseTime.AddSeconds(20),
                PixelSpacing = [0.3, 0.3] },

            // Group 2: 2 files starting 5 minutes later
            new() { FileName = "IM_0010", FilePath = "IM_0010",
                AcquisitionDateTime = baseTime.AddMinutes(5),
                PixelSpacing = [0.3, 0.3] },
            new() { FileName = "IM_0011", FilePath = "IM_0011",
                AcquisitionDateTime = baseTime.AddMinutes(5).AddSeconds(15),
                PixelSpacing = [0.48, 0.48] },

            // Unknown time group
            new() { FileName = "IM_UNKNOWN", FilePath = "IM_UNKNOWN",
                AcquisitionDateTime = null,
                PixelSpacing = [0.5, 0.5] }
        };

        // Act: Group files
        var groupingService = new TimeSeriesGroupingService();
        var groups = groupingService.GroupFiles(files, 60);

        // Assert: Should produce 3 groups (2 timed + 1 unknown)
        Assert.Equal(3, groups.Count);

        // Group 1
        Assert.Equal(3, groups[0].Files.Count);
        Assert.Contains("14:00:00", groups[0].Label);
        Assert.Contains("14:00:20", groups[0].Label);

        // Group 2
        Assert.Equal(2, groups[1].Files.Count);
        Assert.Contains("14:05:00", groups[1].Label);

        // Unknown group
        Assert.Equal("Unknown Time", groups[2].Label);
        Assert.Single(groups[2].Files);

        // Act: Classify each file
        var classificationService = new ClassificationService();
        foreach (var file in files)
        {
            file.Classification = classificationService.Classify(file.PixelSpacing);
        }

        // Assert: IM_0001 and IM_0002 are CEUS (0.5 spacing)
        Assert.Equal(FileClassification.CEUS, files[0].Classification);
        Assert.Equal(FileClassification.CEUS, files[1].Classification);

        // Assert: IM_0003 and IM_0010 are SHI (0.3 spacing)
        Assert.Equal(FileClassification.SHI, files[2].Classification);
        Assert.Equal(FileClassification.SHI, files[3].Classification);

        // Assert: IM_0011 is CEUS (0.48, closer to 0.5)
        Assert.Equal(FileClassification.CEUS, files[4].Classification);
    }

    [Fact]
    public void Workflow_ChangeTimeWindow_AffectsGroupCount()
    {
        var baseTime = new DateTime(2025, 6, 15, 14, 0, 0);
        var files = new List<DicomFileEntry>
        {
            new() { FileName = "f1", FilePath = "f1",
                AcquisitionDateTime = baseTime },
            new() { FileName = "f2", FilePath = "f2",
                AcquisitionDateTime = baseTime.AddSeconds(30) },
            new() { FileName = "f3", FilePath = "f3",
                AcquisitionDateTime = baseTime.AddSeconds(100) }
        };

        var service = new TimeSeriesGroupingService();

        // 60s window: f1 + f2 together (30s gap), f3 separate (70s gap from f2)
        var groups60 = service.GroupFiles(files, 60);
        Assert.Equal(2, groups60.Count);
        Assert.Equal(2, groups60[0].Files.Count);
        Assert.Single(groups60[1].Files);

        // 120s window: all in one group (gaps are 30s and 70s, both < 120s)
        var groups120 = service.GroupFiles(files, 120);
        Assert.Single(groups120);
        Assert.Equal(3, groups120[0].Files.Count);

        // 20s window: f1 alone, f2 alone (30s gap > 20s), f3 alone (70s gap > 20s)
        var groups20 = service.GroupFiles(files, 20);
        Assert.Equal(3, groups20.Count);
    }

    [Fact]
    public void Workflow_FilterGroupedFiles()
    {
        // Arrange: Groups with files, apply filter
        var files = new List<DicomFileEntry>
        {
            new() { FileName = "IM_0001", FilePath = "IM_0001",
                AcquisitionDateTime = DateTime.Now },
            new() { FileName = "IM_0002", FilePath = "IM_0002",
                AcquisitionDateTime = DateTime.Now.AddSeconds(5) }
        };

        var groupingService = new TimeSeriesGroupingService();
        var groups = groupingService.GroupFiles(files, 60);
        Assert.Single(groups);

        // Filter
        var filterService = new FilterService();
        var tags = new List<string> { "0008,0060 Modality: US" };

        Assert.True(filterService.MatchesFilter(
            files[0], "IM_0001", "0001", tags));
        Assert.False(filterService.MatchesFilter(
            files[1], "IM_0002", "0001", tags));
    }

    [Fact]
    public void Workflow_GroupIds_StableAcrossRuns()
    {
        var baseTime = new DateTime(2025, 6, 15, 14, 0, 0);
        var files = new List<DicomFileEntry>
        {
            new() { FileName = "f1", FilePath = "f1",
                AcquisitionDateTime = baseTime },
            new() { FileName = "f2", FilePath = "f2",
                AcquisitionDateTime = baseTime.AddSeconds(10) }
        };

        var service = new TimeSeriesGroupingService();
        var groups1 = service.GroupFiles(files, 60);
        var groups2 = service.GroupFiles(files, 60);

        Assert.Equal(groups1[0].GroupId, groups2[0].GroupId);
        Assert.Equal(groups1[0].Label, groups2[0].Label);
    }

    [Fact]
    public void Workflow_ClassifyThenGroup_ConsistentClassification()
    {
        // Classify files first, then group — classification should stay
        var classificationService = new ClassificationService();
        var files = new List<DicomFileEntry>
        {
            new() { FileName = "ceus", FilePath = "ceus",
                AcquisitionDateTime = DateTime.Now,
                PixelSpacing = [0.5, 0.5] },
            new() { FileName = "shi", FilePath = "shi",
                AcquisitionDateTime = DateTime.Now.AddSeconds(5),
                PixelSpacing = [0.3, 0.3] }
        };

        foreach (var f in files)
            f.Classification = classificationService.Classify(f.PixelSpacing);

        var service = new TimeSeriesGroupingService();
        var groups = service.GroupFiles(files, 60);

        // Classification persists through grouping
        Assert.Equal(FileClassification.CEUS, groups[0].Files[0].Classification);
        Assert.Equal(FileClassification.SHI, groups[0].Files[1].Classification);
    }
}
