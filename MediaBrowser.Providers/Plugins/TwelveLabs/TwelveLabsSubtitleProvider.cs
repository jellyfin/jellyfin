#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.TwelveLabs
{
    /// <summary>
    /// Subtitle provider that uses the TwelveLabs Pegasus video understanding model to generate
    /// a transcript for a library item and exposes it as a WebVTT subtitle track.
    /// </summary>
    /// <remarks>
    /// Pegasus analysis runs server-side, so the media file must be reachable by the TwelveLabs API
    /// over HTTP. The provider maps a configured local path prefix to a public URL prefix; if either
    /// the API key or the URL mapping is not configured it returns no results, making it fully opt-in.
    /// </remarks>
    public class TwelveLabsSubtitleProvider : ISubtitleProvider, IHasOrder
    {
        private const string AnalyzeUrl = "https://api.twelvelabs.io/v1.3/analyze";

        private readonly ILogger<TwelveLabsSubtitleProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILocalizationManager _localizationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwelveLabsSubtitleProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="localizationManager">The localization manager.</param>
        public TwelveLabsSubtitleProvider(
            ILogger<TwelveLabsSubtitleProvider> logger,
            IHttpClientFactory httpClientFactory,
            ILocalizationManager localizationManager)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _localizationManager = localizationManager;
        }

        /// <inheritdoc />
        public string Name => "TwelveLabs";

        /// <inheritdoc />
        public IEnumerable<VideoContentType> SupportedMediaTypes => new[] { VideoContentType.Episode, VideoContentType.Movie };

        // Run after community/file-based fetchers; generation is a fallback, not a preferred source.
        /// <inheritdoc />
        public int Order => 100;

        /// <inheritdoc />
        public Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance.Configuration;
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                return Task.FromResult(Enumerable.Empty<RemoteSubtitleInfo>());
            }

            var url = BuildPublicMediaUrl(request.MediaPath, config.MediaPathPrefix, config.MediaUrlPrefix);
            if (url is null)
            {
                return Task.FromResult(Enumerable.Empty<RemoteSubtitleInfo>());
            }

            var language = request.Language ?? request.TwoLetterISOLanguageName ?? "en";

            // The id round-trips back to GetSubtitles. The subtitle manager strips the provider
            // prefix on the first underscore, so the remainder is delivered intact.
            var id = string.Join('|', language, url);

            var result = new RemoteSubtitleInfo
            {
                Id = id,
                ProviderName = Name,
                Name = "TwelveLabs Pegasus (AI generated)",
                Format = "vtt",
                ThreeLetterISOLanguageName = NormalizeLanguage(language),
                AiTranslated = true,
                MachineTranslated = true,
                IsHashMatch = false
            };

            return Task.FromResult<IEnumerable<RemoteSubtitleInfo>>(new[] { result });
        }

        /// <inheritdoc />
        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(id);

            var config = Plugin.Instance.Configuration;
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException("TwelveLabs API key is not configured.");
            }

            var parts = id.Split('|', 2);
            if (parts.Length != 2)
            {
                throw new ArgumentException("Malformed TwelveLabs subtitle id.", nameof(id));
            }

            var language = parts[0];
            var mediaUrl = parts[1];

            var transcript = await AnalyzeAsync(mediaUrl, config, cancellationToken).ConfigureAwait(false);
            var vtt = BuildWebVtt(transcript);

            return new SubtitleResponse
            {
                Language = NormalizeLanguage(language),
                Format = "vtt",
                IsForced = false,
                Stream = new MemoryStream(Encoding.UTF8.GetBytes(vtt))
            };
        }

        private async Task<string> AnalyzeAsync(string mediaUrl, Configuration.PluginConfiguration config, CancellationToken cancellationToken)
        {
            var body = new AnalyzeRequest
            {
                ModelName = string.IsNullOrWhiteSpace(config.ModelName) ? "pegasus1.5" : config.ModelName,
                Video = new AnalyzeRequest.VideoSource { Type = "url", Url = mediaUrl },
                Prompt = config.Prompt,
                Stream = false,
                MaxTokens = 4096
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnalyzeUrl);
            httpRequest.Headers.Add("x-api-key", config.ApiKey);
            httpRequest.Content = JsonContent.Create(body);

            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            using var response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError("TwelveLabs analyze request failed with status {StatusCode}: {Error}", response.StatusCode, error);
                response.EnsureSuccessStatusCode();
            }

            var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>(cancellationToken).ConfigureAwait(false);
            var text = result?.Data?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                throw new InvalidOperationException("TwelveLabs returned an empty analysis result.");
            }

            return text;
        }

        /// <summary>
        /// Maps a local media path to a public URL the TwelveLabs API can fetch, using the configured
        /// path/URL prefixes. Returns <c>null</c> when the provider should stay disabled for this item.
        /// </summary>
        /// <param name="mediaPath">The local media path (or an existing http(s) URL).</param>
        /// <param name="mediaPathPrefix">The configured local path prefix.</param>
        /// <param name="mediaUrlPrefix">The configured public URL prefix.</param>
        /// <returns>A public URL, or <c>null</c> if one cannot be produced.</returns>
        public static string? BuildPublicMediaUrl(string? mediaPath, string? mediaPathPrefix, string? mediaUrlPrefix)
        {
            if (string.IsNullOrWhiteSpace(mediaUrlPrefix) || string.IsNullOrWhiteSpace(mediaPath))
            {
                return null;
            }

            // Already a URL the API can fetch directly.
            if (mediaPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || mediaPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return mediaPath;
            }

            var pathPrefix = mediaPathPrefix ?? string.Empty;
            if (pathPrefix.Length > 0 && !mediaPath.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // This item lives outside the mapped library root; cannot build a public URL for it.
                return null;
            }

            var relative = mediaPath[pathPrefix.Length..]
                .Replace('\\', '/')
                .TrimStart('/');

            var escaped = string.Join('/', relative.Split('/').Select(Uri.EscapeDataString));

            return string.Concat(mediaUrlPrefix.TrimEnd('/'), "/", escaped);
        }

        private string NormalizeLanguage(string language)
        {
            var culture = _localizationManager.FindLanguageInfo(language);
            return culture?.ThreeLetterISOLanguageName ?? language;
        }

        internal static string BuildWebVtt(string transcript)
        {
            // ponytail: single full-length cue. Pegasus returns prose, not timestamped segments,
            // so we emit one cue spanning a long window rather than faking per-line timings.
            var builder = new StringBuilder();
            builder.Append("WEBVTT\n\n");
            builder.Append("00:00:00.000 --> 99:59:59.000\n");
            builder.Append(transcript.Replace("\r\n", "\n", StringComparison.Ordinal).Trim());
            builder.Append('\n');
            return builder.ToString();
        }

        private sealed class AnalyzeRequest
        {
            [JsonPropertyName("model_name")]
            public string ModelName { get; set; } = "pegasus1.5";

            [JsonPropertyName("video")]
            public VideoSource Video { get; set; } = new VideoSource();

            [JsonPropertyName("prompt")]
            public string Prompt { get; set; } = string.Empty;

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }

            public sealed class VideoSource
            {
                [JsonPropertyName("type")]
                public string Type { get; set; } = "url";

                [JsonPropertyName("url")]
                public string Url { get; set; } = string.Empty;
            }
        }

        private sealed class AnalyzeResponse
        {
            [JsonPropertyName("data")]
            public string? Data { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }
    }
}
