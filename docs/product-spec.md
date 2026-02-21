# DICOM Analysis Application: Product Specification

## 1. Overview

This project is a **single-platform .NET desktop application** for viewing and analyzing ultrasound DICOM studies.

- **Platform mandate:** desktop-only, built with the latest .NET and C#.
- **Primary runtime target:** .NET 10 + C# (latest stable language features supported by the SDK).
- **UI framework:** WPF desktop application.
- **Project purpose:** internal/research use.

The application starts in `Codex`, and sample studies are located in `SampleFiles` at repository root.

## 2. Scope

The app must provide:

- DICOM loading and cine playback.
- Time-series grouping by acquisition time.
- ROI drawing and persistence.
- ROI intensity statistics.
- DICOM tag inspection.
- CEUS/SHI classification using Pixel Spacing.
- Persistent per-folder settings and last directory recall.

## 3. Functional Requirements

### 3.1 DICOM Loading and Display

- Load DICOM files from a selected folder.
- Display images in a scaled canvas/image surface.
- Support multi-frame DICOM as cine/video.
- Provide Previous/Next navigation and playback controls.
- Provide FPS slider for playback speed.

### 3.2 UI Layout

- **Top bar:** folder path input, Browse, Reload, Settings.
- **Left panel:** tree control of grouped files.
- **Right panel:**
  - Controls row (Play, Previous, Next, Clear ROI, FPS + slider).
  - Selected file label.
  - Vertical split between image viewer and tag panel.
  - ROI Mean display and status label.

### 3.3 Time-Series Grouping

- Group by `AcquisitionDateTime` into contiguous sequences.
- Start a new group when gap between consecutive files exceeds time window.
- Default time window: **60 seconds**.
- Group label shows start-end time when applicable.
- Files without `AcquisitionDateTime` are grouped together in an unknown-time group.
- Tree groups are collapsed by default.
- Selecting a group node selects/displays first file in that group.

### 3.4 DICOM Tag Viewer

- Display all tags for selected file, excluding PixelData.
- Render as: `GGGG,EEEE Name: Value`.

### 3.5 ROI Drawing and Persistence

- Draw one rectangular ROI via click-drag.
- ROI coordinates are stored in image space (not canvas space).
- ROI applies to all files in the current time-series group.
- Starting a new ROI replaces prior ROI for that group.
- Clear ROI removes ROI for all files in current group.
- Persist ROI data in `dicom_viewer.roi` in the loaded folder.
- Do not read legacy per-image `.roi` files.

### 3.6 ROI Statistics

- Compute per-file ROI mean intensity across all frames.
- UI shows the selected file’s mean intensity (not group average).
- Update ROI mean immediately after file load and ROI draw.
- Compute/update other files’ group statistics in the background.

### 3.7 CEUS/SHI Classification

- Classify by closest distance to configurable target spacings.
- CEUS default target: **0.5**.
- SHI default target: **0.3**.
- Append `(SHI)` to SHI files in the tree.
- Pixel spacing sources (in order):
  - `PixelSpacing`
  - `ImagerPixelSpacing`
  - DICOM tag `(0028,0030)`
- Handle values in formats such as `0.3\\0.3` and `[0.3, 0.3]`.

### 3.8 Filtering

- Filter box under the tree.
- Case-insensitive substring filter against tag text and file metadata shown in tree.

### 3.9 Settings and Persistence

- All persistent analysis data is stored in the loaded folder.
- Per-folder settings file: `dicom_viewer_directory.settings` with:
  - `timeWindowSeconds`
  - `ceusSpacing`
  - `shiSpacing`
- ROI data file: `dicom_viewer.roi`.
- App-level settings file: `app_settings.json` next to `dicom_viewer.py` in `Codex`.
  - Stores `lastLoadedDirectory`.
  - On startup, auto-load if directory still exists.

### 3.10 Settings Dialog

- Opened from Settings button.
- Editable fields:
  - Time Window (seconds)
  - CEUS PixelSpacing target
  - SHI PixelSpacing target
