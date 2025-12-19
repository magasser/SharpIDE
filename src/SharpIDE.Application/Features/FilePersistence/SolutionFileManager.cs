using Ardalis.GuardClauses;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

using SharpIDE.Application.Features.FileWatching;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.FilePersistence;

public sealed class SolutionFileManager(FileChangedService fileChangedService, ILogger<SolutionFileManager> logger)
{
	private readonly FileChangedService _fileChangedService = fileChangedService;
	private readonly ILogger<SolutionFileManager> _logger = logger;

	/// <summary>
	///     Adds an existing project file to the solution.
	/// </summary>
	/// <param name="solution">The solution to add the project to.</param>
	/// <param name="project">The project to add to the solution.</param>
	/// <param name="solutionFolder">TODO</param>
	/// <param name="cancellationToken">A cancellation token to observe.</param>
	public async Task AddProject(SharpIdeSolutionModel solution, SharpIdeProjectModel project, SharpIdeSolutionFolder? solutionFolder = null, CancellationToken cancellationToken = default)
	{
		using var _ = SharpIdeOtel.Source.StartActivity();

		var vsSolution = await OpenSolutionAsync(solution, cancellationToken);

		var folder = solutionFolder is not null ? vsSolution.FindFolder(solutionFolder.SolutionPath) : null;

		vsSolution.AddProject(project.FilePath, folder: folder);

		_logger.LogInformation("Solution: Added project {Project} to solution {Solution}.", project.Name, solution.Name);
	}

	/// <summary>
	/// TODO
	/// </summary>
	/// <param name="solution"></param>
	/// <param name="project"></param>
	/// <param name="cancellationToken"></param>
	public async Task RemoveProject(
		SharpIdeSolutionModel solution,
		SharpIdeProjectModel project,
		CancellationToken cancellationToken = default)
	{
		using var _ = SharpIdeOtel.Source.StartActivity();

		var vsSolution = await OpenSolutionAsync(solution, cancellationToken);

		var vsProject = vsSolution.FindProject(project.FilePath);
		Guard.Against.Null(vsProject);

		vsSolution.RemoveProject(vsProject);

		_logger.LogInformation("Solution: Removed project {Project} from solution {Solution}.", project.Name, solution.Name);
	}

	private async Task<SolutionModel> OpenSolutionAsync(SharpIdeSolutionModel solution, CancellationToken cancellationToken = default)
	{
		var solutionSerializer = SolutionSerializers.GetSerializerByMoniker(solution.FilePath);
		Guard.Against.Null(solutionSerializer);

		var vsSolution = await solutionSerializer.OpenAsync(solution.FilePath, cancellationToken);

		return vsSolution;
	}
}
