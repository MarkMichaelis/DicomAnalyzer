using ClosedXML.Excel;
using System.IO;
using System.Linq;

namespace QLabExportFrameDataExtract.Tests;

public class DataExtractorIntegrationTests
{
    [Fact]
    public void ExtractAll_FindsRecordsAcrossFiles()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "qlab_integration_test");
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        // Create two sample excel files
        string f1 = Path.Combine(tempDir, "a.xlsx");
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("B3").Value = "DICOM1";
            ws.Cell("B5").Value = "P1";
            ws.Cell("B7").Value = "2025-11-25";
            ws.Cell(3, 3).Value = "Echo Mean (dB)"; // place header at row3 col3
            ws.Cell(4, 3).Value = -5.0;
            ws.Cell(5, 3).Value = -4.9;
            wb.SaveAs(f1);
        }

        string f2 = Path.Combine(tempDir, "b.xlsx");
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("B3").Value = "DICOM2";
            ws.Cell("B5").Value = "P2";
            ws.Cell("B7").Value = "2025-11-26";
            ws.Cell(2, 3).Value = "Echo Mean (dB)"; // header at row2 col3
            ws.Cell(3, 3).Value = -6.1;
            wb.SaveAs(f2);
        }

        try
        {
            var extractor = new DataExtractor();
            var recs = extractor.ExtractAll(tempDir).ToList();

            Assert.Equal(3, recs.Count);
            Assert.Contains(recs, r => r.DICOMFilePath == "DICOM1" && r.Value_dB.StartsWith("-5"));
            Assert.Contains(recs, r => r.DICOMFilePath == "DICOM2" && r.Value_dB.StartsWith("-6"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
