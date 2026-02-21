# Ultrasound DICOM ROI Application: Product Specification

## Overview

This application targets **ultrasound DICOM cine** data. It must enable interactive viewing, advanced ROI analytics, and integration with Excel. Key features include multi-frame ROI drawing (with undo), robust pixel-intensity measurement, motion compensation with a running MIP, and support for advanced ultrasound modes (parametric imaging and respiratory gating). 

The user is an experienced C# developer relying on rapid (“vibe”) coding. The tool will be for internal/research use (non-commercial, no PHI concerns). 

## Functional requirements

- **Viewer:** Display multi-frame DICOM ultrasound. Support cine loop playback and synchronize multiple viewports. Implement free-scroll and frame stepping.  
- **ROI drawing:** Allow drawing **multiple ROIs** of two types: freehand and ellipse/circle. ROIs must persist and be editable across frames and even across different files (same patient/study). Maintain each ROI as an independent annotation.  
- **Undo:** Support undo (Ctrl+Z) for ROI drawing and editing. Each draw or delete action should be revertible by standard undo.  
- **Pixel-intensity analytics:** Compute high-accuracy statistics within each ROI. Specifically:
  - Support multiple intensity domains: **raw stored pixels**, **modality-rescaled** values, and **windowed (VOI)** values.  
  - Handle ultrasound-specific considerations (e.g. gain/dynamic range normalization). [＊Point: use established libraries to apply DICOM LUTs and windowing【6†L4-L9】【7†L39-L46】.*]  
- **Export to Excel:** Export ROI measurements (area, mean, standard deviation, etc.) to Excel-compatible files. Provide rich table data for pivot tables and charts. Include a sample Excel report or template. (Complex pivot/chart automation can be post-processed; initial output can be CSV or simple XLSX.)  
- **Batch import:** Import and manage large directories of DICOM files. Provide a means to scan a folder or index files efficiently. Loading should handle thousands of images (e.g. one-time indexing).  
- **Motion compensation:** Detect and correct patient motion between frames/files. Implement rigid/affine (and optionally deformable) registration so that tracked ROIs stay consistent over time. Allow the user to manually adjust if needed.  
- **Running MIP:** Create a **running maximum-intensity-projection** across frames: align each frame via registration, then accumulate pixelwise maxima into a composite image. This MIP updates as new frames come in. The ROI positions should also be tracked into the MIP view.  
- **Parametric imaging:** Support calculation and display of parametric ultrasound maps (e.g. contrast kinetics, elastography, or other derived quantitative maps from the ROI intensity data). The specific modalities/parameters (strain, time-intensity curves, etc.) should be configurable.  
- **Respiratory gating:** Allow gating or binning of frames based on respiratory phase. This may involve reading external triggers (if available) or manual marking of inhale/exhale frames, to only analyze motion within a consistent breathing phase.  
- **Persistence:** Save and load session data (including ROIs, applied settings, and analysis results) between runs. Use an internal format (JSON or similar) and allow optional export to DICOM formats (SR or GSPS) for interoperability.  
- **User interface:** Provide an intuitive UI with at least two synchronized viewports (for ROI reuse). Include playback controls, ROI editing tools, and a measurement panel. Implement standard UI conventions (Undo/Redo, toolbars).  

## Non-functional requirements

