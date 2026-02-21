using DicomViewer.Core.Models;
using DicomViewer.Core.Services;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Unit tests for SettingsService.
/// </summary>
public class SettingsServiceTests
{
    private readonly SettingsService _sut = new();
    private readonly string _tempDir;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),
            $"DicomSettingsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Clean up any prior app_settings.json in BaseDirectory
        var appSettingsPath = Path.Combine(
            AppContext.BaseDirectory, "app_settings.json");
        if (File.Exists(appSettingsPath))
            File.Delete(appSettingsPath);
    }

    [Fact]
    public void LoadFolderSettings_NoFile_ReturnsDefaults()
    {
        var settings = _sut.LoadFolderSettings(_tempDir);

        Assert.Equal(60, settings.TimeWindowSeconds);
        Assert.Equal(0.5, settings.CeusSpacing);
        Assert.Equal(0.3, settings.ShiSpacing);
    }

    [Fact]
    public void SaveAndLoadFolderSettings_RoundTrips()
    {
        var settings = new FolderSettings
        {
            TimeWindowSeconds = 120,
            CeusSpacing = 0.6,
            ShiSpacing = 0.25
        };
        _sut.SaveFolderSettings(_tempDir, settings);

        var loaded = _sut.LoadFolderSettings(_tempDir);
        Assert.Equal(120, loaded.TimeWindowSeconds);
        Assert.Equal(0.6, loaded.CeusSpacing);
        Assert.Equal(0.25, loaded.ShiSpacing);
    }

    [Fact]
    public void SaveAndLoadAppSettings_RoundTrips()
    {
        var settings = new AppSettings
        {
            LastLoadedDirectory = @"C:\Test\Path"
        };
        _sut.SaveAppSettings(settings);

        var loaded = _sut.LoadAppSettings();
        Assert.Equal(@"C:\Test\Path", loaded.LastLoadedDirectory);

        // Cleanup
        var path = Path.Combine(
            AppContext.BaseDirectory, "app_settings.json");
        if (File.Exists(path)) File.Delete(path);
    }

    [Fact]
    public void LoadAppSettings_NoFile_ReturnsDefaults()
    {
        // Ensure no file exists
        var path = Path.Combine(
            AppContext.BaseDirectory, "app_settings.json");
        if (File.Exists(path)) File.Delete(path);

        var settings = _sut.LoadAppSettings();
        Assert.Null(settings.LastLoadedDirectory);
    }

    ~SettingsServiceTests()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
