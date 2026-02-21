using System.Text.Json;
using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Manages ROI persistence in dicom_viewer.roi files.
/// Supports multiple ROIs per group.
/// </summary>
public class RoiService
{
    private const string RoiFileName = "dicom_viewer.roi";
    private Dictionary<string, List<RoiData>> _rois = [];
    private readonly Dictionary<string, Stack<List<RoiData>?>> _undoStacks = [];

    /// <summary>
    /// Gets the first ROI for a group (backward compatibility).
    /// </summary>
    public RoiData? GetRoi(string groupId)
    {
        return _rois.TryGetValue(groupId, out var list) && list.Count > 0
            ? list[0] : null;
    }

    /// <summary>
    /// Gets all ROIs for a group.
    /// </summary>
    public List<RoiData> GetRois(string groupId)
    {
        return _rois.TryGetValue(groupId, out var list)
            ? new List<RoiData>(list) : new List<RoiData>();
    }

    /// <summary>
    /// Adds a new ROI to a group, assigning a unique ID.
    /// </summary>
    public void AddRoi(string groupId, RoiData roi)
    {
        roi.GroupId = groupId;
        if (string.IsNullOrEmpty(roi.Id))
            roi.Id = Guid.NewGuid().ToString("N")[..8];
        PushUndo(groupId);
        if (!_rois.ContainsKey(groupId))
            _rois[groupId] = new List<RoiData>();
        _rois[groupId].Add(roi);
    }

    /// <summary>
    /// Sets a single ROI for a group, replacing all existing ROIs (backward compatibility).
    /// </summary>
    public void SetRoi(string groupId, RoiData roi)
    {
        roi.GroupId = groupId;
        if (string.IsNullOrEmpty(roi.Id))
            roi.Id = Guid.NewGuid().ToString("N")[..8];
        PushUndo(groupId);
        _rois[groupId] = new List<RoiData> { roi };
    }

    /// <summary>
    /// Removes a specific ROI by ID from a group.
    /// </summary>
    public bool RemoveRoi(string groupId, string roiId)
    {
        if (!_rois.TryGetValue(groupId, out var list)) return false;
        PushUndo(groupId);
        var removed = list.RemoveAll(r => r.Id == roiId) > 0;
        if (list.Count == 0) _rois.Remove(groupId);
        return removed;
    }

    /// <summary>
    /// Finds the first ROI containing the given point.
    /// </summary>
    public RoiData? HitTestRoi(string groupId, double px, double py)
    {
        if (!_rois.TryGetValue(groupId, out var list)) return null;
        return list.FirstOrDefault(r => r.ContainsPoint(px, py));
    }

    /// <summary>Clears the ROI for a group, pushing old value onto undo stack.</summary>
    public void ClearRoi(string groupId)
    {
        PushUndo(groupId);
        _rois.Remove(groupId);
    }

    /// <summary>Undoes the last ROI change for a group. Returns true if undo was performed.</summary>
    public bool UndoRoi(string groupId)
    {
        if (!_undoStacks.TryGetValue(groupId, out var stack)
            || stack.Count == 0)
            return false;

        var previous = stack.Pop();
        if (previous == null)
            _rois.Remove(groupId);
        else
            _rois[groupId] = previous;
        return true;
    }

    /// <summary>Returns true if there is an undo available for the group.</summary>
    public bool CanUndo(string groupId) =>
        _undoStacks.TryGetValue(groupId, out var stack) && stack.Count > 0;

    private void PushUndo(string groupId)
    {
        if (!_undoStacks.ContainsKey(groupId))
            _undoStacks[groupId] = new Stack<List<RoiData>?>();
        _undoStacks[groupId].Push(
            _rois.TryGetValue(groupId, out var list)
                ? new List<RoiData>(list) : null);
    }

    /// <summary>Sets mean intensity for a file in a group.</summary>
    public void SetFileMean(
        string groupId, string fileName, double mean)
    {
        if (_rois.TryGetValue(groupId, out var list) && list.Count > 0)
            list[0].FileMeans[fileName] = mean;
    }

    /// <summary>Loads ROI data from file.</summary>
    public void LoadRois(string directoryPath)
    {
        var path = Path.Combine(directoryPath, RoiFileName);
        if (!File.Exists(path))
        {
            _rois = new Dictionary<string, List<RoiData>>();
            return;
        }
        try
        {
            var json = File.ReadAllText(path);
            // Try new format (list-based) first
            var multiRois = JsonSerializer.Deserialize<Dictionary<string, List<RoiData>>>(json, JsonOpts);
            if (multiRois != null)
            {
                _rois = multiRois;
                return;
            }
        }
        catch
        {
            // Fall back to legacy single-ROI format
            try
            {
                var json = File.ReadAllText(path);
                var legacyRois = JsonSerializer.Deserialize<Dictionary<string, RoiData>>(json, JsonOpts);
                if (legacyRois != null)
                {
                    _rois = legacyRois.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new List<RoiData> { kvp.Value });
                    return;
                }
            }
            catch { }
        }
        _rois = new Dictionary<string, List<RoiData>>();
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
