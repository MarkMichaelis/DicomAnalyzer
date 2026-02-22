using ClosedXML.Excel;
using System.Collections.Generic;
using System.Linq;

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
        using var wb = new XLWorkbook(_path);
        var ws = wb.Worksheets.First();

        var meta = new ExcelMetadata
        {
            DICOMFilePath = ws.Cell("B3").GetString() ?? string.Empty,
            PatientName = ws.Cell("B5").GetString() ?? string.Empty,
            DICOMFileDate = ws.Cell("B7").GetString() ?? string.Empty
        };

        return meta;
    }

    /// <summary>
    /// Finds all columns whose header contains "Echo Mean (dB)" and returns their
    /// header name and list of string values (one per row below the header).
    /// </summary>
    public List<(string ColumnName, List<string> Values)> ExtractEchoMeanColumns(int headerSearchLimit = 20)
    {
        using var wb = new XLWorkbook(_path);
        var ws = wb.Worksheets.First();

        // Find header row by searching the top N rows for any cell that contains the header text
        int headerRow = -1;
        for (int r = 1; r <= headerSearchLimit; r++)
        {
            var row = ws.Row(r);
            if (row.CellsUsed().Any(c => c.GetString().Contains("Echo Mean (dB)", System.StringComparison.OrdinalIgnoreCase)))
            {
                headerRow = r;
                break;
            }
        }

        var results = new List<(string ColumnName, List<string> Values)>();
        if (headerRow == -1)
            return results;

        int lastCol = ws.Row(headerRow).LastCellUsed().Address.ColumnNumber;

        for (int col = 1; col <= lastCol; col++)
        {
            var headerCell = ws.Cell(headerRow, col);
            var headerText = headerCell.GetString();
            if (string.IsNullOrWhiteSpace(headerText))
                continue;

            if (headerText.Contains("Echo Mean (dB)", System.StringComparison.OrdinalIgnoreCase))
            {
                var values = new List<string>();
                int row = headerRow + 1;
                while (true)
                {
                    var cell = ws.Cell(row, col);
                    if (cell.IsEmpty())
                        break;
                    values.Add(cell.GetString());
                    row++;
                }

                results.Add((headerText, values));
            }
        }

        return results;
    }
}
