using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests;

public class AppSettingsNodePathTests
{
    [Fact]
    public void LastSelectedNodePath_RoundTrips_ThroughSettings()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            // Write a settings file with a known path
            var settings = new AppSettings { LastSelectedNodePath = "Group1/IM_0001" };
            var service = new SettingsService();
            service.SaveAppSettings(settings, tempDir);

            // Act
            var loaded = service.LoadAppSettings(tempDir);

            // Assert
            Assert.Equal("Group1/IM_0001", loaded.LastSelectedNodePath);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LastSelectedNodePath_IsNull_ByDefault()
    {
        var settings = new AppSettings();
        Assert.Null(settings.LastSelectedNodePath);
    }
}
