# Ultrasound DICOM ROI & Analytics Application Platform Research

## Executive summary

The requirements span interactive viewing (cine-capable ultrasound DICOM), ROI drawing (freehand and ellipse), ROI reuse across frames/files, **pixel-intensity fidelity**, large-directory ingest, motion compensation, and a running MIP workflow.  We recommend a **hybrid architecture** combining a web-based viewer (OHIF/Cornerstone) and a compute/export backend. This leverages the best open-source viewer controls (ROI tools with stats, measurement workflows) while offloading intensive analytics (registration, running MIP, ROI stats) to a service environment (Python or .NET).  Key findings:

- **Viewer/ROI tools:** OHIF/Cornerstone offers out-of-the-box ROI tools with area/mean/stddev calculation, and supports multi-frame cine playback. It also has an extensible “measurement tracking” system that can export ROI data to CSV or DICOM SR and rehydrate it. ([docs.ohif.org](https://docs.ohif.org/user-guide/viewer/measurement-panel?utm_source=chatgpt.com))
- **Pixel-intensity fidelity:** The analytics pipeline must explicitly handle DICOM’s grayscale pipeline. Statistics can be reported in multiple domains (e.g. raw stored values, modality-rescaled, or windowed) per the spec. We note the user wants **support for multiple intensity modes** and **normalization** of ultrasound gain. ([dicom.nema.org](https://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_c.11.2.html?utm_source=chatgpt.com))
- **Motion compensation:** Robust libraries exist (SimpleITK, Elastix, OpenCV) and SimpleITK’s `MaximumImageFilter` directly supports pixelwise max for running MIP accumulation. These can run offline (preprocessing) rather than real-time, meeting the user’s requirement for offline processing. ([simpleitk.org](https://simpleitk.org/doxygen/v2_4/html/classitk_1_1simple_1_1MaximumImageFilter.html?utm_source=chatgpt.com))
- **Large-directory ingest:** Browser drag/drop is easy for small sets, but for large directories we recommend indexing via Orthanc + DICOMweb. Orthanc’s official DICOMweb plugin implements QIDO/WADO/STOW protocols, enabling scalable queries. ([orthanc.uclouvain.be](https://orthanc.uclouvain.be/book/plugins/dicomweb.html?utm_source=chatgpt.com))
- **Excel export:** Since complex pivot/chart automation can be deferred, start with CSV or simple XLSX export (ClosedXML for .NET or pandas CSV for Python). ([docs.closedxml.io](https://docs.closedxml.io/en/latest/features/pivot-tables.html?utm_source=chatgpt.com)) 

Key decisions gleaned from provided answers:  
- **Intensity domain:** Support multiple domains (stored, rescaled, windowed). Normalization of gain/dynamic range is required.  
- **Transfer syntaxes:** Not yet determined; user will provide sample data. Prepare for common ultrasound syntaxes (uncompressed, JPEG, JPEG-LS/2000, possibly H.264 video). ([dicom.nema.org](https://dicom.nema.org/medical/dicom/2023d/output/chtml/part05/sect_A.4.6.html?utm_source=chatgpt.com))
- **ROI reuse:** Only across same patient & study (frameOfReference).  
- **Persistence:** Use JSON for initial ROI storage; future v2 could add DICOM SR/GSPS export.  
- **ROI editing:** Overwriting previous ROIs is acceptable; support editing with undo (e.g., Ctrl+Z) must be implemented.  
- **Motion model:** Use all reasonable models (rigid, affine, deformable if needed).  
- **MIP scope:** At minimum within a single cine; multi-series MIP is a “nice to have.” The running MIP is used for consistent ROI positioning, not just display.  
- **Data scale:** Up to ~1000 frames per image; one-time directory import. Browser-only file operations should suffice at this scale.  
- **Deployment:** Research-only, internal lab use. Orthanc/AGPL concerns are minimal since no external distribution is planned. No special PHI/IRB measures needed initially.

## Requirements and technical contracts

### Ultrasound DICOM and ROI requirements

Ultrasound cine is formally a DICOM Multi-frame Image IOD, potentially with an **Ultrasound Region Calibration Module** to define physical spacing. ([dicom.nema.org](https://dicom.nema.org/medical/Dicom/2017e/output/chtml/part03/sect_C.8.5.5.html?utm_source=chatgpt.com))  Consistency of ROI reuse across frames relies on the DICOM `FrameOfReferenceUID`; we assume all frames in the same study share it. Thus, reusing ROIs across frames of one file (or files from the same study) is valid.  

ROIs must remain editable, with undo support. We must ensure the UI toolkit or state management tracks ROI actions so that Ctrl+Z can revert the last ROI edit or creation. In practice, Cornerstone’s annotation tools (used by OHIF) allow undo of the last added shape via its tool state management system (on keypress Undo) when integrated properly. If a custom UI is built (.NET or Python), similar undo stacks must be implemented for ROI actions.

### Pixel-intensity fidelity

The user requires “highly accurate pixel-intensity statistics.” In DICOM terms, this means carefully handling the pixel transformation pipeline. Statistics should be available in multiple domains: raw stored pixel values, modality-rescaled values (`RescaleSlope/Intercept`), or VOI-windowed values. The answers clarify normalization of ultrasound gain is needed, so the app likely needs to read any ultrasound-specific gain or TGC settings (if present in DICOM ultrasound tags) and allow post-hoc normalization.  

We will design to compute ROI stats in *all relevant domains*. For example:
- **Stored pixels:** decompress DICOM as-is, cast to full bit-depth, compute stats.  
- **Modality-rescaled:** apply `(stored * RescaleSlope + RescaleIntercept)` before stats.  
- **Windowed (display):** apply WindowCenter/Width or VOI LUT after modality LUT, then stats on those values.  

We must document which one we report by default. Based on user input, we’ll present the “raw” or “rescaled” metrics as primary, plus an option for windowed. Pydicom’s `apply_modality_lut` and `apply_voi_lut` functions formalize these steps【6†L4-L9】【5†L15-L20】. Fo-dicom’s `GrayscaleRenderOptions.FromDataset` provides similar metadata extraction (slope, intercept, window center/width) which we can use in .NET【7†L39-L46】.

### Multi-frame and video considerations

Ultrasound frames may be stored as a multi-frame image or encapsulated video (e.g. MPEG4/H.264). We should verify expected syntaxes by examining a sample directory. DICOM defines video transfer syntaxes (e.g. “MPEG4 AVC/H.264 High Profile / Level 4.1”【19†L1-L9】). Browser JS stacks (OHIF/Cornerstone) typically decode JPEG and JPEG2000 via WebAssembly, and may struggle with MPEG video unless a plugin or back-end provides it. Fo-dicom does not natively decode H.264 without external support (there are community discussions about this)【29†L16-L20】. We note this as an implementation risk to revisit once sample data clarifies the codecs used.

## Platform evaluation (key dimensions)

We compare **.NET**, **Python**, and **JavaScript/Web (OHIF/Cornerstone)** along each requested dimension. The findings are summarized in the table and text below.

| Dimension                              | JavaScript/Web                 | .NET                         | Python                      |
|----------------------------------------|--------------------------------|------------------------------|-----------------------------|
| **Viewer + ROI controls**<br>(cine playback + ROI tools) | *Strong:* OHIF is a full web viewer. Cornerstone3DTools provides built-in ellipse ROI (with area/mean/std-dev stats) and freehand ROI. OHIF’s Measurement Panel can export to CSV and handle measurements via DICOM SR.【1†L9-L13】【1†L14-L16】 | *Moderate:* No turnkey viewer UI. fo-dicom provides DICOM parsing and rendering (including multi-frame)【7†L39-L46】, but interactive UI (windows/forms with cine controls and ROI tools) must be custom-built. | *Weak:* pydicom is data-centric (read/write DICOM), not a viewer. You’d embed it in a GUI (e.g. Qt/VTK) or combine it with a web viewer. |
| **ROI types & stats**<br>(circle/ellipse & freehand) | *Strong:* EllipticalROI and FreehandROI tools exist. Ellipse ROI tool explicitly calculates statistics (area, max, mean, std-dev)【1†L9-L13】. Freehand ROI yields a polygon whose pixels we can sample for stats. | *Custom:* UI must provide ROI drawing (e.g. on a WinForms/WPF canvas). Fo-dicom can parse pixel data, but computing ROI stats is up to you (likely using `PixelData` and computed mask). | *Custom:* Same story as .NET: pydicom gives pixels, ROI UI via chosen GUI toolkit. Stats via NumPy/pandas once ROI mask is defined. |
| **ROI persistence**<br>(DICOM SR / GSPS / JSON) | *Yes – SR:* OHIF has a Measurement Service for exporting measurements as DICOM SR (Structured Report) and reloading SR for rehydration【2†L2-L4】. GSPS (Grayscale Presentation State) is possible but less common; OHIF’s workflow uses SR by default. | *Build:* fo-dicom can write DICOM SR/GSPS if you code it, but has no high-level mapping service. JSON storage can be done for quick iteration. | *Build:* pydicom can write SR/GSPS datasets too, but you must construct them. JSON is easiest for prototypes. |
| **ROI editing / Undo** | *Yes:* Cornerstone’s annotation state can support undo (Ctrl+Z) for drawing operations if properly implemented. OHIF’s tool state management allows last-action undo (for example, undoing the last ellipse draw). | *Yes (custom):* You’d implement an Undo stack in your UI (e.g. storing ROI shapes in a list and handling undo commands). .NET UI frameworks support Ctrl+Z handling out-of-box for text; for ROI shapes, you’d need to code it. | *Yes (custom):* Similar to .NET, implement Undo via storing history of ROIs. Python GUI toolkits (e.g. PyQt) allow intercepting Ctrl+Z to undo shape drawing if programmed. |
| **Pixel-data fidelity** | *Engineered:* Decompression and DICOM transform rules must be implemented in JS. Cornerstone’s DICOM loader handles baseline JPEG and JPEG2000; modality and VOI LUT must be applied manually (or use pydicom-like code in JS). | *Strong:* fo-dicom’s `GrayscaleRenderOptions.FromDataset` automatically loads RescaleSlope/Intercept, window center/width, etc.【7†L39-L46】. It also has a caching mode for multi-frame images. Codec support (JPEG2000, etc.) is via fo-dicom.Codecs (MS-PL; supports JPEG-LS, JPEG2000, HTJ2K). | *Strong:* pydicom provides `apply_modality_lut` and `apply_voi_lut` to correctly implement DICOM pipeline【6†L4-L9】【5†L15-L20】. It warns against using Pillow for decoding (it can irreversibly alter pixel data). Use GDCM or other strict handlers for fidelity. |
| **Cine/Multi-frame perf.** | *Caution:* Cornerstone’s multi-frame playback can drop frames on low-end machines【3†L1-L6】. OHIF released a 4D (cine) player, but performance varies. Must test on target hardware. Browser memory (especially for many frames) is a risk. | *Depend on app:* You manage frame buffering. fo-dicom can decode frames quickly, but the UI thread and rendering strategy dictate smoothness. .NET (especially on WinForms) can be less smooth than optimized C++. | *Depend on app:* GUI stack (Qt, Tk, etc.) drives performance. You could use PyQt or Matplotlib for frames, but those may struggle with 1000 fps refresh; faster path is needed (OpenGL or compiled viewer). |
| **Registration libraries** | *Available:* ITKElastix (WASM) provides elastix registration in-browser. OpenCV.js exists but is limited; heavy computation may lag. | *Strong:* SimpleITK C# bindings work on Windows; OpenCvSharp (Apache-2) provides OpenCV (including optical flow) on .NET (cross-platform)【18†L1-L4】. | *Strong:* SimpleITK Python, elastix Python, and OpenCV Python are all well-supported and cross-platform. |
| **Motion model** | (Multiple) Rigid/affine via elastix, optical flow via OpenCV.js. Deformable via elastix (if WASM build loaded). | (Multiple) Use SimpleITK for rigid/affine/deformable; OpenCV for 2D optical flow. | (Multiple) Use SimpleITK/elastix for all modes; OpenCV for optical flow. |
| **Running MIP** | Implement as: for each frame, register → resample → update accumulator with pixelwise max. Could run in JS if performance suffices, but better on backend service. | Straightforward: after registration, use SimpleITK’s `MaximumImageFilter` to accumulate max image. | Same as .NET with SimpleITK’s `MaximumImageFilter`. |
| **Ingestion strategy** | OHIF local mode: drag-and-drop works and keeps data in browser memory【8†L1-L5】; but doesn’t scale. Better: run a local Orthanc + DICOMweb server for large sets (OHIF has DICOMweb data source support). | Straightforward file I/O. For scale, still likely use a DICOMweb-indexed store. FO-DICOM can talk to Orthanc via REST. | Python: simple `os.walk`; but for large volumes, use Orthanc/dicomweb (e.g., `dicomweb-client`). |
| **Excel export** | OHIF can export CSV; for Excel needed, one would generate XLSX on server. | C#: ClosedXML (MIT) can write XLSX with pivots; charts need Open XML or templates. EPPlus is feature-rich but requires a commercial license for commercial use【16†L1-L7】【15†L1-L5】. | Python: pandas (BSD-3) can output CSV/XLSX. openpyxl (MIT) can preserve but not create pivots. E.g., generate CSV/XLSX data and leave pivots to user. |
| **Licensing** | OHIF/Cornerstone are MIT; Orthanc core is GPL3, DICOMweb plugin is AGPL (important if redistribution). OHIF explicitly says it is *not* FDA cleared or CE marked (suitable language for research tools).【4†L1-L4】 | fo-dicom is MS-PL (permissive); OpenCvSharp is Apache-2. ClosedXML MIT; EPPlus Polyform Noncommercial. System.Drawing (GDI) in .NET may limit Linux builds unless using alternative renderers. | pydicom, pandas, openpyxl are MIT/BSD. ITK & elastix are BSD. SimpleITK is Apache-2. All are cross-platform. |

【1†L9-L13】【7†L39-L46】

- *OHIF measurement panel:* screenshot showing ROI stats export. 
- *Cornerstone elliptical ROI:* example ROI and stats (Illustrative). 

- *SimpleITK MaximumImageFilter:* documentation snippet on maximum filter usage【6†L4-L9】.

## Architectures and recommendations

Based on the above, **two architecture families** emerge:

- **Hybrid (Recommended):** Web viewer + backend compute.  
  - **Viewer:** OHIF (React) + Cornerstone3DTools. Provides multi-frame viewer, ROI tools, and measurement panel with CSV/SR export. Built-in support for ROI editing and undo (via tool state history).  
  - **Archive:** Local Orthanc with DICOMweb plugin. Handles large-directory indexing (QIDO for search, WADO for frame retrieval, STOW for potential SR). OHIF can connect via DICOMweb.  
  - **Compute Service:** A background service (Python or .NET) that pulls pixel data (via DICOMweb), performs registration + resampling (SimpleITK/Elastix/OpenCV), updates the running MIP (SimpleITK max filter) and computes ROI stats in all chosen domains. Exports a “facts table” (CSV/XLSX) for Excel.  
  - **Excel Interaction:** Prototype by emitting CSV. If pivots/charts needed, use ClosedXML (C#) or let users define in Excel.  
  - **Licensing:** All code used is open/permissive except Orthanc plugin (AGPL). Since use is internal research-only, this is acceptable with proper attribution.  

```mermaid
flowchart LR
    Viewer[Viewer/UI\n(OHIF + Cornerstone3DTools)] -.->|Measurement export (CSV/SR)| Archive
    Archive[Orthanc DICOMweb Server\n(QIDO/WADO/STOW)]
    Viewer -- QIDO/WADO --> Archive
    Archive -- WADO-RS --> Compute
    Compute[Analytics Service\n(SimpleITK + Python/.NET)]
    Compute -->|Accumulate MIP & ROI Stats| Viewer
    Compute -->|Export CSV/XLSX| Client[Excel/Post-processing]
```

- **Single-platform .NET Desktop:** All functionality in one app (WPF or WinUI).
  - Pros: One language for all parts, best for a C# dev. Can use fo-dicom for DICOM parsing/rendering and SimpleITK C# + OpenCvSharp for algorithms.
  - Cons: Must build ROI drawing/undo UI, cine player, and DICOM navigation from scratch (no off-the-shelf viewer framework). Windows-centric (WPF), or need cross-platform UI library (Avalonia).
  - Use cases: Lean on ClosedXML for Excel (pivots), Open XML SDK for charts if needed.  
- **Single-platform Python Desktop:** Using PyQt/VTK or similar GUI.
  - Pros: SimpleITK and pydicom are straightforward for analysis, and `dicomweb-client` for data access. Pandas can generate export tables.
  - Cons: Must implement GUI (maybe embed a web viewer or use image widgets) and ROI editing. Fewer ready ROI tools than OHIF.  

Given the user’s C# background and the requirement for fast prototyping with strong ROI tools, the **hybrid approach** strikes the best balance. It offloads UI complexity to OHIF/Cornerstone (which your devs can customize) and leaves algorithmic heavy-lifting to Python or C# services. The .NET-only option is still viable if you want to stay entirely in C#, but expect more initial UI work.

## Prioritized prototype roadmap

To de-risk quickly, we suggest this order:

1. **Dataset & Codec exploration (Week 1–2):** Collect representative ultrasound DICOM cinés. Identify transfer syntaxes (uncompressed, JPEG, JPEG2000, MPEG/H.264, etc.). Ensure at least one decoding path for each (e.g., confirm PNG/MP4 support in test environment).  
2. **Viewer + ROI Basics (Week 2–4):** Stand up OHIF in local mode; test multi-frame cine playback and ROI tools (ellipse + freehand). Hook up measurement panel (CSV export) and test DICOM SR export (optional). Verify ROI drawing is responsive and can be undone (OHIF default behavior allows undo of last annotation).  
3. **Intensity pipeline test (Week 3–4):** Implement a small script (Python or C#) that reads sample DICOM, applies modality LUT and VOI LUT, and computes ROI stats. Compare results across libraries (pydicom vs fo-dicom) for consistency. Determine how to apply normalization (e.g., adjust for probe gain if metadata available).  
4. **Orthanc Integration (Week 4–6):** Install Orthanc + DICOMweb plugin. Import large sample directory. Configure OHIF to use DICOMweb data source. Test querying and retrieving frames via QIDO/WADO.  
5. **Registration prototype (Week 5–8):** In compute service, implement rigid (or affine) registration on two frames with SimpleITK. Add optical flow (OpenCV) as a fast baseline. Evaluate performance/accuracy.  
6. **Running MIP and ROI stats (Week 7–10):** Build the running MIP accumulator: after registration, resample each frame to a ref grid and `max` to accumulator (SimpleITK). Overlay ROI(s) to verify they align over time. Compute ROI stats at each step.  
7. **Export pipeline (Week 9–11):** Format and output the facts table (CSV). Optionally, generate an XLSX via ClosedXML. Verify pivoting in Excel works with the output schema.  

Throughout, keep note of integration points: Undo in ROI drawing (Cornerstone), multi-frame memory use (cache policy), and exact normalization factors needed. If any step fails, prioritize alternate approaches (e.g., if in-browser cine fails, consider desktop viewer fallback).

## Updated product-spec clarifications

Based on the user’s answers, these key points shape the final design:

- **Intensity modes:** We will implement multiple modes. By default, ROI stats will reflect **raw stored values** and **rescaled values**. “Windowed” values will be available on request. Ultrasound *gain normalization* will be a preprocessing step (e.g. using DICOM “Gain Correction” tags if present, or by applying a user-defined gain factor) to meet the “accuracy” requirement.
- **Persistence format:** Initially, ROIs will be stored in a simple JSON format (making development faster). We will ensure it can later be exported to DICOM SR or GSPS for interoperability, but that’s not required in v1.
- **ROI editing:** ROI shapes will be editable. We will implement an undo stack so that each ROI creation/deletion can be undone (Ctrl+Z). Overwriting a previous ROI (i.e. replace rather than layering) is acceptable per spec.
- **Motion compensation:** The tool will support rigid, affine, and (optionally) deformable registration. Motion compensation is performed offline (preprocessing). The ROI will then be fixed in the reference frame while we compute the running MIP.
- **MIP scope:** The running MIP will at least cover each cine loop (single file). Extending the MIP across multiple series/files is a future enhancement.
- **Scale:** Up to ~1000 frames per study is expected. The import is a one-time operation. In-memory handling via Orthanc/DICOMweb is acceptable at this scale, so no heavy optimizations are needed for indexing.
- **Excel export:** A simple CSV “facts table” export is acceptable for the first version. This will contain identifiers and ROI statistics, suitable for pivoting in Excel.
- **Undo functionality:** ROI drawing will support Undo (Ctrl+Z) to revert the last drawn or modified ROI. This is feasible in Cornerstone’s tool architecture and will be built into the UI.
- **Licensing/PHI:** The app is internal research-only. Orthanc/AGPL code can be used as a service without source-distribution risk. No PHI/IRB measures needed at this stage; data can be de-identified before use if required later.

This completes the updated analysis incorporating all clarified answers and the new Undo requirement.