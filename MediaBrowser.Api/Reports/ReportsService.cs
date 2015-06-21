using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using MediaBrowser.Model.Dto;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities.TV;
using System;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Activity;
using MediaBrowser.Controller.Activity;
using System.IO;
using System.Text;

namespace MediaBrowser.Api.Reports
{
	/// <summary> The reports service. </summary>
	/// <seealso cref="T:MediaBrowser.Api.BaseApiService"/>
	public class ReportsService : BaseApiService
	{


		/// <summary> Manager for user. </summary>
		private readonly IUserManager _userManager;

		/// <summary> Manager for library. </summary>
		private readonly ILibraryManager _libraryManager;
		/// <summary> The localization. </summary>
		private readonly ILocalizationManager _localization;

		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportsService class. </summary>
		/// <param name="userManager"> Manager for user. </param>
		/// <param name="libraryManager"> Manager for library. </param>
		/// <param name="localization"> The localization. </param>
		public ReportsService(IUserManager userManager, ILibraryManager libraryManager, ILocalizationManager localization)
		{
			_userManager = userManager;
			_libraryManager = libraryManager;
			_localization = localization;
		}

		/// <summary> Gets the given request. </summary>
		/// <param name="request"> The request. </param>
		/// <returns> A Task&lt;object&gt; </returns>
		public async Task<object> Get(GetReportHeaders request)
		{
			if (string.IsNullOrEmpty(request.IncludeItemTypes))
				return null;

			ReportViewType reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
			ReportBuilder reportBuilder = new ReportBuilder(_libraryManager);
			var reportResult = reportBuilder.GetReportHeaders(reportRowType, request);

			return ToOptimizedResult(reportResult);

		}

		/// <summary> Gets the given request. </summary>
		/// <param name="request"> The request. </param>
		/// <returns> A Task&lt;object&gt; </returns>
		public async Task<object> Get(GetItemReport request)
		{
			if (string.IsNullOrEmpty(request.IncludeItemTypes))
				return null;

			var reportResult = await GetReportResult(request);

			return ToOptimizedResult(reportResult);
		}

		/// <summary> Gets the given request. </summary>
		/// <param name="request"> The request. </param>
		/// <returns> A Task&lt;object&gt; </returns>
		public async Task<object> Get(GetReportDownload request)
		{
			if (string.IsNullOrEmpty(request.IncludeItemTypes))
				return null;

			var headers = new Dictionary<string, string>();
			string fileExtension = "csv";
			string contentType = "text/plain;charset='utf-8'";

			switch (request.ExportType)
			{
				case ReportExportType.CSV:
					break;
				case ReportExportType.Excel:
					contentType = "application/vnd.ms-excel";
					fileExtension = "xls";
					break;
			}

			var filename = "ReportExport." + fileExtension;
			headers["Content-Disposition"] = string.Format("attachment; filename=\"{0}\"", filename);
			headers["Content-Encoding"] = "UTF-8";

			ReportViewType reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
			ReportBuilder reportBuilder = new ReportBuilder(_libraryManager);
			QueryResult<BaseItem> queryResult = await GetQueryResult(request).ConfigureAwait(false);
			ReportResult reportResult = reportBuilder.GetReportResult(queryResult.Items, reportRowType, request);

			reportResult.TotalRecordCount = queryResult.TotalRecordCount;

			string result = string.Empty;
			switch (request.ExportType)
			{
				case ReportExportType.CSV:
					result = new ReportExport().ExportToCsv(reportResult);
					break;
				case ReportExportType.Excel:
					result = new ReportExport().ExportToExcel(reportResult);
					break;
			}

			object ro = ResultFactory.GetResult(result, contentType, headers);
			return ro;
		}

		/// <summary> Gets the given request. </summary>
		/// <param name="request"> The request. </param>
		/// <returns> A Task&lt;object&gt; </returns>
		public async Task<object> Get(GetReportStatistics request)
		{
			if (string.IsNullOrEmpty(request.IncludeItemTypes))
				return null;
			var reportResult = await GetReportStatistic(request);

			return ToOptimizedResult(reportResult);
		}

