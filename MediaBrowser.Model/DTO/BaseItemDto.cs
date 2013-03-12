using System.ComponentModel;
using MediaBrowser.Model.Entities;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// This is strictly used as a data transfer object from the api layer.
    /// This holds information about a BaseItem in a format that is convenient for the client.
    /// </summary>
    [ProtoContract]
    public class BaseItemDto : IHasProviderIds, INotifyPropertyChanged, IItemDto
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ProtoMember(2)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        [ProtoMember(3)]
        public DateTime? DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the name of the sort.
        /// </summary>
        /// <value>The name of the sort.</value>
        [ProtoMember(4)]
        public string SortName { get; set; }

        /// <summary>
        /// Gets or sets the premiere date.
        /// </summary>
        /// <value>The premiere date.</value>
        [ProtoMember(5)]
        public DateTime? PremiereDate { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [ProtoMember(6)]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the official rating.
        /// </summary>
        /// <value>The official rating.</value>
        [ProtoMember(7)]
        public string OfficialRating { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        [ProtoMember(8)]
        public string Overview { get; set; }

        /// <summary>
        /// Gets or sets the taglines.
        /// </summary>
        /// <value>The taglines.</value>
        [ProtoMember(9)]
        public List<string> Taglines { get; set; }

        /// <summary>
        /// Gets or sets the genres.
        /// </summary>
        /// <value>The genres.</value>
        [ProtoMember(10)]
        public List<string> Genres { get; set; }

        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        [ProtoMember(11)]
        public float? CommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        [ProtoMember(12)]
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>The aspect ratio.</value>
        [ProtoMember(13)]
        public string AspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the production year.
        /// </summary>
        /// <value>The production year.</value>
        [ProtoMember(14)]
        public int? ProductionYear { get; set; }

        /// <summary>
        /// Gets or sets the index number.
        /// </summary>
        /// <value>The index number.</value>
        [ProtoMember(15)]
        public int? IndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the parent index number.
        /// </summary>
        /// <value>The parent index number.</value>
        [ProtoMember(16)]
        public int? ParentIndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the trailer urls.
        /// </summary>
        /// <value>The trailer urls.</value>
        [ProtoMember(17)]
        public List<string> TrailerUrls { get; set; }

        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        [ProtoMember(18)]
        public Dictionary<string, string> ProviderIds { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        [ProtoMember(24)]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        [ProtoMember(25)]
        public bool IsFolder { get; set; }

        /// <summary>
        /// Gets or sets the parent id.
        /// </summary>
        /// <value>The parent id.</value>
        [ProtoMember(28)]
        public string ParentId { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [ProtoMember(29)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the people.
        /// </summary>
        /// <value>The people.</value>
        [ProtoMember(30)]
        public BaseItemPerson[] People { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        [ProtoMember(31)]
        public List<string> Studios { get; set; }

        /// <summary>
        /// If the item does not have a logo, this will hold the Id of the Parent that has one.
        /// </summary>
        /// <value>The parent logo item id.</value>
        [ProtoMember(32)]
        public string ParentLogoItemId { get; set; }

        /// <summary>
        /// If the item does not have any backdrops, this will hold the Id of the Parent that has one.
        /// </summary>
        /// <value>The parent backdrop item id.</value>
        [ProtoMember(33)]
        public string ParentBackdropItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent backdrop image tags.
        /// </summary>
        /// <value>The parent backdrop image tags.</value>
        [ProtoMember(34)]
        public List<Guid> ParentBackdropImageTags { get; set; }

        /// <summary>
        /// Gets or sets the local trailer count.
        /// </summary>
        /// <value>The local trailer count.</value>
        [ProtoMember(35)]
        public int? LocalTrailerCount { get; set; }

        /// <summary>
        /// User data for this item based on the user it's being requested for
        /// </summary>
        /// <value>The user data.</value>
        [ProtoMember(36)]
        public UserItemDataDto UserData { get; set; }

        /// <summary>
        /// Gets or sets the recently added item count.
        /// </summary>
        /// <value>The recently added item count.</value>
        [ProtoMember(38)]
        public int? RecentlyAddedItemCount { get; set; }

        /// <summary>
        /// Gets or sets the played percentage.
        /// </summary>
        /// <value>The played percentage.</value>
        [ProtoMember(41)]
        public double? PlayedPercentage { get; set; }

        /// <summary>
        /// Gets or sets the recursive item count.
        /// </summary>
        /// <value>The recursive item count.</value>
        [ProtoMember(42)]
        public int? RecursiveItemCount { get; set; }

        /// <summary>
        /// Gets or sets the child count.
        /// </summary>
        /// <value>The child count.</value>
        [ProtoMember(44)]
        public int? ChildCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        [ProtoMember(45)]
        public string SeriesName { get; set; }

        /// <summary>
        /// Gets or sets the series id.
        /// </summary>
        /// <value>The series id.</value>
        [ProtoMember(46)]
        public string SeriesId { get; set; }

        /// <summary>
        /// Gets or sets the special feature count.
        /// </summary>
        /// <value>The special feature count.</value>
        [ProtoMember(48)]
        public int? SpecialFeatureCount { get; set; }

        /// <summary>
        /// Gets or sets the display preferences.
        /// </summary>
        /// <value>The display preferences.</value>
        [ProtoMember(49)]
        public DisplayPreferences DisplayPreferences { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        [ProtoMember(50)]
        public SeriesStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the air time.
        /// </summary>
        /// <value>The air time.</value>
        [ProtoMember(51)]
        public string AirTime { get; set; }

        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        [ProtoMember(52)]
        public List<DayOfWeek> AirDays { get; set; }

        /// <summary>
        /// Gets or sets the index options.
        /// </summary>
        /// <value>The index options.</value>
        [ProtoMember(54)]
        public string[] IndexOptions { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        [ProtoMember(55)]
        public double? PrimaryImageAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>The artist.</value>
        [ProtoMember(56)]
        public string Artist { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        [ProtoMember(57)]
        public string Album { get; set; }

        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        [ProtoMember(58)]
        public string AlbumArtist { get; set; }

        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        [ProtoMember(59)]
        public List<MediaStream> MediaStreams { get; set; }

        /// <summary>
        /// Gets or sets the type of the video.
        /// </summary>
        /// <value>The type of the video.</value>
        [ProtoMember(60)]
        public VideoType? VideoType { get; set; }

        /// <summary>
        /// Gets or sets the display type of the media.
        /// </summary>
        /// <value>The display type of the media.</value>
        [ProtoMember(61)]
        public string DisplayMediaType { get; set; }

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
        [ProtoMember(62)]
        public Dictionary<ImageType, Guid> ImageTags { get; set; }

        /// <summary>
        /// Gets or sets the backdrop image tags.
        /// </summary>
        /// <value>The backdrop image tags.</value>
        [ProtoMember(63)]
        public List<Guid> BackdropImageTags { get; set; }

        /// <summary>
        /// Gets or sets the parent logo image tag.
        /// </summary>
        /// <value>The parent logo image tag.</value>
        [ProtoMember(64)]
        public Guid? ParentLogoImageTag { get; set; }

        /// <summary>
        /// Gets or sets the chapters.
        /// </summary>
        /// <value>The chapters.</value>
        [ProtoMember(65)]
        public List<ChapterInfoDto> Chapters { get; set; }

        /// <summary>
        /// Gets or sets the video format.
        /// </summary>
        /// <value>The video format.</value>
        [ProtoMember(66)]
        public VideoFormat? VideoFormat { get; set; }

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        [ProtoMember(67)]
        public LocationType LocationType { get; set; }

        /// <summary>
        /// Gets or sets the type of the iso.
        /// </summary>
        /// <value>The type of the iso.</value>
        [ProtoMember(68)]
        public IsoType? IsoType { get; set; }

        /// <summary>
        /// Gets or sets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [ProtoMember(69)]
        public string MediaType { get; set; }

        /// <summary>
        /// Gets or sets the overview HTML.
        /// </summary>
        /// <value>The overview HTML.</value>
        [ProtoMember(70)]
        public string OverviewHtml { get; set; }
        
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
        public bool HasTrailer
        {
            get { return LocalTrailerCount > 0 || (TrailerUrls != null && TrailerUrls.Count > 0); }
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

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
