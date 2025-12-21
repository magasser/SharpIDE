using Godot;

using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot;

public class SharpIdeSolutionManager
{
    public SharpIdeSolutionModel SolutionModel { get; set; } = null!;
    public TaskCompletionSource SolutionReadyTcs { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<SharpIdeSolutionModel> LoadSolution(
        string solutionFilePath,
        CancellationToken cancellationToken = default)
    {
        SolutionModel = await VsPersistenceMapper.GetSolutionModel(solutionFilePath, cancellationToken);
        SolutionReadyTcs.SetResult();

        return SolutionModel;
    }
}