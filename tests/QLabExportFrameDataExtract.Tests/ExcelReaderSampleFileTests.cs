using System;
using System.IO;
using System.Linq;

namespace QLabExportFrameDataExtract.Tests;

public class ExcelReaderSampleFileTests
{
    [Fact]
    public void ExtractMetadataAndEchoValues_FromSample00_41_xls()
    {
        // locate sample file in repository
        string[] candidates = new[] {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "SampleFiles", "DICOM 20251125", "1e3", "00_41.xls"),
            Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "DICOM 20251125", "1e3", "00_41.xls")
        };

        string file = candidates.Select(p => Path.GetFullPath(p)).FirstOrDefault(File.Exists);
        Assert.False(string.IsNullOrEmpty(file), $"Sample file not found. Checked: {string.Join(";", candidates)}");

        var reader = new ExcelReader(file);
        var meta = reader.ExtractMetadata();

        Assert.Equal(Path.GetFullPath(file), Path.GetFullPath(file)); // sanity: path
        Assert.Equal("I:\\BUL Fam\\Hanna\\DICOM 20251125\\IM_0075", meta.DICOMFilePath);
        Assert.Equal("CONCENTRATION SCURVE 20251124", meta.PatientName.Trim());
        Assert.Equal("11/24/2025", meta.DICOMFileDate.Split(' ')[0]);

        var cols = reader.ExtractEchoMeanColumns();
        Assert.NotEmpty(cols);
        var col = cols.First();
        // verify first five frame values
        Assert.True(col.Values.Count >= 5, "Expected at least 5 echo mean values");
        Assert.Equal("38.20", col.Values[0]);
        Assert.Equal("36.01", col.Values[1]);
        Assert.Equal("37.09", col.Values[2]);
        Assert.Equal("36.60", col.Values[3]);
        Assert.Equal("36.76", col.Values[4]);
    }
}
