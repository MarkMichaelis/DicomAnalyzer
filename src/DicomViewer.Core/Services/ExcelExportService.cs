using ClosedXML.Excel;
using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Exports ROI intensity data to Excel (.xlsx) files.
/// </summary>
public class ExcelExportService
{
    /// <summary>
    /// Exports per-file ROI mean intensity data to an Excel workbook
    /// with a data sheet, a pivot-style summary, and a chart-ready layout.
    /// </summary>
    public void ExportIntensityData(
        string outputPath,
        List<TimeSeriesGroup> groups,
        Dictionary<string, RoiData> rois)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Raw data
        var dataSheet = workbook.AddWorksheet("ROI Data");
        WriteDataSheet(dataSheet, groups, rois);

        // Sheet 2: Summary pivot
        var summarySheet = workbook.AddWorksheet("Summary");
        WriteSummarySheet(summarySheet, groups, rois);

        workbook.SaveAs(outputPath);
    }

    private static void WriteDataSheet(
        IXLWorksheet sheet,
        List<TimeSeriesGroup> groups,
        Dictionary<string, RoiData> rois)
    {
        // Headers
        sheet.Cell(1, 1).Value = "Group";
        sheet.Cell(1, 2).Value = "File Name";
        sheet.Cell(1, 3).Value = "ROI Mean Intensity";
        sheet.Cell(1, 4).Value = "ROI Shape";
        sheet.Cell(1, 5).Value = "ROI X";
        sheet.Cell(1, 6).Value = "ROI Y";
        sheet.Cell(1, 7).Value = "ROI Width";
        sheet.Cell(1, 8).Value = "ROI Height";

        var headerRange = sheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor =
            XLColor.LightSteelBlue;

        int row = 2;
        foreach (var group in groups)
        {
            if (!rois.TryGetValue(group.GroupId, out var roi))
                continue;

            foreach (var file in group.Files)
            {
                sheet.Cell(row, 1).Value = group.Label;
                sheet.Cell(row, 2).Value = file.FileName;

                if (roi.FileMeans.TryGetValue(
                    file.FileName, out var mean))
                {
                    sheet.Cell(row, 3).Value = mean;
                    sheet.Cell(row, 3).Style.NumberFormat
                        .Format = "0.00";
                }

                sheet.Cell(row, 4).Value = roi.Shape.ToString();
                sheet.Cell(row, 5).Value = roi.X;
                sheet.Cell(row, 6).Value = roi.Y;
                sheet.Cell(row, 7).Value = roi.Width;
                sheet.Cell(row, 8).Value = roi.Height;
                row++;
            }
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteSummarySheet(
        IXLWorksheet sheet,
        List<TimeSeriesGroup> groups,
        Dictionary<string, RoiData> rois)
    {
        // Pivot-style summary: group → avg, min, max, count
        sheet.Cell(1, 1).Value = "Group";
        sheet.Cell(1, 2).Value = "File Count";
        sheet.Cell(1, 3).Value = "Average Intensity";
        sheet.Cell(1, 4).Value = "Min Intensity";
        sheet.Cell(1, 5).Value = "Max Intensity";
        sheet.Cell(1, 6).Value = "Std Dev";

        var headerRange = sheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor =
            XLColor.LightSteelBlue;

        int row = 2;
        foreach (var group in groups)
        {
            if (!rois.TryGetValue(group.GroupId, out var roi))
                continue;
            if (roi.FileMeans.Count == 0) continue;

            var means = roi.FileMeans.Values.ToList();
            var avg = means.Average();
            var min = means.Min();
            var max = means.Max();
            var stdDev = means.Count > 1
                ? Math.Sqrt(means.Sum(m =>
                    (m - avg) * (m - avg)) / (means.Count - 1))
                : 0;

            sheet.Cell(row, 1).Value = group.Label;
            sheet.Cell(row, 2).Value = means.Count;
            sheet.Cell(row, 3).Value = avg;
            sheet.Cell(row, 4).Value = min;
            sheet.Cell(row, 5).Value = max;
            sheet.Cell(row, 6).Value = stdDev;

            for (int col = 3; col <= 6; col++)
                sheet.Cell(row, col).Style.NumberFormat
                    .Format = "0.00";
            row++;
        }

        sheet.Columns().AdjustToContents();
    }
}