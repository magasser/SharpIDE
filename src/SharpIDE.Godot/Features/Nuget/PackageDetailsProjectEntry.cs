using Godot;
using NuGet.Versioning;
using SharpIDE.Application.Features.Evaluation;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.Nuget;

public partial class PackageDetailsProjectEntry : MarginContainer
{
    private Label _projectNameLabel = null!;
    private Label _installedVersionLabel = null!;
    
    public SharpIdeProjectModel ProjectModel { get; set; } = null!;
    public ProjectPackageReference? ProjectPackageReference { get; set; }
    public override void _Ready()
    {
        _projectNameLabel = GetNode<Label>("%ProjectNameLabel");;
        _installedVersionLabel = GetNode<Label>("%InstalledVersionLabel");
        _installedVersionLabel.Text = string.Empty;
        SetValues();
    }
    
    public void SetValues()
    {
        if (ProjectModel == null) return;
        _projectNameLabel.Text = ProjectModel.Name;
        if (ProjectPackageReference == null) return;
        var isTransitive = ProjectPackageReference.IsTransitive;
        var installedVersion = ProjectPackageReference.InstalledVersion;
        _installedVersionLabel.Text = isTransitive ? $"({installedVersion?.ToNormalizedString()})" : installedVersion?.ToNormalizedString();
        
        if (isTransitive)
        {
            var transitiveOriginsGroupedByVersion = ProjectPackageReference.DependentPackages!.GroupBy(t => t.RequestedVersion)
                .Select(g => new
                {
                    RequestedVersion = g.Key,
                    PackageNames = g.Select(t => t.PackageName).Distinct().ToList()
                })
                .ToList();
            _installedVersionLabel.TooltipText = $"""
                                                  Implicitly Referenced Versions
                                                  {string.Join("\n", transitiveOriginsGroupedByVersion.Select(t => $"{t.RequestedVersion.ToString("p", VersionRangeFormatter.Instance)} by {string.Join(", ", t.PackageNames)}"))}
                                                  """;
        }
    }
    
    public void ClearInstallInfo()
    {
        _installedVersionLabel.Text = string.Empty;
        _installedVersionLabel.TooltipText = string.Empty;
        ProjectPackageReference = null;
    }
}