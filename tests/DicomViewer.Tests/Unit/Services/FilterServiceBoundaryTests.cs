using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests.Unit.Services;

/// <summary>
/// Boundary and edge-case tests for FilterService (Right-BICEP: B, E).
/// </summary>
public class FilterServiceBoundaryTests
{
    private readonly FilterService _sut = new();

    [Fact]
    public void MatchesFilter_EmptyFilter_MatchesViaDisplayName()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string> { "0008,0060 Modality: US" };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "", tags));
    }

    [Fact]
    public void MatchesFilter_EmptyDisplayName_FallsBackToTags()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "" };
        var tags = new List<string> { "0008,0060 Modality: US" };

        Assert.True(_sut.MatchesFilter(file, "", "Modality", tags));
    }

    [Fact]
    public void MatchesFilter_EmptyTagsList_NoTagMatch()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string>();

        Assert.False(_sut.MatchesFilter(file, "IM_0001", "NONEXISTENT", tags));
    }

    [Fact]
    public void MatchesFilter_ExactDisplayNameMatch()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "IM_0001", null));
    }

    [Fact]
    public void MatchesFilter_WhitespaceFilter_MatchesTagsWithSpaces()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string> { "0008,0060 Modality: US" };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", " ", tags));
    }

    [Fact]
    public void MatchesFilter_BackslashInFilter_MatchesLiterally()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string> { "0028,0030 PixelSpacing: 0.3\\0.3" };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "0.3\\0.3", tags));
    }

    [Fact]
    public void MatchesFilter_LastTagMatches_ReturnsTrue()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string>
        {
            "0008,0060 Modality: US",
            "0010,0010 PatientName: DOE^JOHN",
            "0018,602C DeltaX: 0.00014"
        };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "DeltaX", tags));
    }

    [Fact]
    public void MatchesFilter_NoTagsOrDisplayMatch_ReturnsFalse()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string>
        {
            "0008,0060 Modality: US",
            "0010,0010 PatientName: DOE^JOHN"
        };

        Assert.False(_sut.MatchesFilter(file, "IM_0001", "MRI", tags));
    }
}
