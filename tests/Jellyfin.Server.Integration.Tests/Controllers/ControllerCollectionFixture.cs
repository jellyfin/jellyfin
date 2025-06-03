using Xunit;

namespace Jellyfin.Server.Integration.Tests;

[CollectionDefinition("Controller collection")]
public class ControllerCollectionFixture : ICollectionFixture<JellyfinApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
