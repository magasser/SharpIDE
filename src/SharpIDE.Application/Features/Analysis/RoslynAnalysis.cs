using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Ardalis.GuardClauses;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using NuGet.Packaging;
using ObservableCollections;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Analysis;

public static class RoslynAnalysis
{
	public static MSBuildWorkspace? _workspace;
	private static SharpIdeSolutionModel? _sharpIdeSolutionModel;
	private static HashSet<CodeFixProvider> _codeFixProviders = [];
	private static HashSet<CodeRefactoringProvider> _codeRefactoringProviders = [];
	private static TaskCompletionSource _solutionLoadedTcs = new();
	public static void StartSolutionAnalysis(SharpIdeSolutionModel solutionModel)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				await Analyse(solutionModel);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RoslynAnalysis: Error during analysis: {e}");
			}
		});
	}
	public static async Task Analyse(SharpIdeSolutionModel solutionModel)
	{
		Console.WriteLine($"RoslynAnalysis: Loading solution");
		_sharpIdeSolutionModel = solutionModel;
		var timer = Stopwatch.StartNew();
		if (_workspace is null)
		{
			// is this hostServices necessary? test without it - just getting providers from assemblies instead
			var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
			_workspace ??= MSBuildWorkspace.Create(host);
			_workspace.RegisterWorkspaceFailedHandler(o => throw new InvalidOperationException($"Workspace failed: {o.Diagnostic.Message}"));
		}
		var solution = await _workspace.OpenSolutionAsync(_sharpIdeSolutionModel.FilePath, new Progress());
		timer.Stop();
		Console.WriteLine($"RoslynAnalysis: Solution loaded in {timer.ElapsedMilliseconds}ms");
		_solutionLoadedTcs.SetResult();

		foreach (var assembly in MefHostServices.DefaultAssemblies)
		{
			//var assembly = analyzer.GetAssembly();
			var fixers = CodeFixProviderLoader.LoadCodeFixProviders([assembly], LanguageNames.CSharp);
			_codeFixProviders.AddRange(fixers);
			var refactoringProviders = CodeRefactoringProviderLoader.LoadCodeRefactoringProviders([assembly], LanguageNames.CSharp);
			_codeRefactoringProviders.AddRange(refactoringProviders);
		}

		// // TODO: Distinct on the assemblies first
		// foreach (var project in solution.Projects)
		// {
		// 	var relevantAnalyzerReferences = project.AnalyzerReferences.OfType<AnalyzerFileReference>().ToArray();
		// 	var assemblies = relevantAnalyzerReferences.Select(a => a.GetAssembly()).ToArray();
		// 	var language = project.Language;
		// 	//var analyzers = relevantAnalyzerReferences.SelectMany(a => a.GetAnalyzers(language));
		// 	var fixers = CodeFixProviderLoader.LoadCodeFixProviders(assemblies, language);
		// 	_codeFixProviders.AddRange(fixers);
		// 	var refactoringProviders = CodeRefactoringProviderLoader.LoadCodeRefactoringProviders(assemblies, language);
		// 	_codeRefactoringProviders.AddRange(refactoringProviders);
		// }

		_codeFixProviders = _codeFixProviders.DistinctBy(s => s.GetType().Name).ToHashSet();
		_codeRefactoringProviders = _codeRefactoringProviders.DistinctBy(s => s.GetType().Name).ToHashSet();

		await UpdateSolutionDiagnostics();
		foreach (var project in solution.Projects)
		{
			// foreach (var document in project.Documents)
			// {
			// 	var semanticModel = await document.GetSemanticModelAsync();
			// 	Guard.Against.Null(semanticModel, nameof(semanticModel));
			// 	var documentDiagnostics = semanticModel.GetDiagnostics().Where(d => d.Severity is not DiagnosticSeverity.Hidden).ToList();
			// 	foreach (var diagnostic in documentDiagnostics)
			// 	{
			// 		var test = await GetCodeFixesAsync(document, diagnostic);
			// 	}
			// 	// var syntaxTree = await document.GetSyntaxTreeAsync();
			// 	// var root = await syntaxTree!.GetRootAsync();
			// 	// var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, root.FullSpan);
			// 	// foreach (var span in classifiedSpans)
			// 	// {
			// 	// 	var classifiedSpan = root.GetText().GetSubText(span.TextSpan);
			// 	// 	Console.WriteLine($"{span.TextSpan}: {span.ClassificationType}");
			// 	// 	Console.WriteLine(classifiedSpan);
			// 	// }
			// }
		}
		Console.WriteLine("RoslynAnalysis: Analysis completed.");
	}

	public static async Task UpdateSolutionDiagnostics()
	{
		await _solutionLoadedTcs.Task;
		foreach (var project in _sharpIdeSolutionModel!.AllProjects)
		{
			var projectDiagnostics = await GetProjectDiagnostics(project);
			// TODO: only add and remove diffs
			project.Diagnostics.RemoveRange(project.Diagnostics);
			project.Diagnostics.AddRange(projectDiagnostics);
		}
	}

	public static async Task<ImmutableArray<Diagnostic>> GetProjectDiagnostics(SharpIdeProjectModel projectModel)
	{
		await _solutionLoadedTcs.Task;
		var cancellationToken = CancellationToken.None;
		var project = _workspace!.CurrentSolution.Projects.Single(s => s.FilePath == projectModel.FilePath);
		var compilation = await project.GetCompilationAsync(cancellationToken);
		Guard.Against.Null(compilation, nameof(compilation));

		var diagnostics = compilation.GetDiagnostics(cancellationToken);
		diagnostics = diagnostics.Where(d => d.Severity is not DiagnosticSeverity.Hidden).ToImmutableArray();
		return diagnostics;
	}

	public static async Task<ImmutableArray<(FileLinePositionSpan fileSpan, Diagnostic diagnostic)>> GetDocumentDiagnostics(SharpIdeFile fileModel)
	{
		await _solutionLoadedTcs.Task;
		var cancellationToken = CancellationToken.None;
		var project = _workspace!.CurrentSolution.Projects.Single(s => s.FilePath == ((IChildSharpIdeNode)fileModel).GetNearestProjectNode()!.FilePath);
		var document = project.Documents.Single(s => s.FilePath == fileModel.Path);
		//var document = _workspace!.CurrentSolution.GetDocument(fileModel.Path);
		Guard.Against.Null(document, nameof(document));

		var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
		Guard.Against.Null(semanticModel, nameof(semanticModel));

		var diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
		diagnostics = diagnostics.Where(d => d.Severity is not DiagnosticSeverity.Hidden).ToImmutableArray();
		var result = diagnostics.Select(d => (semanticModel.SyntaxTree.GetMappedLineSpan(d.Location.SourceSpan), d)).ToImmutableArray();
		return result;
	}

	public static async Task<IEnumerable<(FileLinePositionSpan fileSpan, ClassifiedSpan classifiedSpan)>> GetDocumentSyntaxHighlighting(SharpIdeFile fileModel)
	{
		await _solutionLoadedTcs.Task;
		var cancellationToken = CancellationToken.None;
		var project = _workspace!.CurrentSolution.Projects.Single(s => s.FilePath == ((IChildSharpIdeNode)fileModel).GetNearestProjectNode()!.FilePath);
		var document = project.Documents.Single(s => s.FilePath == fileModel.Path);
		Guard.Against.Null(document, nameof(document));

		var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
		var root = await syntaxTree!.GetRootAsync(cancellationToken);
		var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, root.FullSpan, cancellationToken);

		var result = classifiedSpans.Select(s => (syntaxTree.GetMappedLineSpan(s.TextSpan), s));

		return result;
	}

	public static async Task<CompletionList> GetCodeCompletionsForDocumentAtPosition(SharpIdeFile fileModel, LinePosition linePosition)
	{
		await _solutionLoadedTcs.Task;
		var project = _workspace!.CurrentSolution.Projects.Single(s => s.FilePath == ((IChildSharpIdeNode)fileModel).GetNearestProjectNode()!.FilePath);
		var document = project.Documents.Single(s => s.FilePath == fileModel.Path);
		Guard.Against.Null(document, nameof(document));
		var completions = await GetCompletionsAsync(document, linePosition).ConfigureAwait(false);
		return completions;
	}

	public static async Task<ImmutableArray<CodeAction>> GetCodeFixesForDocumentAtPosition(SharpIdeFile fileModel, LinePosition linePosition)
	{
		var cancellationToken = CancellationToken.None;
		var project = _workspace!.CurrentSolution.Projects.Single(s => s.FilePath == ((IChildSharpIdeNode)fileModel).GetNearestProjectNode()!.FilePath);
		var document = project.Documents.Single(s => s.FilePath == fileModel.Path);
		Guard.Against.Null(document, nameof(document));
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
		Guard.Against.Null(semanticModel, nameof(semanticModel));

		var diagnostics = semanticModel.GetDiagnostics();
		var sourceText = await document.GetTextAsync(cancellationToken);
		var position = sourceText.Lines.GetPosition(linePosition);
		var diagnosticsAtPosition = diagnostics
			.Where(d => d.Location.IsInSource && d.Location.SourceSpan.Contains(position))
			.ToImmutableArray();

		ImmutableArray<CodeAction> codeActions = [];
		foreach (var diagnostic in diagnosticsAtPosition)
		{
			var actions = await GetCodeFixesAsync(document, diagnostic);
			codeActions = codeActions.AddRange(actions);
		}

		var linePositionSpan = new LinePositionSpan(linePosition, new LinePosition(linePosition.Line, linePosition.Character + 1));
		var selectedSpan = sourceText.Lines.GetTextSpan(linePositionSpan);
		codeActions = codeActions.AddRange(await GetCodeRefactoringsAsync(document, selectedSpan));
		return codeActions;
	}

	public static async Task<ImmutableArray<(FileLinePositionSpan fileSpan, CodeAction codeAction)>> GetCodeFixesAsync(Diagnostic diagnostic)
	{
		var cancellationToken = CancellationToken.None;
		var document = _workspace!.CurrentSolution.GetDocument(diagnostic.Location.SourceTree);
		Guard.Against.Null(document, nameof(document));
		var codeActions = await GetCodeFixesAsync(document, diagnostic);
		var result = codeActions.Select(action => (diagnostic.Location.SourceTree!.GetMappedLineSpan(diagnostic.Location.SourceSpan), action))
			.ToImmutableArray();
		return result;
	}
	private static async Task<ImmutableArray<CodeAction>> GetCodeFixesAsync(Document document, Diagnostic diagnostic)
	{
		var cancellationToken = CancellationToken.None;
		var codeActions = new List<CodeAction>();
		var context = new CodeFixContext(
			document,
			diagnostic,
			(action, _) => codeActions.Add(action), // callback collects fixes
			cancellationToken
		);

		var relevantProviders = _codeFixProviders
			.Where(provider => provider.FixableDiagnosticIds.Contains(diagnostic.Id));

		foreach (var provider in relevantProviders)
		{
			await provider.RegisterCodeFixesAsync(context);
		}

		return codeActions.ToImmutableArray();
	}

	private static async Task<ImmutableArray<CodeAction>> GetCodeRefactoringsAsync(Document document, TextSpan span)
	{
		var cancellationToken = CancellationToken.None;
		var codeActions = new List<CodeAction>();
		var refactorContext = new CodeRefactoringContext(
			document,
			span,
			action => codeActions.Add(action),
			cancellationToken
		);

		foreach (var provider in _codeRefactoringProviders)
		{
			await provider.ComputeRefactoringsAsync(refactorContext).ConfigureAwait(false);
		}

		return codeActions.ToImmutableArray();
	}

	private static async Task<CompletionList> GetCompletionsAsync(Document document, LinePosition linePosition)
	{
		var cancellationToken = CancellationToken.None;
		var completionService = CompletionService.GetService(document);
		if (completionService is null) throw new InvalidOperationException("Completion service is not available for the document.");

		var sourceText = await document.GetTextAsync(cancellationToken);
		var position = sourceText.Lines.GetPosition(linePosition);
		var completions = await completionService.GetCompletionsAsync(document, position, cancellationToken: cancellationToken);

		// foreach (var item in completions.ItemsList)
		// {
		// 	Console.WriteLine($"Completion: {item.DisplayText}");
		// }
		return completions;
	}

	public static async Task ApplyCodeActionAsync(CodeAction codeAction)
	{
		var cancellationToken = CancellationToken.None;
		var operations = await codeAction.GetOperationsAsync(cancellationToken);
		foreach (var operation in operations)
		{
			operation.Apply(_workspace!, cancellationToken);
			// if (operation is ApplyChangesOperation applyChangesOperation)
			// {
			// 	var newSolution = applyChangesOperation.ChangedSolution;
			// 	_workspace.TryApplyChanges(newSolution);
			// }
			// else
			// {
			// 	throw new NotSupportedException($"Unsupported operation type: {operation.GetType().Name}");
			// }
		}
	}

	// TODO: Use AdhocWorkspace or something else, to avoid writing to disk on every change
	public static void UpdateDocument(SharpIdeFile fileModel, string newContent)
	{
		Guard.Against.Null(fileModel, nameof(fileModel));
		Guard.Against.NullOrEmpty(newContent, nameof(newContent));

		var project = _workspace!.CurrentSolution.Projects.Single(s => s.FilePath == ((IChildSharpIdeNode)fileModel).GetNearestProjectNode()!.FilePath);
		var document = project.Documents.Single(s => s.FilePath == fileModel.Path);
		Guard.Against.Null(document, nameof(document));

		//var updatedDocument = document.WithText(SourceText.From(newContent));
		var newSolution = _workspace.CurrentSolution.WithDocumentText(document.Id, SourceText.From(newContent));
		_workspace.TryApplyChanges(newSolution);
	}
}
