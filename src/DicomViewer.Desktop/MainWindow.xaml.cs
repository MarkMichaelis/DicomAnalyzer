using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using DicomViewer.Desktop.ViewModels;

namespace DicomViewer.Desktop;

/// <summary>
/// Minimal code-behind for MainWindow.
/// All business logic lives in MainViewModel.
/// Only UI-specific interactions (canvas drawing, file dialogs) are here.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private Point _roiStartPoint;
    private Rectangle? _roiRect;
    private bool _isDrawingRoi;
    private readonly bool _autoShutdown;

    public MainWindow()
    {
        InitializeComponent();

        var args = Environment.GetCommandLineArgs();
        _autoShutdown = args.Contains("--auto-shutdown");

        // Set up playback timer and hand to ViewModel
        var timer = new DispatcherTimer();
        ViewModel.SetPlaybackTimer(timer);
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        try
        {
            var args = Environment.GetCommandLineArgs();
            var dirIdx = Array.IndexOf(args, "--directory");
            string? autoDir = dirIdx >= 0 && dirIdx + 1 < args.Length
                ? args[dirIdx + 1] : null;

            await ViewModel.InitializeAsync(autoDir, _autoShutdown);

            // Redraw ROI after initial load
            RedrawRoi();

            if (_autoShutdown)
            {
                await Task.Delay(500);
                Close();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UNHANDLED: {ex}");
            if (_autoShutdown) Close();
        }
    }

    #region UI-only event handlers

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select DICOM Folder" };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.FolderPath = dialog.FolderName;
            _ = ViewModel.LoadDirectoryCommand.ExecuteAsync(dialog.FolderName);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsVm = new SettingsViewModel();
        settingsVm.LoadFrom(ViewModel.GetFolderSettings());

        var dlg = new SettingsDialog { DataContext = settingsVm, Owner = this };
        if (dlg.ShowDialog() == true)
        {
            var settings = settingsVm.Validate();
            if (settings != null)
                _ = ViewModel.SaveFolderSettingsAndReload(settings);
        }
    }

    private void FolderPathTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _ = ViewModel.LoadDirectoryCommand.ExecuteAsync(null);
    }

    private void TreeView_SelectedItemChanged(
        object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        switch (e.NewValue)
        {
            case TreeFileViewModel fileVm:
                ViewModel.SelectFileInternal(fileVm.File);
                RedrawRoi();
                break;
            case TreeGroupViewModel groupVm:
                ViewModel.SelectGroup(groupVm.Group);
                RedrawRoi();
                break;
        }
    }

    private void FrameSlider_ValueChanged(
        object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ViewModel.OnFrameChanged((int)e.NewValue);
    }

    #endregion

    #region Canvas ROI Drawing

    private void ImageCanvas_MouseLeftButtonDown(
        object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedFile == null) return;
        _isDrawingRoi = true;
        _roiStartPoint = e.GetPosition(ImageCanvas);
        RemoveRoiRect();
        ImageCanvas.CaptureMouse();
    }

    private void ImageCanvas_MouseMove(
        object sender, MouseEventArgs e)
    {
        if (!_isDrawingRoi) return;
        var pt = e.GetPosition(ImageCanvas);
        RemoveRoiRect();
        DrawTempRect(
            Math.Min(_roiStartPoint.X, pt.X),
            Math.Min(_roiStartPoint.Y, pt.Y),
            Math.Abs(pt.X - _roiStartPoint.X),
            Math.Abs(pt.Y - _roiStartPoint.Y));
    }

    private void ImageCanvas_MouseLeftButtonUp(
        object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawingRoi || ViewModel.SelectedFile == null) return;
        _isDrawingRoi = false;
        ImageCanvas.ReleaseMouseCapture();

        var pt = e.GetPosition(ImageCanvas);
        var cw = ImageCanvas.ActualWidth;
        var ch = ImageCanvas.ActualHeight;
        if (cw <= 0 || ch <= 0) return;

        var file = ViewModel.SelectedFile;
        var sx = file.Width / cw;
        var sy = file.Height / ch;

        var x = Math.Min(_roiStartPoint.X, pt.X) * sx;
        var y = Math.Min(_roiStartPoint.Y, pt.Y) * sy;
        var w = Math.Abs(pt.X - _roiStartPoint.X) * sx;
        var h = Math.Abs(pt.Y - _roiStartPoint.Y) * sy;

        if (w < 2 || h < 2) return;

        ViewModel.SetRoi(x, y, w, h);
        RedrawRoi();
    }

    private void ImageCanvas_SizeChanged(
        object sender, SizeChangedEventArgs e)
    {
        SizeImageToCanvas();
        RedrawRoi();
    }

    private void SizeImageToCanvas()
    {
        DicomImageControl.Width =
            ImageCanvas.ActualWidth > 0 ? ImageCanvas.ActualWidth : 600;
        DicomImageControl.Height =
            ImageCanvas.ActualHeight > 0 ? ImageCanvas.ActualHeight : 400;
    }

    private void RedrawRoi()
    {
        RemoveRoiRect();
        var roi = ViewModel.CurrentRoi;
        if (roi == null || ViewModel.SelectedFile == null) return;

        var file = ViewModel.SelectedFile;
        var cw = ImageCanvas.ActualWidth;
        var ch = ImageCanvas.ActualHeight;
        if (cw <= 0 || ch <= 0) return;

        var sx = cw / file.Width;
        var sy = ch / file.Height;

        DrawTempRect(
            roi.X * sx, roi.Y * sy,
            roi.Width * sx, roi.Height * sy);
    }

    private void DrawTempRect(
        double x, double y, double w, double h)
    {
        _roiRect = new Rectangle
        {
            Stroke = Brushes.Yellow,
            StrokeThickness = 2,
            StrokeDashArray = [4, 2],
            Fill = new SolidColorBrush(
                Color.FromArgb(30, 255, 255, 0)),
            Width = w, Height = h
        };
        Canvas.SetLeft(_roiRect, x);
        Canvas.SetTop(_roiRect, y);
        ImageCanvas.Children.Add(_roiRect);
    }

    private void RemoveRoiRect()
    {
        if (_roiRect != null)
        {
            ImageCanvas.Children.Remove(_roiRect);
            _roiRect = null;
        }
    }

    #endregion
}
