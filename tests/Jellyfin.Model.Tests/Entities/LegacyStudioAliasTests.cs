using System;
using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Model.Tests.Entities;

/// <summary>
/// Tests the studio names kept on the company enums for older clients.
/// </summary>
public class LegacyStudioAliasTests
{
    /// <summary>
    /// Unlike the other two this one is not an alias: an item's kind is serialized, and where two
    /// members share a value the serializer is free to write either name.
    /// </summary>
    [Fact]
    public void BaseItemKind_Studio_ParsesAndLeavesCompanyItsName()
    {
        Assert.Equal(BaseItemKind.Studio, Enum.Parse<BaseItemKind>("Studio"));
        Assert.NotEqual(BaseItemKind.Company, BaseItemKind.Studio);
        Assert.Equal("\"Company\"", JsonSerializer.Serialize(BaseItemKind.Company, JsonDefaults.Options));
    }

    [Fact]
    public void ItemFields_Studios_IsCompanies()
    {
        Assert.Equal(ItemFields.Companies, Enum.Parse<ItemFields>("Studios"));
    }

    [Fact]
    public void MetadataField_Studios_IsCompanies()
    {
        Assert.Equal(MetadataField.Companies, Enum.Parse<MetadataField>("Studios"));

        // Locked fields go back out on the item, so the name the serializer picks matters.
        Assert.Equal("\"Companies\"", JsonSerializer.Serialize(MetadataField.Companies, JsonDefaults.Options));
    }
}
