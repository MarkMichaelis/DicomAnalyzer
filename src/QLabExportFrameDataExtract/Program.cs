using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using ScottPlot;

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

		// Export directly to JSON (CSV export removed)

		// if user requested plotting for concentration 4e3
		var plotIdx = Array.IndexOf(args, "--plot-4e3");
		if (plotIdx >= 0)
		{
			string defaultJson = Path.Combine(inputPath, "sample_master_output.json");
			string defaultOut = Path.Combine(Directory.GetCurrentDirectory(), "plots", "4e3-concentration.png");
			string jsonIn = defaultJson;
			string outPng = defaultOut;
			if (plotIdx + 1 < args.Length && !args[plotIdx + 1].StartsWith("-"))
			{
				outPng = args[plotIdx + 1];
				plotIdx++;
			}
			// optional --plot-4e3-json <path>
			var jsonIdx = Array.IndexOf(args, "--plot-4e3-json");
			if (jsonIdx >= 0 && jsonIdx + 1 < args.Length) jsonIn = args[jsonIdx + 1];

			return Plot4e3FromJson(jsonIn, outPng);
		}

		var extractor = new DataExtractor();
		var records = extractor.ExtractAll(inputPath);

		string outJson = Path.Combine(Directory.GetCurrentDirectory(), "master_output.json");
		JsonExporter.ExportFromRecords(records, outJson);
		Console.WriteLine($"Wrote JSON summary with {records.Count} records to {outJson}");

		// determine graph output options
		string graphOutDir = Path.Combine(Directory.GetCurrentDirectory(), "plots");
		if (Array.IndexOf(args, "--no-graphs") >= 0) return 0;
		var graphIdx = Array.IndexOf(args, "--graph-output");
		if (graphIdx >= 0 && graphIdx + 1 < args.Length) graphOutDir = args[graphIdx + 1];

		// ensure directory exists
		if (!Directory.Exists(graphOutDir)) Directory.CreateDirectory(graphOutDir);

		// export JSON to graph output and plot all concentrations
		string jsonForGraphs = Path.Combine(graphOutDir, "master_output.json");
		JsonExporter.ExportFromRecords(records, jsonForGraphs);
		var rc = PlotAllFromJson(jsonForGraphs, graphOutDir);
		return rc;
	}

	private static string SanitizeFileName(string name)
	{
		foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
		return name.Replace(' ', '_');
	}

	private static int PlotAllFromJson(string jsonPath, string outputDir)
	{
		if (!File.Exists(jsonPath))
		{
			Console.Error.WriteLine($"JSON input not found: {jsonPath}");
			return 2;
		}

		var text = File.ReadAllText(jsonPath);
		using var doc = JsonDocument.Parse(text);
		if (doc.RootElement.ValueKind != JsonValueKind.Array)
		{
			Console.Error.WriteLine("JSON root is not an array");
			return 2;
		}

		// group elements by ConcentrationName
		var byConcentration = new Dictionary<string, List<JsonElement>>();
		foreach (var el in doc.RootElement.EnumerateArray())
		{
			if (!el.TryGetProperty("ConcentrationName", out var concProp)) continue;
			if (concProp.ValueKind != JsonValueKind.String) continue;
			var name = concProp.GetString() ?? "";
			if (!byConcentration.ContainsKey(name)) byConcentration[name] = new List<JsonElement>();
			byConcentration[name].Add(el);
		}

		if (!byConcentration.Any())
		{
			Console.Error.WriteLine("No concentration groups found in JSON");
			return 2;
		}

		foreach (var kv in byConcentration)
		{
			var conc = kv.Key;
			// build MmHg groups
			var groups = new Dictionary<double, List<(double kpa, double psi)>>();
			foreach (var el in kv.Value)
			{
				double? mmhg = null;
				if (el.TryGetProperty("MmHg", out var mmhgProp) && mmhgProp.ValueKind == JsonValueKind.Number) mmhg = mmhgProp.GetDouble();
				if (!mmhg.HasValue) continue;

				double? kpa = null;
				if (el.TryGetProperty("kPa", out var kpaProp) && kpaProp.ValueKind == JsonValueKind.Number) kpa = kpaProp.GetDouble();

				double? psiNorm = null;
				if (el.TryGetProperty("PsiNormalizedPixelIntensity", out var psiProp) && psiProp.ValueKind == JsonValueKind.Number) psiNorm = psiProp.GetDouble();

				if (!kpa.HasValue || !psiNorm.HasValue) continue;

				if (!groups.ContainsKey(mmhg.Value)) groups[mmhg.Value] = new List<(double, double)>();
				groups[mmhg.Value].Add((kpa.Value, psiNorm.Value));
			}

			if (!groups.Any())
			{
				Console.WriteLine($"No data for concentration {conc}, skipping plot.");
				continue;
			}

			var plt = new ScottPlot.Plot(1200, 800);
			var colors = new System.Drawing.Color[] {
				System.Drawing.Color.Blue,
				System.Drawing.Color.Red,
				System.Drawing.Color.Green,
				System.Drawing.Color.Orange,
				System.Drawing.Color.Purple,
				System.Drawing.Color.Brown,
				System.Drawing.Color.Cyan,
				System.Drawing.Color.Magenta,
				System.Drawing.Color.Yellow,
				System.Drawing.Color.Black
			};
			int ci = 0;
			foreach (var g in groups.OrderBy(g => g.Key))
			{
				var pts = g.Value.OrderBy(p => p.kpa).ToArray();
				var xs = pts.Select(p => p.kpa).ToArray();
				var ys = pts.Select(p => p.psi).ToArray();
				plt.AddScatter(xs, ys, color: colors[ci % colors.Length], markerSize:6, label: $"MmHg={g.Key}");
				ci++;
			}

			plt.Legend();
			plt.Title($"Concentration {conc}");
			plt.XLabel("kPa");
			plt.YLabel("PsiNormalizedPixelIntensity");

			var outPng = Path.Combine(outputDir, SanitizeFileName(conc) + ".png");
			plt.SaveFig(outPng);
			Console.WriteLine($"Wrote plot to {outPng}");
		}

		return 0;
	}

	private static int Plot4e3FromJson(string jsonPath, string outputPng)
	{
		if (!File.Exists(jsonPath))
		{
			Console.Error.WriteLine($"JSON input not found: {jsonPath}");
			return 2;
		}

		var text = File.ReadAllText(jsonPath);
		using var doc = JsonDocument.Parse(text);
		if (doc.RootElement.ValueKind != JsonValueKind.Array)
		{
			Console.Error.WriteLine("JSON root is not an array");
			return 2;
		}

		var groups = new Dictionary<double, List<(double kpa, double psi)>>();

		foreach (var el in doc.RootElement.EnumerateArray())
		{
			if (!el.TryGetProperty("ConcentrationName", out var concProp)) continue;
			if (concProp.ValueKind != JsonValueKind.String) continue;
			if (concProp.GetString() != "4e3") continue;

			double? mmhg = null;
			if (el.TryGetProperty("MmHg", out var mmhgProp) && mmhgProp.ValueKind == JsonValueKind.Number)
				mmhg = mmhgProp.GetDouble();

			if (!mmhg.HasValue) continue;

			double? kpa = null;
			if (el.TryGetProperty("kPa", out var kpaProp) && kpaProp.ValueKind == JsonValueKind.Number)
				kpa = kpaProp.GetDouble();

			double? psiNorm = null;
			if (el.TryGetProperty("PsiNormalizedPixelIntensity", out var psiProp) && psiProp.ValueKind == JsonValueKind.Number)
				psiNorm = psiProp.GetDouble();

			if (!kpa.HasValue || !psiNorm.HasValue) continue;

			if (!groups.ContainsKey(mmhg.Value)) groups[mmhg.Value] = new List<(double, double)>();
			groups[mmhg.Value].Add((kpa.Value, psiNorm.Value));
		}

		if (!groups.Any())
		{
			Console.Error.WriteLine("No data found for concentration 4e3");
			return 2;
		}

		var plt = new ScottPlot.Plot(1200, 800);
		var colors = new System.Drawing.Color[] {
			System.Drawing.Color.Blue,
			System.Drawing.Color.Red,
			System.Drawing.Color.Green,
			System.Drawing.Color.Orange,
			System.Drawing.Color.Purple,
			System.Drawing.Color.Brown,
			System.Drawing.Color.Cyan,
			System.Drawing.Color.Magenta,
			System.Drawing.Color.Yellow,
			System.Drawing.Color.Black
		};
		int ci = 0;
		foreach (var kv in groups.OrderBy(g => g.Key))
		{
			var pts = kv.Value.OrderBy(p => p.kpa).ToArray();
			var xs = pts.Select(p => p.kpa).ToArray();
			var ys = pts.Select(p => p.psi).ToArray();
			plt.AddScatter(xs, ys, color: colors[ci % colors.Length], markerSize:6, label: $"MmHg={kv.Key}");
			ci++;
		}

		plt.Legend();
		plt.Title("Concentration 4e3");
		plt.XLabel("kPa");
		plt.YLabel("PsiNormalizedPixelIntensity");

		var dir = Path.GetDirectoryName(outputPng) ?? string.Empty;
		if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
		plt.SaveFig(outputPng);
		Console.WriteLine($"Wrote plot to {outputPng}");
		return 0;
	}
}

