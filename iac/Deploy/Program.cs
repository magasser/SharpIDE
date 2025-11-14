using Deploy.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ParallelPipelines.Host;

var builder = Host.CreateApplicationBuilder(args);

builder
	.Configuration.AddJsonFile("appsettings.Development.json", true)
	.AddUserSecrets<Program>()
	.AddEnvironmentVariables();

builder.Services.AddParallelPipelines(
	builder.Configuration,
	config =>
	{
		config.Local.OutputSummaryToFile = true;
		config.Cicd.OutputSummaryToGithubStepSummary = true;
		config.Cicd.WriteCliCommandOutputsToSummary = true;
	}
);
builder.Services
	.AddStep<RestoreAndBuildStep>()
	;

using var host = builder.Build();

await host.RunAsync();
