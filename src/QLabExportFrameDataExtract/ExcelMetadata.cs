namespace QLabExportFrameDataExtract;

/// <summary>
/// Represents metadata extracted from fixed cells in an Excel file.
/// </summary>
public class ExcelMetadata
{
    public string DICOMFilePath { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DICOMFileDate { get; set; } = string.Empty;
}
