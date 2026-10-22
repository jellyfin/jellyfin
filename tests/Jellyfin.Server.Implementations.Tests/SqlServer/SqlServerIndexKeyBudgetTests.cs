using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds the indexes of the SQL Server model to the size of their keys. An entry of an index that is not clustered holds
/// at most 1700 bytes of key, and a clustered key at most 900. The server creates an index whose declared key is longer
/// with a warning, and refuses each row whose key is, so every such index is listed here with the room its strings have.
/// A new index over the limit fails until it is listed, and a listed index that fits again fails until it is removed.
/// </summary>
public sealed partial class SqlServerIndexKeyBudgetTests
{
    private const int NonClusteredLimit = 1700;
    private const int ClusteredLimit = 900;

    // The indexes over the limit, with their strings that are indexed without a bound of their own, and the room those
    // strings have, in UTF-16 code units, with the other columns of the key at their bound.
    internal static readonly Dictionary<string, (string[] TextColumns, int Room)> OverBudget = new(StringComparer.Ordinal)
    {
        ["IX_BaseItems_Path"] = (["Path"], 850),
        ["IX_BaseItems_Name"] = (["Name"], 850),
        ["IX_BaseItems_PresentationUniqueKey"] = (["PresentationUniqueKey"], 850),
        ["IX_BaseItems_SeriesName"] = (["SeriesName"], 850),
        ["IX_Peoples_Name"] = (["Name"], 850),
        ["IX_Peoples_NameLower"] = (["NameLower"], 850),
        ["IX_ItemValues_Type_CleanValue"] = (["CleanValue"], 848),
        ["IX_MediaStreamInfos_StreamType_ItemId_Language_IsExternal"] = (["Language"], 839),
        ["IX_BaseItems_IsFolder_TopParentId_IsVirtualItem_PresentationUniqueKey_DateCreated"] = (["PresentationUniqueKey"], 837),
        ["IX_BaseItems_MediaType_TopParentId_IsVirtualItem_PresentationUniqueKey"] = (["PresentationUniqueKey"], 777),
        ["IX_BaseItemProviders_ProviderId_ItemId_ProviderValue"] = (["ProviderValue"], 714),
        ["IX_BaseItems_Type_CleanName"] = (["CleanName"], 594),
        ["IX_BaseItems_Type_SeriesPresentationUniqueKey_IsFolder_IsVirtualItem"] = (["SeriesPresentationUniqueKey"], 593),
        ["IX_BaseItems_Type_SeriesPresentationUniqueKey_ParentIndexNumber_IndexNumber"] = (["SeriesPresentationUniqueKey"], 590),
        ["IX_BaseItems_Type_TopParentId_SortName"] = (["SortName"], 586),
        ["IX_BaseItems_Type_TopParentId_PresentationUniqueKey"] = (["PresentationUniqueKey"], 586),
        ["IX_BaseItems_Type_TopParentId_IsVirtualItem_PresentationUniqueKey_DateCreated"] = (["PresentationUniqueKey"], 581),
        ["IX_BaseItems_Type_SeriesPresentationUniqueKey_PresentationUniqueKey_SortName"] = (["SeriesPresentationUniqueKey", "PresentationUniqueKey", "SortName"], 594)
    };

    private static readonly Lazy<IRelationalModel> _model = new(() =>
    {
        using var context = OfflineContexts.SqlServer();
        return context.GetService<IDesignTimeModel>().Model.GetRelationalModel();
    });

    [Fact]
    public void NonClusteredIndexes_OverTheLimit_AreExactlyTheListedOnes()
    {
        var over = _model.Value.Tables
            .SelectMany(t => t.Indexes)
            .Where(i => DeclaredBytes(i.Columns) > NonClusteredLimit)
            .Select(i => i.Name)
            .Order(StringComparer.Ordinal);

        Assert.Equal(OverBudget.Keys.Order(StringComparer.Ordinal), over);
    }

    [Fact]
    public void ListedIndexes_HaveTheListedStringsAndRoom()
    {
        var indexes = _model.Value.Tables.SelectMany(t => t.Indexes).ToDictionary(i => i.Name, StringComparer.Ordinal);

        Assert.All(OverBudget, entry =>
        {
            var index = indexes[entry.Key];
            var text = index.Columns.Where(c => c.StoreType == IndexedTextConvention.ColumnType).ToList();
            var others = index.Columns.Except(text).ToList();

            Assert.Equal(entry.Value.TextColumns, text.Select(c => c.Name));
            Assert.Equal(entry.Value.Room, (NonClusteredLimit - DeclaredBytes(others)) / 2);
        });
    }

    [Fact]
    public void PrimaryKeys_FitTheLimitOfAClusteredKey()
    {
        var keys = _model.Value.Tables.Select(t => t.PrimaryKey!).ToList();

        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.True(DeclaredBytes(key.Columns) <= ClusteredLimit, $"{key.Name} takes {DeclaredBytes(key.Columns)} bytes"));
    }

    [Fact]
    public void UniqueConstraints_FitTheLimitOfAnIndex()
    {
        var constraints = _model.Value.Tables.SelectMany(t => t.UniqueConstraints).Where(c => !c.GetIsPrimaryKey()).ToList();

        Assert.All(constraints, constraint => Assert.True(DeclaredBytes(constraint.Columns) <= NonClusteredLimit, $"{constraint.Name} takes {DeclaredBytes(constraint.Columns)} bytes"));
    }

    // The bytes the server counts for the columns of a key: the bit columns share their bytes, 8 to a byte.
    private static int DeclaredBytes(IEnumerable<IColumn> columns)
    {
        var bits = 0;
        var bytes = 0;
        foreach (var column in columns)
        {
            if (string.Equals(column.StoreType, "bit", StringComparison.Ordinal))
            {
                bits++;
            }
            else
            {
                bytes += Bytes(column.StoreType);
            }
        }

        return bytes + ((bits + 7) / 8);
    }

    private static int Bytes(string storeType)
    {
        var match = SizedType().Match(storeType);
        var name = match.Success ? match.Groups["name"].Value : storeType;
        var size = match.Success ? match.Groups["size"].Value : null;
        return (name, size) switch
        {
            ("nvarchar" or "nchar", { } n) when n != "max" => 2 * int.Parse(n, CultureInfo.InvariantCulture),
            ("varchar" or "char" or "binary" or "varbinary", { } n) when n != "max" => int.Parse(n, CultureInfo.InvariantCulture),
            ("tinyint", null) => 1,
            ("smallint", null) => 2,
            ("int" or "real", null) => 4,
            ("bigint" or "float" or "datetime" or "datetime2", null) => 8,
            ("datetime2", { } p) => int.Parse(p, CultureInfo.InvariantCulture) switch { < 3 => 6, < 5 => 7, _ => 8 },
            ("datetimeoffset", _) => 10,
            ("date", null) => 3,
            ("uniqueidentifier", null) => 16,
            _ => throw new InvalidOperationException($"The size of {storeType} in a key is not known to the test; add it.")
        };
    }

    [GeneratedRegex(@"^(?<name>[a-z0-9]+)\((?<size>[^),]+)(,[^)]*)?\)$")]
    private static partial Regex SizedType();
}
