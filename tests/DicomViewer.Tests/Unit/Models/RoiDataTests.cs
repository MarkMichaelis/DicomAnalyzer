using DicomViewer.Core.Models;

namespace DicomViewer.Tests.Unit.Models;

/// <summary>
/// Unit tests for RoiData geometry (ContainsPoint) for all shapes.
/// </summary>
public class RoiDataTests
{
    #region Rectangle

    [Fact]
    public void Rectangle_ContainsPoint_Inside_ReturnsTrue()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 10, Y = 20, Width = 100, Height = 50
        };
        Assert.True(roi.ContainsPoint(50, 40));
    }

    [Fact]
    public void Rectangle_ContainsPoint_Outside_ReturnsFalse()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 10, Y = 20, Width = 100, Height = 50
        };
        Assert.False(roi.ContainsPoint(5, 40));
        Assert.False(roi.ContainsPoint(50, 75));
    }

    [Fact]
    public void Rectangle_ContainsPoint_OnEdge_ReturnsTrue()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 10, Y = 20, Width = 100, Height = 50
        };
        Assert.True(roi.ContainsPoint(10, 20));
        Assert.True(roi.ContainsPoint(110, 70));
    }

    #endregion

    #region Ellipse

    [Fact]
    public void Ellipse_ContainsPoint_Center_ReturnsTrue()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Ellipse,
            X = 0, Y = 0, Width = 200, Height = 100
        };
        Assert.True(roi.ContainsPoint(100, 50)); // center
    }

    [Fact]
    public void Ellipse_ContainsPoint_Corner_ReturnsFalse()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Ellipse,
            X = 0, Y = 0, Width = 200, Height = 100
        };
        // Corner of bounding box is outside ellipse
        Assert.False(roi.ContainsPoint(1, 1));
        Assert.False(roi.ContainsPoint(199, 99));
    }

    [Fact]
    public void Ellipse_ContainsPoint_ZeroSize_ReturnsFalse()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Ellipse,
            X = 50, Y = 50, Width = 0, Height = 0
        };
        Assert.False(roi.ContainsPoint(50, 50));
    }

    #endregion

    #region Freeform

    [Fact]
    public void Freeform_ContainsPoint_InsideTriangle_ReturnsTrue()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Freeform,
            Points =
            [
                [0.0, 0.0],
                [100.0, 0.0],
                [50.0, 100.0]
            ]
        };
        Assert.True(roi.ContainsPoint(50, 30));
    }

    [Fact]
    public void Freeform_ContainsPoint_OutsideTriangle_ReturnsFalse()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Freeform,
            Points =
            [
                [0.0, 0.0],
                [100.0, 0.0],
                [50.0, 100.0]
            ]
        };
        Assert.False(roi.ContainsPoint(200, 200));
    }

    [Fact]
    public void Freeform_ContainsPoint_TooFewPoints_ReturnsFalse()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Freeform,
            Points = [[0.0, 0.0], [10.0, 10.0]]
        };
        Assert.False(roi.ContainsPoint(5, 5));
    }

    [Fact]
    public void Freeform_ContainsPoint_Square_InsideAndOutside()
    {
        var roi = new RoiData
        {
            Shape = RoiShape.Freeform,
            Points =
            [
                [10.0, 10.0],
                [90.0, 10.0],
                [90.0, 90.0],
                [10.0, 90.0]
            ]
        };
        Assert.True(roi.ContainsPoint(50, 50));
        Assert.False(roi.ContainsPoint(5, 5));
        Assert.False(roi.ContainsPoint(95, 50));
    }

    #endregion
}