# QLabExportFrameDataExtract — Excel Data Extraction Tool Design

**Date:** 2026-02-21
**Status:** Approved

## Problem

Need to consolidate Echo Mean (dB) frame data from multiple Excel files across the SampleFiles directory into a single master CSV for analysis.

## Solution

Create a .NET 10 console application that:
- Recursively scans a configurable input directory (default: ./SampleFiles/DICOM 20251125/)
- Extracts fixed metadata from each .xls file: DICOMFilePath (B3), PatientName (B5), DICOMFileDate (B7)
- Dynamically finds all "Echo Mean (dB)" columns (header location varies by file)
- Extracts all frame values from each Echo Mean column
- Outputs consolidated data to master_output.csv

## Architecture

**Core Components:**
1. **ExcelReader** — Opens .xls files, extracts metadata from fixed cells, dynamically locates and reads all "Echo Mean (dB)" columns
2. **DataExtractor** — Recursively scans the input directory for all .xls files, orchestrates extraction
3. **CsvWriter** — Writes consolidated frame data to CSV
4. **Program.cs** — CLI entry point with argument parsing

## CLI Interface

\\\powershell
QLabExportFrameDataExtract [--input-path <folder>]
\\\

**Default:** \./SampleFiles/DICOM 20251125/\
**With argument:** \QLabExportFrameDataExtract --input-path "C:\\custom\\path"\

## CSV Output Format

**File:** \master_output.csv\

**Columns:**
- \ExcelFilePath\ — Full path to the Excel file being read
- \DICOMFilePath\ — Path from cell B3
- \PatientName\ — Value from cell B5
- \DICOMFileDate\ — Date from cell B7
- \ColumnName\ — The "Echo Mean (dB)" column header (e.g., "Echo Mean (dB)", "Echo Mean (dB) 2")
- \FrameNumber\ — Row number within the column (1-based)
- \Value_dB\ — The dB value for that frame

## Technology Stack

- **.NET 10**
- **ClosedXML** — Modern .xls/.xlsx reading (no Excel COM interop)
- **CsvHelper** — Robust CSV generation

## Error Handling

- Skip files that fail to open; log warnings to console
- Handle missing metadata cells gracefully (use empty string for missing values)
- Validate that "Echo Mean (dB)" header exists before processing
- Continue processing remaining files if one fails

## Testing Strategy

- Unit tests for ExcelReader (fixed cell extraction, dynamic column detection)
- Unit tests for DataExtractor (directory scanning)
- Integration test: process a sample .xls file and verify CSV output matches expected structure
