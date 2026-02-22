using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QLabExportFrameDataExtract.Tests;

public class JsonSummaryTests
{
    [Fact]
    public void SummaryContains_1e3_MmHg0_WithExpectedValues()
    {
        string[] candidates = new[] {
            Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "DICOM 20251125", "sample_master_output.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SampleFiles", "DICOM 20251125", "sample_master_output.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "SampleFiles", "DICOM 20251125", "sample_master_output.json")
        };

        var file = candidates.Select(p => Path.GetFullPath(p)).FirstOrDefault(File.Exists);
        Assert.False(string.IsNullOrEmpty(file), $"sample_master_output.json not found (checked: {string.Join(";", candidates)})");

        var text = File.ReadAllText(file);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        JsonElement? found = null;
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.TryGetProperty("ConcentrationName", out var c) && c.ValueKind == JsonValueKind.String && c.GetString() == "1e3")
            {
                if (el.TryGetProperty("MmHg", out var m) && m.ValueKind == JsonValueKind.Number && m.GetDouble() == 0.0)
                {
                    found = el;
                    break;
                }
            }
        }

        Assert.True(found.HasValue, "Entry for ConcentrationName=1e3 and MmHg=0 not found in JSON");
        var node = found.Value;

        double GetDouble(string name) => node.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDouble() : throw new Exception($"Missing numeric property {name}");
        int GetInt(string name) => node.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : throw new Exception($"Missing int property {name}");

        Assert.Equal(41, GetInt("Attenuation"));
        Assert.Equal(705, GetInt("kPa"));

        // FramePixelIntensity array
        Assert.True(node.TryGetProperty("FramePixelIntensity", out var fp) && fp.ValueKind == JsonValueKind.Array, "FramePixelIntensity missing or not an array");
        var expected = new[] { "38.20", "36.01", "37.09", "36.60", "36.76" };
        var actual = fp.EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(expected, actual);

        double raw = GetDouble("RawPixelIntensity");
        double std = GetDouble("PixelIntensityStd");
        double decAdj = GetDouble("DecibelAdjustmentValue");
        double psiNorm = GetDouble("PsiNormalizedPixelIntensity");
        double atmos = GetDouble("AtmosphericPixelIntensity");
        double atmosNorm = GetDouble("AtmosphericNormalizedPixelIntensity");

        Assert.Equal(36.932, raw, 6);
        Assert.Equal(0.8097345243967328, std, 12);
        Assert.Equal(8.0, decAdj, 6);
        Assert.Equal(44.932, psiNorm, 6);
        Assert.Equal(22.30215833333333, atmos, 12);
        Assert.Equal(22.62984166666667, atmosNorm, 12);
    }
}
