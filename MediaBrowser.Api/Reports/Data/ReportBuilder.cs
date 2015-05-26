using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Reports
{
	/// <summary> A report builder. </summary>
	/// <seealso cref="T:MediaBrowser.Api.Reports.ReportBuilderBase"/>
	public class ReportBuilder : ReportBuilderBase
	{

		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportBuilder class. </summary>
		/// <param name="libraryManager"> Manager for library. </param>
		public ReportBuilder(ILibraryManager libraryManager)
			: base(libraryManager)
		{
		}

		private Func<bool, string> GetBoolString = s => s == true ? "x" : "";

		public ReportResult GetReportResult(BaseItem[] items, ReportViewType reportRowType, BaseReportRequest request)
		{
			List<HeaderMetadata> headersMetadata = this.GetFilteredReportHeaderMetadata(reportRowType, request);

			var headers = GetReportHeaders(reportRowType, headersMetadata);
			var rows = GetReportRows(items, headersMetadata);

			ReportResult result = new ReportResult { Headers = headers };
			HeaderMetadata groupBy = ReportHelper.GetHeaderMetadataType(request.GroupBy);
			int i = headers.FindIndex(x => x.FieldName == groupBy);
			if (groupBy != HeaderMetadata.None && i > 0)
			{
				var rowsGroup = rows.SelectMany(x => x.Columns[i].Name.Split(';'), (x, g) => new { Genre = g.Trim(), Rows = x })
					.GroupBy(x => x.Genre)
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

		public List<ReportHeader> GetReportHeaders(ReportViewType reportRowType, BaseReportRequest request)
		{
			List<ReportHeader> headersMetadata = this.GetReportHeaders(reportRowType);
			if (request != null && !string.IsNullOrEmpty(request.ReportColumns))
			{
				List<HeaderMetadata> headersMetadataFiltered = this.GetFilteredReportHeaderMetadata(reportRowType, request);
				foreach (ReportHeader reportHeader in headersMetadata)
				{
					if (!headersMetadataFiltered.Contains(reportHeader.FieldName))
					{
						reportHeader.Visible = false;
					}
				}


			}

			return headersMetadata;
		}

		public List<ReportHeader> GetReportHeaders(ReportViewType reportRowType, List<HeaderMetadata> headersMetadata = null)
		{
			if (headersMetadata == null)
				headersMetadata = this.GetDefaultReportHeaderMetadata(reportRowType);

			List<ReportOptions<BaseItem>> options = new List<ReportOptions<BaseItem>>();
			foreach (HeaderMetadata header in headersMetadata)
			{
				options.Add(GetReportOption(header));
			}


			List<ReportHeader> headers = new List<ReportHeader>();
			foreach (ReportOptions<BaseItem> option in options)
			{
				headers.Add(option.Header);
			}
			return headers;
		}

		private List<ReportRow> GetReportRows(IEnumerable<BaseItem> items, List<HeaderMetadata> headersMetadata)
		{
			List<ReportOptions<BaseItem>> options = new List<ReportOptions<BaseItem>>();
			foreach (HeaderMetadata header in headersMetadata)
			{
				options.Add(GetReportOption(header));
			}

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
				IsUnidentified = item.IsUnidentified,
				HasLocalTrailer = hasTrailers != null ? hasTrailers.GetTrailerIds().Count() > 0 : false,
				HasImageTagsPrimary = (item.ImageInfos != null && item.ImageInfos.Count(n => n.Type == ImageType.Primary) > 0),
				HasImageTagsBackdrop = (item.ImageInfos != null && item.ImageInfos.Count(n => n.Type == ImageType.Backdrop) > 0),
				HasImageTagsLogo = (item.ImageInfos != null && item.ImageInfos.Count(n => n.Type == ImageType.Logo) > 0),
				HasSpecials = hasSpecialFeatures != null ? hasSpecialFeatures.SpecialFeatureIds.Count > 0 : false,
				HasSubtitles = video != null ? video.HasSubtitles : false,
				RowType = ReportHelper.GetRowType(item.GetClientTypeName())
			};
			return rRow;
		}
		public List<HeaderMetadata> GetFilteredReportHeaderMetadata(ReportViewType reportRowType, BaseReportRequest request)
		{
			if (request != null && !string.IsNullOrEmpty(request.ReportColumns))
			{
				var s = request.ReportColumns.Split('|').Select(x => ReportHelper.GetHeaderMetadataType(x)).Where(x => x != HeaderMetadata.None);
				return s.ToList();
			}
			else
				return this.GetDefaultReportHeaderMetadata(reportRowType);

		}

		public List<HeaderMetadata> GetDefaultReportHeaderMetadata(ReportViewType reportRowType)
		{
			switch (reportRowType)
			{
				case ReportViewType.Season:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
						HeaderMetadata.Series,
						HeaderMetadata.Season,
						HeaderMetadata.SeasonNumber,
						HeaderMetadata.DateAdded,
						HeaderMetadata.Year,
						HeaderMetadata.Genres
					};

				case ReportViewType.Series:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
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

				case ReportViewType.MusicAlbum:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
						HeaderMetadata.Name,
						HeaderMetadata.AlbumArtist,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Tracks,
						HeaderMetadata.Year,
						HeaderMetadata.Genres
					};

				case ReportViewType.MusicArtist:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
						HeaderMetadata.MusicArtist,
						HeaderMetadata.Countries,
						HeaderMetadata.DateAdded,
						HeaderMetadata.Year,
						HeaderMetadata.Genres
					};

				case ReportViewType.Game:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
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

				case ReportViewType.Movie:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
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

				case ReportViewType.Book:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
						HeaderMetadata.Name,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating
					};

				case ReportViewType.BoxSet:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
						HeaderMetadata.Name,
						HeaderMetadata.DateAdded,
						HeaderMetadata.ReleaseDate,
						HeaderMetadata.Year,
						HeaderMetadata.Genres,
						HeaderMetadata.ParentalRating,
						HeaderMetadata.CommunityRating,
						HeaderMetadata.Trailers
					};

				case ReportViewType.Audio:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
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

				case ReportViewType.Episode:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
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

				case ReportViewType.Video:
				case ReportViewType.MusicVideo:
				case ReportViewType.Trailer:
				case ReportViewType.BaseItem:
				default:
					return new List<HeaderMetadata>
					{
						HeaderMetadata.StatusImage,
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
		private ReportOptions<BaseItem> GetReportOption(HeaderMetadata header, string sortField = "")
		{
			ReportHeader reportHeader = new ReportHeader
			{
				HeaderFieldType = ReportFieldType.String,
				SortField = sortField,
				Type = "",
				ItemViewType = ItemViewType.None
			};

			Func<BaseItem, ReportRow, object> column = null;
			Func<BaseItem, object> itemId = null;
			HeaderMetadata internalHeader = header;

			switch (header)
			{
				case HeaderMetadata.StatusImage:
					reportHeader.ItemViewType = ItemViewType.StatusImage;
					internalHeader = HeaderMetadata.Status;
					reportHeader.CanGroup = false;
					break;

				case HeaderMetadata.Name:
					column = (i, r) => i.Name;
					reportHeader.ItemViewType = ItemViewType.Detail;
					reportHeader.SortField = "SortName";
					break;

				case HeaderMetadata.DateAdded:
					column = (i, r) => i.DateCreated;
					reportHeader.SortField = "DateCreated,SortName";
					reportHeader.HeaderFieldType = ReportFieldType.DateTime;
					reportHeader.Type = "";
					break;

				case HeaderMetadata.PremiereDate:
				case HeaderMetadata.ReleaseDate:
					column = (i, r) => i.PremiereDate;
					reportHeader.HeaderFieldType = ReportFieldType.DateTime;
					reportHeader.SortField = "ProductionYear,PremiereDate,SortName";
					break;

				case HeaderMetadata.Runtime:
					column = (i, r) => this.GetRuntimeDateTime(i.RunTimeTicks);
					reportHeader.HeaderFieldType = ReportFieldType.Minutes;
					reportHeader.SortField = "Runtime,SortName";
					break;

				case HeaderMetadata.PlayCount:
					reportHeader.HeaderFieldType = ReportFieldType.Int;
					break;

				case HeaderMetadata.Season:
					column = (i, r) => this.GetEpisode(i);
					reportHeader.ItemViewType = ItemViewType.Detail;
					reportHeader.SortField = "SortName";
					break;

				case HeaderMetadata.SeasonNumber:
					column = (i, r) => this.GetObject<Season, string>(i, (x) => x.IndexNumber == null ? "" : x.IndexNumber.ToString());
					reportHeader.SortField = "IndexNumber";
					reportHeader.HeaderFieldType = ReportFieldType.Int;
					break;

				case HeaderMetadata.Series:
					column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
					reportHeader.ItemViewType = ItemViewType.Detail;
					reportHeader.SortField = "SeriesSortName,SortName";
					break;

				case HeaderMetadata.EpisodeSeries:
					column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
					reportHeader.ItemViewType = ItemViewType.Detail;
					itemId = (i) =>
					{
						Series series = this.GetObject<Episode, Series>(i, (x) => x.Series);
						if (series == null)
							return string.Empty;
						return series.Id;
					};
					reportHeader.SortField = "SeriesSortName,SortName";
					internalHeader = HeaderMetadata.Series;
					break;

				case HeaderMetadata.EpisodeSeason:
					column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
					reportHeader.ItemViewType = ItemViewType.Detail;
					itemId = (i) =>
					{
						Season season = this.GetObject<Episode, Season>(i, (x) => x.Season);
						if (season == null)
							return string.Empty;
						return season.Id;
					};
					reportHeader.SortField = "SortName";
					internalHeader = HeaderMetadata.Season;
					break;

				case HeaderMetadata.Network:
					column = (i, r) => this.GetListAsString(i.Studios);
					itemId = (i) => this.GetStudioID(i.Studios.FirstOrDefault());
					reportHeader.ItemViewType = ItemViewType.ItemByNameDetails;
					reportHeader.SortField = "Studio,SortName";
					break;

				case HeaderMetadata.Year:
					column = (i, r) => this.GetSeriesProductionYear(i);
					reportHeader.SortField = "ProductionYear,PremiereDate,SortName";
					break;

				case HeaderMetadata.ParentalRating:
					column = (i, r) => i.OfficialRating;
					reportHeader.SortField = "OfficialRating,SortName";
					break;

				case HeaderMetadata.CommunityRating:
					column = (i, r) => i.CommunityRating;
					reportHeader.SortField = "CommunityRating,SortName";
					break;

				case HeaderMetadata.Trailers:
					column = (i, r) => this.GetBoolString(r.HasLocalTrailer);
					reportHeader.ItemViewType = ItemViewType.TrailersImage;
					break;

				case HeaderMetadata.Specials:
					column = (i, r) => this.GetBoolString(r.HasSpecials);
					reportHeader.ItemViewType = ItemViewType.SpecialsImage;
					break;

				case HeaderMetadata.GameSystem:
					column = (i, r) => this.GetObject<Game, string>(i, (x) => x.GameSystem);
					reportHeader.SortField = "GameSystem,SortName";
					break;

				case HeaderMetadata.Players:
					column = (i, r) => this.GetObject<Game, int?>(i, (x) => x.PlayersSupported);
					reportHeader.SortField = "Players,GameSystem,SortName";
					break;

				case HeaderMetadata.AlbumArtist:
					column = (i, r) => this.GetObject<MusicAlbum, string>(i, (x) => x.AlbumArtist);
					itemId = (i) => this.GetPersonID(this.GetObject<MusicAlbum, string>(i, (x) => x.AlbumArtist));
					reportHeader.ItemViewType = ItemViewType.Detail;
					reportHeader.SortField = "AlbumArtist,Album,SortName";

					break;
				case HeaderMetadata.MusicArtist:
					column = (i, r) => this.GetObject<MusicArtist, string>(i, (x) => x.GetLookupInfo().Name);
					reportHeader.ItemViewType = ItemViewType.Detail;
					reportHeader.SortField = "AlbumArtist,Album,SortName";
					internalHeader = HeaderMetadata.AlbumArtist;
					break;
				case HeaderMetadata.AudioAlbumArtist:
					column = (i, r) => this.GetListAsString(this.GetObject<Audio, List<string>>(i, (x) => x.AlbumArtists));
					reportHeader.SortField = "AlbumArtist,Album,SortName";
					internalHeader = HeaderMetadata.AlbumArtist;
					break;

				case HeaderMetadata.AudioAlbum:
					column = (i, r) => this.GetObject<Audio, string>(i, (x) => x.Album);
					reportHeader.SortField = "Album,SortName";
					internalHeader = HeaderMetadata.Album;
					break;

				case HeaderMetadata.Countries:
					column = (i, r) => this.GetListAsString(this.GetObject<IHasProductionLocations, List<string>>(i, (x) => x.ProductionLocations));
					break;

				case HeaderMetadata.Disc:
					column = (i, r) => i.ParentIndexNumber;
					break;

				case HeaderMetadata.Track:
					column = (i, r) => i.IndexNumber;
					break;

				case HeaderMetadata.Tracks:
					column = (i, r) => this.GetObject<MusicAlbum, List<Audio>>(i, (x) => x.Tracks.ToList(), new List<Audio>()).Count();
					break;

				case HeaderMetadata.Audio:
					column = (i, r) => this.GetAudioStream(i);
					break;

				case HeaderMetadata.EmbeddedImage:
					break;

				case HeaderMetadata.Video:
					column = (i, r) => this.GetVideoStream(i);
					break;

				case HeaderMetadata.Resolution:
					column = (i, r) => this.GetVideoResolution(i);
					break;

				case HeaderMetadata.Subtitles:
					column = (i, r) => this.GetBoolString(r.HasSubtitles);
					reportHeader.ItemViewType = ItemViewType.SubtitleImage;
					break;

				case HeaderMetadata.Genres:
					column = (i, r) => this.GetListAsString(i.Genres);
					break;

			}

			string headerName = "";
			if (internalHeader != HeaderMetadata.None)
			{
				string localHeader = "Header" + internalHeader.ToString();
				headerName = internalHeader != HeaderMetadata.None ? ReportHelper.GetJavaScriptLocalizedString(localHeader) : "";
				if (string.Compare(localHeader, headerName, StringComparison.CurrentCultureIgnoreCase) == 0)
					headerName = ReportHelper.GetServerLocalizedString(localHeader);
			}

			reportHeader.Name = headerName;
			reportHeader.FieldName = header;
			ReportOptions<BaseItem> option = new ReportOptions<BaseItem>()
			{
				Header = reportHeader,
				Column = column,
				ItemID = itemId
			};
			return option;
		}
	}
}
