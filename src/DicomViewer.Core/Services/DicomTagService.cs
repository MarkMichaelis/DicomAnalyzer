using FellowOakDicom;

namespace DicomViewer.Core.Services;

/// <summary>
/// Extracts and formats DICOM tags from a file.
/// </summary>
public class DicomTagService
{
    /// <summary>
    /// Gets all tags for a file, excluding PixelData.
    /// Formatted as "GGGG,EEEE Name: Value".
    /// </summary>
    public List<string> GetTags(string filePath)
    {
        var tags = new List<string>();
        try
        {
            var dcm = DicomFile.Open(filePath);
            foreach (var item in dcm.Dataset)
            {
                if (item.Tag == DicomTag.PixelData) continue;

                var tagId = $"{item.Tag.Group:X4},{item.Tag.Element:X4}";
                var name = item.Tag.DictionaryEntry?.Name ?? "Unknown";
                string value;
                try
                {
                    value = item switch
                    {
                        DicomElement el => el.Get<string>(-1)
                            ?? string.Empty,
                        DicomSequence seq =>
                            $"[Sequence: {seq.Items.Count} items]",
                        _ => item.ToString() ?? string.Empty
                    };
                }
                catch { value = "[Unable to read]"; }

                tags.Add($"{tagId} {name}: {value}");
            }
        }
        catch (Exception ex)
        {
            tags.Add($"Error reading tags: {ex.Message}");
        }
        return tags;
    }
}
