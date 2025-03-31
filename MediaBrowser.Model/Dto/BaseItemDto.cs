#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// This is strictly used as a data transfer object from the api layer.
    /// This holds information about a BaseItem in a format that is convenient for the client.
    /// </summary>
    public class BaseItemDto : IHasProviderIds, IItemDto, IHasServerId
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public string OriginalTitle { get; set; }

        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        /// <value>The server identifier.</value>
        public string ServerId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the etag.
        /// </summary>
        /// <value>The etag.</value>
        public string Etag { get; set; }

        /// <summary>
        /// Gets or sets the type of the source.
        /// </summary>
        /// <value>The type of the source.</value>
        public string SourceType { get; set; }

        /// <summary>
        /// Gets or sets the playlist item identifier.
        /// </summary>
        /// <value>The playlist item identifier.</value>
        public string PlaylistItemId { get; set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        public DateTime? DateCreated { get; set; }

        public DateTime? DateLastMediaAdded { get; set; }

        public ExtraType? ExtraType { get; set; }

        public int? AirsBeforeSeasonNumber { get; set; }

        public int? AirsAfterSeasonNumber { get; set; }

        public int? AirsBeforeEpisodeNumber { get; set; }

        public bool? CanDelete { get; set; }

        public bool? CanDownload { get; set; }

        public bool? HasLyrics { get; set; }

        public bool? HasSubtitles { get; set; }

        public string PreferredMetadataLanguage { get; set; }

        public string PreferredMetadataCountryCode { get; set; }

        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the name of the sort.
        /// </summary>
        /// <value>The name of the sort.</value>
        public string SortName { get; set; }

        public string ForcedSortName { get; set; }

        /// <summary>
        /// Gets or sets the video3 D format.
        /// </summary>
        /// <value>The video3 D format.</value>
        public Video3DFormat? Video3DFormat { get; set; }

        /// <summary>
        /// Gets or sets the premiere date.
        /// </summary>
        /// <value>The premiere date.</value>
        public DateTime? PremiereDate { get; set; }

        /// <summary>
        /// Gets or sets the external urls.
        /// </summary>
        /// <value>The external urls.</value>
        public ExternalUrl[] ExternalUrls { get; set; }

        /// <summary>
        /// Gets or sets the media versions.
        /// </summary>
        /// <value>The media versions.</value>
        public MediaSourceInfo[] MediaSources { get; set; }

        /// <summary>
        /// Gets or sets the critic rating.
        /// </summary>
        /// <value>The critic rating.</value>
        public float? CriticRating { get; set; }

        public string[] ProductionLocations { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        public bool? EnableMediaSourceDisplay { get; set; }

        /// <summary>
        /// Gets or sets the official rating.
        /// </summary>
        /// <value>The official rating.</value>
        public string OfficialRating { get; set; }

        /// <summary>
        /// Gets or sets the custom rating.
        /// </summary>
        /// <value>The custom rating.</value>
        public string CustomRating { get; set; }

        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public Guid? ChannelId { get; set; }

        public string ChannelName { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        public string Overview { get; set; }

        /// <summary>
        /// Gets or sets the taglines.
        /// </summary>
        /// <value>The taglines.</value>
        public string[] Taglines { get; set; }

        /// <summary>
        /// Gets or sets the genres.
        /// </summary>
        /// <value>The genres.</value>
        public string[] Genres { get; set; }

        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        public float? CommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the cumulative run time ticks.
        /// </summary>
        /// <value>The cumulative run time ticks.</value>
        public long? CumulativeRunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the play access.
        /// </summary>
        /// <value>The play access.</value>
        public PlayAccess? PlayAccess { get; set; }

        /// <summary>
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>The aspect ratio.</value>
        public string AspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the production year.
        /// </summary>
        /// <value>The production year.</value>
        public int? ProductionYear { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is place holder.
        /// </summary>
        /// <value><c>null</c> if [is place holder] contains no value, <c>true</c> if [is place holder]; otherwise, <c>false</c>.</value>
        public bool? IsPlaceHolder { get; set; }

        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>The number.</value>
        public string Number { get; set; }

        public string ChannelNumber { get; set; }

        /// <summary>
        /// Gets or sets the index number.
        /// </summary>
        /// <value>The index number.</value>
        public int? IndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the index number end.
        /// </summary>
        /// <value>The index number end.</value>
        public int? IndexNumberEnd { get; set; }

        /// <summary>
        /// Gets or sets the parent index number.
        /// </summary>
        /// <value>The parent index number.</value>
        public int? ParentIndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the trailer urls.
        /// </summary>
        /// <value>The trailer urls.</value>
        public IReadOnlyCollection<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        public Dictionary<string, string> ProviderIds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is HD.
        /// </summary>
        /// <value><c>null</c> if [is HD] contains no value, <c>true</c> if [is HD]; otherwise, <c>false</c>.</value>
        public bool? IsHD { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        public bool? IsFolder { get; set; }

        /// <summary>
        /// Gets or sets the parent id.
        /// </summary>
        /// <value>The parent id.</value>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public BaseItemKind Type { get; set; }

        /// <summary>
        /// Gets or sets the people.
        /// </summary>
        /// <value>The people.</value>
        public BaseItemPerson[] People { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        public NameGuidPair[] Studios { get; set; }

        public NameGuidPair[] GenreItems { get; set; }

        /// <summary>
        /// Gets or sets whether the item has a logo, this will hold the Id of the Parent that has one.
        /// </summary>
        /// <value>The parent logo item id.</value>
        public Guid? ParentLogoItemId { get; set; }

        /// <summary>
        /// Gets or sets whether the item has any backdrops, this will hold the Id of the Parent that has one.
        /// </summary>
        /// <value>The parent backdrop item id.</value>
        public Guid? ParentBackdropItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent backdrop image tags.
        /// </summary>
        /// <value>The parent backdrop image tags.</value>
        public string[] ParentBackdropImageTags { get; set; }

        /// <summary>
        /// Gets or sets the local trailer count.
        /// </summary>
        /// <value>The local trailer count.</value>
        public int? LocalTrailerCount { get; set; }

        /// <summary>
        /// Gets or sets the user data for this item based on the user it's being requested for.
        /// </summary>
        /// <value>The user data.</value>
        public UserItemDataDto UserData { get; set; }

        /// <summary>
        /// Gets or sets the recursive item count.
        /// </summary>
        /// <value>The recursive item count.</value>
        public int? RecursiveItemCount { get; set; }

        /// <summary>
        /// Gets or sets the child count.
        /// </summary>
        /// <value>The child count.</value>
        public int? ChildCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        public string SeriesName { get; set; }

        /// <summary>
        /// Gets or sets the series id.
        /// </summary>
        /// <value>The series id.</value>
        public Guid? SeriesId { get; set; }

        /// <summary>
        /// Gets or sets the season identifier.
        /// </summary>
        /// <value>The season identifier.</value>
        public Guid? SeasonId { get; set; }

        /// <summary>
        /// Gets or sets the special feature count.
        /// </summary>
        /// <value>The special feature count.</value>
        public int? SpecialFeatureCount { get; set; }

        /// <summary>
        /// Gets or sets the display preferences id.
        /// </summary>
        /// <value>The display preferences id.</value>
        public string DisplayPreferencesId { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the air time.
        /// </summary>
        /// <value>The air time.</value>
        public string AirTime { get; set; }

        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        public DayOfWeek[] AirDays { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public string[] Tags { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio, after image enhancements.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        public double? PrimaryImageAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the artists.
        /// </summary>
        /// <value>The artists.</value>
        public IReadOnlyList<string> Artists { get; set; }

        /// <summary>
        /// Gets or sets the artist items.
        /// </summary>
        /// <value>The artist items.</value>
        public NameGuidPair[] ArtistItems { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string Album { get; set; }

        /// <summary>
        /// Gets or sets the type of the collection.
        /// </summary>
        /// <value>The type of the collection.</value>
        public CollectionType? CollectionType { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        /// <value>The display order.</value>
        public string DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the album id.
        /// </summary>
        /// <value>The album id.</value>
        public Guid? AlbumId { get; set; }

        /// <summary>
        /// Gets or sets the album image tag.
        /// </summary>
        /// <value>The album image tag.</value>
        public string AlbumPrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the series primary image tag.
        /// </summary>
        /// <value>The series primary image tag.</value>
        public string SeriesPrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        public string AlbumArtist { get; set; }

        /// <summary>
        /// Gets or sets the album artists.
        /// </summary>
        /// <value>The album artists.</value>
        public NameGuidPair[] AlbumArtists { get; set; }

        /// <summary>
        /// Gets or sets the name of the season.
        /// </summary>
        /// <value>The name of the season.</value>
        public string SeasonName { get; set; }

        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        public MediaStream[] MediaStreams { get; set; }

        /// <summary>
        /// Gets or sets the type of the video.
        /// </summary>
        /// <value>The type of the video.</value>
        public VideoType? VideoType { get; set; }

        /// <summary>
        /// Gets or sets the part count.
        /// </summary>
        /// <value>The part count.</value>
        public int? PartCount { get; set; }

        public int? MediaSourceCount { get; set; }

        /// <summary>
        /// Gets or sets the image tags.
        /// </summary>
        /// <value>The image tags.</value>
        public Dictionary<ImageType, string> ImageTags { get; set; }

        /// <summary>
        /// Gets or sets the backdrop image tags.
        /// </summary>
        /// <value>The backdrop image tags.</value>
        public string[] BackdropImageTags { get; set; }

        /// <summary>
        /// Gets or sets the screenshot image tags.
        /// </summary>
        /// <value>The screenshot image tags.</value>
        public string[] ScreenshotImageTags { get; set; }

        /// <summary>
        /// Gets or sets the parent logo image tag.
        /// </summary>
        /// <value>The parent logo image tag.</value>
        public string ParentLogoImageTag { get; set; }

        /// <summary>
        /// Gets or sets whether the item has fan art, this will hold the Id of the Parent that has one.
        /// </summary>
        /// <value>The parent art item id.</value>
        public Guid? ParentArtItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent art image tag.
        /// </summary>
        /// <value>The parent art image tag.</value>
        public string ParentArtImageTag { get; set; }

        /// <summary>
        /// Gets or sets the series thumb image tag.
        /// </summary>
        /// <value>The series thumb image tag.</value>
        public string SeriesThumbImageTag { get; set; }

        /// <summary>
        /// Gets or sets the blurhashes for the image tags.
        /// Maps image type to dictionary mapping image tag to blurhash value.
        /// </summary>
        /// <value>The blurhashes.</value>
        public Dictionary<ImageType, Dictionary<string, string>> ImageBlurHashes { get; set; }

        /// <summary>
        /// Gets or sets the series studio.
        /// </summary>
        /// <value>The series studio.</value>
        public string SeriesStudio { get; set; }

        /// <summary>
        /// Gets or sets the parent thumb item id.
        /// </summary>
        /// <value>The parent thumb item id.</value>
        public Guid? ParentThumbItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent thumb image tag.
        /// </summary>
        /// <value>The parent thumb image tag.</value>
        public string ParentThumbImageTag { get; set; }

        /// <summary>
        /// Gets or sets the parent primary image item identifier.
        /// </summary>
        /// <value>The parent primary image item identifier.</value>
        public string ParentPrimaryImageItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent primary image tag.
        /// </summary>
        /// <value>The parent primary image tag.</value>
        public string ParentPrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the chapters.
        /// </summary>
        /// <value>The chapters.</value>
        public List<ChapterInfo> Chapters { get; set; }

        /// <summary>
        /// Gets or sets the trickplay manifest.
        /// </summary>
        /// <value>The trickplay manifest.</value>
        public Dictionary<string, Dictionary<int, TrickplayInfo>> Trickplay { get; set; }

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        public LocationType? LocationType { get; set; }

        /// <summary>
        /// Gets or sets the type of the iso.
        /// </summary>
        /// <value>The type of the iso.</value>
        public IsoType? IsoType { get; set; }

        /// <summary>
        /// Gets or sets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [DefaultValue(MediaType.Unknown)]
        public MediaType MediaType { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the locked fields.
        /// </summary>
        /// <value>The locked fields.</value>
        public MetadataField[] LockedFields { get; set; }

        /// <summary>
        /// Gets or sets the trailer count.
        /// </summary>
        /// <value>The trailer count.</value>
        public int? TrailerCount { get; set; }

        /// <summary>
        /// Gets or sets the movie count.
        /// </summary>
        /// <value>The movie count.</value>
        public int? MovieCount { get; set; }

        /// <summary>
        /// Gets or sets the series count.
        /// </summary>
        /// <value>The series count.</value>
        public int? SeriesCount { get; set; }

        public int? ProgramCount { get; set; }

        /// <summary>
        /// Gets or sets the episode count.
        /// </summary>
        /// <value>The episode count.</value>
        public int? EpisodeCount { get; set; }

        /// <summary>
        /// Gets or sets the song count.
        /// </summary>
        /// <value>The song count.</value>
        public int? SongCount { get; set; }

        /// <summary>
        /// Gets or sets the album count.
        /// </summary>
        /// <value>The album count.</value>
        public int? AlbumCount { get; set; }

        public int? ArtistCount { get; set; }

        /// <summary>
        /// Gets or sets the music video count.
        /// </summary>
        /// <value>The music video count.</value>
        public int? MusicVideoCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable internet providers].
        /// </summary>
        /// <value><c>true</c> if [enable internet providers]; otherwise, <c>false</c>.</value>
        public bool? LockData { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public string CameraMake { get; set; }

        public string CameraModel { get; set; }

        public string Software { get; set; }

        public double? ExposureTime { get; set; }

        public double? FocalLength { get; set; }

        public ImageOrientation? ImageOrientation { get; set; }

        public double? Aperture { get; set; }

        public double? ShutterSpeed { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? Altitude { get; set; }

        public int? IsoSpeedRating { get; set; }

        /// <summary>
        /// Gets or sets the series timer identifier.
        /// </summary>
        /// <value>The series timer identifier.</value>
        public string SeriesTimerId { get; set; }

        /// <summary>
        /// Gets or sets the program identifier.
        /// </summary>
        /// <value>The program identifier.</value>
        public string ProgramId { get; set; }

        /// <summary>
        /// Gets or sets the channel primary image tag.
        /// </summary>
        /// <value>The channel primary image tag.</value>
        public string ChannelPrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the start date of the recording, in UTC.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the completion percentage.
        /// </summary>
        /// <value>The completion percentage.</value>
        public double? CompletionPercentage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is repeat.
        /// </summary>
        /// <value><c>true</c> if this instance is repeat; otherwise, <c>false</c>.</value>
        public bool? IsRepeat { get; set; }

        /// <summary>
        /// Gets or sets the episode title.
        /// </summary>
        /// <value>The episode title.</value>
        public string EpisodeTitle { get; set; }

        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType? ChannelType { get; set; }

        /// <summary>
        /// Gets or sets the audio.
        /// </summary>
        /// <value>The audio.</value>
        public ProgramAudio? Audio { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is movie.
        /// </summary>
        /// <value><c>true</c> if this instance is movie; otherwise, <c>false</c>.</value>
        public bool? IsMovie { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>true</c> if this instance is sports; otherwise, <c>false</c>.</value>
        public bool? IsSports { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is series.
        /// </summary>
        /// <value><c>true</c> if this instance is series; otherwise, <c>false</c>.</value>
        public bool? IsSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is live.
        /// </summary>
        /// <value><c>true</c> if this instance is live; otherwise, <c>false</c>.</value>
        public bool? IsLive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is news.
        /// </summary>
        /// <value><c>true</c> if this instance is news; otherwise, <c>false</c>.</value>
        public bool? IsNews { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>true</c> if this instance is kids; otherwise, <c>false</c>.</value>
        public bool? IsKids { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is premiere.
        /// </summary>
        /// <value><c>true</c> if this instance is premiere; otherwise, <c>false</c>.</value>
        public bool? IsPremiere { get; set; }

        /// <summary>
        /// Gets or sets the timer identifier.
        /// </summary>
        /// <value>The timer identifier.</value>
        public string TimerId { get; set; }

        /// <summary>
        /// Gets or sets the gain required for audio normalization.
        /// </summary>
        /// <value>The gain required for audio normalization.</value>
        public float? NormalizationGain { get; set; }

        /// <summary>
        /// Gets or sets the current program.
        /// </summary>
        /// <value>The current program.</value>
        public BaseItemDto CurrentProgram { get; set; }
    }
}
