using DicomViewer.Core.Models;
using Xunit;

namespace DicomViewer.Tests.Unit.Models;

/// <summary>
/// Unit tests for DicomFileEntry default values and property behavior.
/// </summary>
public class DicomFileEntryTests
{
    [Fact]
    public void DefaultValues_AreReasonable()
    {
        var entry = new DicomFileEntry();

        Assert.Equal(string.Empty, entry.FilePath);
        Assert.Equal(string.Empty, entry.FileName);
        Assert.Equal(0, entry.Width);
        Assert.Equal(0, entry.Height);
        Assert.Equal(1, entry.FrameCount);
        Assert.Null(entry.AcquisitionDateTime);
        Assert.Null(entry.PixelSpacing);
        Assert.Equal(FileClassification.CEUS, entry.Classification);
    }

    [Fact]
    public void Properties_RoundTrip()
    {
        var now = new DateTime(2025, 6, 15, 14, 30, 0);
        var entry = new DicomFileEntry
        {
            FilePath = @"C:\data\IM_0001",
            FileName = "IM_0001",
            Width = 640,
            Height = 480,
            FrameCount = 10,
            AcquisitionDateTime = now,
            PixelSpacing = [0.5, 0.5],
            Classification = FileClassification.SHI
        };

        Assert.Equal(@"C:\data\IM_0001", entry.FilePath);
        Assert.Equal("IM_0001", entry.FileName);
        Assert.Equal(640, entry.Width);
        Assert.Equal(480, entry.Height);
        Assert.Equal(10, entry.FrameCount);
        Assert.Equal(now, entry.AcquisitionDateTime);
        Assert.Equal([0.5, 0.5], entry.PixelSpacing);
        Assert.Equal(FileClassification.SHI, entry.Classification);
    }
}
