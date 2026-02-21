namespace DicomViewer.Core.Models;

/// <summary>
/// Classification of a DICOM file based on pixel spacing.
/// </summary>
public enum FileClassification
{
    /// <summary>Contrast-enhanced ultrasound.</summary>
    CEUS,
    /// <summary>Second harmonic imaging.</summary>
    SHI
}