		/// <summary> Gets report statistic. </summary>
		/// <param name="request"> The request. </param>
		/// <returns> The report statistic. </returns>
		private async Task<ReportStatResult> GetReportStatistic(GetReportStatistics request)
		{
			ReportViewType reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
			QueryResult<BaseItem> queryResult = await GetQueryResult(request).ConfigureAwait(false);

			ReportStatBuilder reportBuilder = new ReportStatBuilder(_libraryManager);
			ReportStatResult reportResult = reportBuilder.GetReportStatResult(queryResult.Items, ReportHelper.GetRowType(request.IncludeItemTypes), request.TopItems ?? 5);
			reportResult.TotalRecordCount = reportResult.Groups.Count();
			return reportResult;
		}

		/// <summary> Gets report result. </summary>
		/// <param name="request"> The request. </param>
		/// <returns> The report result. </returns>
		private async Task<ReportResult> GetReportResult(GetItemReport request)
		{

			ReportViewType reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
			ReportBuilder reportBuilder = new ReportBuilder(_libraryManager);
			QueryResult<BaseItem> queryResult = await GetQueryResult(request).ConfigureAwait(false);
			ReportResult reportResult = reportBuilder.GetReportResult(queryResult.Items, reportRowType, request);
			reportResult.TotalRecordCount = queryResult.TotalRecordCount;

			return reportResult;
		}

		/// <summary> Gets query result. </summary>
		/// <param name="request"> The request. </param>
		/// <returns> The query result. </returns>
		private async Task<QueryResult<BaseItem>> GetQueryResult(BaseReportRequest request)
		{
			// Placeholder in case needed later
			request.Recursive = true;
			var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;
			request.Fields = "MediaSources,DateCreated,Settings,Studios,SyncInfo,ItemCounts";

			var parentItem = string.IsNullOrEmpty(request.ParentId) ?
				(user == null ? _libraryManager.RootFolder : user.RootFolder) :
				_libraryManager.GetItemById(request.ParentId);

			var item = string.IsNullOrEmpty(request.ParentId) ?
				user == null ? _libraryManager.RootFolder : user.RootFolder :
				parentItem;

			IEnumerable<BaseItem> items;

			if (request.Recursive)
			{
				var result = await ((Folder)item).GetItems(GetItemsQuery(request, user)).ConfigureAwait(false);
				return result;
			}
			else
			{
				if (user == null)
				{
					var result = await ((Folder)item).GetItems(GetItemsQuery(request, null)).ConfigureAwait(false);
					return result;
				}

				var userRoot = item as UserRootFolder;

				if (userRoot == null)
				{
					var result = await ((Folder)item).GetItems(GetItemsQuery(request, user)).ConfigureAwait(false);

					return result;
				}

				items = ((Folder)item).GetChildren(user, true);
			}

			return new QueryResult<BaseItem> { Items = items.ToArray() };

		}

