using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Manager for handling Media Attachments.
/// </summary>
/// <param name="dbProvider">Efcore Factory.</param>
public class MediaAttachmentRepository(IDbContextFactory<JellyfinDbContext> dbProvider) : IMediaAttachmentRepository
{
    /// <inheritdoc />
    public void SaveMediaAttachments(
        Guid id,
        IReadOnlyList<MediaAttachment> attachments,
        CancellationToken cancellationToken)
    {
        using var context = dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        context.AttachmentStreamInfos.Where(e => e.ItemId.Equals(id)).ExecuteDelete();
        context.AttachmentStreamInfos.AddRange(attachments.Select(e => Map(e, id)));
        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc />
    public IReadOnlyList<MediaAttachment> GetMediaAttachments(MediaAttachmentQuery filter)
    {
        using var context = dbProvider.CreateDbContext();
        var query = context.AttachmentStreamInfos.AsNoTracking().Where(e => e.ItemId.Equals(filter.ItemId));
        if (filter.Index.HasValue)
        {
            query = query.Where(e => e.Index == filter.Index);
        }

        return query.AsEnumerable().Select(Map).ToArray();
    }

    private MediaAttachment Map(AttachmentStreamInfo attachment)
    {
        return new MediaAttachment()
        {
            Codec = attachment.Codec,
            CodecTag = attachment.CodecTag,
            Comment = attachment.Comment,
            FileName = attachment.Filename,
            Index = attachment.Index,
            MimeType = attachment.MimeType,
        };
    }

    private AttachmentStreamInfo Map(MediaAttachment attachment, Guid id)
    {
        return new AttachmentStreamInfo()
        {
            Codec = attachment.Codec,
            CodecTag = attachment.CodecTag,
            Comment = attachment.Comment,
            Filename = attachment.FileName,
            Index = attachment.Index,
            MimeType = attachment.MimeType,
            ItemId = id,
            Item = null!
        };
    }
}
