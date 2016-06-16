using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CommonIO;

namespace MediaBrowser.Providers.TV
{

    /// <summary>
    /// Class RemoteEpisodeProvider
    /// </summary>
    class TvdbEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        private static readonly string FullIdKey = MetadataProviders.Tvdb + "-Full";

        internal static TvdbEpisodeProvider Current;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public TvdbEpisodeProvider(IFileSystem fileSystem, IServerConfigurationManager config, IHttpClient httpClient, ILogger logger)
        {
            _fileSystem = fileSystem;
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
            Current = this;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var list = new List<RemoteSearchResult>();

			// The search query must either provide an episode number or date
			if (!searchInfo.IndexNumber.HasValue && !searchInfo.PremiereDate.HasValue) 
			{
				return list;
			}

            if (TvdbSeriesProvider.IsValidSeries(searchInfo.SeriesProviderIds))
            {
                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, searchInfo.SeriesProviderIds);

                var searchNumbers = new EpisodeNumbers();

				if (searchInfo.IndexNumber.HasValue) {
					searchNumbers.EpisodeNumber = searchInfo.IndexNumber.Value;
				}
                
                searchNumbers.SeasonNumber = searchInfo.ParentIndexNumber;
                searchNumbers.EpisodeNumberEnd = searchInfo.IndexNumberEnd ?? searchNumbers.EpisodeNumber; 

                try
                {
                    var metadataResult = FetchEpisodeData(searchInfo, searchNumbers, seriesDataPath, cancellationToken);

                    if (metadataResult.HasMetadata)
                    {
                        var item = metadataResult.Item;

                        list.Add(new RemoteSearchResult
                        {
                            IndexNumber = item.IndexNumber,
                            Name = item.Name,
                            ParentIndexNumber = item.ParentIndexNumber,
                            PremiereDate = item.PremiereDate,
                            ProductionYear = item.ProductionYear,
                            ProviderIds = item.ProviderIds,
                            SearchProviderName = Name,
                            IndexNumberEnd = item.IndexNumberEnd
                        });
                    }
                }
                catch (FileNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
                catch (DirectoryNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
            }

            return list;
        }

        public string Name
        {
            get { return "TheTVDB"; }
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>();

            if (TvdbSeriesProvider.IsValidSeries(searchInfo.SeriesProviderIds) && 
				(searchInfo.IndexNumber.HasValue || searchInfo.PremiereDate.HasValue))
            {
                await TvdbSeriesProvider.Current.EnsureSeriesInfo(searchInfo.SeriesProviderIds, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, searchInfo.SeriesProviderIds);

                var searchNumbers = new EpisodeNumbers();
				if (searchInfo.IndexNumber.HasValue) {
					searchNumbers.EpisodeNumber = searchInfo.IndexNumber.Value;
				}
                searchNumbers.SeasonNumber = searchInfo.ParentIndexNumber;
                searchNumbers.EpisodeNumberEnd = searchInfo.IndexNumberEnd ?? searchNumbers.EpisodeNumber; 

                try
                {
                    result = FetchEpisodeData(searchInfo, searchNumbers, seriesDataPath, cancellationToken);
                }
                catch (FileNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
                catch (DirectoryNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
            }
            else
            {
                _logger.Debug("No series identity found for {0}", searchInfo.Name);
            }

            return result;
        }

        /// <summary>
        /// Gets the episode XML files.
        /// </summary>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="searchInfo">The search information.</param>
        /// <returns>List{FileInfo}.</returns>
		internal List<XmlReader> GetEpisodeXmlNodes(string seriesDataPath, EpisodeInfo searchInfo)
        {
			var seriesXmlPath = TvdbSeriesProvider.Current.GetSeriesXmlPath (searchInfo.SeriesProviderIds, searchInfo.MetadataLanguage);
			            
			try 
			{
				return GetXmlNodes(seriesXmlPath, searchInfo);
			}
			catch (DirectoryNotFoundException) 
			{
				return new List<XmlReader> ();
			}
			catch (FileNotFoundException) 
			{
				return new List<XmlReader> ();
			}
        }

        private class EpisodeNumbers
        {
            public int EpisodeNumber;
            public int? SeasonNumber;
            public int EpisodeNumberEnd;
        }

        /// <summary>
        /// Fetches the episode data.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchNumbers">The search numbers.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private MetadataResult<Episode> FetchEpisodeData(EpisodeInfo id, EpisodeNumbers searchNumbers, string seriesDataPath, CancellationToken cancellationToken)
        {
			var result = new MetadataResult<Episode>()
			{
				Item = new Episode
				{
					IndexNumber = id.IndexNumber,
					ParentIndexNumber = id.ParentIndexNumber,
					IndexNumberEnd = id.IndexNumberEnd
				}
			};

			var xmlNodes = GetEpisodeXmlNodes (seriesDataPath, id);

			if (xmlNodes.Count > 0) {
				FetchMainEpisodeInfo(result, xmlNodes[0], cancellationToken);

				result.HasMetadata = true;
			}

			foreach (var node in xmlNodes.Skip(1)) {
				FetchAdditionalPartInfo(result, node, cancellationToken);
			}

            return result;
        }

		private List<XmlReader> GetXmlNodes(string xmlFile, EpisodeInfo searchInfo)
		{
			var list = new List<XmlReader> ();

			if (searchInfo.IndexNumber.HasValue) 
			{
				var files = GetEpisodeXmlFiles (searchInfo.ParentIndexNumber, searchInfo.IndexNumber, searchInfo.IndexNumberEnd, Path.GetDirectoryName (xmlFile));

				list = files.Select (GetXmlReader).ToList ();
			}

			if (list.Count == 0 && searchInfo.PremiereDate.HasValue) {
				list = GetXmlNodesByPremiereDate (xmlFile, searchInfo.PremiereDate.Value);
			}

			return list;
		}

		private List<FileSystemMetadata> GetEpisodeXmlFiles(int? seasonNumber, int? episodeNumber, int? endingEpisodeNumber, string seriesDataPath)
		{
			var files = new List<FileSystemMetadata>();

			if (episodeNumber == null)
			{
				return files;
			}

			var usingAbsoluteData = false;

            if (seasonNumber.HasValue)
            {
                var file = Path.Combine(seriesDataPath, string.Format("episode-{0}-{1}.xml", seasonNumber.Value, episodeNumber));
                var fileInfo = _fileSystem.GetFileInfo(file);

                if (fileInfo.Exists)
                {
                    files.Add(fileInfo);
                }
            }
            else
            {
                usingAbsoluteData = true;
                var file = Path.Combine(seriesDataPath, string.Format("episode-abs-{0}.xml", episodeNumber));
                var fileInfo = _fileSystem.GetFileInfo(file);
                if (fileInfo.Exists)
                {
                    files.Add(fileInfo);
                }
            }

			var end = endingEpisodeNumber ?? episodeNumber;
			episodeNumber++;

			while (episodeNumber <= end)
			{
                string file;

				if (usingAbsoluteData)
				{
					file = Path.Combine(seriesDataPath, string.Format("episode-abs-{0}.xml", episodeNumber));
				}
				else
				{
					file = Path.Combine(seriesDataPath, string.Format("episode-{0}-{1}.xml", seasonNumber.Value, episodeNumber));
				}

				var fileInfo = _fileSystem.GetFileInfo(file);
				if (fileInfo.Exists)
				{
					files.Add(fileInfo);
				}
				else
				{
					break;
				}

				episodeNumber++;
			}

			return files;
		}

		private XmlReader GetXmlReader(FileSystemMetadata xmlFile)
		{
			return GetXmlReader (_fileSystem.ReadAllText(xmlFile.FullName, Encoding.UTF8));
		}

		private XmlReader GetXmlReader(String xml)
		{
			var streamReader = new StringReader (xml);

			return XmlReader.Create (streamReader, new XmlReaderSettings {
				CheckCharacters = false,
				IgnoreProcessingInstructions = true,
				IgnoreComments = true,
				ValidationType = ValidationType.None
			});
		}

		private List<XmlReader> GetXmlNodesByPremiereDate(string xmlFile, DateTime premiereDate)
		{
			var list = new List<XmlReader> ();

			using (var streamReader = new StreamReader (xmlFile, Encoding.UTF8)) {
				// Use XmlReader for best performance
				using (var reader = XmlReader.Create (streamReader, new XmlReaderSettings {
					CheckCharacters = false,
					IgnoreProcessingInstructions = true,
					IgnoreComments = true,
					ValidationType = ValidationType.None
				})) 
				{
					reader.MoveToContent();

					// Loop through each element
					while (reader.Read())
					{
						if (reader.NodeType == XmlNodeType.Element)
						{
							switch (reader.Name)
							{
								case "Episode":
								{
									var outerXml = reader.ReadOuterXml();

									var airDate = GetEpisodeAirDate (outerXml);

									if (airDate.HasValue && premiereDate.Date == airDate.Value.Date) 
									{
										list.Add (GetXmlReader(outerXml));
										return list;
									}

									break;
								}

								default:
									reader.Skip();
									break;
							}
						}
					}
				}
			}

			return list;
		}

		private DateTime? GetEpisodeAirDate(string xml)
		{
			using (var streamReader = new StringReader (xml)) 
			{
				// Use XmlReader for best performance
				using (var reader = XmlReader.Create (streamReader, new XmlReaderSettings {
					CheckCharacters = false,
					IgnoreProcessingInstructions = true,
					IgnoreComments = true,
					ValidationType = ValidationType.None
				})) 
				{
					reader.MoveToContent ();

					// Loop through each element
					while (reader.Read ()) {

						if (reader.NodeType == XmlNodeType.Element) {
							switch (reader.Name) {

								case "FirstAired":
								{
									var val = reader.ReadElementContentAsString ();

									if (!string.IsNullOrWhiteSpace (val)) {
										DateTime date;
										if (DateTime.TryParse (val, out date)) {
											date = date.ToUniversalTime ();

											return date;
										}
									}

									break;
								}

								default:
									reader.Skip ();
									break;
							}
						}
					}
				}
			}
			return null;
		}

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

		private void FetchMainEpisodeInfo(MetadataResult<Episode> result, XmlReader reader, CancellationToken cancellationToken)
        {
            var item = result.Item;

			// Use XmlReader for best performance
			using (reader)
			{
				reader.MoveToContent();

				result.ResetPeople();

				// Loop through each element
				while (reader.Read())
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (reader.NodeType == XmlNodeType.Element)
					{
						switch (reader.Name)
						{
						case "id":
							{
								var val = reader.ReadElementContentAsString();
								if (!string.IsNullOrWhiteSpace(val))
								{
									item.SetProviderId(MetadataProviders.Tvdb, val);
								}
								break;
							}

						case "IMDB_ID":
							{
								var val = reader.ReadElementContentAsString();
								if (!string.IsNullOrWhiteSpace(val))
								{
									item.SetProviderId(MetadataProviders.Imdb, val);
								}
								break;
							}

						case "DVD_episodenumber":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									float num;

									if (float.TryParse(val, NumberStyles.Any, _usCulture, out num))
									{
										item.DvdEpisodeNumber = num;
									}
								}

								break;
							}

                        case "DVD_season":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    float num;

                                    if (float.TryParse(val, NumberStyles.Any, _usCulture, out num))
                                    {
                                        item.DvdSeasonNumber = Convert.ToInt32(num);
                                    }
                                }

                                break;
                            }

                        case "EpisodeNumber":
                            {
                                if (!item.IndexNumber.HasValue)
                                {
                                    var val = reader.ReadElementContentAsString();

                                    if (!string.IsNullOrWhiteSpace(val))
                                    {
                                        int rval;

                                        // int.TryParse is local aware, so it can be probamatic, force us culture
                                        if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                        {
                                            item.IndexNumber = rval;
                                        }
                                    }
                                }

                                break;
                            }

                        case "SeasonNumber":
                            {
                                if (!item.ParentIndexNumber.HasValue)
                                {
                                    var val = reader.ReadElementContentAsString();

                                    if (!string.IsNullOrWhiteSpace(val))
                                    {
                                        int rval;

                                        // int.TryParse is local aware, so it can be probamatic, force us culture
                                        if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                        {
                                            item.ParentIndexNumber = rval;
                                        }
                                    }
                                }

                                break;
                            }

                        case "absolute_number":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									int rval;

									// int.TryParse is local aware, so it can be probamatic, force us culture
									if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
									{
										item.AbsoluteEpisodeNumber = rval;
									}
								}

								break;
							}

						case "airsbefore_episode":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									int rval;

									// int.TryParse is local aware, so it can be probamatic, force us culture
									if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
									{
										item.AirsBeforeEpisodeNumber = rval;
									}
								}

