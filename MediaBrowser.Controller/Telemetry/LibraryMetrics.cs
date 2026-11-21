using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Telemetry;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Telemetry;

/// <summary>
/// Library instruments published on <see cref="JellyfinTelemetry.Meter"/>.
/// </summary>
public static class LibraryMetrics
{
    private const string ChangeTag = "jellyfin.library.change";

    private const string ChangeAdded = "added";
    private const string ChangeUpdated = "updated";
    private const string ChangeRemoved = "removed";

    private static readonly ConcurrentDictionary<BaseItemKind, string> _kindNames = new();

    private static readonly Counter<long> _changes = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.library.changes",
        "{item}",
        "Library items added, updated and removed.");

    /// <summary>
    /// Records that items were added to the library.
    /// </summary>
    /// <param name="items">The items that were added.</param>
    public static void OnItemsAdded(IReadOnlyList<BaseItem> items) => Record(items, ChangeAdded);

    /// <summary>
    /// Records that items in the library were updated.
    /// </summary>
    /// <param name="items">The items that were updated.</param>
    public static void OnItemsUpdated(IReadOnlyList<BaseItem> items) => Record(items, ChangeUpdated);

    /// <summary>
    /// Records that an item was removed from the library.
    /// </summary>
    /// <param name="item">The item that was removed.</param>
    public static void OnItemRemoved(BaseItem item) => Record([item], ChangeRemoved);

    private static void Record(IReadOnlyList<BaseItem> items, string change)
    {
        // Reading SourceType can hit the live TV manager, which is not worth doing when nothing is listening.
        if (!_changes.Enabled)
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            // Live TV guide entries churn constantly and would swamp the counter, the change events
            // skip them for the same reason.
            if (item is null || item.SourceType != SourceType.Library)
            {
                continue;
            }

            _changes.Add(
                1,
                new KeyValuePair<string, object?>(ChangeTag, change),
                new KeyValuePair<string, object?>(TelemetryTags.ItemKindTag, KindName(item.GetBaseItemKind())));
        }
    }

    private static string KindName(BaseItemKind kind) => _kindNames.GetOrAdd(kind, static k => k.ToString());
}
