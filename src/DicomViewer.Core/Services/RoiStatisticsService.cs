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
    /// Supports rectangle, ellipse, and freeform ROI shapes.
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

        // Compute bounding box for scan area
        int scanX, scanY, scanW, scanH;
        if (roi.Shape == RoiShape.Freeform && roi.Points.Count >= 3)
        {
            var minX = roi.Points.Min(p => p[0]);
            var minY = roi.Points.Min(p => p[1]);
            var maxX = roi.Points.Max(p => p[0]);
            var maxY = roi.Points.Max(p => p[1]);
            scanX = (int)Math.Max(0, Math.Floor(minX));
            scanY = (int)Math.Max(0, Math.Floor(minY));
            scanW = (int)Math.Min(width - scanX,
                Math.Ceiling(maxX) - scanX);
            scanH = (int)Math.Min(height - scanY,
                Math.Ceiling(maxY) - scanY);
        }
        else
        {
            scanX = (int)Math.Max(0, Math.Round(roi.X));
            scanY = (int)Math.Max(0, Math.Round(roi.Y));
            scanW = (int)Math.Min(
                width - scanX, Math.Round(roi.Width));
            scanH = (int)Math.Min(
                height - scanY, Math.Round(roi.Height));
        }

        if (scanW <= 0 || scanH <= 0) return 0;

        double totalSum = 0;
        long totalPixels = 0;

        for (int frame = 0; frame < frameCount; frame++)
        {
            try
            {
                var image = new DicomImage(ds);
                var rendered = image.RenderImage(frame);

                for (int y = scanY; y < scanY + scanH && y < height; y++)
                {
                    for (int x = scanX; x < scanX + scanW && x < width; x++)
                    {
                        if (!roi.ContainsPoint(x, y)) continue;
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
