using Godot;

namespace SharpIDE.Godot.Features.Layout;

public partial class ToolArea : Control
{
	public IdeToolInfo? ActiveTool { get; private set; }

	public override void _Ready()
	{
		Visible = false;
	}

	public void SetActiveTool(IdeToolInfo? toolInfo)
	{
		if (ReferenceEquals(ActiveTool, toolInfo))
		{
			return;
		}
		
		if (ActiveTool is not null)
		{
			RemoveChild(ActiveTool.Control);
			ActiveTool.IsVisible = false;
		}
		
		if (toolInfo is null)
		{
			Visible = false;
			ActiveTool = null;
			return;
		}

		if (toolInfo.Control.GetParent() is { } parent)
		{
			parent.RemoveChild(toolInfo.Control);
		}
		
		ActiveTool = toolInfo;
		
		AddChild(ActiveTool.Control);
		Visible = ActiveTool.IsVisible = true;
	}
}
