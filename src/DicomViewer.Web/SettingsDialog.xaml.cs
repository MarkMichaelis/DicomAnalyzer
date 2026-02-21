using System.Windows;
using DicomViewer.Web.ViewModels;

namespace DicomViewer.Web;

/// <summary>
/// Settings dialog. DataContext is SettingsViewModel (set by caller).
/// </summary>
public partial class SettingsDialog : Window
{
    private SettingsViewModel ViewModel =>
        (SettingsViewModel)DataContext;

    public SettingsDialog()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = ViewModel.Validate();
        if (settings != null)
        {
            DialogResult = true;
            Close();
        }
    }
}