- **Platform choice:** Must decide between .NET (WPF/WinUI), Python (Qt), or Web (Electron/React). Consider the best available open DICOM viewer libraries: OHIF/Cornerstone (Web), fo-dicom (C#), pydicom/VTK (Python).  
- **Performance:** Target smooth interactive playback (~30 fps) for a single viewport with up to ~1000 frames. Large datasets (10k+ images) must load within reasonable time (minutes for indexing).  
- **Cross-platform:** If using .NET, consider .NET 6+ for cross-platform (Avalonia/WPF). Python/PyQt is cross-platform; Web (Electron) runs on all desktops.  
- **Licensing:** Use permissively licensed (MIT/BSD/Apache) components. (OHIF/Cornerstone, fo-dicom, pydicom are permissive; Orthanc is GPL/AGPL, but as an offline server it’s allowed for internal use.)  
- **Rapid prototyping:** Favor libraries and patterns that allow “vibe coding” – quick iteration and live reload. For example, a React-based Web UI or a hot-reloading Python GUI.  

## Architecture and implementation notes

Based on these requirements, likely architecture options include:

- **Web-based app (Electron + OHIF):** Leverage OHIF/Cornerstone for viewer and ROI tools【1†L9-L13】. Use DICOMweb (Orthanc) or local file API for data. Implement motion/MIP in a background service (Node or Python) or via WebAssembly (ITK-wasm). Excel export via CSV/JavaScript library.  
- **.NET desktop:** Use fo-dicom for DICOM IO and rendering; build custom UI in WPF/WinUI. Use OpenCvSharp (Apache-2) or SimpleITK C# for registration/MIP; ClosedXML/EPPlus for Excel.  
- **Python desktop:** Use pydicom and a GUI toolkit (e.g. PyQt + VTK). Registration via SimpleITK/elastix, image display via numpy arrays and PyQt OpenGL. Pandas/openpyxl for Excel.  

The choice should prioritize available open ROI tools (favoring Web) vs. language comfort (favoring .NET). If rapid iteration is critical and sample data is available, a Web/Electron approach can leverage OHIF out-of-the-box features【1†L9-L13】.  

## Questions / open items

- What ultrasound parametric maps are specifically required (e.g. elastography, contrast kinetics)?  
- How will respiratory phase be captured (external trigger vs image-based)?  
- What file sizes or frame counts are expected for “large directories”?  

---

## Work Breakdown Structure

Below is a table of features broken into Epics, User Stories, and example technical tasks. Items are roughly sorted with higher-complexity features last.

| Feature/Epic                      | User Story (Scrum)                                  | Technical Tasks (AI-level)                         |
|-----------------------------------|-----------------------------------------------------|----------------------------------------------------|
| **Basic DICOM Viewer**            | As a user, I want to load and view ultrasound DICOM cine so I can scroll through frames. | - Integrate DICOM reading (fo-dicom/pydicom) <br> - Render frames in UI <br> - Implement playback controls |
| **Multi-frame ROI Drawing**       | As a user, I want to draw freehand and ellipse ROIs on images. | - Add freehand and ellipse ROI tools (Cornerstone / custom canvas) <br> - Track ROI coordinates in patient space <br> - Enable moving/resizing shapes |
| **ROI Persistence & Undo**        | As a user, I want ROIs to persist across frames and be undoable. | - Store ROIs in session state (JSON) <br> - Implement Undo stack (Ctrl+Z) for ROI add/delete/edit <br> - Reload ROIs when switching frames/files |
| **Pixel-Intensity Stats**         | As a user, I want accurate stats (mean, max, etc.) for each ROI in various intensity modes. | - Fetch raw pixel data for ROI masks <br> - Apply DICOM rescale and VOI LUT algorithms【6†L4-L9】【7†L39-L46】 <br> - Compute stats and normalize (gain) |
| **Excel Export**                 | As a user, I want ROI stats exported to Excel/CSV for analysis. | - Create export pipeline: compile ROI stats into CSV/Excel format <br> - Implement Excel writing (ClosedXML/ExcelJS/pandas) <br> - Include sample pivot chart in template |
| **Directory Import**              | As a user, I want to import an entire folder of DICOMs at once. | - Implement folder chooser and file scanning <br> - Index DICOM meta for quick lookup <br> - Handle incremental loading and large file sets |
| **Motion Compensation**           | As a user, I want frames aligned so that ROIs track patient anatomy. | - Prototype rigid registration between frames (ITK/Elastix) <br> - Optimize for speed or allow offline batch <br> - Integrate into pipeline after loading |
| **Running MIP**                   | As a user, I want to view a running MIP that accumulates max pixel values. | - After registration, resample images onto ref grid <br> - Apply pixelwise maximum filter (SimpleITK MaxImageFilter) <br> - Display MIP and update per new frame |
| **Parametric Imaging**            | As a user, I want to compute/display parametric maps (e.g. elastography). | - Integrate processing algorithms (e.g. strain calc, time-intensity analysis) <br> - Generate pseudo-color maps and overlay <br> - Allow saving parametric images |
| **Respiratory Gating**           | As a user, I want to limit analysis to specific breathing phases. | - Accept respiration trigger input (file or manual) <br> - Implement frame binning by respiratory cycle <br> - Provide gating controls in UI |
| **Advanced Integration**         | As a user, I want the tool to seamlessly integrate all features with good UX. | - Synchronize multiple viewports (e.g. cine + MIP) <br> - Optimize performance (multi-threading/GPU) <br> - Polish UI elements and help/documentation |

In this table, easier features (basic viewing, ROI tools) are first, while more complex image-processing features (registration, MIP, parametric, gating) are last. Each user story would break into further detailed tasks in actual sprint planning.

**Sources:** We built on knowledge of medical imaging libraries and standards: Cornerstone/OHIF ROI tools【1†L9-L13】, fo-dicom pixel handling【7†L39-L46】, Excel libraries【14†L38-L43】【16†L1-L7】, and registration/MIP filters【6†L4-L9】. Each feature will leverage these open-source components and follow DICOM best practices.