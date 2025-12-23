using Godot;

namespace SharpIDE.Godot.Features.Layout;

public partial class ToolButton : Button
{
	public IdeTool Tool { get; set; }
	
	[Export]
	public Color DragPreviewColor { get; set; }

	private bool _pressedBeforeDrag;
	

	/// <inheritdoc />
	public override Variant _GetDragData(Vector2 atPosition)
	{
		SetDragPreview(CreateDragPreview());

		// We save the state and unpress the button without toggle signal,
		// otherwise Hide() will unpress the button which toggles it.
		_pressedBeforeDrag = ButtonPressed;
		SetPressedNoSignal(false);
		Hide();
		
		return Variant.From(Tool);
	}

	/// <inheritdoc />
	public override void _Notification(int what)
	{
		switch ((long) what)
		{
			case NotificationDragEnd:
				Show();
				SetPressedNoSignal(_pressedBeforeDrag);
				break;
		}
	}

	private Control CreateDragPreview()
	{
		var rect = new ColorRect();
		rect.Size = Size;
		rect.Color = DragPreviewColor;
		
		var icon = new TextureRect
		{
			Texture = Icon,
			ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
		};
		
		icon.SetAnchorsPreset(LayoutPreset.FullRect);
		
		rect.AddChild(icon);
		return rect;
	}
}