								break;
							}

						case "airsafter_season":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									int rval;

									// int.TryParse is local aware, so it can be probamatic, force us culture
									if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
									{
										item.AirsAfterSeasonNumber = rval;
									}
								}

								break;
							}

						case "airsbefore_season":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									int rval;

									// int.TryParse is local aware, so it can be probamatic, force us culture
									if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
									{
										item.AirsBeforeSeasonNumber = rval;
									}
								}

								break;
							}

						case "EpisodeName":
							{
								if (!item.LockedFields.Contains(MetadataFields.Name))
								{
									var val = reader.ReadElementContentAsString();
									if (!string.IsNullOrWhiteSpace(val))
									{
										item.Name = val;
									}
								}
								break;
							}

						case "Overview":
							{
								if (!item.LockedFields.Contains(MetadataFields.Overview))
								{
									var val = reader.ReadElementContentAsString();
									if (!string.IsNullOrWhiteSpace(val))
									{
										item.Overview = val;
									}
								}
								break;
							}
						case "Rating":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									float rval;

									// float.TryParse is local aware, so it can be probamatic, force us culture
									if (float.TryParse(val, NumberStyles.AllowDecimalPoint, _usCulture, out rval))
									{
										item.CommunityRating = rval;
									}
								}
								break;
							}
						case "RatingCount":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									int rval;

									// int.TryParse is local aware, so it can be probamatic, force us culture
									if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
									{
										item.VoteCount = rval;
									}
								}

								break;
							}

						case "FirstAired":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									DateTime date;
									if (DateTime.TryParse(val, out date))
									{
										date = date.ToUniversalTime();

										item.PremiereDate = date;
										item.ProductionYear = date.Year;
									}
								}

								break;
							}

						case "Director":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									if (!item.LockedFields.Contains(MetadataFields.Cast))
									{
										AddPeople(result, val, PersonType.Director);
									}
								}

								break;
							}
						case "GuestStars":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									if (!item.LockedFields.Contains(MetadataFields.Cast))
									{
										AddGuestStars(result, val);
									}
								}

								break;
							}
						case "Writer":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									if (!item.LockedFields.Contains(MetadataFields.Cast))
									{
										AddPeople(result, val, PersonType.Writer);
									}
								}

								break;
							}

						default:
							reader.Skip();
							break;
						}
					}
				}
			}
        }

        private void AddPeople<T>(MetadataResult<T> result, string val, string personType)
        {
            // Sometimes tvdb actors have leading spaces
            foreach (var person in val.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Where(i => !string.IsNullOrWhiteSpace(i))
                                            .Select(str => new PersonInfo { Type = personType, Name = str.Trim() }))
            {
                result.AddPerson(person);
            }
        }

        private void AddGuestStars<T>(MetadataResult<T> result, string val)
            where T : BaseItem
        {
            // Sometimes tvdb actors have leading spaces
            //Regex Info:
            //The first block are the posible delimitators (open-parentheses should be there cause if dont the next block will fail)
            //The second block Allow the delimitators to be part of the text if they're inside parentheses
            var persons = Regex.Matches(val, @"(?<delimitators>([^|,(])|(?<ignoreinParentheses>\([^)]*\)*))+")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(i => !string.IsNullOrWhiteSpace(i) && !string.IsNullOrEmpty(i));

            foreach (var person in persons.Select(str =>
            {
                var nameGroup = str.Split(new[] { '(' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var name = nameGroup[0].Trim();
                var roles = nameGroup.Count() > 1 ? nameGroup[1].Trim() : null;
                if (roles != null)
                    roles = roles.EndsWith(")") ? roles.Substring(0, roles.Length - 1) : roles;

                return new PersonInfo { Type = PersonType.GuestStar, Name = name, Role = roles };
            }))
            {
                if (!string.IsNullOrWhiteSpace(person.Name))
                {
                    result.AddPerson(person);
                }
            }
        }

		private void FetchAdditionalPartInfo(MetadataResult<Episode> result, XmlReader reader, CancellationToken cancellationToken)
        {
            var item = result.Item;

			// Use XmlReader for best performance
			using (reader)
			{
				reader.MoveToContent();

				// Loop through each element
				while (reader.Read())
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (reader.NodeType == XmlNodeType.Element)
					{
						switch (reader.Name)
						{
						case "EpisodeName":
							{
								if (!item.LockedFields.Contains(MetadataFields.Name))
								{
									var val = reader.ReadElementContentAsString();
									if (!string.IsNullOrWhiteSpace(val))
									{
										item.Name += ", " + val;
									}
								}
								break;
							}

						case "Overview":
							{
								if (!item.LockedFields.Contains(MetadataFields.Overview))
								{
									var val = reader.ReadElementContentAsString();
									if (!string.IsNullOrWhiteSpace(val))
									{
										item.Overview += Environment.NewLine + Environment.NewLine + val;
									}
								}
								break;
							}
						case "Director":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									if (!item.LockedFields.Contains(MetadataFields.Cast))
									{
										AddPeople(result, val, PersonType.Director);
									}
								}

								break;
							}
						case "GuestStars":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									if (!item.LockedFields.Contains(MetadataFields.Cast))
									{
										AddGuestStars(result, val);
									}
								}

								break;
							}
						case "Writer":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									if (!item.LockedFields.Contains(MetadataFields.Cast))
									{
										AddPeople(result, val, PersonType.Writer);
									}
								}

								break;
							}

						default:
							reader.Skip();
							break;
						}
					}
				}
			}
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = TvdbSeriesProvider.Current.TvDbResourcePool
            });
        }

        public int Order { get { return 0; } }
    }
}
