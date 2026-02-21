using FellowOakDicom;
using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Service for loading and parsing DICOM files from a directory.
/// </summary>
public class DicomFileService
{
    /// <summary>
    /// Loads all DICOM files from the specified directory.
    /// </summary>
    public List<DicomFileEntry> LoadFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException(
                $"Directory not found: {directoryPath}");

        var entries = new List<DicomFileEntry>();
        var files = Directory.GetFiles(directoryPath)
            .Where(f => !Path.GetFileName(f).StartsWith(".")
                && Path.GetExtension(f) != ".roi"
                && Path.GetExtension(f) != ".settings"
                && Path.GetExtension(f) != ".json")
            .OrderBy(f => f)
            .ToArray();

        foreach (var filePath in files)
        {
            try
            {
                var dcmFile = DicomFile.Open(filePath);
                var dataset = dcmFile.Dataset;
                var entry = new DicomFileEntry
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    Width = dataset.GetSingleValueOrDefault(
                        DicomTag.Columns, 0),
                    Height = dataset.GetSingleValueOrDefault(
                        DicomTag.Rows, 0),
                    FrameCount = dataset.GetSingleValueOrDefault(
                        DicomTag.NumberOfFrames, 1),
                    AcquisitionDateTime = ParseAcquisitionDateTime(dataset),
                    PixelSpacing = ParsePixelSpacing(dataset)
                };
                entries.Add(entry);
            }
            catch (DicomFileException)
            {
                // Skip non-DICOM files silently
            }
        }

        return entries;
    }

    private static DateTime? ParseAcquisitionDateTime(
        DicomDataset dataset)
    {
        var dtStr = dataset.GetSingleValueOrDefault<string>(
            DicomTag.AcquisitionDateTime, null!);
        if (!string.IsNullOrWhiteSpace(dtStr)
            && TryParseDicomDateTime(dtStr, out var dt))
            return dt;

        var dateStr = dataset.GetSingleValueOrDefault<string>(
            DicomTag.AcquisitionDate, null!);
        var timeStr = dataset.GetSingleValueOrDefault<string>(
            DicomTag.AcquisitionTime, null!);
        if (!string.IsNullOrWhiteSpace(dateStr))
        {
            var combined = dateStr + (timeStr ?? "");
            if (TryParseDicomDateTime(combined, out var dt2))
                return dt2;
        }

        return null;
    }

    private static bool TryParseDicomDateTime(
        string value, out DateTime result)
    {
        value = value.Trim();
        var formats = new[]
        {
            "yyyyMMddHHmmss.ffffff",
            "yyyyMMddHHmmss.fff",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMdd"
        };
        return DateTime.TryParseExact(value, formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result);
    }

    /// <summary>
    /// Parses pixel spacing from DICOM dataset.
    /// </summary>
    public static double[]? ParsePixelSpacing(DicomDataset dataset)
    {
        var tags = new[]
        {
            DicomTag.PixelSpacing,
            DicomTag.ImagerPixelSpacing
        };

        foreach (var tag in tags)
        {
            var spacing = TryGetSpacingFromTag(dataset, tag);
            if (spacing != null) return spacing;
        }

        if (dataset.Contains(new DicomTag(0x0028, 0x0030)))
        {
            try
            {
                var raw = dataset.GetDicomItem<DicomElement>(
                    new DicomTag(0x0028, 0x0030));
                if (raw != null)
                    return ParseSpacingString(raw.Get<string>());
            }
            catch { }
        }

        return null;
    }

    private static double[]? TryGetSpacingFromTag(
        DicomDataset dataset, DicomTag tag)
    {
        if (!dataset.Contains(tag)) return null;
        try
        {
            var values = dataset.GetValues<double>(tag);
            if (values.Length >= 2) return [values[0], values[1]];
            if (values.Length == 1) return [values[0], values[0]];
        }
        catch
        {
            try
            {
                var str = dataset.GetSingleValueOrDefault<string>(
                    tag, null!);
                if (str != null) return ParseSpacingString(str);
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// Parses spacing string formats like "0.3\\0.3" or "[0.3, 0.3]".
    /// </summary>
    public static double[]? ParseSpacingString(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim().Trim('[', ']');
        var parts = value.Split(['\\', ','],
            StringSplitOptions.RemoveEmptyEntries);

        var result = new List<double>();
        foreach (var part in parts)
        {
            if (double.TryParse(part.Trim(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var d))
                result.Add(d);
        }

        if (result.Count >= 2) return [result[0], result[1]];
        if (result.Count == 1) return [result[0], result[0]];
        return null;
    }
}
