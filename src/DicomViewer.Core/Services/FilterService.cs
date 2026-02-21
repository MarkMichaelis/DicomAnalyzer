using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Provides filtering logic for DICOM files based on display names and tag values.
/// </summary>
public class FilterService
{
    /// <summary>
    /// Determines whether a file matches the given filter text by checking
    /// its display name and DICOM tag strings.
    /// </summary>
    /// <param name="file">The DICOM file entry to check.</param>
    /// <param name="displayName">The display name shown in the tree view.</param>
    /// <param name="filter">The filter text to match against.</param>
    /// <param name="tags">The list of DICOM tag strings for this file, or null if unavailable.</param>
    /// <returns>True if the file matches the filter; false otherwise.</returns>
    public bool MatchesFilter(DicomFileEntry file, string displayName, string filter, List<string>? tags)
    {
        if (displayName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;

        if (tags != null)
            return tags.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase));

        return false;
    }
}
