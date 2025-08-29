using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace SharpIDE.Application.Features.Debugging;

public class ThreadsStackTraceModel
{
	public List<ThreadModel> Threads { get; set; } = [];
}

public class ThreadModel
{
	public required int Id { get; set; }
	public required string Name { get; set; }
	public List<StackFrameModel> StackFrames { get; set; } = [];
}

public class StackFrameModel
{
	public required string ClassName { get; set; }
	public required string MethodName { get; set; }
	public required string Namespace { get; set; }
	public required string AssemblyName { get; set; }

	public required int Id { get; set; }
	public required string Name { get; set; }
	public required int? Line { get; set; }
	public required int? Column { get; set; }
	public required string? Source { get; set; }
	public List<ScopeModel> Scopes { get; set; } = [];
}

public class ScopeModel
{
	public required string Name { get; set; }
	public List<Variable> Variables { get; set; } = [];
}
