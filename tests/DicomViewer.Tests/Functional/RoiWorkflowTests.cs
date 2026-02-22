using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests.Functional;

/// <summary>
/// Functional tests validating end-to-end ROI workflows:
/// create ROIs, persist, reload, export to Excel, and verify round-trips.
/// </summary>
public class RoiWorkflowTests : IDisposable
{
    private readonly string _tempDir;

    public RoiWorkflowTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),
            $"RoiWorkflow_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void FullWorkflow_CreateRois_SaveReload_VerifyIntegrity()
    {
        // Arrange: Create multiple groups with multiple ROIs
        var roiService = new RoiService();

        roiService.AddRoi("g1", new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 10, Y = 20, Width = 100, Height = 50
        });
        roiService.AddRoi("g1", new RoiData
        {
            Shape = RoiShape.Ellipse,
            X = 200, Y = 200, Width = 60, Height = 40
        });
        roiService.AddRoi("g2", new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 50, Y = 50, Width = 80, Height = 80
        });

        // Set mean intensities
        roiService.SetFileMean("g1", "file1.dcm", 42.5);
        roiService.SetFileMean("g1", "file2.dcm", 38.1);
        roiService.SetFileMean("g2", "file3.dcm", 55.0);

        // Act: Save
        roiService.SaveRois(_tempDir);

        // Act: Reload into fresh instance
        var loadedService = new RoiService();
        loadedService.LoadRois(_tempDir);

        // Assert: Group 1 has 2 ROIs
        var g1Rois = loadedService.GetRois("g1");
        Assert.Equal(2, g1Rois.Count);
        Assert.Equal(RoiShape.Rectangle, g1Rois[0].Shape);
        Assert.Equal(RoiShape.Ellipse, g1Rois[1].Shape);

        // Assert: Group 2 has 1 ROI
        var g2Rois = loadedService.GetRois("g2");
        Assert.Single(g2Rois);

        // Assert: File means preserved
        Assert.Equal(42.5, loadedService.GetRoi("g1")!.FileMeans["file1.dcm"]);
        Assert.Equal(55.0, loadedService.GetRoi("g2")!.FileMeans["file3.dcm"]);
    }

    [Fact]
    public void FullWorkflow_CreateRois_UndoAll_SaveReload_Empty()
    {
        var roiService = new RoiService();

        roiService.AddRoi("g1", new RoiData { X = 10 });
        roiService.AddRoi("g1", new RoiData { X = 20 });

        // Undo both
        roiService.UndoRoi("g1");
        roiService.UndoRoi("g1");

        roiService.SaveRois(_tempDir);

        var loaded = new RoiService();
        loaded.LoadRois(_tempDir);

        Assert.Empty(loaded.GetRois("g1"));
    }

    [Fact]
    public void FullWorkflow_RoiHitTest_CorrectRoiIdentified()
    {
        var roiService = new RoiService();

        // Non-overlapping ROIs
        roiService.AddRoi("g1", new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 0, Y = 0, Width = 50, Height = 50
        });
        roiService.AddRoi("g1", new RoiData
        {
            Shape = RoiShape.Rectangle,
            X = 100, Y = 100, Width = 50, Height = 50
        });

        // Hit first ROI
        var hit1 = roiService.HitTestRoi("g1", 25, 25);
        Assert.NotNull(hit1);
        Assert.Equal(0, hit1!.X);

        // Hit second ROI
        var hit2 = roiService.HitTestRoi("g1", 125, 125);
        Assert.NotNull(hit2);
        Assert.Equal(100, hit2!.X);

        // Miss both
        var miss = roiService.HitTestRoi("g1", 75, 75);
        Assert.Null(miss);
    }

    [Fact]
    public void FullWorkflow_ExcelExport_WithMultiGroupData()
    {
        // Arrange: Build groups and ROIs
        var groups = new List<TimeSeriesGroup>
        {
            new()
            {
                GroupId = "g1",
                Label = "12:00:00 - 12:00:30",
                Files =
                [
                    new DicomFileEntry { FileName = "IM_0001" },
                    new DicomFileEntry { FileName = "IM_0002" },
                    new DicomFileEntry { FileName = "IM_0003" }
                ]
            },
            new()
            {
                GroupId = "g2",
                Label = "12:05:00 - 12:05:20",
                Files =
                [
                    new DicomFileEntry { FileName = "IM_0010" },
                    new DicomFileEntry { FileName = "IM_0011" }
                ]
            }
        };

        var rois = new Dictionary<string, RoiData>
        {
            ["g1"] = new RoiData
            {
                Shape = RoiShape.Rectangle,
                X = 10, Y = 20, Width = 100, Height = 50,
                FileMeans = new()
                {
                    ["IM_0001"] = 40.0,
                    ["IM_0002"] = 45.0,
                    ["IM_0003"] = 50.0
                }
            },
            ["g2"] = new RoiData
            {
                Shape = RoiShape.Ellipse,
                X = 50, Y = 50, Width = 80, Height = 60,
                FileMeans = new()
                {
                    ["IM_0010"] = 60.0,
                    ["IM_0011"] = 65.0
                }
            }
        };

        // Act
        var exportPath = Path.Combine(_tempDir, "export.xlsx");
        var exportService = new ExcelExportService();
        exportService.ExportIntensityData(exportPath, groups, rois);

        // Assert: File created
        Assert.True(File.Exists(exportPath));

        // Verify content
        using var wb = new ClosedXML.Excel.XLWorkbook(exportPath);

        // Data sheet: 5 data rows (3 + 2 files) + header
        var dataSheet = wb.Worksheet("ROI Data");
        Assert.Equal("12:00:00 - 12:00:30", dataSheet.Cell(2, 1).GetString());
        Assert.Equal("IM_0001", dataSheet.Cell(2, 2).GetString());
        Assert.Equal(40.0, dataSheet.Cell(2, 3).GetDouble());
        Assert.Equal("12:05:00 - 12:05:20", dataSheet.Cell(5, 1).GetString());

        // Summary: 2 group rows
        var summarySheet = wb.Worksheet("Summary");
        Assert.Equal(3, summarySheet.Cell(2, 2).GetDouble()); // g1 count
        Assert.Equal(2, summarySheet.Cell(3, 2).GetDouble()); // g2 count
    }

    [Fact]
    public void FullWorkflow_SettingsPersistence_AcrossReloads()
    {
        var settingsService = new SettingsService();

        // Save folder settings
        var folderSettings = new FolderSettings
        {
            TimeWindowSeconds = 120,
            CeusSpacing = 0.55,
            ShiSpacing = 0.28
        };
        settingsService.SaveFolderSettings(_tempDir, folderSettings);

        // Save app settings
        var appSettings = new AppSettings
        {
            LastLoadedDirectory = _tempDir,
            LastSelectedNodePath = "Group1/IM_0005"
        };
        settingsService.SaveAppSettings(appSettings, _tempDir);

        // Reload
        var loadedFolder = settingsService.LoadFolderSettings(_tempDir);
        var loadedApp = settingsService.LoadAppSettings(_tempDir);

        Assert.Equal(120, loadedFolder.TimeWindowSeconds);
        Assert.Equal(0.55, loadedFolder.CeusSpacing);
        Assert.Equal(_tempDir, loadedApp.LastLoadedDirectory);
        Assert.Equal("Group1/IM_0005", loadedApp.LastSelectedNodePath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
