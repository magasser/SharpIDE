using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.Evaluation;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.FileWatching;

public class IdeFileSavedToDiskHandler
{
	public SharpIdeSolutionModel SolutionModel { get; set; } = null!;

	public IdeFileSavedToDiskHandler()
	{
		GlobalEvents.Instance.IdeFileSavedToDisk.Subscribe(HandleIdeFileChanged);
	}

	private async Task HandleIdeFileChanged(SharpIdeFile file)
	{
		if (file.IsCsprojFile)
		{
			await HandleCsprojChanged(file);
		}
		else if (file.IsRoslynWorkspaceFile)
		{
			await HandleWorkspaceFileChanged(file);
		}
	}

	private async Task HandleCsprojChanged(SharpIdeFile file)
	{
		var project = SolutionModel.AllProjects.SingleOrDefault(p => p.FilePath == file.Path);
		if (project is null) return;
		await ProjectEvaluation.ReloadProject(file.Path);
		await RoslynAnalysis.ReloadProject(project);
		await RoslynAnalysis.UpdateSolutionDiagnostics();
	}

	private async Task HandleWorkspaceFileChanged(SharpIdeFile file)
	{
		await RoslynAnalysis.UpdateSolutionDiagnostics();
	}
}
