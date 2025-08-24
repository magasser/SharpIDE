using System.Collections.Concurrent;
using System.Threading.Channels;
using Ardalis.GuardClauses;
using AsyncReadProcess;
using SharpIDE.Application.Features.Evaluation;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Run;

public class RunService
{
	private readonly ConcurrentDictionary<SharpIdeProjectModel, SemaphoreSlim> _projectLocks = [];
	public async Task RunProject(SharpIdeProjectModel project)
	{
		Guard.Against.Null(project, nameof(project));
		Guard.Against.NullOrWhiteSpace(project.FilePath, nameof(project.FilePath), "Project file path cannot be null or empty.");
		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		var semaphoreSlim = _projectLocks.GetOrAdd(project, new SemaphoreSlim(1, 1));
		var waitResult = await semaphoreSlim.WaitAsync(0).ConfigureAwait(false);
		if (waitResult is false) throw new InvalidOperationException($"Project {project.Name} is already running.");
		if (project.RunningCancellationTokenSource is not null) throw new InvalidOperationException($"Project {project.Name} is already running with a cancellation token source.");

		project.RunningCancellationTokenSource = new CancellationTokenSource();
		var dllFullPath = ProjectEvaluation.GetOutputDllFullPath(project);
		var launchProfiles = await LaunchSettingsParser.GetLaunchSettingsProfiles(project);
		var launchProfile = launchProfiles.FirstOrDefault();
		try
		{
			var processStartInfo = new ProcessStartInfo2
			{
				FileName = "dotnet",
				WorkingDirectory = Path.GetDirectoryName(project.FilePath),
				//Arguments = $"run --project \"{project.FilePath}\" --no-restore",
				Arguments = $"\"{dllFullPath}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				EnvironmentVariables = []
			};
			if (launchProfile is not null)
			{
				foreach (var envVar in launchProfile.EnvironmentVariables)
				{
					processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
				}
				if (launchProfile.ApplicationUrl != null) processStartInfo.EnvironmentVariables["ASPNETCORE_URLS"] = launchProfile.ApplicationUrl;
			}

			var process = new Process2
			{
				StartInfo = processStartInfo
			};

			process.Start();

			project.RunningOutputChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = false,
			});
			var logsDrained = new TaskCompletionSource();
			_ = Task.Run(async () =>
			{
				await foreach(var log in process.CombinedOutputChannel.Reader.ReadAllAsync().ConfigureAwait(false))
				{
					//var logString = System.Text.Encoding.UTF8.GetString(log, 0, log.Length);
					//Console.Write(logString);
					await project.RunningOutputChannel.Writer.WriteAsync(log).ConfigureAwait(false);
				}
				project.RunningOutputChannel.Writer.Complete();
				logsDrained.TrySetResult();
			});

			project.Running = true;
			project.OpenInRunPanel = true;
			GlobalEvents.InvokeProjectsRunningChanged();
			GlobalEvents.InvokeStartedRunningProject();
			project.InvokeProjectStartedRunning();
			await process.WaitForExitAsync().WaitAsync(project.RunningCancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			if (project.RunningCancellationTokenSource.IsCancellationRequested)
			{
				process.End();
				await process.WaitForExitAsync().ConfigureAwait(false);
			}

			await logsDrained.Task.ConfigureAwait(false);
			project.RunningCancellationTokenSource.Dispose();
			project.RunningCancellationTokenSource = null;
			project.Running = false;
			GlobalEvents.InvokeProjectsRunningChanged();

			Console.WriteLine("Project finished running");
		}
		finally
		{
			semaphoreSlim.Release();
		}
	}

	public async Task CancelRunningProject(SharpIdeProjectModel project)
	{
		Guard.Against.Null(project, nameof(project));
		if (project.Running is false) throw new InvalidOperationException($"Project {project.Name} is not running.");
		if (project.RunningCancellationTokenSource is null) throw new InvalidOperationException($"Project {project.Name} does not have a running cancellation token source.");

		await project.RunningCancellationTokenSource.CancelAsync().ConfigureAwait(false);
	}
}