		/// <summary> Gets items query. </summary>
		/// <param name="request"> The request. </param>
		/// <param name="user"> The user. </param>
		/// <returns> The items query. </returns>
		private InternalItemsQuery GetItemsQuery(BaseReportRequest request, User user)
		{
			var query = new InternalItemsQuery
			{
				User = user,
				IsPlayed = request.IsPlayed,
				MediaTypes = request.GetMediaTypes(),
				IncludeItemTypes = request.GetIncludeItemTypes(),
				ExcludeItemTypes = request.GetExcludeItemTypes(),
				Recursive = true,
				SortBy = request.GetOrderBy(),
				SortOrder = request.SortOrder ?? SortOrder.Ascending,

				Filter = i => ApplyAdditionalFilters(request, i, user, true, _libraryManager),
				StartIndex = request.StartIndex,
				IsMissing = request.IsMissing,
				IsVirtualUnaired = request.IsVirtualUnaired,
				IsUnaired = request.IsUnaired,
				CollapseBoxSetItems = request.CollapseBoxSetItems,
				NameLessThan = request.NameLessThan,
				NameStartsWith = request.NameStartsWith,
				NameStartsWithOrGreater = request.NameStartsWithOrGreater,
				HasImdbId = request.HasImdbId,
				IsYearMismatched = request.IsYearMismatched,
				IsUnidentified = request.IsUnidentified,
				IsPlaceHolder = request.IsPlaceHolder,
				IsLocked = request.IsLocked,
				IsInBoxSet = request.IsInBoxSet,
				IsHD = request.IsHD,
				Is3D = request.Is3D,
				HasTvdbId = request.HasTvdbId,
				HasTmdbId = request.HasTmdbId,
				HasOverview = request.HasOverview,
				HasOfficialRating = request.HasOfficialRating,
				HasParentalRating = request.HasParentalRating,
				HasSpecialFeature = request.HasSpecialFeature,
				HasSubtitles = request.HasSubtitles,
				HasThemeSong = request.HasThemeSong,
				HasThemeVideo = request.HasThemeVideo,
				HasTrailer = request.HasTrailer,
				Tags = request.GetTags(),
				OfficialRatings = request.GetOfficialRatings(),
				Genres = request.GetGenres(),
				Studios = request.GetStudios(),
				StudioIds = request.GetStudioIds(),
				Person = request.Person,
				PersonIds = request.GetPersonIds(),
				PersonTypes = request.GetPersonTypes(),
				Years = request.GetYears(),
				ImageTypes = request.GetImageTypes().ToArray(),
				VideoTypes = request.GetVideoTypes().ToArray(),
				AdjacentTo = request.AdjacentTo
			};

			if (!string.IsNullOrWhiteSpace(request.Ids))
			{
				query.CollapseBoxSetItems = false;
			}

			foreach (var filter in request.GetFilters())
			{
				switch (filter)
				{
					case ItemFilter.Dislikes:
						query.IsLiked = false;
						break;
					case ItemFilter.IsFavorite:
						query.IsFavorite = true;
						break;
					case ItemFilter.IsFavoriteOrLikes:
						query.IsFavoriteOrLiked = true;
						break;
					case ItemFilter.IsFolder:
						query.IsFolder = true;
						break;
					case ItemFilter.IsNotFolder:
						query.IsFolder = false;
						break;
					case ItemFilter.IsPlayed:
						query.IsPlayed = true;
						break;
					case ItemFilter.IsRecentlyAdded:
						break;
					case ItemFilter.IsResumable:
						query.IsResumable = true;
						break;
					case ItemFilter.IsUnplayed:
						query.IsPlayed = false;
						break;
					case ItemFilter.Likes:
						query.IsLiked = true;
						break;
				}
			}

			if (request.HasQueryLimit)
				query.Limit = request.Limit;
			return query;
		}

		/// <summary> Applies filtering. </summary>
		/// <param name="items"> The items. </param>
		/// <param name="filter"> The filter. </param>
		/// <param name="user"> The user. </param>
		/// <param name="repository"> The repository. </param>
		/// <returns> IEnumerable{BaseItem}. </returns>
		internal static IEnumerable<BaseItem> ApplyFilter(IEnumerable<BaseItem> items, ItemFilter filter, User user, IUserDataManager repository)
		{
			// Avoid implicitly captured closure
			var currentUser = user;

			switch (filter)
			{
				case ItemFilter.IsFavoriteOrLikes:
					return items.Where(item =>
					{
						var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

						if (userdata == null)
						{
							return false;
						}

						var likes = userdata.Likes ?? false;
						var favorite = userdata.IsFavorite;

						return likes || favorite;
					});

				case ItemFilter.Likes:
					return items.Where(item =>
					{
						var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

						return userdata != null && userdata.Likes.HasValue && userdata.Likes.Value;
					});

				case ItemFilter.Dislikes:
					return items.Where(item =>
					{
						var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

						return userdata != null && userdata.Likes.HasValue && !userdata.Likes.Value;
					});

				case ItemFilter.IsFavorite:
					return items.Where(item =>
					{
						var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

						return userdata != null && userdata.IsFavorite;
					});

				case ItemFilter.IsResumable:
					return items.Where(item =>
					{
						var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

						return userdata != null && userdata.PlaybackPositionTicks > 0;
					});

				case ItemFilter.IsPlayed:
					return items.Where(item => item.IsPlayed(currentUser));

				case ItemFilter.IsUnplayed:
					return items.Where(item => item.IsUnplayed(currentUser));

				case ItemFilter.IsFolder:
					return items.Where(item => item.IsFolder);

				case ItemFilter.IsNotFolder:
					return items.Where(item => !item.IsFolder);

				case ItemFilter.IsRecentlyAdded:
					return items.Where(item => (DateTime.UtcNow - item.DateCreated).TotalDays <= 10);
			}

			return items;
		}

