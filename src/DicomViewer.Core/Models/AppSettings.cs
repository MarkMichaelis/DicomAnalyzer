namespace DicomViewer.Core.Models;

/// <summary>
/// Application-level settings persisted across sessions.
/// </summary>
public class AppSettings
{
    /// <summary>Last directory that was loaded.</summary>
    public string? LastLoadedDirectory { get; set; }

    /// <summary>Window left position.</summary>
    public double? WindowLeft { get; set; }

    /// <summary>Window top position.</summary>
    public double? WindowTop { get; set; }

    /// <summary>Window width.</summary>
    public double? WindowWidth { get; set; }

    /// <summary>Window height.</summary>
    public double? WindowHeight { get; set; }

    /// <summary>Window state (Normal, Maximized, Minimized).</summary>
    public string? WindowState { get; set; }
}
