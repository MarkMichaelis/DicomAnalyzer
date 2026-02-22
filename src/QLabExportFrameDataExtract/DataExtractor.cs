using System.Collections.Generic;
using System.IO;

namespace QLabExportFrameDataExtract;

public class DataExtractor
{
    /// <summary>
    /// Recursively scans the input directory for .xls/.xlsx files and extracts frame records.
    /// </summary>
    public List<FrameRecord> ExtractAll(string inputPath)
    {
        var files = Directory.EnumerateFiles(inputPath, "*.xls", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(inputPath, "*.xlsx", SearchOption.AllDirectories));

        var results = new List<FrameRecord>();

        foreach (var file in files)
        {
            try
            {
                var reader = new ExcelReader(file);
                var meta = reader.ExtractMetadata();
                var cols = reader.ExtractEchoMeanColumns();

                foreach (var col in cols)
                {
                    for (int i = 0; i < col.Values.Count; i++)
                    {
                        results.Add(new FrameRecord
                        {
                            ExcelFilePath = Path.GetFullPath(file),
                            DICOMFilePath = meta.DICOMFilePath,
                            PatientName = meta.PatientName,
                            DICOMFileDate = meta.DICOMFileDate,
                            ColumnName = col.ColumnName,
                            FrameNumber = i + 1,
                            Value_dB = col.Values[i]
                        });
                    }
                }
            }
            catch
            {
                // Skip problematic files but continue processing others
            }
        }

        return results;
    }
}
