using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests;

public class MultiRoiServiceTests
{
    [Fact]
    public void AddRoi_CreatesFirstRoiInList()
    {
        var sut = new RoiService();
        var roi = new RoiData { Shape = RoiShape.Rectangle, X = 10, Y = 10, Width = 50, Height = 50 };

        sut.AddRoi("group1", roi);

        var rois = sut.GetRois("group1");
        Assert.Single(rois);
        Assert.Equal(10, rois[0].X);
    }

    [Fact]
    public void AddRoi_MultipleRois_AllStored()
    {
        var sut = new RoiService();
        var roi1 = new RoiData { Shape = RoiShape.Rectangle, X = 10, Y = 10, Width = 50, Height = 50 };
        var roi2 = new RoiData { Shape = RoiShape.Ellipse, X = 100, Y = 100, Width = 30, Height = 30 };

        sut.AddRoi("group1", roi1);
        sut.AddRoi("group1", roi2);

        var rois = sut.GetRois("group1");
        Assert.Equal(2, rois.Count);
    }

    [Fact]
    public void AddRoi_AssignsUniqueId()
    {
        var sut = new RoiService();
        var roi1 = new RoiData { Shape = RoiShape.Rectangle, X = 10, Y = 10, Width = 50, Height = 50 };
        var roi2 = new RoiData { Shape = RoiShape.Ellipse, X = 100, Y = 100, Width = 30, Height = 30 };

        sut.AddRoi("group1", roi1);
        sut.AddRoi("group1", roi2);

        var rois = sut.GetRois("group1");
        Assert.NotEmpty(rois[0].Id);
        Assert.NotEmpty(rois[1].Id);
        Assert.NotEqual(rois[0].Id, rois[1].Id);
    }

    [Fact]
    public void RemoveRoi_ById_RemovesCorrectRoi()
    {
        var sut = new RoiService();
        var roi1 = new RoiData { Shape = RoiShape.Rectangle, X = 10, Y = 10, Width = 50, Height = 50 };
        var roi2 = new RoiData { Shape = RoiShape.Ellipse, X = 100, Y = 100, Width = 30, Height = 30 };

        sut.AddRoi("group1", roi1);
        sut.AddRoi("group1", roi2);
        var addedId = sut.GetRois("group1")[0].Id;

        sut.RemoveRoi("group1", addedId);

        var rois = sut.GetRois("group1");
        Assert.Single(rois);
        Assert.Equal(RoiShape.Ellipse, rois[0].Shape);
    }

    [Fact]
    public void HitTestRoi_ReturnsRoiContainingPoint()
    {
        var sut = new RoiService();
        var roi1 = new RoiData { Shape = RoiShape.Rectangle, X = 10, Y = 10, Width = 50, Height = 50 };
        var roi2 = new RoiData { Shape = RoiShape.Rectangle, X = 200, Y = 200, Width = 50, Height = 50 };

        sut.AddRoi("group1", roi1);
        sut.AddRoi("group1", roi2);

        var hit = sut.HitTestRoi("group1", 30, 30);
        Assert.NotNull(hit);
        Assert.Equal(10, hit!.X);
    }

    [Fact]
    public void HitTestRoi_NoMatchReturnsNull()
    {
        var sut = new RoiService();
        var roi = new RoiData { Shape = RoiShape.Rectangle, X = 10, Y = 10, Width = 50, Height = 50 };

        sut.AddRoi("group1", roi);

        var hit = sut.HitTestRoi("group1", 500, 500);
        Assert.Null(hit);
    }

    [Fact]
    public void GetRoi_LegacyMethod_ReturnsFirstRoi()
    {
        var sut = new RoiService();
        var roi = new RoiData { Shape = RoiShape.Rectangle, X = 10, Y = 10, Width = 50, Height = 50 };

        sut.AddRoi("group1", roi);

        var result = sut.GetRoi("group1");
        Assert.NotNull(result);
        Assert.Equal(10, result!.X);
    }
}
