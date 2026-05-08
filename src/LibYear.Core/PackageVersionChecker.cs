using System.Collections.Concurrent;
using LibYear.Core.FileTypes;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace LibYear.Core;

public class PackageVersionChecker : IPackageVersionChecker
{
	private readonly IReadOnlyList<PackageMetadataResource> _metadataResources;
	private readonly IDictionary<string, IReadOnlyCollection<Release>> _versionCache;

	public PackageVersionChecker(PackageMetadataResource metadataResource)
		: this([metadataResource], new ConcurrentDictionary<string, IReadOnlyCollection<Release>>())
	{
	}

	public PackageVersionChecker(IReadOnlyList<PackageMetadataResource> metadataResources)
		: this(metadataResources, new ConcurrentDictionary<string, IReadOnlyCollection<Release>>())
	{
	}

	public PackageVersionChecker(PackageMetadataResource metadataResource, IDictionary<string, IReadOnlyCollection<Release>> versionCache)
		: this([metadataResource], versionCache)
	{
	}

	public PackageVersionChecker(IReadOnlyList<PackageMetadataResource> metadataResources, IDictionary<string, IReadOnlyCollection<Release>> versionCache)
	{
		_metadataResources = metadataResources;
		_versionCache = versionCache;
	}

	public async Task<SolutionResult> GetPackages(IReadOnlyCollection<IProjectFile> projectFiles)
	{
		var tasks = projectFiles.ToDictionary(proj => proj, GetResults);
		var results = await Task.WhenAll(tasks.Values);
		return new SolutionResult(results);
	}

	private async Task<ProjectResult> GetResults(IProjectFile proj)
	{
		var tasks = proj.Packages.Select(p => GetResult(p.Key, p.Value));
		var results = await Task.WhenAll(tasks);
		return new ProjectResult(proj, results);
	}

	public async Task<Result> GetResult(string packageName, PackageVersion? installed)
	{
		if (!_versionCache.TryGetValue(packageName, out var versions))
		{
			versions = await GetVersions(packageName);
			_versionCache[packageName] = versions;
		}

		var latest = versions.FirstOrDefault(v => v.Version == versions.Where(m => !m.Version.IsPrerelease && m.IsPublished).Max(m => m.Version));
		var current = installed?.WildcardType switch
		{
			WildcardType.Major => latest,
			WildcardType.Minor => versions.FirstOrDefault(v => v.Version == versions.Where(m => !m.Version.IsPrerelease && m.IsPublished && m.Version.Major == installed.Major).Max(m => m.Version)),
			WildcardType.Patch => versions.FirstOrDefault(v => v.Version == versions.Where(m => !m.Version.IsPrerelease && m.IsPublished && m.Version.Major == installed.Major && m.Version.Minor == installed.Minor).Max(m => m.Version)),
			_ => versions.FirstOrDefault(v => v.Version == installed)
		};

		return new Result(packageName, current, latest);
	}

	public async Task<IReadOnlyCollection<Release>> GetVersions(string packageName)
	{
		foreach (var resource in _metadataResources)
		{
			try
			{
				var metadata = await resource.GetMetadataAsync(packageName, true, true, NullSourceCacheContext.Instance, NullLogger.Instance, CancellationToken.None);
				var releases = metadata.Select(m => new Release(m)).ToArray();
				if (releases.Length > 0)
					return releases;
			}
			catch (FatalProtocolException)
			{
			}
		}
		return Array.Empty<Release>();
	}
}