using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Boundary and edge-case tests for LogService (Right-BICEP: B, E, Right).
/// </summary>
public class LogServiceBoundaryTests
{
    [Fact]
    public void LogWarning_WritesWarnLevel()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);
            sut.LogWarning("low disk space");

            var content = File.ReadAllText(logPath);
            Assert.Contains("[WARN] low disk space", content);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void LogError_WithoutException_WritesMessage()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);
            sut.LogError("generic error");

            var content = File.ReadAllText(logPath);
            Assert.Contains("[ERROR] generic error", content);
            Assert.DoesNotContain("System.", content);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void LogInfo_MultipleMessages_AllAppended()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);
            sut.LogInfo("first");
            sut.LogInfo("second");
            sut.LogInfo("third");

            var lines = File.ReadAllLines(logPath);
            Assert.Equal(3, lines.Length);
            Assert.Contains("first", lines[0]);
            Assert.Contains("third", lines[2]);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void LogInfo_ContainsTimestamp()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);
            sut.LogInfo("timestamped");

            var content = File.ReadAllText(logPath);
            // Timestamp format: yyyy-MM-dd HH:mm:ss.fff
            Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}", content);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void IsDiagnosticsEnabled_DefaultsFalse()
    {
        var sut = new LogService();
        Assert.False(sut.IsDiagnosticsEnabled);
    }

    [Fact]
    public void LogDiagnostic_AfterToggle_WritesWhenEnabled()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
        try
        {
            var sut = new LogService(logPath);

            // Disabled - should not write
            sut.LogDiagnostic("hidden");
            Assert.False(File.Exists(logPath));

            // Enable and write
            sut.IsDiagnosticsEnabled = true;
            sut.LogDiagnostic("visible");

            var content = File.ReadAllText(logPath);
            Assert.DoesNotContain("hidden", content);
            Assert.Contains("[DIAG] visible", content);
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Fact]
    public void LogService_InvalidPath_DoesNotThrow()
    {
        // LogService silently catches write failures
        var sut = new LogService(@"Z:\nonexistent\dir\test.log");
        var ex = Record.Exception(() => sut.LogInfo("should not throw"));
        Assert.Null(ex);
    }
}
