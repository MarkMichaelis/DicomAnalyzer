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

		// if user requested JSON export from existing CSV
		var idx = Array.IndexOf(args, "--export-json");
		if (idx >= 0 && idx + 1 < args.Length)
		{
			var jsonOut = args[idx + 1];
			var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "master_output.csv");
			if (!File.Exists(csvPath))
			{
				Console.Error.WriteLine($"master_output.csv not found in {Directory.GetCurrentDirectory()}. Run extractor first.");
				return 2;
			}
			JsonExporter.ExportFromCsv(csvPath, jsonOut);
			Console.WriteLine($"Wrote JSON summary to {jsonOut}");
			return 0;
		}

		var extractor = new DataExtractor();
		var records = extractor.ExtractAll(inputPath);

		string outFile = Path.Combine(Directory.GetCurrentDirectory(), "master_output.csv");
		CsvExporter.Write(outFile, records);

		Console.WriteLine($"Wrote {records.Count} rows to {outFile}");
		return 0;
	}
}

// Additional CLI: export JSON summaries from an existing master CSV
public static class JsonExporter
{
	public static void ExportFromCsv(string csvPath, string jsonOut)
	{
		if (!File.Exists(csvPath)) throw new FileNotFoundException(csvPath);

		var lines = File.ReadAllLines(csvPath).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
		var rows = lines.Select(l => ParseCsvLine(l)).ToList();

		// build lookup table
		var lookup = AttenuationLookupTable.Table;

		// group by ExcelFilePath
		var groups = rows.GroupBy(r => r.ExcelFilePath).ToList();

		var summaries = new List<object>();

		// precompute RawPixelIntensity per ExcelFilePath
		var stats = new Dictionary<string, (double mean, double std)>();
		foreach (var g in groups)
		{
			var vals = g.Select(x => double.TryParse(x.Value_dB, out var v) ? v : double.NaN).Where(d => !double.IsNaN(d)).ToList();
			double mean = vals.Any() ? vals.Average() : double.NaN;
			double std = vals.Count > 1 ? Math.Sqrt(vals.Select(v => (v - mean) * (v - mean)).Sum() / (vals.Count - 1)) : 0.0;
			stats[g.Key] = (mean, std);
		}

		// For atmospheric pixel intensity we need mapping by ConcentrationName and MmHg==0
		// build mapping concentration -> list of groups
		var groupMeta = new Dictionary<string, (string ExcelFilePath, double AmbientPressurePsi, double MmHg, int Attenuation, string Concentration, double Raw) >();
		foreach (var g in groups)
		{
			var file = g.Key;
			var fn = Path.GetFileNameWithoutExtension(file);
			// Attenuation: last two digits of filename
			var attenStr = new string(fn.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
			int attenuation = 0;
			if (attenStr.Length >= 2) attenuation = int.Parse(attenStr.Substring(attenStr.Length - 2));

			// AmbientPressurePsi: parse leading numeric token. If it's a decimal (e.g. 0.4) use it directly.
			// Otherwise take first up-to-2 digits and divide by 10 (e.g. '04' -> 0.4)
			double ambientPsi = 0.0;
			var m = System.Text.RegularExpressions.Regex.Match(fn, "^(\\d+\\.\\d+)");
			if (m.Success)
			{
				double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ambientPsi);
			}
			else
			{
				var m2 = System.Text.RegularExpressions.Regex.Match(fn, "^(\\D*?)(\\d{1,2})");
				if (m2.Success)
				{
					if (int.TryParse(m2.Groups[2].Value, out var fd)) ambientPsi = fd / 10.0;
				}
			}
			double mmhg = ambientPsi * 50.0;

			var parent = Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty) ?? string.Empty;
			var raw = stats.ContainsKey(file) ? stats[file].mean : double.NaN;

			groupMeta[file] = (file, ambientPsi, mmhg, attenuation, parent, raw);
		}

