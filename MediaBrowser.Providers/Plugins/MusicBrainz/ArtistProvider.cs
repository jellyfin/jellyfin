using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.MusicBrainz;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
    {
        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
        {
            // TODO maybe remove when artist metadata can be disabled
            if (!Plugin.Instance.Configuration.Enable)
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            var musicBrainzId = searchInfo.GetMusicBrainzArtistId();

            if (!string.IsNullOrWhiteSpace(musicBrainzId))
            {
                var url = "/ws/2/artist/?query=arid:{0}" + musicBrainzId.ToString(CultureInfo.InvariantCulture);

                using (var response = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false))
                using (var stream = response.Content)
                {
                    return GetResultsFromResponse(stream);
                }
            }
            else
            {
                // They seem to throw bad request failures on any term with a slash
                var nameToSearch = searchInfo.Name.Replace('/', ' ');

                var url = string.Format("/ws/2/artist/?query=\"{0}\"&dismax=true", UrlEncode(nameToSearch));

                using (var response = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false))
                using (var stream = response.Content)
                {
                    var results = GetResultsFromResponse(stream).ToList();

                    if (results.Count > 0)
                    {
                        return results;
                    }
                }

                if (HasDiacritics(searchInfo.Name))
                {
                    // Try again using the search with accent characters url
                    url = string.Format("/ws/2/artist/?query=artistaccent:\"{0}\"", UrlEncode(nameToSearch));

                    using (var response = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false))
                    {
                        using (var stream = response.Content)
                        {
                            return GetResultsFromResponse(stream);
                        }
                    }
                }
            }

            return Enumerable.Empty<RemoteSearchResult>();
        }

        private IEnumerable<RemoteSearchResult> GetResultsFromResponse(Stream stream)
        {
            using (var oReader = new StreamReader(stream, Encoding.UTF8))
            {
                var settings = new XmlReaderSettings()
                {
                    ValidationType = ValidationType.None,
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true
                };

                using (var reader = XmlReader.Create(oReader, settings))
                {
                    reader.MoveToContent();
                    reader.Read();

                    // Loop through each element
                    while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "artist-list":
                                    {
                                        if (reader.IsEmptyElement)
                                        {
                                            reader.Read();
                                            continue;
                                        }
                                        using (var subReader = reader.ReadSubtree())
                                        {
                                            return ParseArtistList(subReader).ToList();
                                        }
                                    }
                                default:
                                    {
                                        reader.Skip();
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                    }

                    return Enumerable.Empty<RemoteSearchResult>();
                }
            }
        }

        private IEnumerable<RemoteSearchResult> ParseArtistList(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "artist":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }
                                var mbzId = reader.GetAttribute("id");

                                using (var subReader = reader.ReadSubtree())
                                {
                                    var artist = ParseArtist(subReader, mbzId);
                                    if (artist != null)
                                    {
                                        yield return artist;
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private RemoteSearchResult ParseArtist(XmlReader reader, string artistId)
        {
            var result = new RemoteSearchResult();

            reader.MoveToContent();
            reader.Read();

            // http://stackoverflow.com/questions/2299632/why-does-xmlreader-skip-every-other-element-if-there-is-no-whitespace-separator

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            {
                                result.Name = reader.ReadElementContentAsString();
                                break;
                            }
                        case "annotation":
                            {
                                result.Overview = reader.ReadElementContentAsString();
                                break;
                            }
                        default:
                            {
                                // there is sort-name if ever needed
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            result.SetProviderId(MetadataProviders.MusicBrainzArtist, artistId);

            if (string.IsNullOrWhiteSpace(artistId) || string.IsNullOrWhiteSpace(result.Name))
            {
                return null;
            }

            return result;
        }

        public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo id, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicArtist>
            {
                Item = new MusicArtist()
            };

            // TODO maybe remove when artist metadata can be disabled
            if (!Plugin.Instance.Configuration.Enable)
            {
                return result;
            }

            var musicBrainzId = id.GetMusicBrainzArtistId();

            if (string.IsNullOrWhiteSpace(musicBrainzId))
            {
                var searchResults = await GetSearchResults(id, cancellationToken).ConfigureAwait(false);

                var singleResult = searchResults.FirstOrDefault();

                if (singleResult != null)
                {
                    musicBrainzId = singleResult.GetProviderId(MetadataProviders.MusicBrainzArtist);
                    result.Item.Overview = singleResult.Overview;

                    if (Plugin.Instance.Configuration.ReplaceArtistName)
                    {
                        result.Item.Name = singleResult.Name;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(musicBrainzId))
            {
                result.HasMetadata = true;
                result.Item.SetProviderId(MetadataProviders.MusicBrainzArtist, musicBrainzId);
            }

            return result;
        }

        /// <summary>
        /// Determines whether the specified text has diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the specified text has diacritics; otherwise, <c>false</c>.</returns>
        private bool HasDiacritics(string text)
        {
            return !string.Equals(text, text.RemoveDiacritics(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Encodes an URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        private static string UrlEncode(string name)
        {
            return WebUtility.UrlEncode(name);
        }

        public string Name => "MusicBrainz";

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
