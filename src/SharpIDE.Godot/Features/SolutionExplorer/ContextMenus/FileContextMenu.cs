using Godot;

using R3;

using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot.Features.SolutionExplorer.ContextMenus.Dialogs;

namespace SharpIDE.Godot.Features.SolutionExplorer;

public partial class SolutionExplorerPanel
{
    private readonly PackedScene _renameFileDialogScene = GD.Load<PackedScene>("uid://b775b5j4rkxxw");
    private void OpenContextMenuFile(SharpIdeFile file)
    {
        var menu = SharpIdeContextMenuBuilder
                   .Create()
                   .AddMenuItem("Open", () => GodotGlobalEvents.Instance.FileSelected.InvokeParallelFireAndForget(file, null))
                   .AddMenuItem("Reveal in File Explorer", () => OS.ShellShowInFileManager(file.Path))
                   .AddSeparator()
                   .AddMenuItem("Copy Full Path", () => DisplayServer.ClipboardSet(file.Path))
                   .AddSeparator()
                   .AddMenuItem("Rename", () => OnRename(file))
                   .AddMenuItem("Delete", () => OnDelete(file))
                   .Build();
        
        AddChild(menu);
			
        menu.Position = GetGlobalMousePosition().ToVector2I();
        menu.Popup();
    }

    private void OnRename(SharpIdeFile file)
    {
        var renameFileDialog = _renameFileDialogScene.Instantiate<RenameFileDialog>();
        renameFileDialog.File = file;
        AddChild(renameFileDialog);
        renameFileDialog.PopupCentered();
    }

    private void OnDelete(SharpIdeFile file)
    {
        var confirmedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var confirmationDialog = new ConfirmationDialog();
        confirmationDialog.Title = "Delete";
        confirmationDialog.DialogText = $"Delete '{file.Name}' file?";
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
                await _ideFileOperationsService.DeleteFile(file);
            }
        });
    }
}