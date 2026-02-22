using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Boundary and edge-case tests for ClassificationService (Right-BICEP: B, I).
/// </summary>
public class ClassificationServiceBoundaryTests
{
    private readonly ClassificationService _sut = new();

    [Fact]
    public void Classify_SingleElementSpacing_UsesFirstElement()
    {
        // Single-element array should use index [0]
        var result = _sut.Classify([0.3]);
        Assert.Equal(FileClassification.SHI, result);
    }

    [Fact]
    public void Classify_SingleElementNearCEUS_ReturnsCEUS()
    {
        var result = _sut.Classify([0.5]);
        Assert.Equal(FileClassification.CEUS, result);
    }

    [Fact]
    public void Classify_VeryLargeSpacing_ReturnsCEUS()
    {
        // Far from both targets, but equidistant favors CEUS
        var result = _sut.Classify([100.0, 100.0]);
        Assert.Equal(FileClassification.CEUS, result);
    }

    [Fact]
    public void Classify_ZeroSpacing_ReturnsSHI()
    {
        // 0.0 is closer to 0.3 (SHI) than 0.5 (CEUS)
        var result = _sut.Classify([0.0, 0.0]);
        Assert.Equal(FileClassification.SHI, result);
    }

    [Fact]
    public void Classify_NegativeSpacing_CalculatesAbsoluteDistance()
    {
        // Math.Abs(-0.3 - 0.3) = 0.6, Math.Abs(-0.3 - 0.5) = 0.8
        // SHI distance < CEUS distance => SHI
        var result = _sut.Classify([-0.3, -0.3]);
        Assert.Equal(FileClassification.SHI, result);
    }

    [Theory]
    [InlineData(0.29, FileClassification.SHI)]
    [InlineData(0.31, FileClassification.SHI)]
    [InlineData(0.49, FileClassification.CEUS)]
    [InlineData(0.51, FileClassification.CEUS)]
    public void Classify_NearBoundaryValues(
        double spacing, FileClassification expected)
    {
        var result = _sut.Classify([spacing, spacing]);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Classify_InverseVerification_CustomTargetsSwapped()
    {
        // When CEUS target is lower than SHI target, classification inverts
        var resultNormal = _sut.Classify([0.3, 0.3],
            ceusTarget: 0.5, shiTarget: 0.3);
        var resultSwapped = _sut.Classify([0.3, 0.3],
            ceusTarget: 0.3, shiTarget: 0.5);

        Assert.Equal(FileClassification.SHI, resultNormal);
        Assert.Equal(FileClassification.CEUS, resultSwapped);
    }

    [Fact]
    public void Classify_MismatchedRowCol_UsesRowSpacing()
    {
        // Array [row, col] — only first element (row) is used
        var result = _sut.Classify([0.3, 0.5]);
        Assert.Equal(FileClassification.SHI, result);
    }

    [Fact]
    public void Classify_MismatchedRowCol_Reversed()
    {
        var result = _sut.Classify([0.5, 0.3]);
        Assert.Equal(FileClassification.CEUS, result);
    }
}
