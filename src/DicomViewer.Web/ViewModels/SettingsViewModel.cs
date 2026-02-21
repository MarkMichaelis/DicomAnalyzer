using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DicomViewer.Core.Models;

namespace DicomViewer.Web.ViewModels;

/// <summary>
/// ViewModel for the settings dialog.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _timeWindowText = "60";

    [ObservableProperty]
    private string _ceusSpacingText = "0.5";

    [ObservableProperty]
    private string _shiSpacingText = "0.3";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>Whether save was successful.</summary>
    public bool SaveSucceeded { get; private set; }

    /// <summary>
    /// Loads values from a FolderSettings instance.
    /// </summary>
    public void LoadFrom(FolderSettings settings)
    {
        TimeWindowText = settings.TimeWindowSeconds.ToString();
        CeusSpacingText = settings.CeusSpacing.ToString();
        ShiSpacingText = settings.ShiSpacing.ToString();
    }

    /// <summary>
    /// Validates and creates a FolderSettings from inputs.
    /// </summary>
    public FolderSettings? Validate()
    {
        if (!double.TryParse(TimeWindowText, out var tw)
            || !double.TryParse(CeusSpacingText, out var ceus)
            || !double.TryParse(ShiSpacingText, out var shi))
        {
            ErrorMessage = "Please enter valid numeric values.";
            return null;
        }

        ErrorMessage = string.Empty;
        SaveSucceeded = true;
        return new FolderSettings
        {
            TimeWindowSeconds = tw,
            CeusSpacing = ceus,
            ShiSpacing = shi
        };
    }
}
