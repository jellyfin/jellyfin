using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// This is strictly used as a data transfer object from the api layer.
    /// This holds information about a BaseItem in a format that is convenient for the client.
    /// </summary>
    public class BaseItemDto : IHasProviderIds, INotifyPropertyChanged, IItemDto
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        public DateTime? DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the name of the sort.
        /// </summary>
        /// <value>The name of the sort.</value>
        public string SortName { get; set; }

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
        /// Gets or sets the critic rating.
        /// </summary>
        /// <value>The critic rating.</value>
        public float? CriticRating { get; set; }

        /// <summary>
        /// Gets or sets the game system.
        /// </summary>
        /// <value>The game system.</value>
        public string GameSystem { get; set; }
        
        /// <summary>
        /// Gets or sets the critic rating summary.
        /// </summary>
        /// <value>The critic rating summary.</value>
        public string CriticRatingSummary { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

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
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        public string Overview { get; set; }

        /// <summary>
        /// Gets or sets the taglines.
        /// </summary>
        /// <value>The taglines.</value>
        public List<string> Taglines { get; set; }

        /// <summary>
        /// Gets or sets the genres.
        /// </summary>
        /// <value>The genres.</value>
        public List<string> Genres { get; set; }

        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        public float? CommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the vote count.
        /// </summary>
        /// <value>The vote count.</value>
        public int? VoteCount { get; set; }

        /// <summary>
        /// Gets or sets the original run time ticks.
        /// </summary>
        /// <value>The original run time ticks.</value>
        public long? OriginalRunTimeTicks { get; set; }

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
        /// Gets or sets the season count.
        /// </summary>
        /// <value>The season count.</value>
        public int? SeasonCount { get; set; }
        
        /// <summary>
        /// Gets or sets the players supported by a game.
        /// </summary>
        /// <value>The players.</value>
        public int? Players { get; set; }

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
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the soundtrack ids.
        /// </summary>
        /// <value>The soundtrack ids.</value>
        public string[] SoundtrackIds { get; set; }
        
        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        public Dictionary<string, string> ProviderIds { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is HD.
        /// </summary>
        /// <value><c>null</c> if [is HD] contains no value, <c>true</c> if [is HD]; otherwise, <c>false</c>.</value>
        public bool? IsHD { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        public bool IsFolder { get; set; }

        /// <summary>
        /// Gets or sets the parent id.
        /// </summary>
        /// <value>The parent id.</value>
        public string ParentId { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the people.
        /// </summary>
        /// <value>The people.</value>
        public BaseItemPerson[] People { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        public StudioDto[] Studios { get; set; }

        /// <summary>
        /// If the item does not have a logo, this will hold the Id of the Parent that has one.
        /// </summary>
        /// <value>The parent logo item id.</value>
        public string ParentLogoItemId { get; set; }

        /// <summary>
        /// If the item does not have any backdrops, this will hold the Id of the Parent that has one.
        /// </summary>
        /// <value>The parent backdrop item id.</value>
        public string ParentBackdropItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent backdrop image tags.
        /// </summary>
        /// <value>The parent backdrop image tags.</value>
        public List<Guid> ParentBackdropImageTags { get; set; }

        /// <summary>
        /// Gets or sets the local trailer count.
        /// </summary>
        /// <value>The local trailer count.</value>
        public int? LocalTrailerCount { get; set; }

        /// <summary>
        /// User data for this item based on the user it's being requested for
        /// </summary>
        /// <value>The user data.</value>
        public UserItemDataDto UserData { get; set; }

        /// <summary>
        /// Gets or sets the recently added item count.
        /// </summary>
        /// <value>The recently added item count.</value>
        public int? RecentlyAddedItemCount { get; set; }

        /// <summary>
        /// Gets or sets the played percentage.
        /// </summary>
        /// <value>The played percentage.</value>
        public double? PlayedPercentage { get; set; }

        /// <summary>
        /// Gets or sets the recursive item count.
        /// </summary>
        /// <value>The recursive item count.</value>
        public int? RecursiveItemCount { get; set; }

        /// <summary>
        /// Gets or sets the recursive unplayed item count.
        /// </summary>
        /// <value>The recursive unplayed item count.</value>
        public int? RecursiveUnplayedItemCount { get; set; }

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
        public string SeriesId { get; set; }

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
        public SeriesStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the air time.
        /// </summary>
        /// <value>The air time.</value>
        public string AirTime { get; set; }

        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        public List<DayOfWeek> AirDays { get; set; }

        /// <summary>
        /// Gets or sets the index options.
        /// </summary>
        /// <value>The index options.</value>
        public string[] IndexOptions { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio, after image enhancements.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        public double? PrimaryImageAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio, before image enhancements.
        /// </summary>
        /// <value>The original primary image aspect ratio.</value>
        public double? OriginalPrimaryImageAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the artists.
        /// </summary>
        /// <value>The artists.</value>
        public List<string> Artists { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string Album { get; set; }

        /// <summary>
        /// Gets or sets the album id.
        /// </summary>
        /// <value>The album id.</value>
        public string AlbumId { get; set; }
        /// <summary>
        /// Gets or sets the album image tag.
        /// </summary>
        /// <value>The album image tag.</value>
        public Guid? AlbumPrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        public string AlbumArtist { get; set; }

        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        public List<MediaStream> MediaStreams { get; set; }

        /// <summary>
        /// Gets or sets the type of the video.
        /// </summary>
        /// <value>The type of the video.</value>
        public VideoType? VideoType { get; set; }

        /// <summary>
        /// Gets or sets the display type of the media.
        /// </summary>
        /// <value>The display type of the media.</value>
        public string DisplayMediaType { get; set; }

        /// <summary>
        /// Gets or sets the part count.
        /// </summary>
        /// <value>The part count.</value>
        public int? PartCount { get; set; }

        /// <summary>
        /// Determines whether the specified type is type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is type; otherwise, <c>false</c>.</returns>
        public bool IsType(Type type)
        {
            return IsType(type.Name);
        }

        /// <summary>
        /// Determines whether the specified type is type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is type; otherwise, <c>false</c>.</returns>
        public bool IsType(string type)
        {
            return Type.Equals(type, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the image tags.
        /// </summary>
        /// <value>The image tags.</value>
        public Dictionary<ImageType, Guid> ImageTags { get; set; }

        /// <summary>
        /// Gets or sets the backdrop image tags.
        /// </summary>
        /// <value>The backdrop image tags.</value>
        public List<Guid> BackdropImageTags { get; set; }

        /// <summary>
        /// Gets or sets the screenshot image tags.
        /// </summary>
        /// <value>The screenshot image tags.</value>
        public List<Guid> ScreenshotImageTags { get; set; }

        /// <summary>
        /// Gets or sets the parent logo image tag.
        /// </summary>
        /// <value>The parent logo image tag.</value>
        public Guid? ParentLogoImageTag { get; set; }

        /// <summary>
        /// If the item does not have a art, this will hold the Id of the Parent that has one.
        /// </summary>
        /// <value>The parent art item id.</value>
        public string ParentArtItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent art image tag.
        /// </summary>
        /// <value>The parent art image tag.</value>
        public Guid? ParentArtImageTag { get; set; }
        
        /// <summary>
        /// Gets or sets the chapters.
        /// </summary>
        /// <value>The chapters.</value>
        public List<ChapterInfoDto> Chapters { get; set; }

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        public LocationType LocationType { get; set; }

        /// <summary>
        /// Gets or sets the type of the iso.
        /// </summary>
        /// <value>The type of the iso.</value>
        public IsoType? IsoType { get; set; }

        /// <summary>
        /// Gets or sets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        public string MediaType { get; set; }

        /// <summary>
        /// Gets or sets the overview HTML.
        /// </summary>
        /// <value>The overview HTML.</value>
        public string OverviewHtml { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        public string HomePageUrl { get; set; }

        /// <summary>
        /// Gets or sets the production locations.
        /// </summary>
        /// <value>The production locations.</value>
        public List<string> ProductionLocations { get; set; }

        /// <summary>
        /// Gets or sets the budget.
        /// </summary>
        /// <value>The budget.</value>
        public double? Budget { get; set; }

        /// <summary>
        /// Gets or sets the revenue.
        /// </summary>
        /// <value>The revenue.</value>
        public double? Revenue { get; set; }

        /// <summary>
        /// Gets or sets the locked fields.
        /// </summary>
        /// <value>The locked fields.</value>
        public List<MetadataFields> LockedFields { get; set; }

        public int? AdultVideoCount { get; set; }
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
        /// <summary>
        /// Gets or sets the episode count.
        /// </summary>
        /// <value>The episode count.</value>
        public int? EpisodeCount { get; set; }
        /// <summary>
        /// Gets or sets the game count.
        /// </summary>
        /// <value>The game count.</value>
        public int? GameCount { get; set; }
        /// <summary>
        /// Gets or sets the trailer count.
        /// </summary>
        /// <value>The trailer count.</value>
        public int? TrailerCount { get; set; }
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
        /// <summary>
        /// Gets or sets the music video count.
        /// </summary>
        /// <value>The music video count.</value>
        public int? MusicVideoCount { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether [enable internet providers].
        /// </summary>
        /// <value><c>true</c> if [enable internet providers]; otherwise, <c>false</c>.</value>
        public bool? EnableInternetProviders { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance can resume.
        /// </summary>
        /// <value><c>true</c> if this instance can resume; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool CanResume
        {
            get { return UserData != null && UserData.PlaybackPositionTicks > 0; }
        }

        /// <summary>
        /// Gets the resume position ticks.
        /// </summary>
        /// <value>The resume position ticks.</value>
        [IgnoreDataMember]
        public long ResumePositionTicks
        {
            get { return UserData == null ? 0 : UserData.PlaybackPositionTicks; }
        }

        /// <summary>
        /// Gets the backdrop count.
        /// </summary>
        /// <value>The backdrop count.</value>
        [IgnoreDataMember]
        public int BackdropCount
        {
            get { return BackdropImageTags == null ? 0 : BackdropImageTags.Count; }
        }

        /// <summary>
        /// Gets the screenshot count.
        /// </summary>
        /// <value>The screenshot count.</value>
        [IgnoreDataMember]
        public int ScreenshotCount
        {
            get { return ScreenshotImageTags == null ? 0 : ScreenshotImageTags.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has banner.
        /// </summary>
        /// <value><c>true</c> if this instance has banner; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasBanner
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Banner); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has art.
        /// </summary>
        /// <value><c>true</c> if this instance has art; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasArtImage
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Art); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has logo.
        /// </summary>
        /// <value><c>true</c> if this instance has logo; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasLogo
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Logo); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has thumb.
        /// </summary>
        /// <value><c>true</c> if this instance has thumb; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasThumb
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Thumb); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has primary image.
        /// </summary>
        /// <value><c>true</c> if this instance has primary image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasPrimaryImage
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Primary); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has disc image.
        /// </summary>
        /// <value><c>true</c> if this instance has disc image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasDiscImage
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Disc); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has box image.
        /// </summary>
        /// <value><c>true</c> if this instance has box image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasBoxImage
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Box); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has box image.
        /// </summary>
        /// <value><c>true</c> if this instance has box image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasBoxRearImage
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.BoxRear); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has menu image.
        /// </summary>
        /// <value><c>true</c> if this instance has menu image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasMenuImage
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Menu); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is video.
        /// </summary>
        /// <value><c>true</c> if this instance is video; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsVideo
        {
            get { return string.Equals(MediaType, Entities.MediaType.Video, StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is audio.
        /// </summary>
        /// <value><c>true</c> if this instance is audio; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsAudio
        {
            get { return string.Equals(MediaType, Entities.MediaType.Audio, StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is game.
        /// </summary>
        /// <value><c>true</c> if this instance is game; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsGame
        {
            get { return string.Equals(MediaType, Entities.MediaType.Game, StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is person.
        /// </summary>
        /// <value><c>true</c> if this instance is person; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsPerson
        {
            get { return string.Equals(Type, "Person", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsRoot
        {
            get { return string.Equals(Type, "AggregateFolder", StringComparison.OrdinalIgnoreCase); }
        }

        [IgnoreDataMember]
        public bool IsMusicGenre
        {
            get { return string.Equals(Type, "MusicGenre", StringComparison.OrdinalIgnoreCase); }
        }

        [IgnoreDataMember]
        public bool IsGameGenre
        {
            get { return string.Equals(Type, "GameGenre", StringComparison.OrdinalIgnoreCase); }
        }

        [IgnoreDataMember]
        public bool IsGenre
        {
            get { return string.Equals(Type, "Genre", StringComparison.OrdinalIgnoreCase); }
        }

        [IgnoreDataMember]
        public bool IsArtist
        {
            get { return string.Equals(Type, "Artist", StringComparison.OrdinalIgnoreCase); }
        }

        [IgnoreDataMember]
        public bool IsStudio
        {
            get { return string.Equals(Type, "Studio", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
