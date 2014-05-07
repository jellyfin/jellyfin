using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Subtitles
{
    public interface ISubtitleProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the supported media types.
        /// </summary>
        /// <value>The supported media types.</value>
        IEnumerable<SubtitleMediaType> SupportedMediaTypes { get; }

        /// <summary>
        /// Searches the subtitles.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteSubtitleInfo}}.</returns>
        Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(SubtitleSearchRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the subtitles.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SubtitleResponse}.</returns>
        Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken);
    }

    public enum SubtitleMediaType
    {
        Episode = 0,
        Movie = 1
    }

    public class SubtitleResponse
    {
        public string Language { get; set; }
        public string Format { get; set; }
        public Stream Stream { get; set; }
    }

    public class SubtitleSearchRequest : IHasProviderIds
    {
        public string Language { get; set; }

        public SubtitleMediaType ContentType { get; set; }

        public string MediaPath { get; set; }
        public string SeriesName { get; set; }
        public string Name { get; set; }
        public int? IndexNumber { get; set; }
        public int? IndexNumberEnd { get; set; }
        public int? ParentIndexNumber { get; set; }
        public int? ProductionYear { get; set; }
        public Dictionary<string, string> ProviderIds { get; set; }

        public SubtitleSearchRequest()
        {
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
