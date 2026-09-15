using Xunit;

namespace Jellyfin.Controller.Tests;

/// <summary>
/// Test classes that mutate the static <c>BaseItem.LibraryManager</c> /
/// <c>BaseItem.MediaSourceManager</c>. Grouped into one collection so xUnit
/// runs them sequentially and they cannot clobber each other's shared state.
/// </summary>
[CollectionDefinition("BaseItem static state")]
public class BaseItemStaticStateCollection
{
}
