#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using Jellyfin.Extensions;

namespace Jellyfin.Server.Migrations.Routines;

internal sealed class LegacyItemValue
{
    public LegacyItemValue(Guid itemValueId, int type, string value)
    {
        ItemValueId = itemValueId;
        Type = type;
        Value = value;

        // Computed rather than read, because the stored clean value can be stale.
        CleanValue = value.GetCleanValue();
    }

    public Guid ItemValueId { get; }

    public int Type { get; }

    public string Value { get; }

    public string CleanValue { get; }
}
