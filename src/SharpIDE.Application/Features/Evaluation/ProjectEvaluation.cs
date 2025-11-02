using Ardalis.GuardClauses;
using Microsoft.Build.Evaluation;
using NuGet.ProjectModel;
using NuGet.Versioning;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Evaluation;

public static class ProjectEvaluation
{
	private static readonly ProjectCollection _projectCollection = ProjectCollection.GlobalProjectCollection;
	public static async Task<Project> GetProject(string projectFilePath)
	{
		using var _ = SharpIdeOtel.Source.StartActivity($"{nameof(ProjectEvaluation)}.{nameof(GetProject)}");
		Guard.Against.Null(projectFilePath, nameof(projectFilePath));

		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		var project = _projectCollection.LoadProject(projectFilePath);
		//Console.WriteLine($"ProjectEvaluation: loaded {project.FullPath}");
		return project;
	}

	public static async Task ReloadProject(string projectFilePath)
	{
		using var _ = SharpIdeOtel.Source.StartActivity($"{nameof(ProjectEvaluation)}.{nameof(ReloadProject)}");
		Guard.Against.Null(projectFilePath, nameof(projectFilePath));

		var project = _projectCollection.GetLoadedProjects(projectFilePath).Single();
		var projectRootElement = project.Xml;
		projectRootElement.Reload(false);
		project.ReevaluateIfNecessary();
	}

	public static string? GetOutputDllFullPath(SharpIdeProjectModel projectModel)
	{
		var project = _projectCollection.GetLoadedProjects(projectModel.FilePath).Single();
		var targetPath = project.GetPropertyValue("TargetPath");
		Guard.Against.NullOrWhiteSpace(targetPath, nameof(targetPath));
		return targetPath;
	}

	public static Guid GetOrCreateDotnetUserSecretsId(SharpIdeProjectModel projectModel)
	{
		Guard.Against.Null(projectModel, nameof(projectModel));

		var project = _projectCollection.GetLoadedProjects(projectModel.FilePath).Single();
		var projectRootElement = project.Xml;
		var userSecretsId = project.GetPropertyValue("UserSecretsId");
		if (string.IsNullOrWhiteSpace(userSecretsId))
		{
			var newGuid = Guid.NewGuid();
			var property = projectRootElement.AddProperty("UserSecretsId", newGuid.ToString());
			project.Save();
			return newGuid;
		}
		return Guid.Parse(userSecretsId);
	}

	public static async Task<List<InstalledPackage>> GetPackageReferencesForProject(SharpIdeProjectModel projectModel, bool includeTransitive = true)
	{
		using var _ = SharpIdeOtel.Source.StartActivity($"{nameof(ProjectEvaluation)}.{nameof(GetPackageReferencesForProject)}");
		Guard.Against.Null(projectModel, nameof(projectModel));

		var project = _projectCollection.GetLoadedProjects(projectModel.FilePath).Single();

		var assetsPath = project.GetPropertyValue("ProjectAssetsFile");

		if (File.Exists(assetsPath) is false)
		{
			throw new FileNotFoundException("Could not find project.assets.json file", assetsPath);
		}
		var lockFileFormat = new LockFileFormat();
		var lockFile = lockFileFormat.Read(assetsPath);
		var packages = GetPackagesFromAssetsFile(lockFile, includeTransitive);
		return packages;
	}

	// ChatGPT Special
	private static List<InstalledPackage> GetPackagesFromAssetsFile(LockFile assetsFile, bool includeTransitive)
	{
		var packages = new List<InstalledPackage>();
		var dependencyMap = NugetDependencyGraph.GetPackageDependencyMap(assetsFile);

		foreach (var target in assetsFile.Targets.Where(t => t.RuntimeIdentifier == null))
		{
			var tfm = target.TargetFramework.GetShortFolderName();
			var tfmInfo = assetsFile.PackageSpec.TargetFrameworks
				.FirstOrDefault(t => t.FrameworkName.Equals(target.TargetFramework));

			if (tfmInfo == null) continue;

			var topLevelDependencies = tfmInfo.Dependencies
				.DistinctBy(s => s.Name)
				.Select(s => s.Name)
				.ToHashSet();

			foreach (var lockFileTargetLibrary in target.Libraries.Where(l => l.Type == "package"))
			{
				var isTopLevel = topLevelDependencies.Contains(lockFileTargetLibrary.Name);
				if (!includeTransitive && !isTopLevel) continue;

				var dependency = tfmInfo.Dependencies
					.FirstOrDefault(d => d.Name.Equals(lockFileTargetLibrary.Name, StringComparison.OrdinalIgnoreCase));

				var dependents = dependencyMap.GetValueOrDefault(lockFileTargetLibrary.Name, []);
				var mappedDependents = dependents.Select(d => new DependentPackage
				{
					PackageName = d.PackageName,
					RequestedVersion = d.PackageDependency.VersionRange
				}).ToList();

				packages.Add(new InstalledPackage
				{
					Name = lockFileTargetLibrary.Name,
					RequestedVersion = dependency?.LibraryRange.VersionRange?.ToString() ?? "",
					ResolvedVersion = lockFileTargetLibrary.Version?.ToString(),
					TargetFramework = tfm,
					IsTopLevel = isTopLevel,
					IsAutoReferenced = dependency?.AutoReferenced ?? false,
					DependentPackages = mappedDependents
				});
			}
		}

		return packages;
	}

public class InstalledPackage
{
	public required string Name { get; set; }
	public required string RequestedVersion { get; set; }
	public required string? ResolvedVersion { get; set; }
	public required string TargetFramework { get; set; }
	public required bool IsTopLevel { get; set; }
	public required bool IsAutoReferenced { get; set; }
	public List<DependentPackage>? DependentPackages { get; set; }
}
public class DependentPackage
{
	public required string PackageName { get; set; }
	public required VersionRange RequestedVersion { get; set; }
}
