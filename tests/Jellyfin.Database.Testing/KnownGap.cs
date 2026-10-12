using System;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Tracks tests that are known to fail on a provider. A marked test is skipped while it fails the way the gap
/// expects and fails once it passes, so the marker cannot go stale. Any other failure fails the test.
/// </summary>
public static class KnownGap
{
    public static void On(KnownGapDefinition gap, Action body)
    {
        if (TestDatabaseEnvironment.Provider != gap.Provider)
        {
            body();
            return;
        }

        try
        {
            body();
        }
        catch (Exception ex) when (gap.Matches(ex))
        {
            Assert.Skip(SkipMessage(gap, ex));
        }

        Assert.Fail(FixedMessage(gap));
    }

    public static async Task OnAsync(KnownGapDefinition gap, Func<Task> body)
    {
        if (TestDatabaseEnvironment.Provider != gap.Provider)
        {
            await body().ConfigureAwait(false);
            return;
        }

        try
        {
            await body().ConfigureAwait(false);
        }
        catch (Exception ex) when (gap.Matches(ex))
        {
            Assert.Skip(SkipMessage(gap, ex));
        }

        Assert.Fail(FixedMessage(gap));
    }

    private static string SkipMessage(KnownGapDefinition gap, Exception ex)
        => $"[known gap {gap.Name}] {ex.GetType().Name}: {ex.Message}";

    private static string FixedMessage(KnownGapDefinition gap)
        => $"[known gap {gap.Name}] The test passes on {gap.Provider}, remove the KnownGap marker.";
}
