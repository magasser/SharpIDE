using Godot;

using R3;

using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.Build;
using SharpIDE.Application.Features.Evaluation;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.Run;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot.Features.BottomPanel;

namespace SharpIDE.Godot.Features.SolutionExplorer;

file enum ProjectContextMenuOptions
{
    CreateNew = 0,
    Run = 1,
    Build = 2,
    Rebuild = 3,
    Clean = 4,
    Restore = 5,
    DotnetUserSecrets = 6,
}

file enum CreateNewSubmenuOptions
{
    Directory = 1,
    CSharpFile = 2
}

public partial class SolutionExplorerPanel
{
    private Texture2D _runIcon = ResourceLoader.Load<Texture2D>("uid://bkty6563cthj8");
    
    [Inject] private readonly BuildService _buildService = null!;
    [Inject] private readonly RunService _runService = null!;
    [Inject] private readonly DotnetUserSecretsService _dotnetUserSecretsService = null!;

    private void OpenContextMenuProject(SharpIdeProjectModel project)
    {
        var menu = SharpIdeContextMenuBuilder
                   .Create()
                   .AddSubmenu(
                       "Add",
                       SharpIdeContextMenuBuilder
                           .Create()
                           .AddMenuItem("Directory", () => OnCreateDirectory(project))
                           .AddMenuItem("C# File", () => OnCreateCsharpFile(project))
                           .Build())
                   .AddIconItem("Run", () => RunProject(project), _runIcon, maxWidth: 20)
                   .AddSeparator()
                   .AddMenuItem("Build", () => MsBuildProject(project, BuildType.Build))
                   .AddMenuItem("Rebuild", () => MsBuildProject(project, BuildType.Rebuild))
                   .AddMenuItem("Clean", () => MsBuildProject(project, BuildType.Clean))
                   .AddMenuItem("Restore", () => MsBuildProject(project, BuildType.Restore))
                   .AddSeparator()
                   .AddMenuItem(".NET User Secrets", () => ShowUserSecrets(project))
                   .Build();
        
        AddChild(menu);
			
        menu.Position = GetGlobalMousePosition().ToVector2I();
        menu.Popup();
    }

    private void RunProject(SharpIdeProjectModel project)
    {
        _ = Task.GodotRun(async () =>
        {
            GodotGlobalEvents.Instance.BottomPanelTabExternallySelected
                             .InvokeParallelFireAndForget(BottomPanelType.Run);
            await _runService.RunProject(project);
        });
    }
    
    private void MsBuildProject(SharpIdeProjectModel project, BuildType buildType)
    {
        _ = Task.GodotRun(async () =>
        {
            GodotGlobalEvents.Instance.BottomPanelTabExternallySelected.InvokeParallelFireAndForget(
                BottomPanelType.Build);
            await _buildService.MsBuildAsync(project.FilePath, buildType);
        });
    }

    private void ShowUserSecrets(SharpIdeProjectModel project)
    {
        _ = Task.GodotRun(async () =>
        {
            var (userSecretsId, filePath) = await _dotnetUserSecretsService.GetOrCreateUserSecretsId(project);
            OS.ShellShowInFileManager(filePath);
            
        });
    }
}
