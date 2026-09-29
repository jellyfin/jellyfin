using Jellyfin.Server.ServerSetupApp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.ServerSetupApp;

public class StartupLoggerTests
{
    [Fact]
    public void BeginAmbientTopic_AttachesNewLoggersToTheTopic()
    {
        var migration = new StartupLogger(NullLogger.Instance).BeginGroup($"Migration");

        using (StartupLogger.BeginAmbientTopic(migration.Topic))
        {
            Assert.Same(migration.Topic, new StartupLogger(NullLogger.Instance).Topic);
        }
    }

    [Fact]
    public void BeginAmbientTopic_RestoresThePreviousTopic()
    {
        var root = new StartupLogger(NullLogger.Instance);
        var outer = root.BeginGroup($"Outer");
        var inner = outer.BeginGroup($"Inner");

        Assert.Null(new StartupLogger(NullLogger.Instance).Topic);

        using (StartupLogger.BeginAmbientTopic(outer.Topic))
        {
            using (StartupLogger.BeginAmbientTopic(inner.Topic))
            {
                Assert.Same(inner.Topic, new StartupLogger(NullLogger.Instance).Topic);
            }

            // Leaving a nested topic has to fall back to the enclosing one, not to the setup UI root.
            Assert.Same(outer.Topic, new StartupLogger(NullLogger.Instance).Topic);
        }

        Assert.Null(new StartupLogger(NullLogger.Instance).Topic);
    }

    [Fact]
    public void BeginGroup_KeepsAnExplicitTopicOverTheAmbientOne()
    {
        var migration = new StartupLogger(NullLogger.Instance).BeginGroup($"Migration");
        var unrelated = new StartupLogger(NullLogger.Instance).BeginGroup($"Unrelated");

        using (StartupLogger.BeginAmbientTopic(migration.Topic))
        {
            Assert.Same(unrelated.Topic, unrelated.With(NullLogger.Instance).Topic);
        }
    }
}
