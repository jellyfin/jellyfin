using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ScheduledTasks;

/// <summary>
/// Tests that assign the static services of <see cref="MediaBrowser.Controller.Entities.BaseItem"/> must not run in parallel.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class BaseItemStaticsCollection
{
    public const string Name = "BaseItem statics";
}
