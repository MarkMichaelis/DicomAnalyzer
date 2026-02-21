using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using Xunit;

namespace DicomViewer.Tests;

public class FilterServiceTests
{
    private readonly FilterService _sut = new();

    [Fact]
    public void MatchesFilter_DisplayNameContainsFilter_ReturnsTrue()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string> { "0008,0060 Modality: US" };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "IM_0001", tags));
    }

    [Fact]
    public void MatchesFilter_TagValueContainsFilter_ReturnsTrue()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string> { "0018,602C FrameTimeVector: 142732" };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "142732", tags));
    }

    [Fact]
    public void MatchesFilter_NoMatch_ReturnsFalse()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string> { "0008,0060 Modality: US" };

        Assert.False(_sut.MatchesFilter(file, "IM_0001", "NONEXISTENT", tags));
    }

    [Fact]
    public void MatchesFilter_NullTags_ReturnsFalse()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };

        Assert.False(_sut.MatchesFilter(file, "IM_0001", "142732", null));
    }

    [Fact]
    public void MatchesFilter_CaseInsensitive_ReturnsTrue()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string> { "0008,0060 Modality: US" };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "modality", tags));
    }

    [Fact]
    public void MatchesFilter_PartialTagIdMatch_ReturnsTrue()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        var tags = new List<string> { "0008,0060 Modality: US" };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "0008", tags));
    }

    [Fact]
    public void MatchesFilter_NumericTagValue_ReturnsTrue()
    {
        var file = new DicomFileEntry { FilePath = "test.dcm", FileName = "IM_0001" };
        // Simulate the exact format from DicomTagService
        var tags = new List<string>
        {
            "0008,0060 Modality: US",
            "0018,602C PhysicalDeltaX: 0.000142732"
        };

        Assert.True(_sut.MatchesFilter(file, "IM_0001", "142732", tags));
    }
}
