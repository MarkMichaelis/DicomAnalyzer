using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Boundary and error tests for SettingsService (Right-BICEP: B, E).
/// </summary>
public class SettingsServiceBoundaryTests : IDisposable
{
    private readonly SettingsService _sut = new();
    private readonly string _tempDir;

    public SettingsServiceBoundaryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),
            $"SettingsBoundaryTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void LoadFolderSettings_CorruptedFile_ReturnsDefaults()
    {
        var path = Path.Combine(_tempDir, "dicom_viewer_directory.settings");
        File.WriteAllText(path, "not valid json at all{{}}}");

        var settings = _sut.LoadFolderSettings(_tempDir);

        Assert.Equal(60, settings.TimeWindowSeconds);
        Assert.Equal(0.5, settings.CeusSpacing);
        Assert.Equal(0.3, settings.ShiSpacing);
    }

    [Fact]
    public void LoadFolderSettings_EmptyFile_ReturnsDefaults()
    {
        var path = Path.Combine(_tempDir, "dicom_viewer_directory.settings");
        File.WriteAllText(path, "");

        var settings = _sut.LoadFolderSettings(_tempDir);

        Assert.Equal(60, settings.TimeWindowSeconds);
    }

    [Fact]
    public void SaveFolderSettings_CreatesFile()
    {
        var settings = new FolderSettings
        {
            TimeWindowSeconds = 90,
            CeusSpacing = 0.6,
            ShiSpacing = 0.25
        };

        _sut.SaveFolderSettings(_tempDir, settings);

        var path = Path.Combine(_tempDir, "dicom_viewer_directory.settings");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void SaveAndLoadAppSettings_WithDirectory_RoundTrips()
    {
        var settings = new AppSettings
        {
            LastLoadedDirectory = @"C:\Test",
            WindowLeft = 100.5,
            WindowTop = 200.5,
            WindowWidth = 800,
            WindowHeight = 600,
            WindowState = "Maximized",
            LastSelectedNodePath = "Group1/IM_0001"
        };

        _sut.SaveAppSettings(settings, _tempDir);
        var loaded = _sut.LoadAppSettings(_tempDir);

        Assert.Equal(@"C:\Test", loaded.LastLoadedDirectory);
        Assert.Equal(100.5, loaded.WindowLeft);
        Assert.Equal(200.5, loaded.WindowTop);
        Assert.Equal(800, loaded.WindowWidth);
        Assert.Equal(600, loaded.WindowHeight);
        Assert.Equal("Maximized", loaded.WindowState);
        Assert.Equal("Group1/IM_0001", loaded.LastSelectedNodePath);
    }

    [Fact]
    public void LoadAppSettings_CorruptedFile_ReturnsDefaults()
    {
        var path = Path.Combine(_tempDir, "app_settings.json");
        File.WriteAllText(path, "{{broken json}}");

        var settings = _sut.LoadAppSettings(_tempDir);

        Assert.Null(settings.LastLoadedDirectory);
    }

    [Fact]
    public void LoadAppSettings_EmptyJsonObject_ReturnsDefaults()
    {
        var path = Path.Combine(_tempDir, "app_settings.json");
        File.WriteAllText(path, "{}");

        var settings = _sut.LoadAppSettings(_tempDir);

        Assert.Null(settings.LastLoadedDirectory);
        Assert.Null(settings.WindowLeft);
    }

    [Fact]
    public void SaveAppSettings_NonexistentDirectory_CreatesIt()
    {
        var nested = Path.Combine(_tempDir, "sub", "dir");

        _sut.SaveAppSettings(new AppSettings { LastLoadedDirectory = "test" }, nested);

        Assert.True(File.Exists(Path.Combine(nested, "app_settings.json")));
    }

    [Fact]
    public void FolderSettings_DefaultValues()
    {
        var settings = new FolderSettings();

        Assert.Equal(60, settings.TimeWindowSeconds);
        Assert.Equal(0.5, settings.CeusSpacing);
        Assert.Equal(0.3, settings.ShiSpacing);
    }

    [Fact]
    public void AppSettings_DefaultValues()
    {
        var settings = new AppSettings();

        Assert.Null(settings.LastLoadedDirectory);
        Assert.Null(settings.WindowLeft);
        Assert.Null(settings.WindowTop);
        Assert.Null(settings.WindowWidth);
        Assert.Null(settings.WindowHeight);
        Assert.Null(settings.WindowState);
        Assert.Null(settings.LastSelectedNodePath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
