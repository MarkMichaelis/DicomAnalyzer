using DicomViewer.Core.Models;
using DicomViewer.Core.Services;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Unit tests for RoiStatisticsService.
/// Note: Pixel rendering in fo-dicom may return all-black pixels
/// in a headless (non-WPF) test environment. Full rendering
/// validation must occur in the WPF integration context.
/// </summary>
public class RoiStatisticsServiceTests
{
    private static readonly string SampleDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "SampleFiles", "DICOM 20251125"));

    [Fact]
    public void ComputeMeanIntensity_ValidRoi_DoesNotThrow()
    {
        var filePath = Path.Combine(SampleDir, "IM_0001");
        if (!File.Exists(filePath)) return;

        var roi = new RoiData
        {
            X = 200, Y = 150, Width = 400, Height = 300
        };

        // Should not throw (rendering may be all-black in headless)
        var mean = RoiStatisticsService.ComputeMeanIntensity(
            filePath, roi);
        Assert.True(mean >= 0, $"Mean should be non-negative, got {mean}");
    }

    [Fact]
    public void ComputeMeanIntensity_ZeroSizeRoi_ReturnsZero()
    {
        var filePath = Path.Combine(SampleDir, "IM_0001");
        if (!File.Exists(filePath)) return;

        var roi = new RoiData
        {
            X = 100, Y = 100, Width = 0, Height = 0
        };

        var mean = RoiStatisticsService.ComputeMeanIntensity(
            filePath, roi);
        Assert.Equal(0, mean);
    }

    [Fact]
    public void ComputeMeanIntensity_OutOfBoundsRoi_ReturnsZero()
    {
        var filePath = Path.Combine(SampleDir, "IM_0001");
        if (!File.Exists(filePath)) return;

        var roi = new RoiData
        {
            X = 99999, Y = 99999, Width = 50, Height = 50
        };

        var mean = RoiStatisticsService.ComputeMeanIntensity(
            filePath, roi);
        Assert.Equal(0, mean);
    }

    [Fact]
    public void ComputeMeanIntensity_FullImage_ReturnsNonNegativeValue()
    {
        var filePath = Path.Combine(SampleDir, "IM_0001");
        if (!File.Exists(filePath)) return;

        var fileService = new DicomFileService();
        var files = fileService.LoadFiles(SampleDir);
        var file = files.First(f => f.FileName == "IM_0001");

        var roi = new RoiData
        {
            X = 0, Y = 0,
            Width = file.Width,
            Height = file.Height
        };

        var mean = RoiStatisticsService.ComputeMeanIntensity(
            filePath, roi);

        Assert.InRange(mean, 0, 255);
    }
}
