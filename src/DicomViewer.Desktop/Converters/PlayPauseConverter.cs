using System.Globalization;
using System.Windows.Data;

namespace DicomViewer.Desktop.Converters;

/// <summary>
/// Converts IsPlaying bool to "Pause"/"Play" string.
/// </summary>
public class PlayPauseConverter : IValueConverter
{
    public object Convert(
        object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        return value is true ? "Pause" : "Play";
    }

    public object ConvertBack(
        object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
