using System.Text.Json;
using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Manages app-level and per-folder settings persistence.
/// </summary>
public class SettingsService
{
    private const string FolderFile = "dicom_viewer_directory.settings";
    private const string AppFile = "app_settings.json";

    /// <summary>Loads per-folder settings.</summary>
    public FolderSettings LoadFolderSettings(string directoryPath)
    {
        var path = Path.Combine(directoryPath, FolderFile);
        if (!File.Exists(path)) return new FolderSettings();
        try
        {
            return JsonSerializer.Deserialize<FolderSettings>(
                File.ReadAllText(path), JsonOpts)
                ?? new FolderSettings();
        }
        catch { return new FolderSettings(); }
    }

    /// <summary>Saves per-folder settings.</summary>
    public void SaveFolderSettings(
        string directoryPath, FolderSettings settings)
    {
        var path = Path.Combine(directoryPath, FolderFile);
        File.WriteAllText(path,
            JsonSerializer.Serialize(settings, JsonOpts));
    }

    /// <summary>Loads application-level settings.</summary>
    public AppSettings LoadAppSettings()
    {
        var path = GetAppSettingsPath();
        if (!File.Exists(path)) return new AppSettings();
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(path), JsonOpts)
                ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    /// <summary>Saves application-level settings.</summary>
    public void SaveAppSettings(AppSettings settings)
    {
        var path = GetAppSettingsPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path,
            JsonSerializer.Serialize(settings, JsonOpts));
    }

    private static string GetAppSettingsPath() =>
        Path.Combine(AppContext.BaseDirectory, AppFile);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
