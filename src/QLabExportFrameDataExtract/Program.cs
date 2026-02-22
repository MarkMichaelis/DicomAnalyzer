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

		var extractor = new DataExtractor();
		var records = extractor.ExtractAll(inputPath);

		string outFile = Path.Combine(Directory.GetCurrentDirectory(), "master_output.csv");
		CsvExporter.Write(outFile, records);

		Console.WriteLine($"Wrote {records.Count} rows to {outFile}");
		return 0;
	}
}
