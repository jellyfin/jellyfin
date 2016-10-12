using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
    {
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
        {
            var musicBrainzId = searchInfo.GetMusicBrainzArtistId();

            if (!string.IsNullOrWhiteSpace(musicBrainzId))
            {
                var url = string.Format("/ws/2/artist/?query=arid:{0}", musicBrainzId);

                var doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, false, cancellationToken)
                            .ConfigureAwait(false);

                return GetResultsFromResponse(doc);
            }
            else
            {
                // They seem to throw bad request failures on any term with a slash
                var nameToSearch = searchInfo.Name.Replace('/', ' ');

                var url = String.Format("/ws/2/artist/?query=artist:\"{0}\"", UrlEncode(nameToSearch));

                var doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false);

                var results = GetResultsFromResponse(doc).ToList();

                if (results.Count > 0)
                {
                    return results;
                }

                if (HasDiacritics(searchInfo.Name))
                {
                    // Try again using the search with accent characters url
                    url = String.Format("/ws/2/artist/?query=artistaccent:\"{0}\"", UrlEncode(nameToSearch));

                    doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false);

                    return GetResultsFromResponse(doc);
                }
            }

            return new List<RemoteSearchResult>();
        }

        private IEnumerable<RemoteSearchResult> GetResultsFromResponse(XmlDocument doc)
        {
            var list = new List<RemoteSearchResult>();

            var docElem = doc.DocumentElement;

            if (docElem == null)
            {
                return list;
            }

            var artistList = docElem.FirstChild;
            if (artistList == null)
            {
                return list;
            }

            var nodes = artistList.ChildNodes;

            if (nodes != null)
            {
                foreach (var node in nodes.Cast<XmlNode>())
                {
                    if (node.Attributes != null)
                    {
                        string name = null;
                        string overview = null;
                        string mbzId = node.Attributes["id"].Value;

                        foreach (var child in node.ChildNodes.Cast<XmlNode>())
                        {
                            if (string.Equals(child.Name, "name", StringComparison.OrdinalIgnoreCase))
                            {
                                name = child.InnerText;
                            }
                            if (string.Equals(child.Name, "annotation", StringComparison.OrdinalIgnoreCase))
                            {
                                overview = child.InnerText;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(mbzId) && !string.IsNullOrWhiteSpace(name))
                        {
                            var result = new RemoteSearchResult
                            {
                                Name = name,
                                Overview = overview
                            };

                            result.SetProviderId(MetadataProviders.MusicBrainzArtist, mbzId);

                            list.Add(result);
                        }
                    }
                }
            }

            return list;
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
            return !String.Equals(text, RemoveDiacritics(text), StringComparison.Ordinal);
        }

        /// <summary>
        /// Removes the diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        private string RemoveDiacritics(string text)
        {
            return String.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
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
