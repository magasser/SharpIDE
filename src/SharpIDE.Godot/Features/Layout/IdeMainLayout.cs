using Godot;

using SharpIDE.Application.Features.Events;

namespace SharpIDE.Godot.Features.Layout;

public partial class IdeMainLayout : Control
{
	private readonly Dictionary<ToolAnchor, ToolArea> _toolAreaMap = [];
	private readonly Dictionary<ToolAnchor, Control> _sidebarToolsMap = [];
	private readonly Dictionary<ToolAnchor, ButtonGroup> _toolButtonGroupMap = [];
	private readonly Dictionary<IdeTool, ToolButton> _toolButtonMap = [];

	private Dictionary<IdeTool, IdeToolInfo> _toolMap = [];

	private Sidebar _leftSidebar = null!;
	private Sidebar _rightSidebar = null!;
	private Control _bottomArea = null!;
	private ToolDragOverlay _toolDragOverlay = null!;

	public override void _Ready()
	{
		_leftSidebar = GetNode<Sidebar>("%LeftSidebar");
		_rightSidebar = GetNode<Sidebar>("%RightSidebar");
		_bottomArea = GetNode<Control>("%BottomArea");
		_toolDragOverlay = GetNode<ToolDragOverlay>("%ToolDragOverlay");
		
		_toolAreaMap[ToolAnchor.LeftTop] = GetNode<ToolArea>("%LeftTopToolArea");
		_toolAreaMap[ToolAnchor.RightTop] = GetNode<ToolArea>("%RightTopToolArea");
		_toolAreaMap[ToolAnchor.BottomLeft] = GetNode<ToolArea>("%BottomLeftToolArea");
		_toolAreaMap[ToolAnchor.BottomRight] = GetNode<ToolArea>("%BottomRightToolArea");

		_sidebarToolsMap[ToolAnchor.LeftTop] = _leftSidebar.TopTools;
		_sidebarToolsMap[ToolAnchor.RightTop] = _rightSidebar.TopTools;
		_sidebarToolsMap[ToolAnchor.BottomLeft] = _leftSidebar.BottomTools;
		_sidebarToolsMap[ToolAnchor.BottomRight] = _rightSidebar.BottomTools;

		_toolButtonGroupMap[ToolAnchor.LeftTop] = new ButtonGroup
			{ ResourceName = $"{ToolAnchor.LeftTop}", AllowUnpress = true };
		_toolButtonGroupMap[ToolAnchor.RightTop] = new ButtonGroup
			{ ResourceName = $"{ToolAnchor.RightTop}", AllowUnpress = true };
		_toolButtonGroupMap[ToolAnchor.BottomLeft] = new ButtonGroup
			{ ResourceName = $"{ToolAnchor.BottomLeft}", AllowUnpress = true };
		_toolButtonGroupMap[ToolAnchor.BottomRight] = new ButtonGroup
			{ ResourceName = $"{ToolAnchor.BottomRight}", AllowUnpress = true };

		GodotGlobalEvents.Instance.IdeToolExternallySelected.Subscribe(tool =>
		{
			CallDeferred(
				nameof(OnIdeToolExternallySelected),
				Variant.From(tool));
			return Task.CompletedTask;
		});

		_toolDragOverlay.ToolDropped += OnToolDropped;
	}

	private void OnIdeToolExternallySelected(IdeTool tool)
	{
		var anchor = _toolMap[tool].Anchor;
		
		foreach (var toolInfo in _toolMap.Values.Where(t => t.Anchor == anchor))
		{
			_toolButtonMap[toolInfo.Tool].SetPressedNoSignal(toolInfo.Tool == tool);
			toolInfo.IsVisible = toolInfo.Tool == tool;
		}
		
		ToggleTool(tool, toggledOn: true);
	}

