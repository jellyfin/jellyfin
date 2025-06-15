using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Manager for handling Media Attachments.
/// </summary>
/// <param name="dbProvider">Efcore Factory.</param>
/// <param name="writeBehavior">Instance of the <see cref="IEntityFrameworkDatabaseLockingBehavior"/> interface.</param>
public class MediaAttachmentRepository(IDbContextFactory<JellyfinDbContext> dbProvider, IEntityFrameworkDatabaseLockingBehavior writeBehavior) : IMediaAttachmentRepository
{
    /// <inheritdoc />
    public void SaveMediaAttachments(
        Guid id,
        IReadOnlyList<MediaAttachment> attachments,
        CancellationToken cancellationToken)
    {
        using var context = dbProvider.CreateDbContext();
        using var dbLock = writeBehavior.AcquireWriterLock(context);
        using var transaction = context.Database.BeginTransaction();
        context.AttachmentStreamInfos.Where(e => e.ItemId.Equals(id)).ExecuteDelete();
        context.AttachmentStreamInfos.AddRange(attachments.Select(e => Map(e, id)));
        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc />
    public IReadOnlyList<MediaAttachment> GetMediaAttachments(MediaAttachmentQuery filter)
    {
        AttachmentStreamInfo[] items;
        using (var context = dbProvider.CreateDbContext())
        {
            using var dbLock = writeBehavior.AcquireReaderLock(context);
            var query = context.AttachmentStreamInfos.AsNoTracking().Where(e => e.ItemId.Equals(filter.ItemId));
            if (filter.Index.HasValue)
            {
                query = query.Where(e => e.Index == filter.Index);
            }

            items = query.ToArray();
        }

        return items.Select(Map).ToArray();
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