// Additional CLI: export JSON summaries from an existing master CSV
public static class JsonExporter
{
	public static void ExportFromRecords(IEnumerable<FrameRecord> records, string jsonOut)
	{
		var lookup = AttenuationLookupTable.Table;

		// group by ExcelFilePath
		var groups = records.GroupBy(r => r.ExcelFilePath).ToList();

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

		// build metadata per group
		var groupMeta = new Dictionary<string, (string ExcelFilePath, string DICOMFilePath, string PatientName, string DICOMFileDate, double AmbientPressurePsi, double MmHg, int Attenuation, string Concentration, double Raw)>();
		foreach (var g in groups)
		{
			var file = g.Key;
			var fn = Path.GetFileNameWithoutExtension(file);

			var attenStr = new string(fn.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
			int attenuation = 0;
			if (attenStr.Length >= 2) int.TryParse(attenStr.Substring(attenStr.Length - 2), out attenuation);

			double ambientPsi = 0.0;
			var m = System.Text.RegularExpressions.Regex.Match(fn, "^(\\d+\\.\\d+");
			if (m.Success)
				double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ambientPsi);
			else
			{
				var m2 = System.Text.RegularExpressions.Regex.Match(fn, "^(\\D*?)(\\d{1,2})");
				if (m2.Success && int.TryParse(m2.Groups[2].Value, out var fd)) ambientPsi = fd / 10.0;
			}

			double mmhg = ambientPsi * 50.0;
			var parent = Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty) ?? string.Empty;
			var raw = stats.ContainsKey(file) ? stats[file].mean : double.NaN;

			var first = g.First();
			var dicomPath = first.DICOMFilePath ?? string.Empty;
			var patientName = first.PatientName ?? string.Empty;
			var dicomDate = first.DICOMFileDate ?? string.Empty;

			groupMeta[file] = (file, dicomPath, patientName, dicomDate, ambientPsi, mmhg, attenuation, parent, raw);
		}

		foreach (var g in groups)
		{
			var file = g.Key;
			var meta = groupMeta[file];
			var frameValues = g.Select(x => x.Value_dB).ToList();
			var raw = stats[file].mean;
			var std = stats[file].std;

			lookup.TryGetValue(meta.Attenuation, out var lookupEntry);

			double decibelAdj = lookupEntry?.DecibelAdjustmentValue ?? 0.0;
			double kpa = lookupEntry?.Kpa ?? double.NaN;

			double psiNormalized = double.IsNaN(raw) ? double.NaN : raw + decibelAdj;

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
				["DICOMFilePath"] = meta.DICOMFilePath,
				["PatientName"] = meta.PatientName,
				["DICOMFileDate"] = meta.DICOMFileDate,
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

		var json = System.Text.Json.JsonSerializer.Serialize(summaries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(jsonOut, json);
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
