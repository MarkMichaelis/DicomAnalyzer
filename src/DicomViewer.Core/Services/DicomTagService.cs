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
                        DicomElement el => GetElementValueString(el),
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

    /// <summary>
    /// Gets a string representation of all values in a DICOM element.
    /// Handles multi-valued elements by joining values with backslash.
    /// Falls back to ToString() if Get fails.
    /// </summary>
    private static string GetElementValueString(DicomElement el)
    {
        try
        {
            if (el.Count <= 1)
                return el.Get<string>() ?? string.Empty;

            var values = new List<string>();
            for (int i = 0; i < el.Count; i++)
            {
                try
                {
                    values.Add(el.Get<string>(i) ?? string.Empty);
                }
                catch
                {
                    values.Add("[Unable to read]");
                }
            }
            return string.Join("\\", values);
        }
        catch
        {
            // Fallback: use ToString which often provides a useful representation
            return el.ToString() ?? string.Empty;
        }
    }
}
