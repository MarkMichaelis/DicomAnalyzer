namespace DicomViewer.Core.Models;

/// <summary>
/// Represents a loaded DICOM file with its metadata.
/// </summary>
public class DicomFileEntry
{
    /// <summary>Full file path on disk.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>File name without directory.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Image width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Image height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Number of frames (1 for single-frame, &gt;1 for cine).</summary>
    public int FrameCount { get; set; } = 1;

    /// <summary>Acquisition date/time if available.</summary>
    public DateTime? AcquisitionDateTime { get; set; }

    /// <summary>Pixel spacing values [row, column] if available.</summary>
    public double[]? PixelSpacing { get; set; }

    /// <summary>Classification: CEUS or SHI.</summary>
    public FileClassification Classification { get; set; } = FileClassification.CEUS;
}
