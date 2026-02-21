using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Classifies DICOM files as CEUS or SHI based on pixel spacing.
/// </summary>
public class ClassificationService
{
    /// <summary>
    /// Classifies a file by closest pixel spacing to targets.
    /// </summary>
    public FileClassification Classify(
        double[]? pixelSpacing,
        double ceusTarget = 0.5,
        double shiTarget = 0.3)
    {
        if (pixelSpacing == null || pixelSpacing.Length == 0)
            return FileClassification.CEUS;

        var spacing = pixelSpacing[0];
        var ceusDist = Math.Abs(spacing - ceusTarget);
        var shiDist = Math.Abs(spacing - shiTarget);

        return shiDist < ceusDist
            ? FileClassification.SHI
            : FileClassification.CEUS;
    }
}
