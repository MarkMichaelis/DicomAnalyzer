using System.Text.Json;
using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Manages ROI persistence in dicom_viewer.roi files.
/// </summary>
public class RoiService
{
    private const string RoiFileName = "dicom_viewer.roi";
    private Dictionary<string, RoiData> _rois = [];

    /// <summary>Gets the ROI for a specific group.</summary>
    public RoiData? GetRoi(string groupId) =>
        _rois.GetValueOrDefault(groupId);

    /// <summary>Sets or replaces the ROI for a group.</summary>
    public void SetRoi(string groupId, RoiData roi)
    {
        roi.GroupId = groupId;
        _rois[groupId] = roi;
    }

    /// <summary>Clears the ROI for a group.</summary>
    public void ClearRoi(string groupId) =>
        _rois.Remove(groupId);

    /// <summary>Sets mean intensity for a file in a group.</summary>
    public void SetFileMean(
        string groupId, string fileName, double mean)
    {
        if (_rois.TryGetValue(groupId, out var roi))
            roi.FileMeans[fileName] = mean;
    }

    /// <summary>Loads ROI data from file.</summary>
    public void LoadRois(string directoryPath)
    {
        var path = Path.Combine(directoryPath, RoiFileName);
        if (!File.Exists(path)) { _rois = []; return; }

        try
        {
            var json = File.ReadAllText(path);
            _rois = JsonSerializer.Deserialize<
                Dictionary<string, RoiData>>(json, JsonOpts) ?? [];
        }
        catch { _rois = []; }
    }

    /// <summary>Saves ROI data to file.</summary>
    public void SaveRois(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath)) return;
        var path = Path.Combine(directoryPath, RoiFileName);
        File.WriteAllText(path,
            JsonSerializer.Serialize(_rois, JsonOpts));
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
