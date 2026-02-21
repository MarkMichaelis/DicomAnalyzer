using ClosedXML.Excel;
using DicomViewer.Core.Models;
using DicomViewer.Core.Services;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Unit tests for ExcelExportService.
/// </summary>
public class ExcelExportServiceTests : IDisposable
{
    private readonly ExcelExportService _sut = new();
    private readonly string _tempDir;

    public ExcelExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),
            $"ExcelTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void ExportIntensityData_CreatesValidExcelFile()
    {
        var groups = CreateSampleGroups();
        var rois = CreateSampleRois();
        var path = Path.Combine(_tempDir, "test.xlsx");

        _sut.ExportIntensityData(path, groups, rois);

        Assert.True(File.Exists(path));
        using var wb = new XLWorkbook(path);
        Assert.Equal(2, wb.Worksheets.Count);
        Assert.Equal("ROI Data", wb.Worksheets.First().Name);
        Assert.Equal("Summary", wb.Worksheets.Last().Name);
    }

    [Fact]
    public void ExportIntensityData_DataSheet_HasCorrectRows()
    {
        var groups = CreateSampleGroups();
        var rois = CreateSampleRois();
        var path = Path.Combine(_tempDir, "test_rows.xlsx");

        _sut.ExportIntensityData(path, groups, rois);

        using var wb = new XLWorkbook(path);
        var sheet = wb.Worksheet("ROI Data");
        // Header + 2 data rows (2 files in group)
        Assert.Equal("Group", sheet.Cell(1, 1).GetString());
        Assert.Equal("File Name", sheet.Cell(1, 2).GetString());
        Assert.Equal("file1.dcm", sheet.Cell(2, 2).GetString());
        Assert.Equal("file2.dcm", sheet.Cell(3, 2).GetString());
    }

    [Fact]
    public void ExportIntensityData_SummarySheet_HasStats()
    {
        var groups = CreateSampleGroups();
        var rois = CreateSampleRois();
        var path = Path.Combine(_tempDir, "test_summary.xlsx");

        _sut.ExportIntensityData(path, groups, rois);

        using var wb = new XLWorkbook(path);
        var sheet = wb.Worksheet("Summary");
        Assert.Equal("Group", sheet.Cell(1, 1).GetString());
        Assert.Equal(2, sheet.Cell(2, 2).GetDouble()); // file count
    }

    [Fact]
    public void ExportIntensityData_EmptyRois_CreatesEmptySheets()
    {
        var groups = CreateSampleGroups();
        var rois = new Dictionary<string, RoiData>();
        var path = Path.Combine(_tempDir, "test_empty.xlsx");

        _sut.ExportIntensityData(path, groups, rois);

        Assert.True(File.Exists(path));
        using var wb = new XLWorkbook(path);
        var sheet = wb.Worksheet("ROI Data");
        // Only header row
        Assert.Equal("Group", sheet.Cell(1, 1).GetString());
    }

    private static List<TimeSeriesGroup> CreateSampleGroups()
    {
        return
        [
            new TimeSeriesGroup
            {
                GroupId = "g1",
                Label = "12:00:00",
                Files =
                [
                    new DicomFileEntry { FileName = "file1.dcm" },
                    new DicomFileEntry { FileName = "file2.dcm" }
                ]
            }
        ];
    }

    private static Dictionary<string, RoiData> CreateSampleRois()
    {
        return new()
        {
            ["g1"] = new RoiData
            {
                GroupId = "g1",
                X = 10, Y = 20, Width = 100, Height = 50,
                FileMeans = new()
                {
                    ["file1.dcm"] = 42.5,
                    ["file2.dcm"] = 38.1
                }
            }
        };
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}