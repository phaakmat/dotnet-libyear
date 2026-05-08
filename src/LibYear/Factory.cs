using System;
using System.IO.Abstractions;
using LibYear.Core;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using Spectre.Console;

namespace LibYear;

public static class Factory
{
	private const string DefaultPackageSource = "https://api.nuget.org/v3/index.json";
	private const string PackageSourceEnvironmentVariable = "LIBYEAR_PACKAGE_SOURCE";
	private const string PackageSourcePatEnvironmentVariable = "LIBYEAR_PACKAGE_SOURCE_PAT";

	public static App App(IAnsiConsole console, Settings settings)
	{
		var packageVersionChecker = new PackageVersionChecker(PackageMetadataResources());
		var fileSystem = new FileSystem();
		var projectRetriever = new ProjectFileManager(fileSystem);
		IOutputFormatter formatter = settings.Json
			? new JsonOutputFormatter()
			: new ConsoleOutputFormatter(console, settings.QuietMode);
		return new App(packageVersionChecker, projectRetriever, console, formatter);
	}

	private static IReadOnlyList<PackageMetadataResource> PackageMetadataResources()
	{
		var resources = new List<PackageMetadataResource> { PackageMetadataResource(DefaultPackageSource, null) };

		var packageSource = Environment.GetEnvironmentVariable(PackageSourceEnvironmentVariable);
		if (!string.IsNullOrWhiteSpace(packageSource) && packageSource != DefaultPackageSource)
		{
			var pat = Environment.GetEnvironmentVariable(PackageSourcePatEnvironmentVariable);
			resources.Add(PackageMetadataResource(packageSource, pat));
		}

		return resources;
	}

	private static PackageMetadataResource PackageMetadataResource(string sourceUrl, string? pat)
	{
		var source = new PackageSource(sourceUrl);
		if (!string.IsNullOrWhiteSpace(pat))
			source.Credentials = new PackageSourceCredential(source.Source, "az", pat, isPasswordClearText: true, validAuthenticationTypesText: null);
		var provider = Repository.Provider.GetCoreV3();
		var repo = new SourceRepository(source, provider);
		return repo.GetResource<PackageMetadataResource>();
	}
}