		foreach (var g in groups)
		{
			var file = g.Key;
			var meta = groupMeta[file];
			var frameValues = g.Select(x => x.Value_dB).ToList();
			var raw = stats[file].mean;
			var std = stats[file].std;

			// lookup attenuation
			lookup.TryGetValue(meta.Attenuation, out var lookupEntry);

			double decibelAdj = lookupEntry?.DecibelAdjustmentValue ?? 0.0;
			double kpa = lookupEntry?.Kpa ?? double.NaN;

			double psiNormalized = double.IsNaN(raw) ? double.NaN : raw + decibelAdj;

			// find atmospheric (MmHg==0) within same concentration
			double? atmospheric = null;
			var sameCon = groupMeta.Values.Where(v => v.Concentration == meta.Concentration && Math.Abs(v.MmHg - 0.0) < 1e-6).ToList();
			if (sameCon.Any())
			{
				atmospheric = sameCon.Select(v => v.Raw).Where(d => !double.IsNaN(d)).DefaultIfEmpty(double.NaN).Average();
				if (double.IsNaN(atmospheric.Value)) atmospheric = null;
			}

			double? atmosNorm = null;
			if (atmospheric.HasValue && !double.IsNaN(psiNormalized)) atmosNorm = psiNormalized - atmospheric.Value;

			object NullableDouble(double d) => double.IsNaN(d) ? null : (object)d;

			var obj = new Dictionary<string, object?>()
			{
				["ExcelFilePath"] = file,
				["ConcentrationName"] = meta.Concentration,
				["AmbientPressurePsi"] = NullableDouble(meta.AmbientPressurePsi),
				["MmHg"] = NullableDouble(meta.MmHg),
				["Attenuation"] = NullableDouble(meta.Attenuation),
				["kPa"] = NullableDouble(kpa),
				["FramePixelIntensity"] = frameValues,
				["RawPixelIntensity"] = NullableDouble(raw),
				["PixelIntensityStd"] = NullableDouble(std),
				["DecibelAdjustmentValue"] = NullableDouble(decibelAdj),
				["PsiNormalizedPixelIntensity"] = NullableDouble(psiNormalized)
			};

			if (atmospheric.HasValue) obj["AtmosphericPixelIntensity"] = atmospheric.Value;
			if (atmosNorm.HasValue) obj["AtmosphericNormalizedPixelIntensity"] = atmosNorm.Value;

			summaries.Add(obj);
		}

		var json = System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions{WriteIndented=true});
		File.WriteAllText(jsonOut, json);
	}

	private static (string ExcelFilePath, string DICOMFilePath, string PatientName, string DICOMFileDate, string ColumnName, int FrameNumber, string Value_dB) ParseCsvLine(string line)
	{
		// simple split by comma, CSV values don't contain commas in this dataset
		var parts = line.Split(',');
		return (parts[0], parts[1], parts[2], parts[3], parts[4], int.Parse(parts[5]), parts[6]);
	}
}

public class AttenuationLookupTableEntry { public int Atten { get; set; } public double Mpa { get; set; } public double Kpa { get; set; } public double DecibelAdjustmentValue { get; set; } }
public static class AttenuationLookupTable
{
	public static Dictionary<int, AttenuationLookupTableEntry> Table = new Dictionary<int, AttenuationLookupTableEntry>
	{
		[81] = new AttenuationLookupTableEntry{Atten=81, Mpa=0.010858263, Kpa=11, DecibelAdjustmentValue=0},
		[74] = new AttenuationLookupTableEntry{Atten=74, Mpa=0.034830711, Kpa=35, DecibelAdjustmentValue=0},
		[69] = new AttenuationLookupTableEntry{Atten=69, Mpa=0.073047737, Kpa=73, DecibelAdjustmentValue=0},
		[65] = new AttenuationLookupTableEntry{Atten=65, Mpa=0.120399211, Kpa=120, DecibelAdjustmentValue=0},
		[62] = new AttenuationLookupTableEntry{Atten=62, Mpa=0.163957053, Kpa=164, DecibelAdjustmentValue=0},
		[57] = new AttenuationLookupTableEntry{Atten=57, Mpa=0.256446158, Kpa=256, DecibelAdjustmentValue=0},
		[53] = new AttenuationLookupTableEntry{Atten=53, Mpa=0.352600053, Kpa=353, DecibelAdjustmentValue=0},
		[50] = new AttenuationLookupTableEntry{Atten=50, Mpa=0.427226132, Kpa=427, DecibelAdjustmentValue=8},
		[47] = new AttenuationLookupTableEntry{Atten=47, Mpa=0.521499895, Kpa=521, DecibelAdjustmentValue=8},
		[45] = new AttenuationLookupTableEntry{Atten=45, Mpa=0.586335789, Kpa=586, DecibelAdjustmentValue=8},
		[43] = new AttenuationLookupTableEntry{Atten=43, Mpa=0.637719053, Kpa=638, DecibelAdjustmentValue=8},
		[41] = new AttenuationLookupTableEntry{Atten=41, Mpa=0.705052974, Kpa=705, DecibelAdjustmentValue=8}
	};
}
