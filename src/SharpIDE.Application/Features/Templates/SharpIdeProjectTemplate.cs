namespace SharpIDE.Application.Features.Templates;

public interface ISharpIdeProjectTemplate
{
	string Sdk { get; }

	string TargetFramework { get; }

	IReadOnlyList<ISharpIdeProjectSubTemplate> SubTemplates { get; }
}

public interface ISharpIdeProjectSubTemplate
{
	string Name { get; }
}

public class SharpIdeClassLibraryProjectTemplate : ISharpIdeProjectTemplate
{
	/// <inheritdoc />
	public required string Sdk { get; init; }

	/// <inheritdoc />
	public required string TargetFramework { get; init; }

	/// <inheritdoc />
	public IReadOnlyList<ISharpIdeProjectSubTemplate> SubTemplates { get; } = [];
}

public class SharpIdeDesktopProjectTemplate : ISharpIdeProjectTemplate
{
	/// <inheritdoc />
	public required string Sdk { get; init; }

	/// <inheritdoc />
	public required string TargetFramework { get; init; }

	/// <inheritdoc />
	public IReadOnlyList<ISharpIdeProjectSubTemplate> SubTemplates { get; } = [new SharpIdeWpfAppProjectSubTemplate()];
}

public class SharpIdeWpfAppProjectSubTemplate : ISharpIdeProjectSubTemplate
{
	/// <inheritdoc />
	public string Name { get; } = "WPF Application";
}
