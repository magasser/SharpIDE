using System.Diagnostics.CodeAnalysis;

using Godot;

namespace SharpIDE.Godot.Features.SolutionExplorer;

public sealed class SharpIdeContextMenuBuilder
{
    private int _currentId = 0;
    private List<ContextMenuNode> _nodes;
    
    private bool _isBuilt = false;
    private PopupMenu? _builtMenu = null;

    private SharpIdeContextMenuBuilder()
    {
        _nodes = new List<ContextMenuNode>();
    }
    
    public static SharpIdeContextMenuBuilder Create()
    {
        return new SharpIdeContextMenuBuilder();
    }

    public SharpIdeContextMenuBuilder AddMenuItem(string label, Action onPressed)
    {
        _nodes.Add(new MenuItem
        {
            Id = _currentId++,
            Label = label,
            OnPressed = onPressed
        });

        return this;
    }

    public SharpIdeContextMenuBuilder AddSeparator(string label = "")
    {
        _nodes.Add(new Separator
        {
            Id = -1,
            Label = label
        });

        return this;
    }

    public SharpIdeContextMenuBuilder AddSubmenu(string label, PopupMenu submenu)
    {
        _nodes.Add(new SubmenuItem
        {
            Id = _currentId++,
            Label = label,
            Menu = submenu
        });

        return this;
    }

    public SharpIdeContextMenuBuilder AddIconItem(string label, Action onPressed, Texture2D icon, int maxWidth)
    {
        _nodes.Add(new IconItem
        {
            Id = _currentId++,
            Label = label,
            OnPressed = onPressed,
            Icon = icon,
            MaxWidth = maxWidth
        });
    }

    public PopupMenu Build()
    {
        var menu = new PopupMenu();

        foreach (var node in _nodes)
        {
            switch (node)
            {
                case MenuItem:
                    menu.AddItem(node.Label, (int)node.Id);
                    break;
                case Separator:
                    menu.AddSeparator(node.Label, (int)node.Id);
                    break;
                case SubmenuItem submenu:
                    menu.AddSubmenuNodeItem(submenu.Label, submenu.Menu, (int)submenu.Id);
                    break;
            }
        }
        
        var items = _nodes.OfType<MenuItem>().ToDictionary(item => item.Id);

        menu.PopupHide += () => menu.QueueFree();
        menu.IdPressed += id =>
        {
            if (!items.TryGetValue(id, out var item))
            {
                return;
            }

            item.OnPressed.Invoke();
        };

        _builtMenu = menu;
        _isBuilt = true;

        return _builtMenu;
    }
    
    private abstract class ContextMenuNode
    {
        public required long Id { get; init; }
        
        public required string Label { get; init; }
        
    }

    private class MenuItem : ContextMenuNode
    {
        public required Action OnPressed { get; init; }
    }

    private sealed class Separator : ContextMenuNode
    {
        
    }

    private sealed class SubmenuItem : ContextMenuNode
    {
        public required PopupMenu Menu { get; init; }
    }

    private sealed class IconItem : MenuItem
    {
        public required Texture2D Icon { get; init; }
        
        public required int MaxWidth { get; init; }
    }
}