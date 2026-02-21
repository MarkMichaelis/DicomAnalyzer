using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DicomViewer.Core.Models;
using DicomViewer.Core.Services;
using FellowOakDicom.Imaging;
using FellowOakDicom;

namespace DicomViewer.Desktop.ViewModels;

/// <summary>
/// Primary ViewModel for the DICOM ROI Analyzer application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly DicomFileService _fileService = new();
    private readonly ClassificationService _classificationService = new();
    private readonly TimeSeriesGroupingService _groupingService = new();
    private readonly RoiService _roiService = new();
    private readonly SettingsService _settingsService = new();
    private readonly DicomTagService _tagService = new();

    private List<DicomFileEntry> _allFiles = [];
    private List<TimeSeriesGroup> _allGroups = [];
    private Dictionary<string, List<string>> _fileTagCache = [];
    private string _currentDirectory = string.Empty;
    private AppSettings _appSettings = new();
    private System.Windows.Threading.DispatcherTimer? _playbackTimer;

    #region Observable Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReloadCommand))]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _fileInfoText = "No file selected";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _roiMeanText = "ROI Mean: --";

    [ObservableProperty]
    private string _frameText = "0/0";

    [ObservableProperty]
    private int _fps = 10;

    [ObservableProperty]
    private int _currentFrame;

    [ObservableProperty]
    private int _maxFrame;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private BitmapSource? _currentImage;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _hasFiles;

    [ObservableProperty]
    private DicomFileEntry? _selectedFile;

    [ObservableProperty]
    private TimeSeriesGroup? _selectedGroup;

    [ObservableProperty]
    private RoiData? _currentRoi;

    #endregion

    /// <summary>Tree groups displayed in the UI.</summary>
    public ObservableCollection<TreeGroupViewModel> TreeGroups { get; } = [];

    /// <summary>DICOM tags for the selected file.</summary>
    public ObservableCollection<string> DicomTags { get; } = [];

    public MainViewModel()
    {
        _appSettings = _settingsService.LoadAppSettings();
    }

    /// <summary>Gets the current app settings for window position restoration.</summary>
    public AppSettings GetAppSettings() => _appSettings;

    /// <summary>Saves window position to app settings.</summary>
    public void SaveWindowPosition(
        double left, double top, double width, double height, string state)
    {
        _appSettings.WindowLeft = left;
        _appSettings.WindowTop = top;
        _appSettings.WindowWidth = width;
        _appSettings.WindowHeight = height;
        _appSettings.WindowState = state;
        _settingsService.SaveAppSettings(_appSettings);
    }

    /// <summary>
    /// Initializes the ViewModel and auto-loads directory if applicable.
    /// </summary>
    public async Task InitializeAsync(
        string? autoLoadDir = null, bool autoShutdown = false)
    {
        try
        {
            var dirToLoad = autoLoadDir
                ?? _appSettings.LastLoadedDirectory;

            if (!string.IsNullOrEmpty(dirToLoad)
                && Directory.Exists(dirToLoad))
            {
                FolderPath = dirToLoad;
                await LoadDirectoryAsync(dirToLoad);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Init error: {ex.Message}";
            Console.Error.WriteLine($"INIT ERROR: {ex}");
        }
    }

    #region Commands

    /// <summary>Loads files from the current folder path.</summary>
    [RelayCommand]
    private async Task LoadDirectoryAsync(string? path = null)
    {
        var dir = path ?? FolderPath;
        if (string.IsNullOrWhiteSpace(dir)) return;

        try
        {
            StatusText = "Loading...";
            _currentDirectory = dir;
            FolderPath = dir;

            var folderSettings =
                _settingsService.LoadFolderSettings(dir);
            _allFiles = await Task.Run(
                () => _fileService.LoadFiles(dir));

            foreach (var file in _allFiles)
            {
                file.Classification = _classificationService.Classify(
                    file.PixelSpacing,
                    folderSettings.CeusSpacing,
                    folderSettings.ShiSpacing);
            }

            _allGroups = _groupingService.GroupFiles(
                _allFiles, folderSettings.TimeWindowSeconds);

            _roiService.LoadRois(dir);
            _appSettings.LastLoadedDirectory = dir;
            _settingsService.SaveAppSettings(_appSettings);

            // Cache tags per file for filter search
            _fileTagCache = [];
            foreach (var file in _allFiles)
            {
                _fileTagCache[file.FilePath] =
                    _tagService.GetTags(file.FilePath);
            }

            BuildTree();
            HasFiles = _allFiles.Count > 0;

            if (_allFiles.Count > 0)
                SelectFileInternal(_allFiles[0]);

            StatusText = $"Loaded {_allFiles.Count} files in "
                + $"{_allGroups.Count} groups.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            Console.Error.WriteLine($"LOAD ERROR: {ex}");
        }
    }

    /// <summary>Reloads the current directory.</summary>
    [RelayCommand(CanExecute = nameof(CanReload))]
    private async Task ReloadAsync() =>
        await LoadDirectoryAsync(_currentDirectory);

    private bool CanReload() =>
        !string.IsNullOrEmpty(FolderPath);

    /// <summary>Navigate to the previous file.</summary>
    [RelayCommand]
    private void PreviousFile()
    {
        if (SelectedFile == null || _allFiles.Count == 0) return;
        var idx = _allFiles.IndexOf(SelectedFile);
        if (idx > 0) SelectFileInternal(_allFiles[idx - 1]);
    }

    /// <summary>Navigate to the next file.</summary>
    [RelayCommand]
    private void NextFile()
    {
        if (SelectedFile == null || _allFiles.Count == 0) return;
        var idx = _allFiles.IndexOf(SelectedFile);
        if (idx < _allFiles.Count - 1)
            SelectFileInternal(_allFiles[idx + 1]);
    }

    /// <summary>Toggle play/pause for cine playback.</summary>
    [RelayCommand]
    private void TogglePlayback()
    {
        IsPlaying = !IsPlaying;
        if (IsPlaying)
            StartPlayback();
        else
            StopPlayback();
    }

    /// <summary>Clear the ROI for the current group.</summary>
    [RelayCommand]
    private void ClearRoi()
    {
        if (SelectedGroup == null) return;
        _roiService.ClearRoi(SelectedGroup.GroupId);
        _roiService.SaveRois(_currentDirectory);
        CurrentRoi = null;
        RoiMeanText = "ROI Mean: --";
        StatusText = "ROI cleared.";
    }

    /// <summary>Applies the filter to the tree.</summary>
    [RelayCommand]
    private void ApplyFilter() => BuildTree();

    #endregion

    #region File Selection & Display

    /// <summary>
    /// Selects a file and updates all display properties.
    /// </summary>
    public void SelectFileInternal(DicomFileEntry file)
    {
        SelectedFile = file;
        SelectedGroup = _allGroups.FirstOrDefault(
            g => g.Files.Contains(file));

        FileInfoText = $"{file.FileName} - {file.Width}x{file.Height}"
            + $" - {file.FrameCount} frame(s)";

        MaxFrame = Math.Max(0, file.FrameCount - 1);
        CurrentFrame = 0;
        FrameText = $"1/{file.FrameCount}";

        RenderFrame(file, 0);
        LoadTags(file);
        UpdateRoiDisplay();
        ComputeRoiMean();
    }

    /// <summary>
    /// Selects a file from a tree group-node click.
    /// </summary>
    public void SelectGroup(TimeSeriesGroup group)
    {
        if (group.Files.Count > 0)
            SelectFileInternal(group.Files[0]);
    }

    /// <summary>
    /// Called when frame slider changes.
    /// </summary>
    public void OnFrameChanged(int frameIndex)
    {
        if (SelectedFile == null) return;
        if (frameIndex < 0 || frameIndex >= SelectedFile.FrameCount)
            return;

        CurrentFrame = frameIndex;
        RenderFrame(SelectedFile, frameIndex);
        FrameText = $"{frameIndex + 1}/{SelectedFile.FrameCount}";
    }

    /// <summary>
    /// Renders a specific frame of a DICOM file.
    /// </summary>
    public void RenderFrame(DicomFileEntry file, int frameIndex)
    {
        try
        {
            var dcm = DicomFile.Open(file.FilePath);
            var image = new DicomImage(dcm.Dataset);
            var rendered = image.RenderImage(frameIndex);

            int w = rendered.Width;
            int h = rendered.Height;
            var wb = new WriteableBitmap(
                w, h, 96, 96, PixelFormats.Bgra32, null);
            wb.Lock();
            try
            {
                unsafe
                {
                    var ptr = (byte*)wb.BackBuffer;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            var c = rendered.GetPixel(x, y);
                            int offset = (y * wb.BackBufferStride)
                                + (x * 4);
                            ptr[offset + 0] = c.B;
                            ptr[offset + 1] = c.G;
                            ptr[offset + 2] = c.R;
                            ptr[offset + 3] = c.A;
                        }
                    }
                }
                wb.AddDirtyRect(
                    new Int32Rect(0, 0, w, h));
            }
            finally { wb.Unlock(); }
            wb.Freeze();
            CurrentImage = wb;
        }
        catch (Exception ex)
        {
            StatusText = $"Render error: {ex.Message}";
        }
    }

    private void LoadTags(DicomFileEntry file)
    {
        DicomTags.Clear();
        foreach (var tag in _tagService.GetTags(file.FilePath))
            DicomTags.Add(tag);
    }

    #endregion

    #region ROI

    /// <summary>
    /// Called when user completes drawing an ROI on the canvas.
    /// Image-coordinate ROI rectangle.
    /// </summary>
    public void SetRoi(
        double x, double y, double width, double height)
    {
        if (SelectedGroup == null) return;

        var roi = new RoiData
        {
            GroupId = SelectedGroup.GroupId,
            X = x, Y = y, Width = width, Height = height
        };
        _roiService.SetRoi(SelectedGroup.GroupId, roi);
        _roiService.SaveRois(_currentDirectory);
        CurrentRoi = roi;
        StatusText = "ROI drawn and saved.";
        ComputeRoiMean();
    }

    private void UpdateRoiDisplay()
    {
        CurrentRoi = SelectedGroup != null
            ? _roiService.GetRoi(SelectedGroup.GroupId)
            : null;
    }

    private void ComputeRoiMean()
    {
        if (SelectedFile == null || SelectedGroup == null
            || CurrentRoi == null)
        {
            RoiMeanText = "ROI Mean: --";
            return;
        }

        var file = SelectedFile;
        var group = SelectedGroup;
        var roi = CurrentRoi;

        Task.Run(() =>
        {
            try
            {
                var mean = RoiStatisticsService
                    .ComputeMeanIntensity(file.FilePath, roi);
                System.Windows.Application.Current?.Dispatcher.Invoke(
                    () => RoiMeanText = $"ROI Mean: {mean:F2}");

                // Background compute for other group files
                ComputeGroupStatsBackground(group, roi);
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(
                    () =>
                    {
                        RoiMeanText = "ROI Mean: Error";
                        StatusText = $"Stats error: {ex.Message}";
                    });
            }
        });
    }

    private void ComputeGroupStatsBackground(
        TimeSeriesGroup group, RoiData roi)
    {
        foreach (var file in group.Files)
        {
            try
            {
                var mean = RoiStatisticsService
                    .ComputeMeanIntensity(file.FilePath, roi);
                _roiService.SetFileMean(
                    group.GroupId, file.FileName, mean);
            }
            catch { /* Skip failed files */ }
        }
        _roiService.SaveRois(_currentDirectory);
        System.Windows.Application.Current?.Dispatcher.Invoke(
            () => StatusText = "Group stats computed and saved.");
    }

    #endregion

    #region Playback

    /// <summary>Sets the dispatcher timer for playback.</summary>
    public void SetPlaybackTimer(
        System.Windows.Threading.DispatcherTimer timer)
    {
        _playbackTimer = timer;
        _playbackTimer.Tick += OnPlaybackTick;
        UpdateTimerInterval();
    }

    partial void OnFpsChanged(int value) =>
        UpdateTimerInterval();

    private void UpdateTimerInterval()
    {
        if (_playbackTimer != null)
            _playbackTimer.Interval =
                TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, Fps));
    }

    private void StartPlayback() =>
        _playbackTimer?.Start();

    private void StopPlayback() =>
        _playbackTimer?.Stop();

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        if (SelectedFile == null) return;

        if (SelectedFile.FrameCount > 1)
        {
            var next = (CurrentFrame + 1)
                % SelectedFile.FrameCount;
            OnFrameChanged(next);
        }
        else
        {
            NextFile();
        }
    }

    #endregion

    #region Tree Building

    private void BuildTree()
    {
        TreeGroups.Clear();
        foreach (var group in _allGroups)
        {
            var vm = new TreeGroupViewModel(group);
            foreach (var file in group.Files)
            {
                var display = file.Classification == FileClassification.SHI
                    ? $"{file.FileName} (SHI)"
                    : file.FileName;

                if (!string.IsNullOrEmpty(FilterText)
                    && !MatchesFilter(file, display, FilterText))
                    continue;

                vm.Children.Add(new TreeFileViewModel(file, display));
            }

            if (vm.Children.Count > 0 || string.IsNullOrEmpty(FilterText))
                TreeGroups.Add(vm);
        }
    }

    /// <summary>
    /// Checks if a file matches the filter text against display name,
    /// DICOM tag names, and DICOM tag values.
    /// </summary>
    private bool MatchesFilter(
        DicomFileEntry file, string display, string filter)
    {
        if (display.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;

        if (_fileTagCache.TryGetValue(file.FilePath, out var tags))
        {
            return tags.Any(t => t.Contains(
                filter, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    partial void OnFilterTextChanged(string value) => BuildTree();

    #endregion

    #region Settings Dialog Support

    /// <summary>
    /// Gets the current folder settings.
    /// </summary>
    public FolderSettings GetFolderSettings() =>
        string.IsNullOrEmpty(_currentDirectory)
            ? new FolderSettings()
            : _settingsService.LoadFolderSettings(_currentDirectory);

    /// <summary>
    /// Saves folder settings and reloads the directory.
    /// </summary>
    public async Task SaveFolderSettingsAndReload(
        FolderSettings settings)
    {
        if (string.IsNullOrEmpty(_currentDirectory)) return;
        _settingsService.SaveFolderSettings(
            _currentDirectory, settings);
        await LoadDirectoryAsync(_currentDirectory);
    }

    #endregion
}
