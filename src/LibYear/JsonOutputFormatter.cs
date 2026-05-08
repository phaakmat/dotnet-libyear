using System.Text.Json;
using System.Text.Json.Serialization;
using LibYear.Core;

namespace LibYear;

public class JsonOutputFormatter : IOutputFormatter
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public void Write(SolutionResult result)
	{
		var output = new
		{
			projects = result.Details.Select(p => new
			{
				name = p.ProjectFile.FileName,
				yearsBehind = p.YearsBehind,
				packages = p.Details.Select(r => new
				{
					name = r.Name,
					installed = r.Installed == null ? null : new
					{
						version = r.Installed.Version.ToString(),
						released = r.Installed.Date?.ToString("yyyy-MM-dd")
					},
					latest = r.Latest == null ? null : new
					{
						version = r.Latest.Version.ToString(),
						released = r.Latest.Date?.ToString("yyyy-MM-dd")
					},
					yearsBehind = r.YearsBehind
				})
			}),
			totalYearsBehind = result.YearsBehind
		};

		Console.WriteLine(JsonSerializer.Serialize(output, SerializerOptions));
	}
}
