#nullable disable

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.StudioImages
{
    /// <summary>
    /// Studio image provider. Serves images from the local jellyfin-artwork bundle maintained by
    /// <see cref="RefreshStudioArtworkTask"/>; returns nothing when no local match exists.
    /// </summary>
    public class StudiosImageProvider : IRemoteImageProvider
    {
        /// <inheritdoc />
        public string Name => "Artwork Repository";

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Studio;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return [ImageType.Primary, ImageType.Thumb, ImageType.Logo];
        }

        /// <inheritdoc />
        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var slug = StudioArtworkManager.ResolveStudioSlug(item.Name, item.ProviderIds);
            var hasPrimary = StudioArtworkManager.TryGetStudioImagePath(slug, "primary", out var primaryPath);
            var hasThumb = StudioArtworkManager.TryGetStudioImagePath(slug, "thumb", out var thumbPath);
            var hasLogo = StudioArtworkManager.TryGetStudioImagePath(slug, "logo", out var logoPath);

            // Last-resort: bundle-wide placeholders at <artworkRoot>/placeholder-<kind>.<ext>.
            // These let clients render *something* for studios that aren't represented in the
            // bundle, instead of leaving the slot empty. The bundle ships placeholder-primary
            // and placeholder-thumb; there's no dedicated logo placeholder, so the logo slot
            // falls back to placeholder-primary as the closest visual analog.
            if (!hasPrimary && StudioArtworkManager.TryGetPlaceholderImagePath("primary", out var placeholderPrimaryPath))
            {
                hasPrimary = true;
                primaryPath = placeholderPrimaryPath;
            }

            if (!hasThumb && StudioArtworkManager.TryGetPlaceholderImagePath("thumb", out var placeholderThumbPath))
            {
                hasThumb = true;
                thumbPath = placeholderThumbPath;
            }

            if (!hasLogo && StudioArtworkManager.TryGetPlaceholderImagePath("primary", out var placeholderLogoPath))
            {
                hasLogo = true;
                logoPath = placeholderLogoPath;
            }

            var results = new List<RemoteImageInfo>(capacity: 3);

            if (hasPrimary)
            {
                results.Add(new RemoteImageInfo { ProviderName = Name, Type = ImageType.Primary, Url = primaryPath });
            }

            if (hasThumb)
            {
                results.Add(new RemoteImageInfo { ProviderName = Name, Type = ImageType.Thumb, Url = thumbPath });
            }

            if (hasLogo)
            {
                results.Add(new RemoteImageInfo { ProviderName = Name, Type = ImageType.Logo, Url = logoPath });
            }

            return Task.FromResult<IEnumerable<RemoteImageInfo>>(results);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            // The only URLs this provider hands out are local paths into the artwork bundle, so
            // serve them straight from disk. Read fully into memory because the returned
            // HttpResponseMessage outlives this method; a StreamContent wrapping a still-open
            // FileStream would either leak the handle or be disposed too early.
            var bytes = await File.ReadAllBytesAsync(url, cancellationToken).ConfigureAwait(false);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };

            var mime = MimeTypes.GetMimeType(url);
            if (!string.IsNullOrEmpty(mime))
            {
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
            }

            return response;
        }

        /// <summary>
        /// Converts a display name to the kebab-case machine-name used by the jellyfin-artwork layout.
        /// </summary>
        /// <remarks>
        /// Falls back to ICU transliteration (<see cref="StringExtensions.Transliterated(string)"/>)
        /// when plain NFKD normalisation yields nothing - that's the case for names written entirely
        /// in non-Latin scripts (CJK, Cyrillic, Arabic, ...). Without the fallback those names would
        /// slug to the empty string and never resolve against the bundle.
        /// </remarks>
        /// <param name="name">The display name.</param>
        /// <returns>The slug.</returns>
        public static string Slugify(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var slug = SlugifyCore(name);
            if (!string.IsNullOrEmpty(slug))
            {
                return slug;
            }

            // Names made up entirely of glyphs the ASCII filter drops (e.g. "中央电视台",
            // "Кино", "السينما") slug to the empty string. Run the original through the
            // ICU transliterator - which maps Han → Latin via pinyin, Cyrillic via BGN, etc. -
            // and re-slug the Latin result. The transliterator's output is already lowercase
            // ASCII with diacritics and punctuation stripped, but SlugifyCore still handles
            // collapsing whitespace to hyphens and trimming.
            return SlugifyCore(name.Transliterated());
        }

        private static string SlugifyCore(string name)
        {
            // The jellyfin-artwork bundle uses ASCII-only kebab-case directory names. NFKD
            // (compatibility) decomposition reduces most Latin accents to base letter +
            // combining mark (e.g. "é" -> "e" + COMBINING ACUTE; we drop the combining mark)
            // AND expands compatibility characters to their ASCII equivalents - superscripts
            // (² -> 2, ³ -> 3), roman numerals (Ⅱ -> II), ligatures (ﬁ -> fi), etc. Plain NFD
            // would skip those, so e.g. "EMT²" -> "emt" instead of "emt2", which doesn't
            // match the bundle's "studios/emt2/" directory (the bundle is built with NFKD).
            // Letters that do not decompose (ł, ø, đ, ...) and non-Latin scripts (CJK,
            // Cyrillic, ...) are NOT letter-or-digit in the ASCII range, so they become
            // hyphens.
            var normalized = name.Normalize(NormalizationForm.FormKD);
            var builder = new StringBuilder(normalized.Length);
            var lastWasHyphen = true;

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (c < 128 && char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                    lastWasHyphen = false;
                }
                else if (!lastWasHyphen)
                {
                    builder.Append('-');
                    lastWasHyphen = true;
                }
            }

            return builder.ToString().Trim('-');
        }
    }
}
