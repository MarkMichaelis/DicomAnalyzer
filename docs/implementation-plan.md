# DICOM Analysis Application Implementation Plan

## 1. Goal and Constraints

- Build a single-platform desktop application using **WPF + .NET 10 + C#**.
- Implement only the in-scope requirements from `docs/product-spec.md`.
- Keep UI responsive for cine playback and ROI/stat computation.
- Persist settings/ROI data exactly as specified.

## 2. Milestones

## Milestone 0: Foundation Setup

### Objectives
- Establish project structure, coding standards, and test harness.

### Tasks
- Create/verify solution structure:
  - `DicomViewer.Web` (current desktop host logic/UI assets)
  - `DicomViewer.Tests`
- Confirm package baselines:
  - `fo-dicom`
  - test dependencies (`xUnit`, `Moq` as needed)
- Define architecture layers:
  - Models
  - Services
  - Controllers/UI orchestration
- Add/update `.gitignore` for build artifacts.
- Add CI script or local script for repeatable test runs.

### Exit Criteria
- Solution builds cleanly.
- Test project runs from command line.

## Milestone 1: Directory Load + Core Viewer

### Objectives
- Load DICOM files from folder and display selected image/frame.

### Tasks
- Folder load flow:
  - Input path, browse path, reload actions.
  - Validate directory exists.
- Parse DICOM metadata and pixel data with fo-dicom.
- Render selected frame with proper scaling.
- Implement frame controls:
  - Previous/Next file
  - Play/Pause
  - FPS slider
  - Frame slider for multi-frame files
- Display selected file info label (`name`, dimensions, frame count).

### Exit Criteria
- Files load from sample directory.
- Multi-frame playback works with adjustable FPS.

## Milestone 2: Time-Series Grouping + Tree UI

### Objectives
- Group by `AcquisitionDateTime` and render hierarchical tree.

### Tasks
- Implement grouping algorithm:
  - New group when time gap > `timeWindowSeconds`
  - Unknown time group for missing timestamps
- Generate stable group labels (`start - end` or `Unknown Time`).
- Render groups in collapsed-by-default tree.
- Group-node click behavior:
  - Select first file in group
  - Do not require expansion first
- Add filter box for case-insensitive tree filtering.

### Exit Criteria
- Tree reflects expected groups for 60s and 600s settings.
- Group selection reliably loads first image.

## Milestone 3: DICOM Tags + Classification

### Objectives
- Show tag panel and CEUS/SHI file classification.

### Tasks
- Tag panel:
  - Load all tags except `PixelData`
  - Format display as `GGGG,EEEE Name: Value`
- Classification logic:
  - Parse Pixel Spacing from:
    - `PixelSpacing`
    - `ImagerPixelSpacing`
    - `(0028,0030)`
  - Handle strings like `0.3\\0.3` and `[0.3, 0.3]`
  - Classify by nearest of `ceusSpacing` and `shiSpacing`
- Tree annotation:
  - Append `(SHI)` for SHI files.

### Exit Criteria
- Tag panel updates on file selection.
- SHI tagging appears correctly in tree.

## Milestone 4: ROI Draw/Clear + Persistence

### Objectives
- Implement rectangular ROI and persistence semantics.

### Tasks
- Draw rectangular ROI via click-drag on image.
- Convert and store ROI in image coordinates.
- Group-wide ROI behavior:
  - New ROI replaces previous ROI for current group.
  - Clear ROI removes group ROI from all files in group.
- Persist to `dicom_viewer.roi` in loaded folder.
- Load ROI file on directory load.

### Exit Criteria
- ROI renders consistently when switching files in group.
- Clear ROI removes all corresponding group entries.

## Milestone 5: ROI Statistics + Background Work

### Objectives
- Compute per-file mean intensity and keep UI responsive.

### Tasks
- Compute mean intensity per file over all frames inside ROI.
- Display current file mean (`ROI Mean`) in UI.
- Trigger updates:
  - On file selection
  - After ROI draw
  - After ROI clear
- Background processing:
  - Compute non-selected files in group asynchronously.
  - Update persisted ROI stats when complete.
- Add status updates for long-running operations.

### Exit Criteria
- Current-file mean appears quickly and accurately.
- Background calculations do not freeze UI.

## Milestone 6: Settings and Startup Persistence

### Objectives
- Implement folder-level and app-level persistence.

### Tasks
- Per-folder settings file `dicom_viewer_directory.settings`:
  - `timeWindowSeconds`
  - `ceusSpacing`
  - `shiSpacing`
- Settings dialog:
  - Edit all three fields
  - Save and rebuild tree/grouping
- App settings file `app_settings.json` in `Codex`:
  - Save `lastLoadedDirectory`
  - Auto-load on startup if path still exists

### Exit Criteria
- Restarting app restores last directory automatically.
- Updated settings survive reload/restart.

## Milestone 7: Test Completion and Hardening

### Objectives
- Ensure core behavior is covered by unit tests and regression-safe.

### Required Tests
- Classification tests:
  - Verify CEUS sample list from `SampleFiles\\DICOM 20251125`
- Time-series grouping tests:
  - Validate expected groups at 60s and 600s
- ROI tests:
  - Group-wide apply and clear behavior
  - Per-file mean persistence
- Settings tests:
  - Folder settings read/write
  - Last-loaded directory read/write

### Hardening Tasks
- Eliminate unstable group ID behavior (deterministic group identity).
- Verify enum/string JSON serialization contract for frontend/UI bindings.
- Validate no UI controls remain enabled when no files are loaded.
- Ensure no blocking calls on UI thread for image/stats load.

### Exit Criteria
- Build succeeds.
- All tests pass.
- Manual smoke test completed on sample directory.

## 3. Suggested Sprint Order

- Sprint 1: Milestones 0-1
- Sprint 2: Milestones 2-3
- Sprint 3: Milestones 4-5
- Sprint 4: Milestones 6-7

## 4. Definition of Done (Release Candidate)

- All in-scope product-spec features implemented.
- Persisted files created in correct locations and formats.
- No known crash on startup, directory load, file switch, ROI save/clear.
- Test suite green and reproducible from CLI.
- Documentation updated (`docs/product-spec.md` + this implementation plan).

## 5. Deferred (Out of Scope for Current Version)

- Motion compensation workflows.
- Running MIP processing.
- Parametric imaging.
- Respiratory gating.
- DICOM SR/GSPS export.
- Orthanc/DICOMweb integration.
