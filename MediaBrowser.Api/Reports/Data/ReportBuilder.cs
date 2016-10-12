using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.Reports
{
    /// <summary> A report builder. </summary>
    /// <seealso cref="T:MediaBrowser.Api.Reports.ReportBuilderBase"/>
    public class ReportBuilder : ReportBuilderBase
    {

        #region [Constructors]

        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportBuilder class. </summary>
        /// <param name="libraryManager"> Manager for library. </param>
        public ReportBuilder(ILibraryManager libraryManager)
            : base(libraryManager)
        {
        }

        #endregion

        #region [Public Methods]

        /// <summary> Gets report result. </summary>
        /// <param name="items"> The items. </param>
        /// <param name="request"> The request. </param>
        /// <returns> The report result. </returns>
        public ReportResult GetResult(BaseItem[] items, IReportsQuery request)
        {
            ReportIncludeItemTypes reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
            ReportDisplayType displayType = ReportHelper.GetReportDisplayType(request.DisplayType);

            List<ReportOptions<BaseItem>> options = this.GetReportOptions<BaseItem>(request,
                () => this.GetDefaultHeaderMetadata(reportRowType),
                (hm) => this.GetOption(hm)).Where(x => this.DisplayTypeVisible(x.Header.DisplayType, displayType)).ToList();

            var headers = GetHeaders<BaseItem>(options);
            var rows = GetReportRows(items, options);

            ReportResult result = new ReportResult { Headers = headers };
            HeaderMetadata groupBy = ReportHelper.GetHeaderMetadataType(request.GroupBy);
            int i = headers.FindIndex(x => x.FieldName == groupBy);
            if (groupBy != HeaderMetadata.None && i >= 0)
            {
                var rowsGroup = rows.SelectMany(x => x.Columns[i].Name.Split(';'), (x, g) => new { Group = g.Trim(), Rows = x })
                    .GroupBy(x => x.Group)
                    .OrderBy(x => x.Key)
                    .Select(x => new ReportGroup { Name = x.Key, Rows = x.Select(r => r.Rows).ToList() });

                result.Groups = rowsGroup.ToList();
                result.IsGrouped = true;
            }
            else
            {
                result.Rows = rows;
                result.IsGrouped = false;
            }

            return result;
        }

        #endregion

        #region [Protected Internal Methods]

        /// <summary> Gets the headers. </summary>
        /// <typeparam name="H"> Type of the header. </typeparam>
        /// <param name="request"> The request. </param>
        /// <returns> The headers. </returns>
        /// <seealso cref="M:MediaBrowser.Api.Reports.ReportBuilderBase.GetHeaders{H}(H)"/>
        protected internal override List<ReportHeader> GetHeaders<H>(H request)
        {
            ReportIncludeItemTypes reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
            return this.GetHeaders<BaseItem>(request, () => this.GetDefaultHeaderMetadata(reportRowType), (hm) => this.GetOption(hm));
        }

        #endregion

        #region [Private Methods]

        /// <summary> Gets default report header metadata. </summary>
        /// <param name="reportIncludeItemTypes"> Type of the report row. </param>
        /// <returns> The default report header metadata. </returns>
        private List<HeaderMetadata> GetDefaultHeaderMetadata(ReportIncludeItemTypes reportIncludeItemTypes)
        {
            switch (reportIncludeItemTypes)
            {
                case ReportIncludeItemTypes.Season:
                    return new List<HeaderMetadata>
					{   
                        HeaderMetadata.Status,                     
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Series,
						HeaderMetadata.Season,
						HeaderMetadata.SeasonNumber,
						HeaderMetadata.DateAdded,
						HeaderMetadata.Year,
						HeaderMetadata.Genres
					};

                case ReportIncludeItemTypes.Series:
                    return new List<HeaderMetadata>
					{     
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.Network,
						HeaderMetadata.DateAdded,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating,
						HeaderMetadata.Runtime,
						HeaderMetadata.Trailers,
						HeaderMetadata.Specials
					};

                case ReportIncludeItemTypes.MusicAlbum:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.AlbumArtist,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Tracks,
						HeaderMetadata.Year,
						HeaderMetadata.Genres
					};

                case ReportIncludeItemTypes.MusicArtist:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.MusicArtist,
						HeaderMetadata.Countries,
						HeaderMetadata.DateAdded,
						HeaderMetadata.Year,
						HeaderMetadata.Genres
					};

                case ReportIncludeItemTypes.Game:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.GameSystem,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating,
						HeaderMetadata.Players,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.Trailers
					};

                case ReportIncludeItemTypes.Movie:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating,
						HeaderMetadata.Runtime,
						HeaderMetadata.Video,
						HeaderMetadata.Resolution,
						HeaderMetadata.Audio,
						HeaderMetadata.Subtitles,
						HeaderMetadata.Trailers,
						HeaderMetadata.Specials
					};

                case ReportIncludeItemTypes.Book:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating
					};

                case ReportIncludeItemTypes.BoxSet:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating,
						HeaderMetadata.Trailers
					};

                case ReportIncludeItemTypes.Audio:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.AudioAlbumArtist,
						HeaderMetadata.AudioAlbum,
						HeaderMetadata.Disc,
						HeaderMetadata.Track,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating,
						HeaderMetadata.Runtime,
						HeaderMetadata.Audio
					};

                case ReportIncludeItemTypes.Episode:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.EpisodeSeries,
						HeaderMetadata.Season,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating,
						HeaderMetadata.Runtime,
						HeaderMetadata.Video,
						HeaderMetadata.Resolution,
						HeaderMetadata.Audio,
						HeaderMetadata.Subtitles,
						HeaderMetadata.Trailers,
						HeaderMetadata.Specials
					};

                case ReportIncludeItemTypes.Video:
                case ReportIncludeItemTypes.MusicVideo:
                case ReportIncludeItemTypes.Trailer:
                case ReportIncludeItemTypes.BaseItem:
                default:
                    return new List<HeaderMetadata>
					{
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
						HeaderMetadata.Name,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating,
						HeaderMetadata.Runtime,
						HeaderMetadata.Video,
						HeaderMetadata.Resolution,
						HeaderMetadata.Audio,
						HeaderMetadata.Subtitles,
						HeaderMetadata.Trailers,
						HeaderMetadata.Specials
					};

            }

        }

        /// <summary> Gets report option. </summary>
        /// <param name="header"> The header. </param>
        /// <param name="sortField"> The sort field. </param>
        /// <returns> The report option. </returns>
        private ReportOptions<BaseItem> GetOption(HeaderMetadata header, string sortField = "")
        {
            HeaderMetadata internalHeader = header;

            ReportOptions<BaseItem> option = new ReportOptions<BaseItem>()
            {
                Header = new ReportHeader
                {
                    HeaderFieldType = ReportFieldType.String,
                    SortField = sortField,
                    Type = "",
                    ItemViewType = ItemViewType.None
                }
            };

            switch (header)
            {
                case HeaderMetadata.Status:
                    option.Header.ItemViewType = ItemViewType.StatusImage;
                    internalHeader = HeaderMetadata.Status;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Screen;
                    break;
                case HeaderMetadata.Locked:
                    option.Column = (i, r) => this.GetBoolString(r.HasLockData);
                    option.Header.ItemViewType = ItemViewType.LockDataImage;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Export;
                    break;
                case HeaderMetadata.ImagePrimary:
                    option.Column = (i, r) => this.GetBoolString(r.HasImageTagsPrimary);
                    option.Header.ItemViewType = ItemViewType.TagsPrimaryImage;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Export;
                    break;
                case HeaderMetadata.ImageBackdrop:
                    option.Column = (i, r) => this.GetBoolString(r.HasImageTagsBackdrop);
                    option.Header.ItemViewType = ItemViewType.TagsBackdropImage;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Export;
                    break;
                case HeaderMetadata.ImageLogo:
                    option.Column = (i, r) => this.GetBoolString(r.HasImageTagsLogo);
                    option.Header.ItemViewType = ItemViewType.TagsLogoImage;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Export;
                    break;

                case HeaderMetadata.Name:
                    option.Column = (i, r) => i.Name;
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "SortName";
                    break;

                case HeaderMetadata.DateAdded:
                    option.Column = (i, r) => i.DateCreated;
                    option.Header.SortField = "DateCreated,SortName";
                    option.Header.HeaderFieldType = ReportFieldType.DateTime;
                    option.Header.Type = "";
                    break;

                case HeaderMetadata.PremiereDate:
                case HeaderMetadata.ReleaseDate:
                    option.Column = (i, r) => i.PremiereDate;
                    option.Header.HeaderFieldType = ReportFieldType.DateTime;
                    option.Header.SortField = "ProductionYear,PremiereDate,SortName";
                    break;

                case HeaderMetadata.Runtime:
                    option.Column = (i, r) => this.GetRuntimeDateTime(i.RunTimeTicks);
                    option.Header.HeaderFieldType = ReportFieldType.Minutes;
                    option.Header.SortField = "Runtime,SortName";
                    break;

                case HeaderMetadata.PlayCount:
                    option.Header.HeaderFieldType = ReportFieldType.Int;
                    break;

                case HeaderMetadata.Season:
                    option.Column = (i, r) => this.GetEpisode(i);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "SortName";
                    break;

                case HeaderMetadata.SeasonNumber:
                    option.Column = (i, r) => this.GetObject<Season, string>(i, (x) => x.IndexNumber == null ? "" : x.IndexNumber.ToString());
                    option.Header.SortField = "IndexNumber";
                    option.Header.HeaderFieldType = ReportFieldType.Int;
                    break;

                case HeaderMetadata.Series:
                    option.Column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "SeriesSortName,SortName";
                    break;

                case HeaderMetadata.EpisodeSeries:
                    option.Column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.ItemID = (i) =>
                    {
                        Series series = this.GetObject<Episode, Series>(i, (x) => x.Series);
                        if (series == null)
                            return string.Empty;
                        return series.Id;
                    };
                    option.Header.SortField = "SeriesSortName,SortName";
                    internalHeader = HeaderMetadata.Series;
                    break;

                case HeaderMetadata.EpisodeSeason:
                    option.Column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.ItemID = (i) =>
                    {
                        Season season = this.GetObject<Episode, Season>(i, (x) => x.Season);
                        if (season == null)
                            return string.Empty;
                        return season.Id;
                    };
                    option.Header.SortField = "SortName";
                    internalHeader = HeaderMetadata.Season;
                    break;

                case HeaderMetadata.Network:
                    option.Column = (i, r) => this.GetListAsString(i.Studios);
                    option.ItemID = (i) => this.GetStudioID(i.Studios.FirstOrDefault());
                    option.Header.ItemViewType = ItemViewType.ItemByNameDetails;
                    option.Header.SortField = "Studio,SortName";
                    break;

                case HeaderMetadata.Year:
                    option.Column = (i, r) => this.GetSeriesProductionYear(i);
                    option.Header.SortField = "ProductionYear,PremiereDate,SortName";
                    break;

                case HeaderMetadata.ParentalRating:
                    option.Column = (i, r) => i.OfficialRating;
                    option.Header.SortField = "OfficialRating,SortName";
                    break;

                case HeaderMetadata.CommunityRating:
                    option.Column = (i, r) => i.CommunityRating;
                    option.Header.SortField = "CommunityRating,SortName";
                    break;

                case HeaderMetadata.Trailers:
                    option.Column = (i, r) => this.GetBoolString(r.HasLocalTrailer);
                    option.Header.ItemViewType = ItemViewType.TrailersImage;
                    break;

                case HeaderMetadata.Specials:
                    option.Column = (i, r) => this.GetBoolString(r.HasSpecials);
                    option.Header.ItemViewType = ItemViewType.SpecialsImage;
                    break;

                case HeaderMetadata.GameSystem:
                    option.Column = (i, r) => this.GetObject<Game, string>(i, (x) => x.GameSystem);
                    option.Header.SortField = "GameSystem,SortName";
                    break;

                case HeaderMetadata.Players:
                    option.Column = (i, r) => this.GetObject<Game, int?>(i, (x) => x.PlayersSupported);
                    option.Header.SortField = "Players,GameSystem,SortName";
                    break;

                case HeaderMetadata.AlbumArtist:
                    option.Column = (i, r) => this.GetObject<MusicAlbum, string>(i, (x) => x.AlbumArtist);
                    option.ItemID = (i) => this.GetPersonID(this.GetObject<MusicAlbum, string>(i, (x) => x.AlbumArtist));
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "AlbumArtist,Album,SortName";

                    break;
                case HeaderMetadata.MusicArtist:
                    option.Column = (i, r) => this.GetObject<MusicArtist, string>(i, (x) => x.GetLookupInfo().Name);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "AlbumArtist,Album,SortName";
                    internalHeader = HeaderMetadata.AlbumArtist;
                    break;
                case HeaderMetadata.AudioAlbumArtist:
                    option.Column = (i, r) => this.GetListAsString(this.GetObject<Audio, List<string>>(i, (x) => x.AlbumArtists));
                    option.Header.SortField = "AlbumArtist,Album,SortName";
                    internalHeader = HeaderMetadata.AlbumArtist;
                    break;

                case HeaderMetadata.AudioAlbum:
                    option.Column = (i, r) => this.GetObject<Audio, string>(i, (x) => x.Album);
                    option.Header.SortField = "Album,SortName";
                    internalHeader = HeaderMetadata.Album;
                    break;

                case HeaderMetadata.Disc:
                    option.Column = (i, r) => i.ParentIndexNumber;
                    break;

                case HeaderMetadata.Track:
                    option.Column = (i, r) => i.IndexNumber;
                    break;

                case HeaderMetadata.Tracks:
                    option.Column = (i, r) => this.GetObject<MusicAlbum, List<Audio>>(i, (x) => x.Tracks.ToList(), new List<Audio>()).Count();
                    break;

                case HeaderMetadata.Audio:
                    option.Column = (i, r) => this.GetAudioStream(i);
                    break;

                case HeaderMetadata.EmbeddedImage:
                    break;

                case HeaderMetadata.Video:
                    option.Column = (i, r) => this.GetVideoStream(i);
                    break;

                case HeaderMetadata.Resolution:
                    option.Column = (i, r) => this.GetVideoResolution(i);
                    break;

                case HeaderMetadata.Subtitles:
                    option.Column = (i, r) => this.GetBoolString(r.HasSubtitles);
                    option.Header.ItemViewType = ItemViewType.SubtitleImage;
                    break;

                case HeaderMetadata.Genres:
                    option.Column = (i, r) => this.GetListAsString(i.Genres);
                    break;

            }

            option.Header.Name = GetLocalizedHeader(internalHeader);
            option.Header.FieldName = header;

            return option;
        }

        /// <summary> Gets report rows. </summary>
        /// <param name="items"> The items. </param>
        /// <param name="options"> Options for controlling the operation. </param>
        /// <returns> The report rows. </returns>
        private List<ReportRow> GetReportRows(IEnumerable<BaseItem> items, List<ReportOptions<BaseItem>> options)
        {
            var rows = new List<ReportRow>();

            foreach (BaseItem item in items)
            {
                ReportRow rRow = GetRow(item);
                foreach (ReportOptions<BaseItem> option in options)
                {
                    object itemColumn = option.Column != null ? option.Column(item, rRow) : "";
                    object itemId = option.ItemID != null ? option.ItemID(item) : "";
                    ReportItem rItem = new ReportItem
                    {
                        Name = ReportHelper.ConvertToString(itemColumn, option.Header.HeaderFieldType),
                        Id = ReportHelper.ConvertToString(itemId, ReportFieldType.Object)
                    };
                    rRow.Columns.Add(rItem);
                }

                rows.Add(rRow);
            }

            return rows;
        }

        /// <summary> Gets a row. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The row. </returns>
        private ReportRow GetRow(BaseItem item)
        {
            var hasTrailers = item as IHasTrailers;
            var hasSpecialFeatures = item as IHasSpecialFeatures;
            var video = item as Video;
            ReportRow rRow = new ReportRow
            {
                Id = item.Id.ToString("N"),
                HasLockData = item.IsLocked,
                HasLocalTrailer = hasTrailers != null ? hasTrailers.GetTrailerIds().Count() > 0 : false,
                HasImageTagsPrimary = item.ImageInfos != null && item.ImageInfos.Count(n => n.Type == ImageType.Primary) > 0,
                HasImageTagsBackdrop = item.ImageInfos != null && item.ImageInfos.Count(n => n.Type == ImageType.Backdrop) > 0,
                HasImageTagsLogo = item.ImageInfos != null && item.ImageInfos.Count(n => n.Type == ImageType.Logo) > 0,
                HasSpecials = hasSpecialFeatures != null ? hasSpecialFeatures.SpecialFeatureIds.Count > 0 : false,
                HasSubtitles = video != null ? video.HasSubtitles : false,
                RowType = ReportHelper.GetRowType(item.GetClientTypeName())
            };
            return rRow;
        }

        #endregion

    }
}
