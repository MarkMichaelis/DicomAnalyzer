namespace DicomViewer.Core.Models;

/// <summary>
/// Represents a time-series group of DICOM files.
/// </summary>
public class TimeSeriesGroup
{
    /// <summary>Stable identifier for this group.</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>Display label (e.g., "12:00:01 - 12:00:55" or "Unknown Time").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Files in this group, ordered by acquisition time.</summary>
    public List<DicomFileEntry> Files { get; set; } = [];
}
