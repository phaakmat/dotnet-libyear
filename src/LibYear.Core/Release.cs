using NuGet.Protocol.Core.Types;

namespace LibYear.Core;

public class Release
{
	public PackageVersion Version { get; }
	public DateTime? Date { get; }
	public bool IsPublished { get; }

	public Release(IPackageSearchMetadata metadata) : this(new PackageVersion(metadata.Identity.Version), metadata.Published?.Date, metadata.IsListed)
	{
	}

	public Release(PackageVersion version, DateTime? released, bool isPublished = true)
	{
		Version = version;
		Date = released is { Year: > 1900 } ? released : null;
		IsPublished = isPublished;
	}
}