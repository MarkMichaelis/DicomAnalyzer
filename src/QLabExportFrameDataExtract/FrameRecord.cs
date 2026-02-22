namespace QLabExportFrameDataExtract;

public class FrameRecord
{
    public string ExcelFilePath { get; set; } = string.Empty;
    public string DICOMFilePath { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DICOMFileDate { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public int FrameNumber { get; set; }
    public string Value_dB { get; set; } = string.Empty;
}
