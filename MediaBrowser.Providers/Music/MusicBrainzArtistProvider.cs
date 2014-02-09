using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
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
        public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo id, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicArtist>();

            var musicBrainzId = id.GetMusicBrainzArtistId() ?? await FindId(id, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(musicBrainzId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                result.Item = new MusicArtist();
                result.HasMetadata = true;

                result.Item.SetProviderId(MetadataProviders.MusicBrainzArtist, musicBrainzId);
            }

            return result;
        }
        
        /// <summary>
        /// Finds the id from music brainz.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> FindId(ItemLookupInfo item, CancellationToken cancellationToken)
        {
            // They seem to throw bad request failures on any term with a slash
            var nameToSearch = item.Name.Replace('/', ' ');

            var url = String.Format("http://www.musicbrainz.org/ws/2/artist/?query=artist:\"{0}\"", UrlEncode(nameToSearch));

            var doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
            var node = doc.SelectSingleNode("//mb:artist-list/mb:artist/@id", ns);

            if (node != null && node.Value != null)
            {
                return node.Value;
            }

            if (HasDiacritics(item.Name))
            {
                // Try again using the search with accent characters url
                url = String.Format("http://www.musicbrainz.org/ws/2/artist/?query=artistaccent:\"{0}\"", UrlEncode(nameToSearch));

                doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

                ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
                node = doc.SelectSingleNode("//mb:artist-list/mb:artist/@id", ns);

                if (node != null && node.Value != null)
                {
                    return node.Value;
                }
            }

            return null;
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
    }
}
