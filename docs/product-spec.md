# DICOM Analysis Application: Product Specification

## 1. Overview

This project is a **single-platform .NET desktop application** for viewing and analyzing ultrasound DICOM studies.

- **Platform mandate:** desktop-only, built with the latest .NET and C#.
- **Primary runtime target:** .NET 10 + C# (latest stable language features supported by the SDK).
- **UI framework:** WPF desktop application.
- **Project purpose:** internal/research use.

The application sample studies are located in `SampleFiles` at repository root.

## 2. Scope

The app must provide:

- DICOM loading and cine playback.
- Time-series grouping by acquisition time.
- ROI drawing (rectangle, ellipse, freeform) and persistence.
- ROI intensity statistics.
- DICOM tag inspection and copying.
- CEUS/SHI classification using Pixel Spacing.
- Persistent per-folder settings, last directory recall, and window position recall.
- Excel export of ROI intensity data.
- Motion compensation between frames/files.
- Maximum Intensity Projection (MIP).
- Parametric imaging.
- Respiratory gating.

## 3. Functional Requirements

### 3.1 DICOM Loading and Display

- Load DICOM files from a selected folder.
- Display images in a scaled canvas/image surface.
- Support multi-frame DICOM as cine/video.
- Provide Previous/Next navigation and playback controls.
- Provide FPS slider for playback speed.

### 3.2 UI Layout

- **Top bar:** folder path input, Browse, Reload, Export Excel, Settings.
- **Left panel:** tree control of grouped files.
- **Right panel:**
  - Controls row (Play, Previous, Next, Clear ROI, ROI shape selector, FPS + slider).
  - Selected file label.
  - Vertical split between image viewer and tag panel.
  - ROI Mean display and status label.
- **Application icon:** ultrasound-themed icon displayed in title bar and taskbar.
- **Window position:** saved and restored between sessions. Window is ensured to be on-screen.

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
- Right-click context menu with:
  - **Copy Selected Tag(s):** copies one or more selected tags to clipboard.
  - **Copy All Tags:** copies all displayed tags to clipboard.
- Multi-select enabled (Extended selection mode).

### 3.5 ROI Drawing and Persistence

- Support three ROI shapes:
  - **Rectangle:** click-drag to draw axis-aligned rectangle.
  - **Ellipse:** click-drag to draw axis-aligned ellipse (inscribed in bounding box).
  - **Freeform:** click to add polygon vertices, double-click to close the shape.
- ROI shape selector in the controls row.
- ROI coordinates are stored in image space (not canvas space).
- ROI applies to all files in the current time-series group.
- Starting a new ROI replaces prior ROI for that group.
- Clear ROI removes ROI for all files in current group.
- Persist ROI data in `dicom_viewer.roi` in the loaded folder.
- Do not read legacy per-image `.roi` files.
- **Undo (Ctrl+Z):** restores the previous ROI state. Supports multi-level undo with full history stack.
- Visual appearance:
  - Rectangle: yellow dashed stroke with semi-transparent yellow fill.
  - Ellipse: cyan dashed stroke with semi-transparent cyan fill.
  - Freeform: lime dashed stroke with semi-transparent green fill.

### 3.6 ROI Statistics

- Compute per-file ROI mean intensity across all frames.
- Shape-aware intensity computation: uses each ROI shape's `ContainsPoint` geometry to determine pixel inclusion.
- For freeform shapes, computes bounding box from polygon vertices and uses ray-casting point-in-polygon test.
- UI shows the selected file's mean intensity (not group average).
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
- Case-insensitive substring filter against:
  - File name / tree display text.
  - DICOM tag names (e.g., "PatientName").
  - DICOM tag values (e.g., "Smith").
- Tag data is cached per file on directory load for fast filtering.
- Filter updates the tree in real-time as the user types.

### 3.9 Settings and Persistence

- All persistent analysis data is stored in the loaded folder.
- Per-folder settings file: `dicom_viewer_directory.settings` with:
  - `timeWindowSeconds`
  - `ceusSpacing`
  - `shiSpacing`
- ROI data file: `dicom_viewer.roi`.
- App-level settings file: `app_settings.json` in `AppContext.BaseDirectory`.
  - Stores `lastLoadedDirectory`.
  - Stores window position: `windowLeft`, `windowTop`, `windowWidth`, `windowHeight`, `windowState`.
  - On startup, auto-load last directory if it still exists.
  - On startup, restore window position/size (with on-screen validation).

