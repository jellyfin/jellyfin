namespace Jellyfin.Data.Enums;

/// <summary>
/// These represent sort orders.
/// </summary>
public enum ItemSortBy
{
    /// <summary>
    /// Default sort order.
    /// </summary>
    Default = 0,

    /// <summary>
    /// The aired episode order.
    /// </summary>
    AiredEpisodeOrder = 1,

    /// <summary>
    /// The album.
    /// </summary>
    Album = 2,

    /// <summary>
    /// The album artist.
    /// </summary>
    AlbumArtist = 3,

    /// <summary>
    /// The artist.
    /// </summary>
    Artist = 4,

    /// <summary>
    /// The date created.
    /// </summary>
    DateCreated = 5,

    /// <summary>
    /// The official rating.
    /// </summary>
    OfficialRating = 6,

    /// <summary>
    /// The date played.
    /// </summary>
    DatePlayed = 7,

    /// <summary>
    /// The premiere date.
    /// </summary>
    PremiereDate = 8,

    /// <summary>
    /// The start date.
    /// </summary>
    StartDate = 9,

    /// <summary>
    /// The sort name.
    /// </summary>
    SortName = 10,

    /// <summary>
    /// The name.
    /// </summary>
    Name = 11,

    /// <summary>
    /// The random.
    /// </summary>
    Random = 12,

    /// <summary>
    /// The runtime.
    /// </summary>
    Runtime = 13,

    /// <summary>
    /// The community rating.
    /// </summary>
    CommunityRating = 14,

    /// <summary>
    /// The production year.
    /// </summary>
    ProductionYear = 15,

    /// <summary>
    /// The play count.
    /// </summary>
    PlayCount = 16,

    /// <summary>
    /// The critic rating.
    /// </summary>
    CriticRating = 17,

    /// <summary>
    /// The IsFolder boolean.
    /// </summary>
    IsFolder = 18,

    /// <summary>
    /// The IsUnplayed boolean.
    /// </summary>
    IsUnplayed = 19,

    /// <summary>
    /// The IsPlayed boolean.
    /// </summary>
    IsPlayed = 20,

    /// <summary>
    /// The series sort.
    /// </summary>
    SeriesSortName = 21,

    /// <summary>
    /// The video bitrate.
    /// </summary>
    VideoBitRate = 22,

    /// <summary>
    /// The air time.
    /// </summary>
    AirTime = 23,

    /// <summary>
    /// The studio.
    /// </summary>
    Studio = 24,

    /// <summary>
    /// The IsFavouriteOrLiked boolean.
    /// </summary>
    IsFavoriteOrLiked = 25,

    /// <summary>
    /// The last content added date.
    /// </summary>
    DateLastContentAdded = 26,

    /// <summary>
    /// The series last played date.
    /// </summary>
    SeriesDatePlayed = 27,

    /// <summary>
    /// The parent index number.
    /// </summary>
    ParentIndexNumber = 28,

    /// <summary>
    /// The index number.
    /// </summary>
    IndexNumber = 29,
}
