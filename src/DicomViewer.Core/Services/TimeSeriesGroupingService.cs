using DicomViewer.Core.Models;

namespace DicomViewer.Core.Services;

/// <summary>
/// Groups DICOM files into time series by acquisition timestamps.
/// </summary>
public class TimeSeriesGroupingService
{
    /// <summary>
    /// Groups files by AcquisitionDateTime with a time window.
    /// </summary>
    public List<TimeSeriesGroup> GroupFiles(
        List<DicomFileEntry> files,
        double timeWindowSeconds = 60)
    {
        var groups = new List<TimeSeriesGroup>();

        var withTime = files
            .Where(f => f.AcquisitionDateTime.HasValue)
            .OrderBy(f => f.AcquisitionDateTime!.Value)
            .ToList();
        var withoutTime = files
            .Where(f => !f.AcquisitionDateTime.HasValue)
            .ToList();

        TimeSeriesGroup? current = null;
        DateTime? lastTime = null;

        foreach (var file in withTime)
        {
            var t = file.AcquisitionDateTime!.Value;
            if (current == null || lastTime == null
                || (t - lastTime.Value).TotalSeconds > timeWindowSeconds)
            {
                current = new TimeSeriesGroup();
                groups.Add(current);
            }
            current.Files.Add(file);
            lastTime = t;
        }

        foreach (var group in groups)
        {
            var first = group.Files[0].AcquisitionDateTime!.Value;
            var last = group.Files[^1].AcquisitionDateTime!.Value;
            group.Label = first == last
                ? first.ToString("HH:mm:ss")
                : $"{first:HH:mm:ss} - {last:HH:mm:ss}";
            group.GroupId = $"group_{first:yyyyMMddHHmmss}";
        }

        if (withoutTime.Count > 0)
        {
            groups.Add(new TimeSeriesGroup
            {
                GroupId = "group_unknown",
                Label = "Unknown Time",
                Files = withoutTime
            });
        }

        return groups;
    }
}
