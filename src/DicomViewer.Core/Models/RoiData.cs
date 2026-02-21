namespace DicomViewer.Core.Models;

/// <summary>
/// Describes the shape type for an ROI.
/// </summary>
public enum RoiShape
{
    /// <summary>Axis-aligned rectangle.</summary>
    Rectangle,
    /// <summary>Axis-aligned ellipse (bounded by X,Y,Width,Height).</summary>
    Ellipse,
    /// <summary>Free-form polygon defined by Points.</summary>
    Freeform
}

/// <summary>
/// ROI data stored in image coordinates.
/// </summary>
public class RoiData
{
    /// <summary>Unique ID for this ROI.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Group this ROI belongs to.</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>Shape type of this ROI.</summary>
    public RoiShape Shape { get; set; } = RoiShape.Rectangle;

    /// <summary>X coordinate of bounding box in image space.</summary>
    public double X { get; set; }

    /// <summary>Y coordinate of bounding box in image space.</summary>
    public double Y { get; set; }

    /// <summary>Width of bounding box in image space.</summary>
    public double Width { get; set; }

    /// <summary>Height of bounding box in image space.</summary>
    public double Height { get; set; }

    /// <summary>Points for freeform ROI (image-space coordinates).</summary>
    public List<double[]> Points { get; set; } = [];

    /// <summary>Per-file mean intensities keyed by filename.</summary>
    public Dictionary<string, double> FileMeans { get; set; } = [];

    /// <summary>
    /// Tests whether an image-space point is inside this ROI.
    /// </summary>
    public bool ContainsPoint(double px, double py)
    {
        return Shape switch
        {
            RoiShape.Rectangle => ContainsPointRect(px, py),
            RoiShape.Ellipse => ContainsPointEllipse(px, py),
            RoiShape.Freeform => ContainsPointFreeform(px, py),
            _ => ContainsPointRect(px, py)
        };
    }

    private bool ContainsPointRect(double px, double py) =>
        px >= X && px <= X + Width && py >= Y && py <= Y + Height;

    private bool ContainsPointEllipse(double px, double py)
    {
        var cx = X + Width / 2.0;
        var cy = Y + Height / 2.0;
        var rx = Width / 2.0;
        var ry = Height / 2.0;
        if (rx <= 0 || ry <= 0) return false;
        var dx = (px - cx) / rx;
        var dy = (py - cy) / ry;
        return (dx * dx + dy * dy) <= 1.0;
    }

    private bool ContainsPointFreeform(double px, double py)
    {
        if (Points.Count < 3) return false;
        // Ray-casting algorithm
        bool inside = false;
        int n = Points.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var xi = Points[i][0]; var yi = Points[i][1];
            var xj = Points[j][0]; var yj = Points[j][1];
            if (((yi > py) != (yj > py))
                && (px < (xj - xi) * (py - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
        }
        return inside;
    }
}
