using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
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
        /// Gets the subtitles.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SubtitleResponse}.</returns>
        Task<SubtitleResponse> GetSubtitles(SubtitleRequest request, CancellationToken cancellationToken);
    }

    public enum SubtitleMediaType
    {
        Episode = 0,
        Movie = 1
    }

    public class SubtitleResponse
    {
        public string Format { get; set; }
        public bool HasContent { get; set; }
        public Stream Stream { get; set; }
    }

    public class SubtitleRequest
    {
        public string Language { get; set; }

        public SubtitleMediaType ContentType { get; set; }

        public string MediaPath { get; set; }
        public string SeriesName { get; set; }
        public string Name { get; set; }
        public int? IndexNumber { get; set; }
        public int? ParentIndexNumber { get; set; }
        public long ImdbId { get; set; }
    }
}
