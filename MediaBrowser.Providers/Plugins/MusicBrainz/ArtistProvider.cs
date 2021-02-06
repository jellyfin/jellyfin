#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Music;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class ArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
    {
        public string Name => "MusicBrainz";

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
        {
            var musicBrainzId = searchInfo.GetMusicBrainzArtistId();

            if (!string.IsNullOrWhiteSpace(musicBrainzId))
            {
                var url = "/ws/2/artist/?query=arid:{0}" + musicBrainzId.ToString(CultureInfo.InvariantCulture);

                using var response = await AlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return GetResultsFromResponse(stream);
            }
            else
            {
                // They seem to throw bad request failures on any term with a slash
                var nameToSearch = searchInfo?.Name.Replace('/', ' ');

                var url = string.Format(CultureInfo.InvariantCulture, "/ws/2/artist/?query=\"{0}\"&dismax=true", UrlEncode(nameToSearch));

                using (var response = await AlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false))
                await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
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
                    url = string.Format(CultureInfo.InvariantCulture, "/ws/2/artist/?query=artistaccent:\"{0}\"", UrlEncode(nameToSearch));

                    using var response = await AlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    return GetResultsFromResponse(stream);
                }
            }

            return Enumerable.Empty<RemoteSearchResult>();
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
                    musicBrainzId = singleResult.GetProviderId(MetadataProvider.MusicBrainzArtist);
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
                result.Item.SetProviderId(MetadataProvider.MusicBrainzArtist, musicBrainzId);
            }

            return result;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<RemoteSearchResult> GetResultsFromResponse(Stream stream)
        {
            var settings = new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

            using var oReader = new StreamReader(stream, Encoding.UTF8);
            using var reader = XmlReader.Create(oReader, settings);
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if ("artist-list".Equals(reader.Name, StringComparison.Ordinal))
                    {
                        if (reader.IsEmptyElement)
                        {
                            reader.Read();
                            continue;
                        }

                        using var subReader = reader.ReadSubtree();
                        return ParseArtistList(subReader).ToList();
                    }

                    reader.Skip();
                }
                else
                {
                    reader.Read();
                }
            }

            return Enumerable.Empty<RemoteSearchResult>();
        }

        private static IEnumerable<RemoteSearchResult> ParseArtistList(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if ("artist".Equals(reader.Name, StringComparison.Ordinal))
                    {
                        if (reader.IsEmptyElement)
                        {
                            reader.Read();
                            continue;
                        }

                        var mbzId = reader.GetAttribute("id");

                        using var subReader = reader.ReadSubtree();
                        var artist = ParseArtist(subReader, mbzId);
                        if (artist != null)
                        {
                            yield return artist;
                        }
                    }
                    else
                    {
                        reader.Skip();
                        break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private static RemoteSearchResult ParseArtist(XmlReader reader, string artistId)
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
                            result.Name = reader.ReadElementContentAsString();
                            break;

                        case "annotation":
                            result.Overview = reader.ReadElementContentAsString();
                            break;

                        default:
                            // there is sort-name if ever needed
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            result.SetProviderId(MetadataProvider.MusicBrainzArtist, artistId);

            if (string.IsNullOrWhiteSpace(artistId) || string.IsNullOrWhiteSpace(result.Name))
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Determines whether the specified text has diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the specified text has diacritics; otherwise, <c>false</c>.</returns>
        private static bool HasDiacritics(string text)
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
    }
}
