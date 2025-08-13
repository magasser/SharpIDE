using System.Diagnostics;
using Ardalis.GuardClauses;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.MSBuild;

namespace SharpIDE.Application.Features.Analysis;

public static class RoslynTest
{
	public static async Task Analyse(string solutionFilePath)
	{
		var workspace = MSBuildWorkspace.Create();
		var timer = Stopwatch.StartNew();
		workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
		var solution = await workspace.OpenSolutionAsync(solutionFilePath, new Progress());
		timer.Stop();
		Console.WriteLine($"RoslynTest: Solution loaded in {timer.ElapsedMilliseconds}ms");
		Console.WriteLine();

		foreach (var project in solution.Projects)
		{
			Console.WriteLine($"Project: {project.Name}");
			foreach (var document in project.Documents)
			{
				Console.WriteLine($"Document: {document.Name}");
				// var compilation = await project.GetCompilationAsync();
				// Guard.Against.Null(compilation, nameof(compilation));
				//
				// // Get diagnostics (built-in or custom analyzers)
				// var diagnostics = compilation.GetDiagnostics();
				//
				// foreach (var diagnostic in diagnostics)
				// {
				// 	Console.WriteLine(diagnostic);
				// 	// Optionally run CodeFixProviders here
				// }
				// var syntaxTree = await document.GetSyntaxTreeAsync();
				// var root = await syntaxTree!.GetRootAsync();
				// var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, root.FullSpan);
				// foreach (var span in classifiedSpans)
				// {
				// 	var classifiedSpan = root.GetText().GetSubText(span.TextSpan);
				// 	Console.WriteLine($"{span.TextSpan}: {span.ClassificationType}");
				// 	Console.WriteLine(classifiedSpan);
				// }
			}
		}

	}
}
