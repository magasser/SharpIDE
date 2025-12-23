using Godot;

namespace SharpIDE.Godot.Features.Layout;

public sealed record ToolDropData(IdeTool Tool, ToolAnchor Anchor, int Index);

public partial class ToolDragOverlay : Control
{
	private readonly Dictionary<DropZone, ToolAnchor> _dropZoneMap = [];
	private readonly Dictionary<ToolAnchor, Sidebar> _sidebarMap = [];

	[Export]
	public Sidebar LeftSidebar { get; set; } = null!;

	[Export]
	public Sidebar RightSidebar { get; set; } = null!;

	[Export]
	public DropZone LeftTopZone { get; set; } = null!;

	[Export]
	public DropZone RightTopZone { get; set; } = null!;

	[Export]
	public DropZone BottomLeftZone { get; set; } = null!;

	[Export]
	public DropZone BottomRightZone { get; set; } = null!;

	public event EventHandler<ToolDropData>? ToolDropped;

	public override void _Ready()
	{
		// TODO: Sidebar visibility not working
		_dropZoneMap[LeftTopZone] = ToolAnchor.LeftTop;
		_dropZoneMap[RightTopZone] = ToolAnchor.RightTop;
		_dropZoneMap[BottomLeftZone] = ToolAnchor.BottomLeft;
		_dropZoneMap[BottomRightZone] = ToolAnchor.BottomRight;

		_sidebarMap[ToolAnchor.LeftTop] = LeftSidebar;
		_sidebarMap[ToolAnchor.RightTop] = RightSidebar;
		_sidebarMap[ToolAnchor.BottomLeft] = LeftSidebar;
		_sidebarMap[ToolAnchor.BottomRight] = RightSidebar;

		VisibilityChanged += OnVisibilityChanged;
	}

	/// <inheritdoc />
	public override bool _CanDropData(Vector2 _, Variant data)
	{
		HideToolPreviews();
		HideDropZoneHighlights();

		var mousePosition = GetGlobalMousePosition();

		foreach (var (zone, anchor) in _dropZoneMap)
		{
			if (InDropZone(zone, mousePosition))
			{
				ShowGhostPreview(anchor, mousePosition);
				zone.Highlight.Show();
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc />
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var mousePosition = GetGlobalMousePosition();

		foreach (var (zone, anchor) in _dropZoneMap)
		{
			if (InDropZone(zone, mousePosition))
			{
				ToolDropped?.Invoke(
					sender: this,
					new ToolDropData(
						data.As<IdeTool>(),
						anchor,
						CalculateInsertionIndex(GetSidebarTools(anchor), mousePosition)));
				return;
			}
		}
	}

	private void OnVisibilityChanged()
	{
		if (!Visible)
		{
			HideToolPreviews();
			HideDropZoneHighlights();
		}
	}

	private void RaiseToolDropped(IdeTool tool, ToolAnchor anchor) { }

	private void ShowGhostPreview(ToolAnchor anchor, Vector2 mousePosition)
	{
		var sidebar = _sidebarMap[anchor];
		var tools = GetSidebarTools(anchor);
		var previewIndex = CalculateInsertionIndex(tools, mousePosition);

		tools.AddChild(sidebar.ToolPreview);
		tools.MoveChild(sidebar.ToolPreview, previewIndex);
	}

	private Container GetSidebarTools(ToolAnchor anchor)
	{
		var sidebar = _sidebarMap[anchor];

		return anchor switch
		{
			ToolAnchor.LeftTop or ToolAnchor.RightTop => sidebar.TopTools,
			ToolAnchor.BottomLeft or ToolAnchor.BottomRight => sidebar.BottomTools,
			_ => throw new ArgumentException($"No tools to show preview in for anchor '{anchor}'.", nameof(anchor))
		};
	}

	private void HideToolPreviews()
	{
		if (LeftSidebar.ToolPreview.GetParent() is { } leftParent)
		{
			leftParent.RemoveChild(LeftSidebar.ToolPreview);
		}

		if (RightSidebar.ToolPreview.GetParent() is { } rightParent)
		{
			rightParent.RemoveChild(RightSidebar.ToolPreview);
		}
	}

	private void HideDropZoneHighlights()
	{
		foreach (var zone in _dropZoneMap.Keys)
		{
			zone.Highlight.Hide();
		}
	}

	private static int CalculateInsertionIndex(Container tools, Vector2 mousePosition)
	{
		foreach (var child in tools.GetChildren().OfType<ToolButton>())
		{
			var rect = child.GetGlobalRect();

			var midpoint = rect.Position.Y + rect.Size.Y / 2.0f;

			if (mousePosition.Y < midpoint)
			{
				return child.GetIndex();
			}
		}

		return tools.GetChildCount();
	}

	private static bool InDropZone(Control dropZone, Vector2 position)
	{
		return dropZone.GetGlobalRect().HasPoint(position);
	}
}
