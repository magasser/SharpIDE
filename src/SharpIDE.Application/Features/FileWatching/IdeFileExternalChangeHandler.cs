using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.Evaluation;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.FileWatching;

public class IdeFileExternalChangeHandler
{
	public SharpIdeSolutionModel SolutionModel { get; set; } = null!;
	public IdeFileExternalChangeHandler()
	{
		GlobalEvents.Instance.FileSystemWatcherInternal.FileChanged.Subscribe(OnFileChanged);
	}

	private async Task OnFileChanged(string filePath)
	{
		var sharpIdeFile = SolutionModel.AllFiles.SingleOrDefault(f => f.Path == filePath);
		if (sharpIdeFile is null) return;
		if (sharpIdeFile.SuppressDiskChangeEvents is true) return;
		if (sharpIdeFile.LastIdeWriteTime is not null)
		{
			var now = DateTimeOffset.Now;
			if (now - sharpIdeFile.LastIdeWriteTime.Value < TimeSpan.FromMilliseconds(300))
			{
				Console.WriteLine($"IdeFileExternalChangeHandler: Ignored - {filePath}");
				return;
			}
		}
		Console.WriteLine($"IdeFileExternalChangeHandler: Changed - {filePath}");
		var file = SolutionModel.AllFiles.SingleOrDefault(f => f.Path == filePath);
		if (file is not null)
		{
			await GlobalEvents.Instance.IdeFileChanged.InvokeParallelAsync(file);
		}
	}
}
