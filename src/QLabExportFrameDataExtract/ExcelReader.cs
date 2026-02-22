using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;

namespace QLabExportFrameDataExtract;

public class ExcelReader
{
    private readonly string _path;

    public ExcelReader(string path)
    {
        _path = path;
    }

    public ExcelMetadata ExtractMetadata()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var stream = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var ds = reader.AsDataSet();
        var table = ds.Tables[0];

        string dicomPath = string.Empty;
        string patient = string.Empty;
        string date = string.Empty;

        // B3 -> row index 2, col index 1
        if (table.Rows.Count > 2 && table.Columns.Count > 1)
            dicomPath = table.Rows[2][1]?.ToString() ?? string.Empty;
        if (table.Rows.Count > 4 && table.Columns.Count > 1)
            patient = table.Rows[4][1]?.ToString() ?? string.Empty;
        if (table.Rows.Count > 6 && table.Columns.Count > 1)
            date = table.Rows[6][1]?.ToString() ?? string.Empty;

        return new ExcelMetadata
        {
            DICOMFilePath = dicomPath,
            PatientName = patient,
            DICOMFileDate = date
        };
    }

    /// <summary>
    /// Finds all columns whose header contains "Echo Mean (dB)" and returns their
    /// header name and list of string values (one per row below the header).
    /// </summary>
    public List<(string ColumnName, List<string> Values)> ExtractEchoMeanColumns(int headerSearchLimit = 20)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var stream = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var ds = reader.AsDataSet();
        var table = ds.Tables[0];

        int headerRow = -1;
        int maxSearch = Math.Min(headerSearchLimit, table.Rows.Count);
        for (int r = 0; r < maxSearch; r++)
        {
            for (int c = 0; c < table.Columns.Count; c++)
            {
                var cell = table.Rows[r][c]?.ToString() ?? string.Empty;
                if (cell.IndexOf("Echo Mean (dB)", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    headerRow = r;
                    break;
                }
            }
            if (headerRow != -1) break;
        }

        var results = new List<(string ColumnName, List<string> Values)>();
        if (headerRow == -1) return results;

        for (int c = 0; c < table.Columns.Count; c++)
        {
            var headerText = table.Rows[headerRow][c]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(headerText)) continue;
            if (headerText.IndexOf("Echo Mean (dB)", StringComparison.OrdinalIgnoreCase) < 0) continue;

            var values = new List<string>();
            for (int r = headerRow + 1; r < table.Rows.Count; r++)
            {
                var cell = table.Rows[r][c];
                var s = cell?.ToString();
                if (string.IsNullOrWhiteSpace(s)) break;
                values.Add(s);
            }

            results.Add((headerText, values));
        }

        return results;
    }
}
