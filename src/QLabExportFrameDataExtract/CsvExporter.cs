using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;

namespace QLabExportFrameDataExtract;

public static class CsvExporter
{
    public static void Write(string outputPath, IEnumerable<FrameRecord> records)
    {
        using var writer = new StreamWriter(outputPath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteHeader<FrameRecord>();
        csv.NextRecord();
        foreach (var r in records)
        {
            csv.WriteRecord(r);
            csv.NextRecord();
        }
    }
}
