#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

public class BaseItemKindCache
{
    private static Lazy<FrozenDictionary<BaseItemKind, string>>? _kindToTypeName;
    private static Lazy<FrozenDictionary<string, BaseItemKind>>? _typeNameToKind;

    public IReadOnlyList<BaseItemKind> MusicGenreTypes { get; } = [
        BaseItemKind.Audio,
        BaseItemKind.MusicVideo,
        BaseItemKind.MusicAlbum,
        BaseItemKind.MusicArtist,
    ];

    public string GetTypeNameByKind(BaseItemKind kind, IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _kindToTypeName ??= new Lazy<FrozenDictionary<BaseItemKind, string>>(
            () =>
            {
                using var context = dbProvider.CreateDbContext();
                return context.BaseItemKinds
                    .AsNoTracking()
                    .ToFrozenDictionary(x => (BaseItemKind)x.Kind, x => x.TypeName);
            },
            isThreadSafe: true);

        return _kindToTypeName.Value.TryGetValue(kind, out var typeName)
            ? typeName
            : throw new ArgumentException($"Unknown BaseItemKind: {kind}", nameof(kind));
    }

    public BaseItemKind GetKindByTypeName(string typeName, IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _typeNameToKind ??= new Lazy<FrozenDictionary<string, BaseItemKind>>(
            () =>
            {
                using var context = dbProvider.CreateDbContext();
                return context.BaseItemKinds
                    .AsNoTracking()
                    .ToFrozenDictionary(x => x.TypeName, x => (BaseItemKind)x.Kind);
            },
            isThreadSafe: true);

        return _typeNameToKind.Value.TryGetValue(typeName, out var kind)
            ? kind
            : throw new ArgumentException($"Unknown type name: {typeName}", nameof(typeName));
    }
}