	/// <inheritdoc />
	public override void _Notification(int what)
	{
		switch ((long)what)
		{
			case NotificationDragBegin:
				_leftSidebar.Visible = true;
				_rightSidebar.Visible = true;
				_toolDragOverlay.Visible = true;
				break;
			case NotificationDragEnd:
				_toolDragOverlay.Visible = false;
				UpdateSidebarVisibility();
				break;
		}
	}

	private void OnToolDropped(object? _, ToolDropData dropData)
	{
		var toolInfo = _toolMap[dropData.Tool];
		var toolButton = _toolButtonMap[dropData.Tool];
		var tools = _sidebarToolsMap[dropData.Anchor];
		var buttonGroup = _toolButtonGroupMap[dropData.Anchor];
		var oldToolArea = _toolAreaMap[toolInfo.Anchor];
		var newToolArea = _toolAreaMap[dropData.Anchor];

		if (toolButton.GetParent() is { } buttonParent)
		{
			buttonParent.RemoveChild(toolButton);
		}

		if (oldToolArea.ActiveTool?.Tool == dropData.Tool)
		{
			oldToolArea.SetActiveTool(null);
		}

		toolInfo.Anchor = dropData.Anchor;

		toolButton.ButtonGroup = buttonGroup;
		tools.AddChild(toolButton);
		tools.MoveChild(toolButton, dropData.Index);

		if (toolButton.ButtonPressed)
		{
			OnIdeToolExternallySelected(toolInfo.Tool);
		}
	}

	public void InitializeLayout(IReadOnlyList<IdeToolInfo> toolInfos)
	{
		_toolMap = toolInfos.ToDictionary(toolInfo => toolInfo.Tool);
		
		foreach (var sidebarToolGroup in _sidebarToolsMap.Values)
		{
			sidebarToolGroup.RemoveChildren();
		}
		
		foreach (var toolInfo in toolInfos)
		{
			var sidebarToolGroup = _sidebarToolsMap[toolInfo.Anchor];

			var button = CreateToolButton(toolInfo);
			_toolButtonMap[toolInfo.Tool] = button;
			
			sidebarToolGroup.AddChild(button);

			if (toolInfo is { IsPinned: true, IsVisible: true })
			{
				_toolAreaMap[toolInfo.Anchor].SetActiveTool(toolInfo);
			}
		}
		
		UpdateSidebarVisibility();
		UpdateBottomAreaVisibility();
	}

	private void UpdateSidebarVisibility()
	{
		_leftSidebar.Visible =
			_toolMap.Values.Any(toolInfo => toolInfo.Anchor.IsLeft() && toolInfo.IsPinned);

		_rightSidebar.Visible =
			_toolMap.Values.Any(toolInfo => toolInfo.Anchor.IsRight() && toolInfo.IsPinned);
	}

	private ToolButton CreateToolButton(IdeToolInfo toolInfo)
	{
		var toolButton = ResourceLoader.Load<PackedScene>("uid://gcpcsulb43in").Instantiate<ToolButton>();

		toolButton.Tool = toolInfo.Tool;
		toolButton.SetButtonIcon(toolInfo.Icon);
		toolButton.Toggled += toggledOn => ToggleTool(toolInfo.Tool,  toggledOn);
		toolButton.ButtonGroup = _toolButtonGroupMap[toolInfo.Anchor];
		toolButton.ButtonPressed = toolInfo is { IsPinned: true, IsVisible: true };

		return toolButton;
	}

	private void ToggleTool(IdeTool tool, bool toggledOn)
	{
		var toolInfo = _toolMap[tool];
		var toolArea = _toolAreaMap[toolInfo.Anchor];
		
		toolArea.SetActiveTool(toggledOn ? toolInfo : null);
		
		UpdateBottomAreaVisibility();
	}

	private void UpdateBottomAreaVisibility()
	{
		_bottomArea.Visible =
			_toolMap.Values.Any(toolInfo => toolInfo.Anchor.IsBottom()
											 && toolInfo is { IsPinned: true, IsVisible: true });
	}
}
