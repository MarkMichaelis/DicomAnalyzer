using System;
using System.IO;

namespace QLabExportFrameDataExtract;

class Program
{
	static int Main(string[] args)
	{
		string defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", "DICOM 20251125");
		string inputPath = defaultPath;

		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == "--input-path" || args[i] == "-i")
			{
				if (i + 1 < args.Length)
				{
					inputPath = args[i + 1];
					i++;
				}
			}
		}

		if (!Directory.Exists(inputPath))
		{
			Console.Error.WriteLine($"Input path not found: {inputPath}");
			return 2;
		}

		// Debug mode: inspect a single file
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == "--debug-file" && i + 1 < args.Length)
			{
				var file = args[i + 1];
				var reader = new ExcelReader(file);
				var meta = reader.ExtractMetadata();
				Console.WriteLine($"Metadata: DICOMFilePath={meta.DICOMFilePath}, PatientName={meta.PatientName}, DICOMFileDate={meta.DICOMFileDate}");
				var cols = reader.ExtractEchoMeanColumns();
				Console.WriteLine($"Found {cols.Count} Echo columns");
				foreach (var c in cols)
				{
					Console.WriteLine($"Column: {c.ColumnName}, Values: {string.Join(",", c.Values.Take(5))} (showing up to 5)");
				}
				return 0;
			}
		}

		var extractor = new DataExtractor();
		var records = extractor.ExtractAll(inputPath);

		string outFile = Path.Combine(Directory.GetCurrentDirectory(), "master_output.csv");
		CsvExporter.Write(outFile, records);

		Console.WriteLine($"Wrote {records.Count} rows to {outFile}");
		return 0;
	}
}
