#nullable disable

#pragma warning disable CS1591

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface IAttachmentExtractor
    {
        Task<(MediaAttachment Attachment, Stream Stream)> GetAttachment(
            BaseItem item,
            string mediaSourceId,
            int attachmentStreamIndex,
            CancellationToken cancellationToken);

        Task ExtractAllAttachments(
            string inputFile,
            MediaSourceInfo mediaSource,
            string outputPath,
            CancellationToken cancellationToken);

        Task ExtractAllAttachmentsExternal(
            string inputArgument,
            string id,
            string outputPath,
            CancellationToken cancellationToken);
    }
}