		/// <summary> Applies the additional filters. </summary>
		/// <param name="request"> The request. </param>
		/// <param name="i"> Zero-based index of the. </param>
		/// <param name="user"> The user. </param>
		/// <param name="isPreFiltered"> true if this object is pre filtered. </param>
		/// <param name="libraryManager"> Manager for library. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
        private bool ApplyAdditionalFilters(BaseReportRequest request, BaseItem i, User user, bool isPreFiltered, ILibraryManager libraryManager)
		{
			var video = i as Video;

			if (!isPreFiltered)
			{
				var mediaTypes = request.GetMediaTypes();
				if (mediaTypes.Length > 0)
				{
					if (!(!string.IsNullOrEmpty(i.MediaType) && mediaTypes.Contains(i.MediaType, StringComparer.OrdinalIgnoreCase)))
					{
						return false;
					}
				}

				if (request.IsPlayed.HasValue)
				{
					var val = request.IsPlayed.Value;
					if (i.IsPlayed(user) != val)
					{
						return false;
					}
				}

				// Exclude item types
				var excluteItemTypes = request.GetExcludeItemTypes();
				if (excluteItemTypes.Length > 0 && excluteItemTypes.Contains(i.GetType().Name, StringComparer.OrdinalIgnoreCase))
				{
					return false;
				}

				// Include item types
				var includeItemTypes = request.GetIncludeItemTypes();
				if (includeItemTypes.Length > 0 && !includeItemTypes.Contains(i.GetType().Name, StringComparer.OrdinalIgnoreCase))
				{
					return false;
				}

				if (request.IsInBoxSet.HasValue)
				{
					var val = request.IsInBoxSet.Value;
					if (i.Parents.OfType<BoxSet>().Any() != val)
					{
						return false;
					}
				}

				// Filter by Video3DFormat
				if (request.Is3D.HasValue)
				{
					var val = request.Is3D.Value;

					if (video == null || val != video.Video3DFormat.HasValue)
					{
						return false;
					}
				}

				if (request.IsHD.HasValue)
				{
					var val = request.IsHD.Value;

					if (video == null || val != video.IsHD)
					{
						return false;
					}
				}

				if (request.IsUnidentified.HasValue)
				{
					var val = request.IsUnidentified.Value;
					if (i.IsUnidentified != val)
					{
						return false;
					}
				}

				if (request.IsLocked.HasValue)
				{
					var val = request.IsLocked.Value;
					if (i.IsLocked != val)
					{
						return false;
					}
				}

				if (request.HasOverview.HasValue)
				{
					var filterValue = request.HasOverview.Value;

					var hasValue = !string.IsNullOrEmpty(i.Overview);

					if (hasValue != filterValue)
					{
						return false;
					}
				}

				if (request.HasImdbId.HasValue)
				{
					var filterValue = request.HasImdbId.Value;

					var hasValue = !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Imdb));

					if (hasValue != filterValue)
					{
						return false;
					}
				}

				if (request.HasTmdbId.HasValue)
				{
					var filterValue = request.HasTmdbId.Value;

					var hasValue = !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Tmdb));

