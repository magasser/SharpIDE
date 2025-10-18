using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.Evaluation;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.FilePersistence;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.FileWatching;

public class IdeFileSavedToDiskHandler
{
	private readonly IdeOpenTabsFileManager _openTabsFileManager;
	private readonly RoslynAnalysis _roslynAnalysis;
	public SharpIdeSolutionModel SolutionModel { get; set; } = null!;

	public IdeFileSavedToDiskHandler(IdeOpenTabsFileManager openTabsFileManager, RoslynAnalysis roslynAnalysis)
	{
		_openTabsFileManager = openTabsFileManager;
		_roslynAnalysis = roslynAnalysis;
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
		await _roslynAnalysis.ReloadProject(project);
		await _roslynAnalysis.UpdateSolutionDiagnostics();
	}

	private async Task HandleWorkspaceFileChanged(SharpIdeFile file)
	{
		// TODO: Don't reload from disk if we raised the change event ourselves (e.g. save from IDE). Cleanup this whole disaster
		var wasOpenAndUpdated = await _openTabsFileManager.ReloadFileFromDiskIfOpenInEditor(file);
		if (file.IsRoslynWorkspaceFile)
		{
			var fileText = wasOpenAndUpdated ?
				await _openTabsFileManager.GetFileTextAsync(file) :
				await File.ReadAllTextAsync(file.Path);
			await _roslynAnalysis.UpdateDocument(file, fileText);
			GlobalEvents.Instance.SolutionAltered.InvokeParallelFireAndForget();
		}
		await _roslynAnalysis.UpdateSolutionDiagnostics();
	}
}
