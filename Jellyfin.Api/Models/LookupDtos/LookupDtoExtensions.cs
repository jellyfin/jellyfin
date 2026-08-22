// The mapper has to keep carrying the deprecated members for as long as they are part of the contract.
#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using Internal = MediaBrowser.Controller.Providers;

namespace Jellyfin.Api.Models.LookupDtos;

/// <summary>
/// Maps the remote search request bodies onto the lookup info the metadata providers consume.
/// </summary>
/// <remarks>
/// The two are kept apart so the provider types can change without altering the API contract.
/// The DTOs therefore mirror the serialized shape of the lookup info exactly and must not drift from it.
/// </remarks>
public static class LookupDtoExtensions
{
    /// <summary>
    /// Converts a remote search query.
    /// </summary>
    /// <param name="query">The query to convert.</param>
    /// <param name="convert">Converts the contained search info.</param>
    /// <typeparam name="TDto">The type of the search info to convert.</typeparam>
    /// <typeparam name="TInfo">The type of the lookup info to convert to.</typeparam>
    /// <returns>The converted query.</returns>
    public static Internal.RemoteSearchQuery<TInfo> ToLookupQuery<TDto, TInfo>(this RemoteSearchQuery<TDto> query, Func<TDto, TInfo> convert)
        where TDto : ItemLookupInfo
        where TInfo : Internal.ItemLookupInfo
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(convert);

        return new Internal.RemoteSearchQuery<TInfo>
        {
            // Left null when the client sends no search info, so the behaviour matches binding it directly.
            SearchInfo = query.SearchInfo is null ? null! : convert(query.SearchInfo),
            ItemId = query.ItemId,
            SearchProviderName = query.SearchProviderName,
            IncludeDisabledProviders = query.IncludeDisabledProviders
        };
    }

    /// <summary>
    /// Converts movie lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.MovieInfo ToLookupInfo(this MovieInfo dto)
        => CopyTo(dto, new Internal.MovieInfo());

    /// <summary>
    /// Converts trailer lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.TrailerInfo ToLookupInfo(this TrailerInfo dto)
        => CopyTo(dto, new Internal.TrailerInfo());

    /// <summary>
    /// Converts series lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.SeriesInfo ToLookupInfo(this SeriesInfo dto)
        => CopyTo(dto, new Internal.SeriesInfo());

    /// <summary>
    /// Converts box set lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.BoxSetInfo ToLookupInfo(this BoxSetInfo dto)
        => CopyTo(dto, new Internal.BoxSetInfo());

    /// <summary>
    /// Converts person lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.PersonLookupInfo ToLookupInfo(this PersonLookupInfo dto)
        => CopyTo(dto, new Internal.PersonLookupInfo());

    /// <summary>
    /// Converts book lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.BookInfo ToLookupInfo(this BookInfo dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var info = CopyTo(dto, new Internal.BookInfo());
        info.SeriesName = dto.SeriesName;

        return info;
    }

    /// <summary>
    /// Converts music video lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.MusicVideoInfo ToLookupInfo(this MusicVideoInfo dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var info = CopyTo(dto, new Internal.MusicVideoInfo());
        info.Artists = dto.Artists;

        return info;
    }

    /// <summary>
    /// Converts artist lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.ArtistInfo ToLookupInfo(this ArtistInfo dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var info = CopyTo(dto, new Internal.ArtistInfo());
        // Null-forgiving because a client can send an explicit null, exactly as it could before the split.
        info.SongInfos = ToLookupInfos(dto.SongInfos)!;

        return info;
    }

    /// <summary>
    /// Converts album lookup info.
    /// </summary>
    /// <param name="dto">The lookup info to convert.</param>
    /// <returns>The converted lookup info.</returns>
    public static Internal.AlbumInfo ToLookupInfo(this AlbumInfo dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var info = CopyTo(dto, new Internal.AlbumInfo());
        info.AlbumArtists = dto.AlbumArtists;
        info.ArtistProviderIds = dto.ArtistProviderIds;
        // Null-forgiving because a client can send an explicit null, exactly as it could before the split.
        info.SongInfos = ToLookupInfos(dto.SongInfos)!;

        return info;
    }

    private static List<Internal.SongInfo>? ToLookupInfos(List<SongInfo>? songInfos)
    {
        if (songInfos is null)
        {
            return null;
        }

        var infos = new List<Internal.SongInfo>(songInfos.Count);
        foreach (var songInfo in songInfos)
        {
            var info = CopyTo(songInfo, new Internal.SongInfo());
            info.Album = songInfo.Album;
            info.AlbumArtists = songInfo.AlbumArtists;
            info.Artists = songInfo.Artists;

            infos.Add(info);
        }

        return infos;
    }

    private static TInfo CopyTo<TInfo>(ItemLookupInfo dto, TInfo info)
        where TInfo : Internal.ItemLookupInfo
    {
        ArgumentNullException.ThrowIfNull(dto);

        info.Name = dto.Name;
        info.OriginalTitle = dto.OriginalTitle;
        info.Path = dto.Path;
        info.MetadataLanguage = dto.MetadataLanguage;
        info.MetadataCountryCode = dto.MetadataCountryCode;
        info.ProviderIds = dto.ProviderIds;
        info.Year = dto.Year;
        info.IndexNumber = dto.IndexNumber;
        info.ParentIndexNumber = dto.ParentIndexNumber;
        info.PremiereDate = dto.PremiereDate;
        info.IsAutomated = dto.IsAutomated;

        return info;
    }
}