### 3.10 Settings Dialog

- Opened from Settings button.
- Editable fields:
  - Time Window (seconds)
  - CEUS PixelSpacing target
  - SHI PixelSpacing target
- Saving persists settings and rebuilds the group/file tree.

### 3.11 Excel Export

- Export ROI intensity data to `.xlsx` via the Export Excel button.
- Save file dialog defaults to `ROI_Data.xlsx`.
- Workbook contains two sheets:
  - **ROI Data:** one row per DICOM file with columns for Group, File, Mean Intensity, Min, Max, Pixel Count.
  - **Summary:** one row per time-series group with average, min, max, and standard deviation of mean intensities.
- Uses ClosedXML library.

### 3.12 Motion Compensation (Planned)

- Compensate for inter-frame motion in CEUS sequences.
- Not yet implemented.

### 3.13 Maximum Intensity Projection — MIP (Planned)

- Generate MIP images from time-series groups.
- Not yet implemented.

### 3.14 Parametric Imaging (Planned)

- Generate parametric maps from CEUS perfusion data.
- Not yet implemented.

### 3.15 Respiratory Gating (Planned)

- Gate frames by respiratory cycle to reduce motion artifacts.
- Not yet implemented.

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
- **Excel export:** ClosedXML 0.104.2
- **Testing:** xUnit 2.9.3 (and Moq 4.20.72 where needed)
- **Solution format:** .slnx (new .NET 10 format)

### 5.1 Architecture

The application follows a strict MVVM pattern:

- **Models** (`DicomViewer.Core/Models/`): Pure data classes — `DicomFileEntry`, `TimeSeriesGroup`, `RoiData` (with `RoiShape` enum: Rectangle/Ellipse/Freeform), `FolderSettings`, `AppSettings`, `FileClassification`.
- **Services** (`DicomViewer.Core/Services/`): All business logic — `DicomFileService`, `ClassificationService`, `TimeSeriesGroupingService`, `RoiService`, `RoiStatisticsService`, `SettingsService`, `DicomTagService`, `ExcelExportService`.
- **ViewModels** (`DicomViewer.Desktop/ViewModels/`): `MainViewModel` (primary), `SettingsViewModel`, `TreeGroupViewModel`, `TreeFileViewModel`. Use CommunityToolkit.Mvvm source generators for observable properties and commands.
- **Views** (`DicomViewer.Desktop/`): `MainWindow.xaml`, `SettingsDialog.xaml`. Minimal code-behind — only UI-specific interactions (canvas ROI drawing, file dialogs, tree selection routing).
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
- Verify undo stack behavior: undo after set, after replace, after clear, multiple undos, and CanUndo state.

### 6.4 ROI Geometry Tests

- Verify `ContainsPoint` for rectangle ROI (inside, outside, edge).
- Verify `ContainsPoint` for ellipse ROI (inside, outside, center, near-boundary).
- Verify `ContainsPoint` for freeform polygon ROI (inside, outside, concave regions).

### 6.5 Excel Export Tests

- Verify `.xlsx` file is created with correct sheets.
- Verify ROI Data sheet contains correct per-file rows.
- Verify Summary sheet contains correct aggregate statistics.
- Verify empty ROI groups produce zero-value rows.

## 8. Known Limitations

- fo-dicom pixel rendering in headless (non-WPF) test environments returns all-black pixels. ROI mean intensity unit tests verify non-negative rather than positive values. Full rendering validation must occur in the WPF context.
- fo-dicom.Imaging.ImageSharp package does not support net10.0. Frame rendering uses WriteableBitmap with unsafe pixel-by-pixel copy from `IImage.GetPixel()`.
- App settings are stored next to the application binary (`AppContext.BaseDirectory`), not in a user profile directory.
- ClosedXML does not support embedded charts in exported Excel files; users must create charts manually.

## 9. Deferred / Out-of-Scope for This Version

The following ideas are explicitly deferred to keep the desktop implementation focused:

- Web/OHIF/Cornerstone architecture.
- DICOMweb/Orthanc integration.
- Motion compensation (see §3.12).
- Maximum Intensity Projection — MIP (see §3.13).
- Parametric imaging (see §3.14).
- Respiratory gating (see §3.15).
- DICOM SR/GSPS export.

These can be added as future milestones after the baseline desktop requirements above are complete.
