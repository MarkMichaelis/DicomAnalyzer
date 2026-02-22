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
        try
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
        catch
        {
            // Fallback: try parse as Unicode text (some .xls are text/TSV saved with .xls extension)
            try
            {
                var lines = File.ReadAllLines(_path, Encoding.Unicode);
                string dicomPath = string.Empty;
                string patient = string.Empty;
                string date = string.Empty;

                foreach (var line in lines)
                {
                    var l = line.Trim();
                    if (l.StartsWith("File Path", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = l.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        // take last token(s) after label
                        int idx = l.IndexOfAny(new[] { ':', '\t' });
                        if (idx >= 0 && idx + 1 < l.Length)
                            dicomPath = l.Substring(idx + 1).Trim();
                        else
                        {
                            var pv = l.Substring("File Path".Length).Trim();
                            dicomPath = pv;
                        }
                    }
                    else if (l.StartsWith("Patient Name", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = l.IndexOf(':');
                        if (idx >= 0 && idx + 1 < l.Length) patient = l.Substring(idx + 1).Trim();
                        else patient = l.Substring("Patient Name".Length).Trim();
                    }
                    else if (l.StartsWith("Image Date", StringComparison.OrdinalIgnoreCase) || l.StartsWith("Image Date and Time", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = l.IndexOf(':');
                        if (idx >= 0 && idx + 1 < l.Length) date = l.Substring(idx + 1).Trim();
                        else date = l.Substring("Image Date and Time".Length).Trim();
                        // normalize date to first token
                        var tok = date.Split(new[] { ' ', '\t', '(' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tok.Length > 0) date = tok[0];
                    }
                }

                return new ExcelMetadata
                {
                    DICOMFilePath = dicomPath,
                    PatientName = patient,
                    DICOMFileDate = date
                };
            }
            catch
            {
                return new ExcelMetadata();
            }
        }
    }

    /// <summary>
    /// Finds all columns whose header contains "Echo Mean (dB)" and returns their
    /// header name and list of string values (one per row below the header).
    /// </summary>
    public List<(string ColumnName, List<string> Values)> ExtractEchoMeanColumns(int headerSearchLimit = 20)
    {
        try
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
        catch
        {
            // Fallback: parse as Unicode text
            try
            {
                var lines = File.ReadAllLines(_path, Encoding.Unicode).ToList();

                int headerLineIndex = -1;
                for (int i = 0; i < Math.Min(headerSearchLimit, lines.Count); i++)
                {
                    if (lines[i].IndexOf("Echo Mean", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        headerLineIndex = i;
                        break;
                    }
                }

                var results = new List<(string ColumnName, List<string> Values)>();
                if (headerLineIndex == -1) return results;

                // Split header into columns by two-or-more spaces (the export uses spaced columns)
                var headerTokens = System.Text.RegularExpressions.Regex.Split(lines[headerLineIndex].Trim(), "\\s{2,}");
                var echoIndexes = new List<int>();
                for (int t = 0; t < headerTokens.Length; t++)
                {
                    if (headerTokens[t].IndexOf("Mean", StringComparison.OrdinalIgnoreCase) >= 0 || headerTokens[t].IndexOf("Echo Mean", StringComparison.OrdinalIgnoreCase) >= 0 || headerTokens[t].IndexOf("Mean(dB)", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        echoIndexes.Add(t);
                    }
                }

                // For each echo column, collect following numeric rows until separator or blank
                for (int ei = 0; ei < echoIndexes.Count; ei++)
                {
                    int colIdx = echoIndexes[ei];
                    var values = new List<string>();
                    for (int r = headerLineIndex + 1; r < lines.Count; r++)
                    {
                        var line = lines[r];
                        if (string.IsNullOrWhiteSpace(line)) break;
                        if (line.TrimStart().StartsWith("-")) break;
                        var toks = System.Text.RegularExpressions.Regex.Split(line.Trim(), "\\s{2,}");
                        if (toks.Length <= colIdx) break;
                        var token = toks[colIdx].Trim();
                        // basic numeric check (allow negative and decimal)
                        if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                            values.Add(token);
                        else
                            break;
                    }

                    var colName = headerTokens[colIdx];
                    results.Add((colName, values));
                }

                return results;
            }
            catch
            {
                return new List<(string, List<string>)>();
            }
        }
    }
}
