using System.Text.Json;
using System.Text.Json.Serialization;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Run;

public static class LaunchSettingsParser
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip
	};

	public static async Task<List<ProjectLaunchSettingsModel>> GetLaunchSettingsProfiles(SharpIdeProjectModel projectModel)
	{
		var launchSettingsFilePath = Path.Combine(Path.GetDirectoryName(projectModel.FilePath)!, "Properties", "launchSettings.json");
		var launchSettingsFile = new FileInfo(launchSettingsFilePath);
		if (launchSettingsFile.Exists is false)
		{
			return [];
		}

		await using var stream = launchSettingsFile.OpenRead();
		var launchSettings = await JsonSerializer.DeserializeAsync<LaunchSettings>(stream, _jsonSerializerOptions);
		if (launchSettings is null) return [];

		var result = launchSettings.Profiles.Select(s => new ProjectLaunchSettingsModel
		{
			LaunchProfileName = s.Key,
			CommandLineArgs = s.Value.CommandName,
			DotNetRunMessages = s.Value.DotnetRunMessages,
			LaunchBrowser = s.Value.LaunchBrowser,
			LaunchUrl = s.Value.LaunchUrl,
			ApplicationUrl = s.Value.ApplicationUrl,
			EnvironmentVariables = s.Value.EnvironmentVariables
		}).ToList();
		return result;
	}
}

public class ProjectLaunchSettingsModel
{
	public required string? LaunchProfileName { get; set; }
	public required string? CommandLineArgs { get; set; }
	public required bool LaunchBrowser { get; set; }
	public required string? LaunchUrl { get; set; }
	public required string? ApplicationUrl { get; set; }
	public required bool DotNetRunMessages { get; set; }
	public required Dictionary<string, string> EnvironmentVariables { get; init; }
}

// Json models
public class LaunchSettings
{
	public required Dictionary<string, Profile> Profiles { get; set; }
}

public class Profile
{
	public string CommandName { get; set; }
	public bool DotnetRunMessages { get; set; }
	public bool LaunchBrowser { get; set; }
	public string LaunchUrl { get; set; }
	public string ApplicationUrl { get; set; }
	public Dictionary<string, string> EnvironmentVariables { get; set; }
}

