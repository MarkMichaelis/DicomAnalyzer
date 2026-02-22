using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Boundary tests for DicomFileService.ParseSpacingString (Right-BICEP: B, E).
/// </summary>
public class ParseSpacingBoundaryTests
{
    [Fact]
    public void ParseSpacingString_Null_ReturnsNull()
    {
        Assert.Null(DicomFileService.ParseSpacingString(null!));
    }

    [Fact]
    public void ParseSpacingString_Whitespace_ReturnsNull()
    {
        Assert.Null(DicomFileService.ParseSpacingString("   "));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("not a number")]
    [InlineData("[]")]
    public void ParseSpacingString_NonNumeric_ReturnsNull(string input)
    {
        Assert.Null(DicomFileService.ParseSpacingString(input));
    }

    [Theory]
    [InlineData("0.3,0.3", 0.3, 0.3)]
    [InlineData("0.3, 0.3", 0.3, 0.3)]
    [InlineData("0.3\\0.3", 0.3, 0.3)]
    public void ParseSpacingString_CommaSeparated_ParsesCorrectly(
        string input, double expectedRow, double expectedCol)
    {
        var result = DicomFileService.ParseSpacingString(input);
        Assert.NotNull(result);
        Assert.Equal(expectedRow, result![0], 6);
        Assert.Equal(expectedCol, result[1], 6);
    }

    [Theory]
    [InlineData("[0.5, 0.5]", 0.5, 0.5)]
    [InlineData("[0.3,0.3]", 0.3, 0.3)]
    public void ParseSpacingString_BracketWrapped_ParsesCorrectly(
        string input, double expectedRow, double expectedCol)
    {
        var result = DicomFileService.ParseSpacingString(input);
        Assert.NotNull(result);
        Assert.Equal(expectedRow, result![0], 6);
        Assert.Equal(expectedCol, result[1], 6);
    }

    [Fact]
    public void ParseSpacingString_ThreeValues_UsesFirstTwo()
    {
        var result = DicomFileService.ParseSpacingString("0.1,0.2,0.3");
        Assert.NotNull(result);
        Assert.Equal(0.1, result![0], 6);
        Assert.Equal(0.2, result[1], 6);
    }

    [Fact]
    public void ParseSpacingString_SingleValue_DuplicatesIt()
    {
        var result = DicomFileService.ParseSpacingString("0.42");
        Assert.NotNull(result);
        Assert.Equal(0.42, result![0], 6);
        Assert.Equal(0.42, result[1], 6);
    }

    [Fact]
    public void ParseSpacingString_ScientificNotation_Parses()
    {
        var result = DicomFileService.ParseSpacingString("3.0E-4\\3.0E-4");
        Assert.NotNull(result);
        Assert.Equal(0.0003, result![0], 6);
    }

    [Fact]
    public void ParseSpacingString_LeadingTrailingWhitespace_Trimmed()
    {
        var result = DicomFileService.ParseSpacingString("  0.5 \\ 0.5  ");
        Assert.NotNull(result);
        Assert.Equal(0.5, result![0], 6);
        Assert.Equal(0.5, result[1], 6);
    }
}
