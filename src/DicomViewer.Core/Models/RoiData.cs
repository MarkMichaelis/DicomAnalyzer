namespace DicomViewer.Core.Models;

/// <summary>
/// Rectangular ROI data stored in image coordinates.
/// </summary>
public class RoiData
{
    /// <summary>Group this ROI belongs to.</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>X coordinate in image space.</summary>
    public double X { get; set; }

    /// <summary>Y coordinate in image space.</summary>
    public double Y { get; set; }

    /// <summary>Width in image space.</summary>
    public double Width { get; set; }

    /// <summary>Height in image space.</summary>
    public double Height { get; set; }

    /// <summary>Per-file mean intensities keyed by filename.</summary>
    public Dictionary<string, double> FileMeans { get; set; } = [];
}
