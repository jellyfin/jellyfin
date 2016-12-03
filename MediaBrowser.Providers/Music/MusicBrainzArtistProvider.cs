using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
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
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
    {
        private readonly IXmlReaderSettingsFactory _xmlSettings;

        public MusicBrainzArtistProvider(IXmlReaderSettingsFactory xmlSettings)
        {
            _xmlSettings = xmlSettings;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
        {
            var musicBrainzId = searchInfo.GetMusicBrainzArtistId();

            if (!string.IsNullOrWhiteSpace(musicBrainzId))
            {
                var url = string.Format("/ws/2/artist/?query=arid:{0}", musicBrainzId);

                using (var stream = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, false, cancellationToken)
                            .ConfigureAwait(false))
                {
                    return GetResultsFromResponse(stream);
                }
            }
            else
            {
                // They seem to throw bad request failures on any term with a slash
                var nameToSearch = searchInfo.Name.Replace('/', ' ');

                var url = String.Format("/ws/2/artist/?query=artist:\"{0}\"", UrlEncode(nameToSearch));

                using (var stream = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false))
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
                    url = String.Format("/ws/2/artist/?query=artistaccent:\"{0}\"", UrlEncode(nameToSearch));

                    using (var stream = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false))
                    {
                        return GetResultsFromResponse(stream);
                    }
                }
            }

            return new List<RemoteSearchResult>();
        }

        private IEnumerable<RemoteSearchResult> GetResultsFromResponse(Stream stream)
        {
            using (var oReader = new StreamReader(stream, Encoding.UTF8))
            {
                var settings = _xmlSettings.Create(false);

                settings.CheckCharacters = false;
                settings.IgnoreProcessingInstructions = true;
                settings.IgnoreComments = true;

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
                                            return ParseArtistList(subReader);
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

                    return new List<RemoteSearchResult>();
                }
            }
        }

        private IEnumerable<RemoteSearchResult> ParseArtistList(XmlReader reader)
        {
            var list = new List<RemoteSearchResult>();

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
                                        list.Add(artist);
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

            return list;
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

            var musicBrainzId = id.GetMusicBrainzArtistId();

            if (string.IsNullOrWhiteSpace(musicBrainzId))
            {
                var searchResults = await GetSearchResults(id, cancellationToken).ConfigureAwait(false);

                var singleResult = searchResults.FirstOrDefault();

                if (singleResult != null)
                {
                    musicBrainzId = singleResult.GetProviderId(MetadataProviders.MusicBrainzArtist);
                    //result.Item.Name = singleResult.Name;
                    result.Item.Overview = singleResult.Overview;
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
            return !String.Equals(text, text.RemoveDiacritics(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Encodes an URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        private string UrlEncode(string name)
        {
            return WebUtility.UrlEncode(name);
        }

        public string Name
        {
            get { return "MusicBrainz"; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
