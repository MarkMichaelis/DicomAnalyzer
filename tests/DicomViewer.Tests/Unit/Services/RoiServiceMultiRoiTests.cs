using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Tests for multi-ROI features: persistence, removal edge cases,
/// and cross-group isolation (Right-BICEP: Right, Boundary, Error).
/// </summary>
public class RoiServiceMultiRoiTests : IDisposable
{
    private readonly RoiService _sut = new();
    private readonly string _tempDir;

    public RoiServiceMultiRoiTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),
            $"MultiRoiTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void AddRoi_MultipleGroups_AreIsolated()
    {
        var roi1 = new RoiData { Shape = RoiShape.Rectangle, X = 10 };
        var roi2 = new RoiData { Shape = RoiShape.Ellipse, X = 99 };

        _sut.AddRoi("group1", roi1);
        _sut.AddRoi("group2", roi2);

        Assert.Single(_sut.GetRois("group1"));
        Assert.Single(_sut.GetRois("group2"));
        Assert.Equal(10, _sut.GetRois("group1")[0].X);
        Assert.Equal(99, _sut.GetRois("group2")[0].X);
    }

    [Fact]
    public void GetRois_UnknownGroup_ReturnsEmptyList()
    {
        var rois = _sut.GetRois("nonexistent");
        Assert.Empty(rois);
    }

    [Fact]
    public void RemoveRoi_NonexistentId_ReturnsFalse()
    {
        _sut.AddRoi("g1", new RoiData { Shape = RoiShape.Rectangle });
        Assert.False(_sut.RemoveRoi("g1", "nonexistent-id"));
    }

    [Fact]
    public void RemoveRoi_NonexistentGroup_ReturnsFalse()
    {
        Assert.False(_sut.RemoveRoi("nonexistent", "any-id"));
    }

    [Fact]
    public void RemoveRoi_LastRoi_RemovesGroupEntry()
    {
        var roi = new RoiData { Shape = RoiShape.Rectangle };
        _sut.AddRoi("g1", roi);
        var id = _sut.GetRois("g1")[0].Id;

        _sut.RemoveRoi("g1", id);

        Assert.Empty(_sut.GetRois("g1"));
        Assert.Null(_sut.GetRoi("g1"));
    }

    [Fact]
    public void SaveAndLoadRois_MultipleRoisPerGroup_RoundTrips()
    {
        var roi1 = new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 10, Y = 20, Width = 100, Height = 50
        };
        var roi2 = new RoiData
        {
            Shape = RoiShape.Ellipse,
            X = 200, Y = 200, Width = 30, Height = 30
        };

        _sut.AddRoi("g1", roi1);
        _sut.AddRoi("g1", roi2);
        _sut.SaveRois(_tempDir);

        var loaded = new RoiService();
        loaded.LoadRois(_tempDir);

        var rois = loaded.GetRois("g1");
        Assert.Equal(2, rois.Count);
        Assert.Equal(RoiShape.Rectangle, rois[0].Shape);
        Assert.Equal(RoiShape.Ellipse, rois[1].Shape);
        Assert.Equal(10, rois[0].X);
        Assert.Equal(200, rois[1].X);
    }

    [Fact]
    public void SaveAndLoadRois_MultipleGroups_RoundTrips()
    {
        _sut.AddRoi("g1", new RoiData { X = 1 });
        _sut.AddRoi("g2", new RoiData { X = 2 });
        _sut.AddRoi("g3", new RoiData { X = 3 });
        _sut.SaveRois(_tempDir);

        var loaded = new RoiService();
        loaded.LoadRois(_tempDir);

        Assert.Equal(1, loaded.GetRoi("g1")!.X);
        Assert.Equal(2, loaded.GetRoi("g2")!.X);
        Assert.Equal(3, loaded.GetRoi("g3")!.X);
    }

    [Fact]
    public void SaveRois_EmptyDirectory_DoesNotThrow()
    {
        // No ROIs added, save should still succeed
        var ex = Record.Exception(() => _sut.SaveRois(_tempDir));
        Assert.Null(ex);
    }

    [Fact]
    public void SaveRois_NullDirectory_DoesNotThrow()
    {
        var ex = Record.Exception(() => _sut.SaveRois(null!));
        Assert.Null(ex);
    }

    [Fact]
    public void SaveRois_EmptyStringDirectory_DoesNotThrow()
    {
        var ex = Record.Exception(() => _sut.SaveRois(""));
        Assert.Null(ex);
    }

    [Fact]
    public void LoadRois_CorruptedFile_InitializesEmpty()
    {
        var path = Path.Combine(_tempDir, "dicom_viewer.roi");
        File.WriteAllText(path, "{ invalid json ]]");

        _sut.LoadRois(_tempDir);

        Assert.Null(_sut.GetRoi("anything"));
    }

    [Fact]
    public void HitTestRoi_MultipleOverlapping_ReturnsFirst()
    {
        var roi1 = new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 0, Y = 0, Width = 100, Height = 100
        };
        var roi2 = new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 50, Y = 50, Width = 100, Height = 100
        };

        _sut.AddRoi("g1", roi1);
        _sut.AddRoi("g1", roi2);

        var hit = _sut.HitTestRoi("g1", 75, 75);
        Assert.NotNull(hit);
        Assert.Equal(roi1.Id, hit!.Id);
    }

    [Fact]
    public void HitTestRoi_NonexistentGroup_ReturnsNull()
    {
        var hit = _sut.HitTestRoi("nonexistent", 50, 50);
        Assert.Null(hit);
    }

    [Fact]
    public void AddRoi_SetsGroupIdOnRoi()
    {
        var roi = new RoiData { Shape = RoiShape.Rectangle };
        _sut.AddRoi("my-group", roi);

        var stored = _sut.GetRois("my-group")[0];
        Assert.Equal("my-group", stored.GroupId);
    }

    [Fact]
    public void SetFileMean_NoRois_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _sut.SetFileMean("nonexistent", "file.dcm", 42.0));
        Assert.Null(ex);
    }

    [Fact]
    public void UndoRoi_AfterAddMultiple_RestoresPreviousState()
    {
        _sut.AddRoi("g1", new RoiData { X = 10 });
        _sut.AddRoi("g1", new RoiData { X = 20 });

        Assert.Equal(2, _sut.GetRois("g1").Count);

        _sut.UndoRoi("g1");
        Assert.Single(_sut.GetRois("g1"));
        Assert.Equal(10, _sut.GetRois("g1")[0].X);
    }

    [Fact]
    public void UndoRoi_AfterRemove_RestoresRemovedRoi()
    {
        _sut.AddRoi("g1", new RoiData { X = 10 });
        _sut.AddRoi("g1", new RoiData { X = 20 });
        var secondId = _sut.GetRois("g1")[1].Id;

        _sut.RemoveRoi("g1", secondId);
        Assert.Single(_sut.GetRois("g1"));

        _sut.UndoRoi("g1");
        Assert.Equal(2, _sut.GetRois("g1").Count);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
