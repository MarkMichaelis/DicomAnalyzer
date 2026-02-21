using CommunityToolkit.Mvvm.ComponentModel;
using DicomViewer.Core.Models;

namespace DicomViewer.Web.ViewModels;

/// <summary>
/// ViewModel for a file node in the tree.
/// </summary>
public partial class TreeFileViewModel : ObservableObject
{
    /// <summary>The underlying file entry.</summary>
    public DicomFileEntry File { get; }

    /// <summary>Display text (includes SHI annotation).</summary>
    public string DisplayName { get; }

    public TreeFileViewModel(DicomFileEntry file, string displayName)
    {
        File = file;
        DisplayName = displayName;
    }
}
