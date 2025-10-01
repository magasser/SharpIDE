using Godot;
using SharpIDE.Godot.Features.IdeSettings;

namespace SharpIDE.Godot.Features.SlnPicker;

public partial class PreviousSlnEntry : HBoxContainer
{
    private Label _slnPathLabel = null!;
    private Label _slnNameLabel = null!;
    
    public PreviouslyOpenedSln PreviouslyOpenedSln { get; set; } = null!;

    public override void _Ready()
    {
        if (PreviouslyOpenedSln is null) return;
        _slnNameLabel = GetNode<Label>("%SlnNameLabel");
        _slnPathLabel = GetNode<Label>("%SlnPathLabel");
        _slnNameLabel.Text = PreviouslyOpenedSln.Name;
        _slnPathLabel.Text = PreviouslyOpenedSln.FilePath;
    }
}