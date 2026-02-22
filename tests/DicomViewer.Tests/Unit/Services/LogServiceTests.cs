using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests;

public class LogServiceTests
{
    [Fact]
    public void LogInfo_WritesToLogFile()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);
            sut.LogInfo("test message");

            var content = File.ReadAllText(logPath);
            Assert.Contains("[INFO] test message", content);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void LogError_IncludesExceptionDetails()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);
            var ex = new InvalidOperationException("test error");
            sut.LogError("Something failed", ex);

            var content = File.ReadAllText(logPath);
            Assert.Contains("[ERROR]", content);
            Assert.Contains("test error", content);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void LogDiagnostic_WhenDisabled_DoesNotWrite()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);
            sut.IsDiagnosticsEnabled = false;
            sut.LogDiagnostic("should not appear");

            Assert.False(File.Exists(logPath));
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Fact]
    public void LogDiagnostic_WhenEnabled_WritesToFile()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);
            sut.IsDiagnosticsEnabled = true;
            sut.LogDiagnostic("diagnostic info");

            var content = File.ReadAllText(logPath);
            Assert.Contains("[DIAG] diagnostic info", content);
        }
        finally
        {
            File.Delete(logPath);
        }
    }
}
