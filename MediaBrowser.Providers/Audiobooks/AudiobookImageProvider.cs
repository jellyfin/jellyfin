using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Audiobooks;

/// <summary>
/// Provides the primary image for audiobook items that have embedded covers.
/// </summary>
public class AudiobookImageProvider : IDynamicImageProvider
{
    private readonly ILogger<AudiobookImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudiobookImageProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{AudiobookImageProvider}"/> interface.</param>
    public AudiobookImageProvider(ILogger<AudiobookImageProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Audiobook Metadata";

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        if (item is not AudioBook)
        {
            return false;
        }

        var extension = Path.GetExtension(item.Path);
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return !string.IsNullOrEmpty(item.Path) &&
               AudiobookUtils.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    /// <inheritdoc />
    public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
    {
        if (type != ImageType.Primary)
        {
            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }

        if (!AudiobookUtils.IsValidAudiobookFile(item.Path))
        {
            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }

        try
        {
            return Task.FromResult(ExtractCoverFromAudiobook(item.Path, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting cover from audiobook file: {Path}", item.Path);
            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }
    }

    private DynamicImageResponse ExtractCoverFromAudiobook(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;

            if (tag?.Pictures == null || tag.Pictures.Length == 0)
            {
                return new DynamicImageResponse { HasImage = false };
            }

            // Get the first picture (usually the cover)
            var picture = tag.Pictures[0];

            if (picture.Data?.Data == null || picture.Data.Data.Length == 0)
            {
                return new DynamicImageResponse { HasImage = false };
            }

            var memoryStream = new MemoryStream(picture.Data.Data);

            var response = new DynamicImageResponse
            {
                HasImage = true,
                Stream = memoryStream
            };

            // Set the format based on the MIME type
            if (!string.IsNullOrEmpty(picture.MimeType))
            {
                response.SetFormatFromMimeType(picture.MimeType);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract cover art from audiobook file: {Path}", filePath);
            return new DynamicImageResponse { HasImage = false };
        }
    }
}
