using ClosedXML.Excel;

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
}
