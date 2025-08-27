using SharpIDE.Application.Features.Build;
using SharpIDE.Application.Features.Run;

namespace SharpIDE.Godot;

public static class Singletons
{
    public static RunService RunService { get; } = new RunService();
    public static BuildService BuildService { get; } = new BuildService();
}