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
    private OptionButton _solutionOrProjectOptionButton = null!;
    
    private NugetPackageDetails _nugetPackageDetails = null!;

    private SharpIdeSolutionModel? _solution;
    
    [Inject] private readonly NugetClientService _nugetClientService = null!;
    [Inject] private readonly SharpIdeSolutionAccessor _sharpIdeSolutionAccessor;
    
    private readonly PackedScene _packageEntryScene = ResourceLoader.Load<PackedScene>("uid://cqc2xlt81ju8s");
    private readonly Texture2D _csprojIcon = ResourceLoader.Load<Texture2D>("uid://cqt30ma6xgder");
    
    private IdePackageResult? _selectedPackage;

    public override void _Ready()
    {
        _installedPackagesVboxContainer = GetNode<VBoxContainer>("%InstalledPackagesVBoxContainer");
        _implicitlyInstalledPackagesItemList = GetNode<VBoxContainer>("%ImplicitlyInstalledPackagesVBoxContainer");
        _availablePackagesItemList = GetNode<VBoxContainer>("%AvailablePackagesVBoxContainer");
        _solutionOrProjectOptionButton = GetNode<OptionButton>("%SolutionOrProjectOptionButton");
        _nugetPackageDetails = GetNode<NugetPackageDetails>("%NugetPackageDetails");
        _nugetPackageDetails.Visible = false;
        _installedPackagesVboxContainer.QueueFreeChildren();
        _implicitlyInstalledPackagesItemList.QueueFreeChildren();
        _availablePackagesItemList.QueueFreeChildren();
        
        _ = Task.GodotRun(_AsyncReady);
    }

    private async Task _AsyncReady()
    {
        await _sharpIdeSolutionAccessor.SolutionReadyTcs.Task;
        _solution = _sharpIdeSolutionAccessor.SolutionModel;
        await this.InvokeAsync(() =>
        {
            foreach (var project in _solution!.AllProjects.OrderBy(s => s.Name))
            {
                _solutionOrProjectOptionButton.AddIconItem(_csprojIcon, project.Name);
            }
            _solutionOrProjectOptionButton.ItemSelected += OnSolutionOrProjectSelected;
        });
        OnSolutionOrProjectSelected(0);
    }

    private void OnSolutionOrProjectSelected(long index)
    {
        _ = Task.GodotRun(async () =>
        {
            if (_solution is null) throw new InvalidOperationException("Solution is null but should not be");
            _ = Task.GodotRun(PopulateSearchResults);
            _ = Task.GodotRun(PopulateInstalledPackages);
        });
    }

    private async Task OnPackageSelected(IdePackageResult packageResult)
    {
        _selectedPackage = packageResult;
        await _nugetPackageDetails.SetPackage(packageResult);
    }

    private async Task PopulateSearchResults()
    {
        var result = await _nugetClientService.GetTop100Results(_solution!.DirectoryPath);
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
    }

    private async Task PopulateInstalledPackages()
    {
        var project = _solution!.AllProjects.First(s => s.Name == "ProjectA");
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
    }
}