- Saving persists settings and rebuilds the group/file tree.

## 4. Non-Functional Requirements

- Use latest .NET/C# supported by project SDK.
- Keep UI responsive during image load and stats computation.
- Run long operations on background threads; marshal UI updates to main thread.
- Maintain stable behavior for studies with up to ~1000 frames per file.
- Prefer permissive OSS dependencies suitable for internal research usage.

## 5. Technical Stack

- **Application:** WPF desktop using **MVVM** (Model–View–ViewModel) architecture
- **Runtime:** .NET 10
- **Language:** C# (latest supported)
- **MVVM toolkit:** CommunityToolkit.Mvvm 8.4.0 (ObservableObject, RelayCommand, ObservableProperty source generators)
- **DICOM library:** fo-dicom 5.2.5
- **Image rendering:** WriteableBitmap with unsafe pixel copy from fo-dicom IImage
- **Testing:** xUnit 2.9.3 (and Moq 4.20.72 where needed)
- **Solution format:** .slnx (new .NET 10 format)

### 5.1 Architecture

The application follows a strict MVVM pattern:

- **Models** (`DicomViewer.Core/Models/`): Pure data classes — `DicomFileEntry`, `TimeSeriesGroup`, `RoiData`, `FolderSettings`, `AppSettings`, `FileClassification`.
- **Services** (`DicomViewer.Core/Services/`): All business logic — `DicomFileService`, `ClassificationService`, `TimeSeriesGroupingService`, `RoiService`, `RoiStatisticsService`, `SettingsService`, `DicomTagService`.
- **ViewModels** (`DicomViewer.Web/ViewModels/`): `MainViewModel` (primary), `SettingsViewModel`, `TreeGroupViewModel`, `TreeFileViewModel`. Use CommunityToolkit.Mvvm source generators for observable properties and commands.
- **Views** (`DicomViewer.Web/`): `MainWindow.xaml`, `SettingsDialog.xaml`. Minimal code-behind — only UI-specific interactions (canvas ROI drawing, file dialogs, tree selection routing).
- **Core library** (`DicomViewer.Core`): Shared between WPF host and test project. Contains all models and services with no WPF dependencies.

## 6. Testing Requirements

### 6.1 Classification Test

- Add/maintain CEUS classification unit test using files from `SampleFiles\\DICOM 20251125`.
- Expected CEUS files:
  - `IM_0001`, `IM_0012`, `IM_0023`, `IM_0032`, `IM_0043`, `IM_0054`, `IM_0065`,
    `IM_0076`, `IM_0087`, `IM_0098`, `IM_0108`, `IM_0119`, `IM_0130`, `IM_0141`,
    `IM_0152`, `IM_0163`, `IM_0164`.

### 6.2 Time-Series Grouping Test

- Use a reduced sample subset for speed.
- Validate group counts for 60-second and 600-second windows.

### 6.3 ROI Behavior Tests

- Verify group-wide ROI apply/clear behavior.
- Verify mean intensity is computed and persisted per file.
- Verify selected-file ROI mean is displayed correctly.

## 8. Known Limitations

- fo-dicom pixel rendering in headless (non-WPF) test environments returns all-black pixels. ROI mean intensity unit tests verify non-negative rather than positive values. Full rendering validation must occur in the WPF context.
- fo-dicom.Imaging.ImageSharp package does not support net10.0. Frame rendering uses WriteableBitmap with unsafe pixel-by-pixel copy from `IImage.GetPixel()`.
- App settings are stored next to the application binary (`AppContext.BaseDirectory`), not in a user profile directory.

## 9. Deferred / Out-of-Scope for This Version

The following ideas are explicitly deferred to keep the desktop implementation focused:

- Web/OHIF/Cornerstone architecture.
- DICOMweb/Orthanc integration.
- Advanced motion compensation workflows.
- Running MIP pipeline.
- Parametric imaging and respiratory gating.
- DICOM SR/GSPS export.

These can be added as future milestones after the baseline desktop requirements above are complete.