					if (hasValue != filterValue)
					{
						return false;
					}
				}

				if (request.HasTvdbId.HasValue)
				{
					var filterValue = request.HasTvdbId.Value;

					var hasValue = !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Tvdb));

					if (hasValue != filterValue)
					{
						return false;
					}
				}

				if (request.IsYearMismatched.HasValue)
				{
					var filterValue = request.IsYearMismatched.Value;

					if (UserViewBuilder.IsYearMismatched(i, libraryManager) != filterValue)
					{
						return false;
					}
				}

				if (request.HasOfficialRating.HasValue)
				{
					var filterValue = request.HasOfficialRating.Value;

					var hasValue = !string.IsNullOrEmpty(i.OfficialRating);

					if (hasValue != filterValue)
					{
						return false;
					}
				}

				if (request.IsPlaceHolder.HasValue)
				{
					var filterValue = request.IsPlaceHolder.Value;

					var isPlaceHolder = false;

					var hasPlaceHolder = i as ISupportsPlaceHolders;

					if (hasPlaceHolder != null)
					{
						isPlaceHolder = hasPlaceHolder.IsPlaceHolder;
					}

					if (isPlaceHolder != filterValue)
					{
						return false;
					}
				}

				if (request.HasSpecialFeature.HasValue)
				{
					var filterValue = request.HasSpecialFeature.Value;

					var movie = i as IHasSpecialFeatures;

					if (movie != null)
					{
						var ok = filterValue
							? movie.SpecialFeatureIds.Count > 0
							: movie.SpecialFeatureIds.Count == 0;

						if (!ok)
						{
							return false;
						}
					}
					else
					{
						return false;
					}
				}

				if (request.HasSubtitles.HasValue)
				{
					var val = request.HasSubtitles.Value;

					if (video == null || val != video.HasSubtitles)
					{
						return false;
					}
				}

				if (request.HasParentalRating.HasValue)
				{
					var val = request.HasParentalRating.Value;

					var rating = i.CustomRating;

					if (string.IsNullOrEmpty(rating))
					{
						rating = i.OfficialRating;
					}

					if (val)
					{
						if (string.IsNullOrEmpty(rating))
						{
							return false;
						}
					}
					else
					{
						if (!string.IsNullOrEmpty(rating))
						{
							return false;
						}
					}
				}

				if (request.HasTrailer.HasValue)
				{
					var val = request.HasTrailer.Value;
					var trailerCount = 0;

					var hasTrailers = i as IHasTrailers;
					if (hasTrailers != null)
					{
						trailerCount = hasTrailers.GetTrailerIds().Count;
					}

					var ok = val ? trailerCount > 0 : trailerCount == 0;

					if (!ok)
					{
						return false;
					}
				}

				if (request.HasThemeSong.HasValue)
				{
					var filterValue = request.HasThemeSong.Value;

					var themeCount = 0;
					var iHasThemeMedia = i as IHasThemeMedia;

					if (iHasThemeMedia != null)
					{
						themeCount = iHasThemeMedia.ThemeSongIds.Count;
					}
					var ok = filterValue ? themeCount > 0 : themeCount == 0;

					if (!ok)
					{
						return false;
					}
				}

				if (request.HasThemeVideo.HasValue)
				{
					var filterValue = request.HasThemeVideo.Value;

					var themeCount = 0;
					var iHasThemeMedia = i as IHasThemeMedia;

					if (iHasThemeMedia != null)
					{
						themeCount = iHasThemeMedia.ThemeVideoIds.Count;
					}
					var ok = filterValue ? themeCount > 0 : themeCount == 0;

					if (!ok)
					{
						return false;
					}
				}

				// Apply tag filter
				var tags = request.GetTags();
				if (tags.Length > 0)
				{
					var hasTags = i as IHasTags;
					if (hasTags == null)
					{
						return false;
					}
					if (!(tags.Any(v => hasTags.Tags.Contains(v, StringComparer.OrdinalIgnoreCase))))
					{
						return false;
					}
				}

				// Apply official rating filter
				var officialRatings = request.GetOfficialRatings();
				if (officialRatings.Length > 0 && !officialRatings.Contains(i.OfficialRating ?? string.Empty))
				{
					return false;
				}

				// Apply genre filter
				var genres = request.GetGenres();
				if (genres.Length > 0 && !(genres.Any(v => i.Genres.Contains(v, StringComparer.OrdinalIgnoreCase))))
				{
					return false;
				}

				// Filter by VideoType
				var videoTypes = request.GetVideoTypes();
				if (videoTypes.Length > 0 && (video == null || !videoTypes.Contains(video.VideoType)))
				{
					return false;
				}

				var imageTypes = request.GetImageTypes().ToList();
				if (imageTypes.Count > 0)
				{
					if (!(imageTypes.Any(i.HasImage)))
					{
						return false;
					}
				}

				// Apply studio filter
				var studios = request.GetStudios();
				if (studios.Length > 0 && !studios.Any(v => i.Studios.Contains(v, StringComparer.OrdinalIgnoreCase)))
				{
					return false;
				}

				// Apply studio filter
				var studioIds = request.GetStudioIds();
				if (studioIds.Length > 0 && !studioIds.Any(id =>
				{
					var studioItem = libraryManager.GetItemById(id);
					return studioItem != null && i.Studios.Contains(studioItem.Name, StringComparer.OrdinalIgnoreCase);
				}))
				{
					return false;
				}

				// Apply year filter
				var years = request.GetYears();
				if (years.Length > 0 && !(i.ProductionYear.HasValue && years.Contains(i.ProductionYear.Value)))
				{
					return false;
				}

				// Apply person filter
				var personIds = request.GetPersonIds();
				if (personIds.Length > 0)
				{
					var names = personIds
						.Select(libraryManager.GetItemById)
						.Select(p => p == null ? "-1" : p.Name)
						.ToList();

					if (!(names.Any(v => _libraryManager.GetPeople(i).Select(p => p.Name).Contains(v, StringComparer.OrdinalIgnoreCase))))
					{
						return false;
					}
				}

				// Apply person filter
				if (!string.IsNullOrEmpty(request.Person))
				{
					var personTypes = request.GetPersonTypes();

					if (personTypes.Length == 0)
					{
                        if (!(_libraryManager.GetPeople(i).Any(p => string.Equals(p.Name, request.Person, StringComparison.OrdinalIgnoreCase))))
						{
							return false;
						}
					}
					else
					{
						var types = personTypes;

						var ok = new[] { i }.Any(item =>
                                _libraryManager.GetPeople(i).Any(p =>
									p.Name.Equals(request.Person, StringComparison.OrdinalIgnoreCase) && (types.Contains(p.Type, StringComparer.OrdinalIgnoreCase) || types.Contains(p.Role, StringComparer.OrdinalIgnoreCase))));

						if (!ok)
						{
							return false;
						}
					}
				}
			}

			if (request.MinCommunityRating.HasValue)
			{
				var val = request.MinCommunityRating.Value;

				if (!(i.CommunityRating.HasValue && i.CommunityRating >= val))
				{
					return false;
				}
			}

			if (request.MinCriticRating.HasValue)
			{
				var val = request.MinCriticRating.Value;

				var hasCriticRating = i as IHasCriticRating;

				if (hasCriticRating != null)
				{
					if (!(hasCriticRating.CriticRating.HasValue && hasCriticRating.CriticRating >= val))
					{
						return false;
					}
				}
				else
				{
					return false;
				}
			}

			// Artists
			if (!string.IsNullOrEmpty(request.ArtistIds))
			{
				var artistIds = request.ArtistIds.Split('|');

				var audio = i as IHasArtist;

				if (!(audio != null && artistIds.Any(id =>
				{
					var artistItem = libraryManager.GetItemById(id);
					return artistItem != null && audio.HasAnyArtist(artistItem.Name);
				})))
				{
					return false;
				}
			}

			// Artists
			if (!string.IsNullOrEmpty(request.Artists))
			{
				var artists = request.Artists.Split('|');

				var audio = i as IHasArtist;

				if (!(audio != null && artists.Any(audio.HasAnyArtist)))
				{
					return false;
				}
			}

			// Albums
			if (!string.IsNullOrEmpty(request.Albums))
			{
				var albums = request.Albums.Split('|');

				var audio = i as Audio;

				if (audio != null)
				{
					if (!albums.Any(a => string.Equals(a, audio.Album, StringComparison.OrdinalIgnoreCase)))
					{
						return false;
					}
				}

				var album = i as MusicAlbum;

				if (album != null)
				{
					if (!albums.Any(a => string.Equals(a, album.Name, StringComparison.OrdinalIgnoreCase)))
					{
						return false;
					}
				}

				var musicVideo = i as MusicVideo;

				if (musicVideo != null)
				{
					if (!albums.Any(a => string.Equals(a, musicVideo.Album, StringComparison.OrdinalIgnoreCase)))
					{
						return false;
					}
				}

				return false;
			}

			// Min index number
			if (request.MinIndexNumber.HasValue)
			{
				if (!(i.IndexNumber.HasValue && i.IndexNumber.Value >= request.MinIndexNumber.Value))
				{
					return false;
				}
			}

			// Min official rating
			if (!string.IsNullOrEmpty(request.MinOfficialRating))
			{
				var level = _localization.GetRatingLevel(request.MinOfficialRating);

				if (level.HasValue)
				{
					var rating = i.CustomRating;

					if (string.IsNullOrEmpty(rating))
					{
						rating = i.OfficialRating;
					}

					if (!string.IsNullOrEmpty(rating))
					{
						var itemLevel = _localization.GetRatingLevel(rating);

						if (!(!itemLevel.HasValue || itemLevel.Value >= level.Value))
						{
							return false;
						}
					}
				}
			}

			// Max official rating
			if (!string.IsNullOrEmpty(request.MaxOfficialRating))
			{
				var level = _localization.GetRatingLevel(request.MaxOfficialRating);

				if (level.HasValue)
				{
					var rating = i.CustomRating;

					if (string.IsNullOrEmpty(rating))
					{
						rating = i.OfficialRating;
					}

					if (!string.IsNullOrEmpty(rating))
					{
						var itemLevel = _localization.GetRatingLevel(rating);

						if (!(!itemLevel.HasValue || itemLevel.Value <= level.Value))
						{
							return false;
						}
					}
				}
			}

			// LocationTypes
			if (!string.IsNullOrEmpty(request.LocationTypes))
			{
				var vals = request.LocationTypes.Split(',');
				if (!vals.Contains(i.LocationType.ToString(), StringComparer.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			// ExcludeLocationTypes
			if (!string.IsNullOrEmpty(request.ExcludeLocationTypes))
			{
				var vals = request.ExcludeLocationTypes.Split(',');
				if (vals.Contains(i.LocationType.ToString(), StringComparer.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			if (!string.IsNullOrEmpty(request.AlbumArtistStartsWithOrGreater))
			{
				var ok = new[] { i }.OfType<IHasAlbumArtist>()
					.Any(p => string.Compare(request.AlbumArtistStartsWithOrGreater, p.AlbumArtists.FirstOrDefault(), StringComparison.CurrentCultureIgnoreCase) < 1);

				if (!ok)
				{
					return false;
				}
			}

			// Filter by Series Status
			if (!string.IsNullOrEmpty(request.SeriesStatus))
			{
				var vals = request.SeriesStatus.Split(',');

				var ok = new[] { i }.OfType<Series>().Any(p => p.Status.HasValue && vals.Contains(p.Status.Value.ToString(), StringComparer.OrdinalIgnoreCase));

				if (!ok)
				{
					return false;
				}
			}

			// Filter by Series AirDays
			if (!string.IsNullOrEmpty(request.AirDays))
			{
				var days = request.AirDays.Split(',').Select(d => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), d, true));

				var ok = new[] { i }.OfType<Series>().Any(p => p.AirDays != null && days.Any(d => p.AirDays.Contains(d)));

				if (!ok)
				{
					return false;
				}
			}

			if (request.MinPlayers.HasValue)
			{
				var filterValue = request.MinPlayers.Value;

				var game = i as Game;

				if (game != null)
				{
					var players = game.PlayersSupported ?? 1;

					var ok = players >= filterValue;

					if (!ok)
					{
						return false;
					}
				}
				else
				{
					return false;
				}
			}

			if (request.MaxPlayers.HasValue)
			{
				var filterValue = request.MaxPlayers.Value;

				var game = i as Game;

				if (game != null)
				{
					var players = game.PlayersSupported ?? 1;

					var ok = players <= filterValue;

					if (!ok)
					{
						return false;
					}
				}
				else
				{
					return false;
				}
			}

			if (request.ParentIndexNumber.HasValue)
			{
				var filterValue = request.ParentIndexNumber.Value;

				var episode = i as Episode;

				if (episode != null)
				{
					if (episode.ParentIndexNumber.HasValue && episode.ParentIndexNumber.Value != filterValue)
					{
						return false;
					}
				}

				var song = i as Audio;

				if (song != null)
				{
					if (song.ParentIndexNumber.HasValue && song.ParentIndexNumber.Value != filterValue)
					{
						return false;
					}
				}
			}

			if (request.AiredDuringSeason.HasValue)
			{
				var episode = i as Episode;

				if (episode == null)
				{
					return false;
				}

				if (!Series.FilterEpisodesBySeason(new[] { episode }, request.AiredDuringSeason.Value, true).Any())
				{
					return false;
				}
			}

			if (!string.IsNullOrEmpty(request.MinPremiereDate))
			{
				var date = DateTime.Parse(request.MinPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

				if (!(i.PremiereDate.HasValue && i.PremiereDate.Value >= date))
				{
					return false;
				}
			}

			if (!string.IsNullOrEmpty(request.MaxPremiereDate))
			{
				var date = DateTime.Parse(request.MaxPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

				if (!(i.PremiereDate.HasValue && i.PremiereDate.Value <= date))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary> Applies the paging. </summary>
		/// <param name="request"> The request. </param>
		/// <param name="items"> The items. </param>
		/// <returns> IEnumerable{BaseItem}. </returns>
		private IEnumerable<BaseItem> ApplyPaging(GetItems request, IEnumerable<BaseItem> items)
		{
			// Start at
			if (request.StartIndex.HasValue)
			{
				items = items.Skip(request.StartIndex.Value);
			}

			// Return limit
			if (request.Limit.HasValue)
			{
				items = items.Take(request.Limit.Value);
			}

			return items;
		}

	}
}
