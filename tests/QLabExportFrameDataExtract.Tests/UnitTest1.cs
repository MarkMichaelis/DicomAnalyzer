using ClosedXML.Excel;

namespace QLabExportFrameDataExtract.Tests;

public class ExcelReaderTests
{
    [Fact]
    public void ExtractMetadata_ReadsFixedCells_ReturnsCorrectValues()
    {
        // Arrange
        string testFilePath = Path.Combine(Path.GetTempPath(), "test_metadata.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Sheet1");
            worksheet.Cell("B3").Value = "/path/to/dicom/file";
            worksheet.Cell("B5").Value = "John Doe";
            worksheet.Cell("B7").Value = "2025-11-25";
            workbook.SaveAs(testFilePath);
        }

        try
        {
            // Act
            var reader = new ExcelReader(testFilePath);
            var metadata = reader.ExtractMetadata();

            // Assert
            Assert.Equal("/path/to/dicom/file", metadata.DICOMFilePath);
            Assert.Equal("John Doe", metadata.PatientName);
            Assert.Equal("2025-11-25", metadata.DICOMFileDate);
        }
        finally
        {
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
    }

    [Fact]
    public void ExtractEchoMeanColumns_FindsMultipleColumnsAndValues()
    {
        // Arrange
        string testFilePath = Path.Combine(Path.GetTempPath(), "test_echo_columns.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Sheet1");
            // place headers on row 4 to ensure search works
            worksheet.Cell(4, 2).Value = "Echo Mean (dB)";
            worksheet.Cell(4, 4).Value = "Echo Mean (dB) 2";

            // Column 2 values
            worksheet.Cell(5, 2).Value = -5.2;
            worksheet.Cell(6, 2).Value = -4.8;
            worksheet.Cell(7, 2).Value = -5.1;

            // Column 4 values
            worksheet.Cell(5, 4).Value = -6.3;
            worksheet.Cell(6, 4).Value = -6.1;

            workbook.SaveAs(testFilePath);
        }

        try
        {
            var reader = new ExcelReader(testFilePath);
            var cols = reader.ExtractEchoMeanColumns();

            // Assert we found two columns
            Assert.Equal(2, cols.Count);

            var first = cols.First(c => c.ColumnName.Contains("Echo Mean (dB)") && !c.ColumnName.Contains("2"));
            Assert.Equal(3, first.Values.Count);
            Assert.Equal("-5.2", first.Values[0]);

            var second = cols.First(c => c.ColumnName.Contains("Echo Mean (dB) 2"));
            Assert.Equal(2, second.Values.Count);
            Assert.Equal("-6.3", second.Values[0]);
        }
        finally
        {
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
    }
}
