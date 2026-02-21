using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DicomViewer.Core.Models;

namespace DicomViewer.Desktop.ViewModels;

/// <summary>
/// ViewModel for a time-series group node in the tree.
/// </summary>
public partial class TreeGroupViewModel : ObservableObject
{
    /// <summary>The underlying group model.</summary>
    public TimeSeriesGroup Group { get; }

    /// <summary>Display label for the group.</summary>
    public string Label => Group.Label;

    /// <summary>Child file nodes.</summary>
    public ObservableCollection<TreeFileViewModel> Children { get; } = [];

    [ObservableProperty]
    private bool _isExpanded;

    public TreeGroupViewModel(TimeSeriesGroup group)
    {
        Group = group;
    }
}
