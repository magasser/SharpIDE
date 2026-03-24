using Godot;
using System;

using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot;
using SharpIDE.Godot.Features.CodeEditor;

public sealed partial class SharpIdeCodeEditTab : Control
{
	public SharpIdeSolutionModel Solution { get; set; } = null!;
	public SharpIdeFile File { get; set; } = null!;

	private static readonly PackedScene SharpIdeCodeEditScene =
		ResourceLoader.Load<PackedScene>("res://Features/CodeEditor/SharpIdeCodeEdit.tscn");

	private SharpIdeCodeEditContainer? _container;

	public SharpIdeCodeEdit CodeEdit
	{
		get
		{
			if (_container is not null) return _container.CodeEdit;

			_container = SharpIdeCodeEditScene.Instantiate<SharpIdeCodeEditContainer>();
			_container.CodeEdit.Solution = Solution;
			Dispatcher.SynchronizationContext.Send(
				static void (state) =>
				{
					var (tab, container) = ((SharpIdeCodeEditTab, SharpIdeCodeEditContainer))state!;
					tab.AddChild(container);
				}, (Tab: this, Container: _container));
			
			_ = _container.CodeEdit.SetSharpIdeFile(File, fileLinePosition: null);

			return _container.CodeEdit;
		}
	}
}
