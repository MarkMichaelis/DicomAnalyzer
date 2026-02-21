namespace DicomViewer.Core.Services;

/// <summary>
/// Simple file-based logging service for diagnostics and error tracking.
/// </summary>
public class LogService
{
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private bool _isDiagnosticsEnabled;

    public LogService(string? logFilePath = null)
    {
        _logFilePath = logFilePath ?? Path.Combine(AppContext.BaseDirectory, "app.log");
    }

    /// <summary>
    /// Gets or sets whether diagnostics (verbose) logging is enabled.
    /// </summary>
    public bool IsDiagnosticsEnabled
    {
        get => _isDiagnosticsEnabled;
        set => _isDiagnosticsEnabled = value;
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void LogWarning(string message)
    {
        WriteLog("WARN", message);
    }

    /// <summary>
    /// Logs an error message with optional exception details.
    /// </summary>
    public void LogError(string message, Exception? ex = null)
    {
        var text = ex != null ? $"{message}: {ex}" : message;
        WriteLog("ERROR", text);
    }

    /// <summary>
    /// Logs a diagnostic message (only written when diagnostics mode is enabled).
    /// </summary>
    public void LogDiagnostic(string message)
    {
        if (_isDiagnosticsEnabled)
            WriteLog("DIAG", message);
    }

    private void WriteLog(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
            catch
            {
                // Silently fail if unable to write log
            }
        }
    }
}
