using DicomViewer.Core.Models;
using DicomViewer.Core.Services;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Unit tests for ClassificationService.
/// </summary>
public class ClassificationServiceTests
{
    private readonly ClassificationService _sut = new();

    [Fact]
    public void Classify_NullSpacing_ReturnsCEUS()
    {
        var result = _sut.Classify(null);
        Assert.Equal(FileClassification.CEUS, result);
    }

    [Fact]
    public void Classify_EmptySpacing_ReturnsCEUS()
    {
        var result = _sut.Classify([]);
        Assert.Equal(FileClassification.CEUS, result);
    }

    [Fact]
    public void Classify_SpacingNearCEUS_ReturnsCEUS()
    {
        var result = _sut.Classify([0.48, 0.48]);
        Assert.Equal(FileClassification.CEUS, result);
    }

    [Fact]
    public void Classify_SpacingNearSHI_ReturnsSHI()
    {
        var result = _sut.Classify([0.28, 0.28]);
        Assert.Equal(FileClassification.SHI, result);
    }

    [Fact]
    public void Classify_ExactCEUS_ReturnsCEUS()
    {
        var result = _sut.Classify([0.5, 0.5]);
        Assert.Equal(FileClassification.CEUS, result);
    }

    [Fact]
    public void Classify_ExactSHI_ReturnsSHI()
    {
        var result = _sut.Classify([0.3, 0.3]);
        Assert.Equal(FileClassification.SHI, result);
    }

    [Fact]
    public void Classify_EquidistantDefaultsToCEUS()
    {
        // 0.4 is equidistant from 0.3 and 0.5
        var result = _sut.Classify([0.4, 0.4]);
        Assert.Equal(FileClassification.CEUS, result);
    }

    [Fact]
    public void Classify_CustomTargets()
    {
        var result = _sut.Classify([0.7, 0.7],
            ceusTarget: 1.0, shiTarget: 0.6);
        Assert.Equal(FileClassification.SHI, result);
    }

    [Fact]
    public void Classify_SampleFiles_CEUS()
    {
        // Integration-style test with real files
        var sampleDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "SampleFiles", "DICOM 20251125"));
        if (!Directory.Exists(sampleDir)) return;

        var fileService = new DicomFileService();
        var files = fileService.LoadFiles(sampleDir);

        var expectedCeus = new HashSet<string>
        {
            "IM_0001", "IM_0012", "IM_0023", "IM_0032", "IM_0043",
            "IM_0054", "IM_0065", "IM_0076", "IM_0087", "IM_0098",
            "IM_0108", "IM_0119", "IM_0130", "IM_0141", "IM_0152",
            "IM_0163", "IM_0164"
        };

        foreach (var file in files)
        {
            var classification = _sut.Classify(file.PixelSpacing);
            if (expectedCeus.Contains(file.FileName))
            {
                Assert.Equal(FileClassification.CEUS, classification);
            }
        }
    }
}
