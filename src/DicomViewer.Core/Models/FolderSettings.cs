namespace DicomViewer.Core.Models;

/// <summary>
/// Per-folder configuration stored alongside DICOM files.
/// </summary>
public class FolderSettings
{
    /// <summary>Time window in seconds for grouping. Default 60.</summary>
    public double TimeWindowSeconds { get; set; } = 60;

    /// <summary>Target pixel spacing for CEUS classification. Default 0.5.</summary>
    public double CeusSpacing { get; set; } = 0.5;

    /// <summary>Target pixel spacing for SHI classification. Default 0.3.</summary>
    public double ShiSpacing { get; set; } = 0.3;
}
