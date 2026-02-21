using DicomViewer.Core.Models;
using DicomViewer.Core.Services;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Unit tests for RoiService.
/// </summary>
public class RoiServiceTests
{
    private readonly RoiService _sut = new();
    private readonly string _tempDir;

    public RoiServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),
            $"DicomTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void GetRoi_NoRoisSet_ReturnsNull()
    {
        Assert.Null(_sut.GetRoi("group1"));
    }

    [Fact]
    public void SetRoi_ThenGetRoi_ReturnsSameRoi()
    {
        var roi = new RoiData
        {
            X = 10, Y = 20, Width = 100, Height = 50
        };
        _sut.SetRoi("g1", roi);

        var result = _sut.GetRoi("g1");
        Assert.NotNull(result);
        Assert.Equal(10, result!.X);
        Assert.Equal(20, result.Y);
        Assert.Equal(100, result.Width);
        Assert.Equal(50, result.Height);
    }

    [Fact]
    public void SetRoi_ReplacesExisting()
    {
        _sut.SetRoi("g1", new RoiData { X = 10 });
        _sut.SetRoi("g1", new RoiData { X = 99 });

        Assert.Equal(99, _sut.GetRoi("g1")!.X);
    }

    [Fact]
    public void ClearRoi_RemovesRoi()
    {
        _sut.SetRoi("g1", new RoiData { X = 10 });
        _sut.ClearRoi("g1");

        Assert.Null(_sut.GetRoi("g1"));
    }

    [Fact]
    public void SetFileMean_StoresValueInRoi()
    {
        _sut.SetRoi("g1", new RoiData());
        _sut.SetFileMean("g1", "file1.dcm", 42.5);

        var roi = _sut.GetRoi("g1");
        Assert.Equal(42.5, roi!.FileMeans["file1.dcm"]);
    }

    [Fact]
    public void SaveAndLoadRois_RoundTrips()
    {
        _sut.SetRoi("g1", new RoiData
        {
            X = 5, Y = 10, Width = 50, Height = 25
        });
        _sut.SetFileMean("g1", "f1", 33.3);

        _sut.SaveRois(_tempDir);

        // Load into fresh instance
        var sut2 = new RoiService();
        sut2.LoadRois(_tempDir);

        var loaded = sut2.GetRoi("g1");
        Assert.NotNull(loaded);
        Assert.Equal(5, loaded!.X);
        Assert.Equal(10, loaded.Y);
        Assert.Equal(33.3, loaded.FileMeans["f1"], 1);
    }

    [Fact]
    public void LoadRois_MissingFile_InitializesEmpty()
    {
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);
        _sut.LoadRois(emptyDir);

        Assert.Null(_sut.GetRoi("anything"));
    }

    ~RoiServiceTests()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
