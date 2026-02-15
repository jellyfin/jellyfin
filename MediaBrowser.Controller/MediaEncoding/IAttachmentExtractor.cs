#nullable disable

#pragma warning disable CS1591

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding;

public interface IAttachmentExtractor
{
    /// <summary>
    /// Gets the path to the attachment file.
    /// </summary>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="attachmentStreamIndex">The attachment index.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The async task.</returns>
    Task<(MediaAttachment Attachment, Stream Stream)> GetAttachment(
        BaseItem item,
        string mediaSourceId,
        int attachmentStreamIndex,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the path to the attachment file.
    /// </summary>
    /// <param name="inputFile">The input file path.</param>
    /// <param name="mediaSource">The <see cref="MediaSourceInfo" /> source id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The async task.</returns>
    Task ExtractAllAttachments(
        string inputFile,
        MediaSourceInfo mediaSource,
        CancellationToken cancellationToken);
}
