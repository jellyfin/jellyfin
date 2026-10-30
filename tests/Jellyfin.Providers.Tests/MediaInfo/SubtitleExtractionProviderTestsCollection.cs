using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

/// <summary>
/// Other test classes replace the static BaseItem managers without joining a collection,
/// so <see cref="SubtitleExtractionProviderTests"/> runs on its own to avoid test interference.
/// </summary>
[CollectionDefinition(nameof(SubtitleExtractionProviderTestsCollection), DisableParallelization = true)]
public sealed class SubtitleExtractionProviderTestsCollection
{
}
