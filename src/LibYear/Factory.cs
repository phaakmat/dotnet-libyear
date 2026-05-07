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

	public static App App(IAnsiConsole console)
	{
		var packageVersionChecker = new PackageVersionChecker(PackageMetadataResource());
		var fileSystem = new FileSystem();
		var projectRetriever = new ProjectFileManager(fileSystem);
		return new App(packageVersionChecker, projectRetriever, console);
	}

	private static PackageMetadataResource PackageMetadataResource()
	{
		var packageSource = Environment.GetEnvironmentVariable(PackageSourceEnvironmentVariable);
		var source = new PackageSource(string.IsNullOrWhiteSpace(packageSource) ? DefaultPackageSource : packageSource);

		var pat = Environment.GetEnvironmentVariable(PackageSourcePatEnvironmentVariable);
		if (!string.IsNullOrWhiteSpace(pat))
			source.Credentials = new PackageSourceCredential(source.Source, "az", pat, isPasswordClearText: true, validAuthenticationTypes: null);

		var provider = Repository.Provider.GetCoreV3();
		var repo = new SourceRepository(source, provider);
		return repo.GetResource<PackageMetadataResource>();
	}
}
