using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using R3;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.SolutionDiscovery;

public class SharpIdeFile : ISharpIdeNode, IChildSharpIdeNode
{
	public required IExpandableSharpIdeNode Parent { get; set; }
	public required string Path { get; set; }
	public required string Name { get; set; }
	public bool IsRazorFile => Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
	public bool IsCshtmlFile => Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
	public bool IsCsharpFile => Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
	public bool IsRoslynWorkspaceFile => IsCsharpFile || IsRazorFile || IsCshtmlFile;
	public required ReactiveProperty<bool> IsDirty { get; set; }

	[SetsRequiredMembers]
	internal SharpIdeFile(string fullPath, string name, IExpandableSharpIdeNode parent, ConcurrentBag<SharpIdeFile> allFiles)
	{
		Path = fullPath;
		Name = name;
		Parent = parent;
		IsDirty = new ReactiveProperty<bool>(false);
		allFiles.Add(this);
	}
}
