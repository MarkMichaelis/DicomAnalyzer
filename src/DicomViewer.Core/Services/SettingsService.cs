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
        return LoadAppSettings(AppContext.BaseDirectory);
    }

    /// <summary>Loads application-level settings from a specific directory.</summary>
    public AppSettings LoadAppSettings(string directory)
    {
        var path = Path.Combine(directory, AppFile);
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
        SaveAppSettings(settings, AppContext.BaseDirectory);
    }

    /// <summary>Saves application-level settings to a specific directory.</summary>
    public void SaveAppSettings(AppSettings settings, string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, AppFile);
        File.WriteAllText(path,
            JsonSerializer.Serialize(settings, JsonOpts));
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
