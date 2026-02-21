namespace DicomViewer.Core.Models;

/// <summary>
/// Application-level settings persisted across sessions.
/// </summary>
public class AppSettings
{
    /// <summary>Last directory that was loaded.</summary>
    public string? LastLoadedDirectory { get; set; }
}
