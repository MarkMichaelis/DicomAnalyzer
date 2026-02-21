using FellowOakDicom;
using FellowOakDicom.Imaging;
using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Computes ROI intensity statistics for DICOM files.
/// </summary>
public static class RoiStatisticsService
{
    /// <summary>
    /// Computes mean pixel intensity within the ROI across all frames.
    /// </summary>
    public static double ComputeMeanIntensity(
        string filePath, RoiData roi)
    {
        var dcm = DicomFile.Open(filePath);
        var ds = dcm.Dataset;
        var frameCount = ds.GetSingleValueOrDefault(
            DicomTag.NumberOfFrames, 1);
        var width = ds.GetSingleValueOrDefault(DicomTag.Columns, 0);
        var height = ds.GetSingleValueOrDefault(DicomTag.Rows, 0);

        if (width == 0 || height == 0) return 0;

        var roiX = (int)Math.Max(0, Math.Round(roi.X));
        var roiY = (int)Math.Max(0, Math.Round(roi.Y));
        var roiW = (int)Math.Min(
            width - roiX, Math.Round(roi.Width));
        var roiH = (int)Math.Min(
            height - roiY, Math.Round(roi.Height));

        if (roiW <= 0 || roiH <= 0) return 0;

        double totalSum = 0;
        long totalPixels = 0;

        for (int frame = 0; frame < frameCount; frame++)
        {
            try
            {
                var image = new DicomImage(ds);
                var rendered = image.RenderImage(frame);

                for (int y = roiY; y < roiY + roiH && y < height; y++)
                {
                    for (int x = roiX; x < roiX + roiW && x < width; x++)
                    {
                        var pixel = rendered.GetPixel(x, y);
                        var intensity = 0.299 * pixel.R
                            + 0.587 * pixel.G
                            + 0.114 * pixel.B;
                        totalSum += intensity;
                        totalPixels++;
                    }
                }
            }
            catch { /* Skip frames that fail to render */ }
        }

        return totalPixels > 0 ? totalSum / totalPixels : 0;
    }
}
