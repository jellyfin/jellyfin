#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;

namespace Jellyfin.Server.Migrations.Routines;

internal sealed class LegacyItemValueLink
{
    public LegacyItemValueLink(LegacyItemValue value, Guid itemId, string? itemType)
    {
        Value = value;
        ItemId = itemId;
        ItemType = itemType;
    }

    public LegacyItemValue Value { get; }

    public Guid ItemId { get; }

    public string? ItemType { get; }
}
