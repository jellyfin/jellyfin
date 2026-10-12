using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Activity;
using Jellyfin.Server.Implementations.Tests.Item;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Activity;

/// <summary>
/// The text filters of the activity log match a substring of Name, Overview, ShortOverview, Type and
/// Username, ignoring ASCII case. A <c>\</c> in the filter is an ordinary character; <c>%</c> and
/// <c>_</c> are wildcards.
/// </summary>
public sealed class ActivityManagerFilterTests : SqliteDbTestFixture
{
    private readonly ActivityManager _manager;

    public ActivityManagerFilterTests()
    {
        var alice = new User("Alice", "auth", "reset");
        var bob = new User("Bob", "auth", "reset");

        using (var context = CreateDbContext())
        {
            context.Users.Add(alice);
            context.Users.Add(bob);
            context.ActivityLogs.Add(new ActivityLog("Admin Logged In", "SessionStarted", alice.Id)
            {
                Overview = "Login From Chrome",
                ShortOverview = "Chrome Login"
            });
            context.ActivityLogs.Add(new ActivityLog("Plugin Updated", "PluginUpdated", bob.Id)
            {
                Overview = "Updated Plugin",
                ShortOverview = "Plugin"
            });
            context.ActivityLogs.Add(new ActivityLog("AC\\DC Concert", "Other", bob.Id));
            context.ActivityLogs.Add(new ActivityLog("ACDC Concert", "Other", bob.Id));
            context.SaveChanges();
        }

        _manager = new ActivityManager(CreateDbContextFactory());
    }

    [Theory]
    [InlineData(nameof(ActivityLogQuery.Name), "LOGGED")]
    [InlineData(nameof(ActivityLogQuery.Name), "logged")]
    [InlineData(nameof(ActivityLogQuery.Overview), "FROM chrome")]
    [InlineData(nameof(ActivityLogQuery.Overview), "from CHROME")]
    [InlineData(nameof(ActivityLogQuery.ShortOverview), "CHROME")]
    [InlineData(nameof(ActivityLogQuery.ShortOverview), "chrome")]
    [InlineData(nameof(ActivityLogQuery.Type), "SESSIONstarted")]
    [InlineData(nameof(ActivityLogQuery.Type), "sessionSTARTED")]
    [InlineData(nameof(ActivityLogQuery.Username), "ALICE")]
    [InlineData(nameof(ActivityLogQuery.Username), "alice")]
    public async Task GetPagedResultAsync_FilterAgainstMixedCaseValue_IgnoresAsciiCase(string filter, string term)
    {
        Assert.Equal(["Admin Logged In"], await GetNamesAsync(filter, term).ConfigureAwait(true));
    }

    [Fact]
    public async Task GetPagedResultAsync_BackslashInFilter_IsLiteral()
    {
        Assert.Equal(["AC\\DC Concert"], await GetNamesAsync(nameof(ActivityLogQuery.Name), "AC\\DC").ConfigureAwait(true));
    }

    [Theory]
    [InlineData("Plug%dated", "Plugin Updated")]
    [InlineData("Admin_Logged", "Admin Logged In")]
    public async Task GetPagedResultAsync_PercentAndUnderscoreInFilter_StayWildcards(string term, string expected)
    {
        Assert.Equal([expected], await GetNamesAsync(nameof(ActivityLogQuery.Name), term).ConfigureAwait(true));
    }

    private async Task<string[]> GetNamesAsync(string filter, string term)
    {
        var query = new ActivityLogQuery();
        switch (filter)
        {
            case nameof(ActivityLogQuery.Name):
                query.Name = term;
                break;
            case nameof(ActivityLogQuery.Overview):
                query.Overview = term;
                break;
            case nameof(ActivityLogQuery.ShortOverview):
                query.ShortOverview = term;
                break;
            case nameof(ActivityLogQuery.Type):
                query.Type = term;
                break;
            case nameof(ActivityLogQuery.Username):
                query.Username = term;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
        }

        var result = await _manager.GetPagedResultAsync(query).ConfigureAwait(false);

        return result.Items.Select(e => e.Name).Order(StringComparer.Ordinal).ToArray();
    }
}
