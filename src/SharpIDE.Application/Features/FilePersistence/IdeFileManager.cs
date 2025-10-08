using System.Collections.Concurrent;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.SolutionDiscovery;

namespace SharpIDE.Application.Features.FilePersistence;
#pragma warning disable VSTHRD011

/// Holds the in memory copies of files, and manages saving/loading them to/from disk.
public class IdeFileManager
{
	private ConcurrentDictionary<SharpIdeFile, Lazy<Task<string>>> _openFiles = new();

	/// Implicitly 'opens' a file if not already open, and returns the text.
	public async Task<string> GetFileTextAsync(SharpIdeFile file)
	{
		var textTaskLazy = _openFiles.GetOrAdd(file, f =>
		{
			var lazy = new Lazy<Task<string>>(Task<string> () => File.ReadAllTextAsync(f.Path));
			return lazy;
		});
		var textTask = textTaskLazy.Value;
		var text = await textTask;
		return text;
	}

	// Calling this assumes that the file is already open - may need to be revisited for code fixes and refactorings. I think all files involved in a multi-file fix/refactor shall just be saved to disk immediately.
	public void UpdateFileTextInMemory(SharpIdeFile file, string newText)
	{
		if (!_openFiles.ContainsKey(file)) throw new InvalidOperationException("File is not open in memory.");

		var newLazyTask = new Lazy<Task<string>>(() => Task.FromResult(newText));
		_openFiles[file] = newLazyTask;
		// Potentially should be event based?
		if (file.IsRoslynWorkspaceFile)
		{
			RoslynAnalysis.UpdateDocument(file, newText);
		}
	}

	public async Task SaveFileAsync(SharpIdeFile file)
	{
		if (!_openFiles.ContainsKey(file)) throw new InvalidOperationException("File is not open in memory.");

		var text = await GetFileTextAsync(file);
		await File.WriteAllTextAsync(file.Path, text);
		file.IsDirty.Value = false;
	}

	public async Task UpdateInMemoryIfOpenAndSaveAsync(SharpIdeFile file, string newText)
	{
		if (_openFiles.ContainsKey(file))
		{
			UpdateFileTextInMemory(file, newText);
			await SaveFileAsync(file);
		}
		else
		{
			await File.WriteAllTextAsync(file.Path, newText);
		}
	}

	public async Task SaveAllOpenFilesAsync()
	{
		foreach (var file in _openFiles.Keys)
		{
			await SaveFileAsync(file);
		}
	}
}

#pragma warning restore VSTHRD011
