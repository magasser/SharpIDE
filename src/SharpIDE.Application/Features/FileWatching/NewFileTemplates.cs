using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.FileWatching;

public static class NewFileTemplates
{
	public static string CsharpClass(string className, string @namespace)
	{
		var text = $$"""
		           namespace {{@namespace}};

		           public class {{className}}
		           {

		           }
		           """;
		return text;
	}

	public static string ComputeNamespace(IFolderOrProject folder)
	{
		var names = new List<string>();
		IFolderOrProject? current = folder;
		while (current is not null)
		{
			names.Add(current.Name);
			current = current.Parent as IFolderOrProject;
		}
		names.Reverse();
		return string.Join('.', names);
	}

	public static string CsprojLibrary(string sdk, string targetFramework)
	{
		var text = $$"""
		             <Project Sdk="{{sdk}}">

		                 <PropertyGroup>
		                     <OutputType>Exe</OutputType>
		                     <TargetFramework>{{targetFramework}}</TargetFramework>
		                     <ImplicitUsings>enable</ImplicitUsings>
		                     <Nullable>enable</Nullable>
		                 </PropertyGroup>

		             </Project>

		             """;

		return text;
	}

	public static string CsprojConsole(string sdk, string targetFramework)
	{
		var text = $$"""
		             <Project Sdk="{{sdk}}">

		                 <PropertyGroup>
		                     <OutputType>Exe</OutputType>
		                     <TargetFramework>{{targetFramework}}</TargetFramework>
		                     <ImplicitUsings>enable</ImplicitUsings>
		                     <Nullable>enable</Nullable>
		                 </PropertyGroup>

		             </Project>

		             """;

		return text;
	}

	public static string CsprojWpfApp(string sdk, string targetFramework)
	{
		var text = $$"""
		             <Project Sdk="{{sdk}}">

		                 <PropertyGroup>
		                     <OutputType>Exe</OutputType>
		                     <TargetFramework>{{targetFramework}}</TargetFramework>
		                     <ImplicitUsings>enable</ImplicitUsings>
		                     <Nullable>enable</Nullable>
		                     <UseWPF>true</UseWPF>
		                 </PropertyGroup>

		             </Project>

		             """;

		return text;
	}
}
