using Godot;
using SharpIDE.Application.Features.Evaluation;
using SharpIDE.Application.Features.Nuget;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.Nuget;

public partial class NugetPanel : Control
{
    private VBoxContainer _installedPackagesVboxContainer = null!;
    private VBoxContainer _implicitlyInstalledPackagesItemList = null!;
    private VBoxContainer _availablePackagesItemList = null!;
    
    private NugetPackageDetails _nugetPackageDetails = null!;
    
    public SharpIdeSolutionModel? Solution { get; set; }
    
    [Inject] private readonly NugetClientService _nugetClientService = null!;
    
    private readonly PackedScene _packageEntryScene = ResourceLoader.Load<PackedScene>("uid://cqc2xlt81ju8s");
    
    private IdePackageResult? _selectedPackage;

    public override void _Ready()
    {
        _installedPackagesVboxContainer = GetNode<VBoxContainer>("%InstalledPackagesVBoxContainer");
        _implicitlyInstalledPackagesItemList = GetNode<VBoxContainer>("%ImplicitlyInstalledPackagesVBoxContainer");
        _availablePackagesItemList = GetNode<VBoxContainer>("%AvailablePackagesVBoxContainer");
        _nugetPackageDetails = GetNode<NugetPackageDetails>("%NugetPackageDetails");
        _nugetPackageDetails.Visible = false;
        _installedPackagesVboxContainer.QueueFreeChildren();
        _implicitlyInstalledPackagesItemList.QueueFreeChildren();
        _availablePackagesItemList.QueueFreeChildren();

        _ = Task.GodotRun(async () =>
        {
            await Task.Delay(300);
            var result = await _nugetClientService.GetTop100Results(Solution!.DirectoryPath);
            
            _ = Task.GodotRun(async () =>
            {
                var project = Solution.AllProjects.First(s => s.Name == "ProjectB");
                await project.MsBuildEvaluationProjectTask;
                var installedPackages = await ProjectEvaluation.GetPackageReferencesForProject(project);
                var idePackageResult = await _nugetClientService.GetPackagesForInstalledPackages(project.ChildNodeBasePath, installedPackages);
                var scenes = idePackageResult.Select(s =>
                {
                    var scene = _packageEntryScene.Instantiate<PackageEntry>();
                    scene.PackageResult = s;
                    scene.PackageSelected += OnPackageSelected;
                    return scene;
                }).ToList();
                await this.InvokeAsync(() =>
                {
                    foreach (var scene in scenes)
                    {
                        var container = scene.PackageResult.InstalledNugetPackageInfo!.IsTransitive ? _implicitlyInstalledPackagesItemList : _installedPackagesVboxContainer;
                        container.AddChild(scene);
                    }
                });
            });
            var scenes = result.Select(s =>
            {
                var scene = _packageEntryScene.Instantiate<PackageEntry>();
                scene.PackageResult = s;
                scene.PackageSelected += OnPackageSelected;
                return scene;
            }).ToList();
            await this.InvokeAsync(() =>
            {
                foreach (var scene in scenes)
                {
                    _availablePackagesItemList.AddChild(scene);
                }
            });
        });
    }

    private async Task OnPackageSelected(IdePackageResult packageResult)
    {
        _selectedPackage = packageResult;
        await _nugetPackageDetails.SetPackage(packageResult);
    }
}