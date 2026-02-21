using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using DicomViewer.Desktop.ViewModels;
using DicomViewer.Core.Models;

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
    private Shape? _roiShape;
    private Polyline? _freeformLine;
    private List<Point> _freeformPoints = [];
    private bool _isDrawingRoi;
    private bool _isDrawingFreeform;
    private readonly bool _autoShutdown;

    public MainWindow()
    {
        InitializeComponent();

        var args = Environment.GetCommandLineArgs();
        _autoShutdown = args.Contains("--auto-shutdown");

        // Set up playback timer and hand to ViewModel
        var timer = new DispatcherTimer();
        ViewModel.SetPlaybackTimer(timer);

        RestoreWindowPosition();
    }

    /// <summary>Saves window position when closing.</summary>
    protected override void OnClosing(
        System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowPosition();
        base.OnClosing(e);
    }

    private void RestoreWindowPosition()
    {
        var settings = ViewModel.GetAppSettings();
        if (settings.WindowWidth.HasValue
            && settings.WindowHeight.HasValue
            && settings.WindowWidth > 0
            && settings.WindowHeight > 0)
        {
            Left = settings.WindowLeft ?? 0;
            Top = settings.WindowTop ?? 0;
            Width = settings.WindowWidth.Value;
            Height = settings.WindowHeight.Value;
            WindowStartupLocation = WindowStartupLocation.Manual;

            if (Enum.TryParse<WindowState>(
                settings.WindowState, out var state))
            {
                WindowState = state;
            }

            // Ensure window is at least partially on-screen
            EnsureOnScreen();
        }
    }

    private void EnsureOnScreen()
    {
        var screen = SystemParameters.WorkArea;
        if (Left + Width < 50) Left = 0;
        if (Top + Height < 50) Top = 0;
        if (Left > screen.Width - 50) Left = screen.Width - Width;
        if (Top > screen.Height - 50) Top = screen.Height - Height;
    }

    private void SaveWindowPosition()
    {
        if (WindowState == WindowState.Minimized) return;
        var state = WindowState;
        // Save normal bounds even when maximized
        if (state == WindowState.Maximized)
        {
            ViewModel.SaveWindowPosition(
                RestoreBounds.Left, RestoreBounds.Top,
                RestoreBounds.Width, RestoreBounds.Height,
                state.ToString());
        }
        else
        {
            ViewModel.SaveWindowPosition(
                Left, Top, Width, Height, state.ToString());
        }
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

    private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export ROI Data to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = "roi_intensity_data.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.ExportToExcel(dialog.FileName);
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
                var parentGroup = ViewModel.SelectedGroup;
                ViewModel.SaveSelectedNodePath(
                    MainViewModel.BuildNodePath(
                        parentGroup?.GroupId ?? "", fileVm.File.FileName));
                RedrawRoi();
                break;
            case TreeGroupViewModel groupVm:
                ViewModel.SelectGroup(groupVm.Group);
                ViewModel.SaveSelectedNodePath(
                    MainViewModel.BuildNodePath(groupVm.Group.GroupId));
                RedrawRoi();
                break;
        }
    }

    private void FrameSlider_ValueChanged(
        object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ViewModel.OnFrameChanged((int)e.NewValue);
    }

    private void CopySelectedTags_Click(object sender, RoutedEventArgs e)
    {
        var selected = TagListBox.SelectedItems
            .Cast<string>().ToList();
        if (selected.Count > 0)
            Clipboard.SetText(string.Join(Environment.NewLine, selected));
    }

    private void CopyAllTags_Click(object sender, RoutedEventArgs e)
    {
        var all = ViewModel.DicomTags.ToList();
        if (all.Count > 0)
            Clipboard.SetText(string.Join(Environment.NewLine, all));
    }

    #endregion

    #region Canvas ROI Drawing

    private void RoiShapeCombo_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        var idx = RoiShapeCombo.SelectedIndex;
        ViewModel.SelectedRoiShape = idx switch
        {
            1 => RoiShape.Ellipse,
            2 => RoiShape.Freeform,
            _ => RoiShape.Rectangle
        };
        // Cancel any in-progress freeform drawing
        CancelFreeformDrawing();
    }

    private void ImageCanvas_MouseLeftButtonDown(
        object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedFile == null) return;

        if (ViewModel.SelectedRoiShape == RoiShape.Freeform)
        {
            HandleFreeformClick(e);
            return;
        }

        // Rectangle or Ellipse: drag to draw
        _isDrawingRoi = true;
        _roiStartPoint = e.GetPosition(ImageCanvas);
        RemoveRoiVisual();
        ImageCanvas.CaptureMouse();
    }

    private void ImageCanvas_MouseMove(
        object sender, MouseEventArgs e)
    {
        if (_isDrawingFreeform)
        {
            // Update freeform preview line
            return;
        }
        if (!_isDrawingRoi) return;
        var pt = e.GetPosition(ImageCanvas);
        RemoveRoiVisual();

        var x = Math.Min(_roiStartPoint.X, pt.X);
        var y = Math.Min(_roiStartPoint.Y, pt.Y);
        var w = Math.Abs(pt.X - _roiStartPoint.X);
        var h = Math.Abs(pt.Y - _roiStartPoint.Y);

        if (ViewModel.SelectedRoiShape == RoiShape.Ellipse)
            DrawTempEllipse(x, y, w, h);
        else
            DrawTempRect(x, y, w, h);
    }

    private void ImageCanvas_MouseLeftButtonUp(
        object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedRoiShape == RoiShape.Freeform)
            return; // Freeform uses click-click-doubleclick

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

    #region Freeform drawing

    private void HandleFreeformClick(MouseButtonEventArgs e)
    {
        var pt = e.GetPosition(ImageCanvas);

        if (e.ClickCount >= 2 && _freeformPoints.Count >= 3)
        {
            // Double-click completes the freeform ROI
            FinishFreeformDrawing();
            return;
        }

        _freeformPoints.Add(pt);
        _isDrawingFreeform = true;
        UpdateFreeformVisual();
    }

    private void FinishFreeformDrawing()
    {
        if (ViewModel.SelectedFile == null || _freeformPoints.Count < 3)
        {
            CancelFreeformDrawing();
            return;
        }

        var cw = ImageCanvas.ActualWidth;
        var ch = ImageCanvas.ActualHeight;
        if (cw <= 0 || ch <= 0) return;

        var file = ViewModel.SelectedFile;
        var sx = file.Width / cw;
        var sy = file.Height / ch;

        var imagePoints = _freeformPoints
            .Select(p => new double[] { p.X * sx, p.Y * sy })
            .ToList();

        ViewModel.SetFreeformRoi(imagePoints);
        _freeformPoints.Clear();
        _isDrawingFreeform = false;
        RedrawRoi();
    }

    private void CancelFreeformDrawing()
    {
        _freeformPoints.Clear();
        _isDrawingFreeform = false;
        RemoveFreeformVisual();
    }

    private void UpdateFreeformVisual()
    {
        RemoveFreeformVisual();
        if (_freeformPoints.Count < 2) return;

        _freeformLine = new Polyline
        {
            Stroke = Brushes.Yellow,
            StrokeThickness = 2,
            StrokeDashArray = [4, 2],
            Points = new PointCollection(_freeformPoints)
        };
        ImageCanvas.Children.Add(_freeformLine);
    }

    private void RemoveFreeformVisual()
    {
        if (_freeformLine != null)
        {
            ImageCanvas.Children.Remove(_freeformLine);
            _freeformLine = null;
        }
    }

    #endregion

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
        RemoveRoiVisual();
        RemoveFreeformVisual();
        var roi = ViewModel.CurrentRoi;
        if (roi == null || ViewModel.SelectedFile == null) return;

        var file = ViewModel.SelectedFile;
        var cw = ImageCanvas.ActualWidth;
        var ch = ImageCanvas.ActualHeight;
        if (cw <= 0 || ch <= 0) return;

        var sx = cw / file.Width;
        var sy = ch / file.Height;

        switch (roi.Shape)
        {
            case RoiShape.Ellipse:
                DrawTempEllipse(
                    roi.X * sx, roi.Y * sy,
                    roi.Width * sx, roi.Height * sy);
                break;
            case RoiShape.Freeform:
                DrawFreeformRoi(roi, sx, sy);
                break;
            default:
                DrawTempRect(
                    roi.X * sx, roi.Y * sy,
                    roi.Width * sx, roi.Height * sy);
                break;
        }
    }

    private void DrawTempRect(
        double x, double y, double w, double h)
    {
        _roiShape = new Rectangle
        {
            Stroke = Brushes.Yellow,
            StrokeThickness = 2,
            StrokeDashArray = [4, 2],
            Fill = new SolidColorBrush(
                Color.FromArgb(30, 255, 255, 0)),
            Width = w, Height = h
        };
        Canvas.SetLeft(_roiShape, x);
        Canvas.SetTop(_roiShape, y);
        ImageCanvas.Children.Add(_roiShape);
    }

    private void DrawTempEllipse(
        double x, double y, double w, double h)
    {
        _roiShape = new Ellipse
        {
            Stroke = Brushes.Cyan,
            StrokeThickness = 2,
            StrokeDashArray = [4, 2],
            Fill = new SolidColorBrush(
                Color.FromArgb(30, 0, 255, 255)),
            Width = w, Height = h
        };
        Canvas.SetLeft(_roiShape, x);
        Canvas.SetTop(_roiShape, y);
        ImageCanvas.Children.Add(_roiShape);
    }

    private void DrawFreeformRoi(RoiData roi, double sx, double sy)
    {
        if (roi.Points.Count < 3) return;
        var points = roi.Points
            .Select(p => new Point(p[0] * sx, p[1] * sy));

        var polygon = new Polygon
        {
            Stroke = Brushes.Lime,
            StrokeThickness = 2,
            StrokeDashArray = [4, 2],
            Fill = new SolidColorBrush(
                Color.FromArgb(30, 0, 255, 0)),
            Points = new PointCollection(points)
        };
        ImageCanvas.Children.Add(polygon);
        // Store as _roiShape for cleanup
        _roiShape = polygon;
    }

    private void RemoveRoiVisual()
    {
        if (_roiShape != null)
        {
            ImageCanvas.Children.Remove(_roiShape);
            _roiShape = null;
        }
    }

    #endregion
}
