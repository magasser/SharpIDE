using Godot;

using R3;

using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.SolutionExplorer;

file enum SolutionFolderContextMenuOptions
{
    Add = 1,
    Delete = 2,
    Rename = 3
}

file enum AddSubmenuOptions
{
    NewProject = 1,
    NewSolutionFolder = 2,
    ExistingProject = 3,
}

public partial class SolutionExplorerPanel
{
    private void OpenContextMenuSolutionFolder(SharpIdeSolutionFolder solutionFolder)
    {
        var menu = new PopupMenu();
        AddChild(menu);

        var addSubmenu = new PopupMenu();
        menu.AddSubmenuNodeItem("Add", addSubmenu, (int)SolutionFolderContextMenuOptions.Add);
        addSubmenu.AddItem("New Project", (int)AddSubmenuOptions.NewProject);
        addSubmenu.AddItem("New Solution Folder", (int)AddSubmenuOptions.NewSolutionFolder);
        addSubmenu.AddSeparator();
        addSubmenu.AddItem("Existing Project", (int)AddSubmenuOptions.ExistingProject);
        addSubmenu.IdPressed += id => OnAddSubmenuPressed(id, solutionFolder);
        
        menu.AddSeparator();
        menu.AddItem("Delete", (int)SolutionFolderContextMenuOptions.Delete);
        menu.AddItem("Rename", (int)SolutionFolderContextMenuOptions.Delete);
        menu.PopupHide += () => menu.QueueFree();
        menu.IdPressed += id => OnSubmenuPressed(id, solutionFolder);

        menu.Position = GetGlobalMousePosition().ToVector2I();
        menu.Popup();
    }

    private void OnSubmenuPressed(long id, SharpIdeSolutionFolder solutionFolder)
    {
        var actionId = (SolutionFolderContextMenuOptions)id;

        switch (actionId)
        {
            case SolutionFolderContextMenuOptions.Delete:
                var confirmedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var confirmationDialog = new ConfirmationDialog();
                confirmationDialog.Title = "Delete";
                confirmationDialog.DialogText = $"Delete '{solutionFolder.Name}' solution folder?";
                confirmationDialog.Confirmed += () =>
                {
                    confirmedTcs.SetResult(true);
                };
                confirmationDialog.Canceled += () =>
                {
                    confirmedTcs.SetResult(false);
                };
                AddChild(confirmationDialog);
                confirmationDialog.PopupCentered();
                _ = Task.GodotRun(async () =>
                {
                    var confirmed = await confirmedTcs.Task;

                    if (confirmed)
                    {
                        // TODO: Delete solution folder
                    }
                });
                break;
            case SolutionFolderContextMenuOptions.Rename:
                // TODO: Rename
                break;
            default:
                break;
        }
    }

    private void OnAddSubmenuPressed(long id, SharpIdeSolutionFolder solutionFolder)
    {
        
    }
}