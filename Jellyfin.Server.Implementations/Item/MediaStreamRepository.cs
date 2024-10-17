using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Initializes a new instance of the <see cref="MediaStreamRepository"/> class.
/// </summary>
/// <param name="dbProvider">The EFCore db factory.</param>
/// <param name="serverApplicationHost">The Application host.</param>
/// <param name="localization">The Localisation Provider.</param>
public class MediaStreamRepository(IDbContextFactory<JellyfinDbContext> dbProvider, IServerApplicationHost serverApplicationHost, ILocalizationManager localization) : IMediaStreamRepository
{
    /// <inheritdoc />
    public void SaveMediaStreams(Guid id, IReadOnlyList<MediaStream> streams, CancellationToken cancellationToken)
    {
        using var context = dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        context.MediaStreamInfos.Where(e => e.ItemId.Equals(id)).ExecuteDelete();
        context.MediaStreamInfos.AddRange(streams.Select(f => Map(f, id)));
        context.SaveChanges();

        transaction.Commit();
    }

    /// <inheritdoc />
    public IReadOnlyList<MediaStream> GetMediaStreams(MediaStreamQuery filter)
    {
        using var context = dbProvider.CreateDbContext();
        return TranslateQuery(context.MediaStreamInfos.AsNoTracking(), filter).AsEnumerable().Select(Map).ToImmutableArray();
    }

    private string? GetPathToSave(string? path)
    {
        if (path is null)
        {
            return null;
        }

        return serverApplicationHost.ReverseVirtualPath(path);
    }

    private string? RestorePath(string? path)
    {
        if (path is null)
        {
            return null;
        }

        return serverApplicationHost.ExpandVirtualPath(path);
    }

    private IQueryable<MediaStreamInfo> TranslateQuery(IQueryable<MediaStreamInfo> query, MediaStreamQuery filter)
    {
        query = query.Where(e => e.ItemId.Equals(filter.ItemId));
        if (filter.Index.HasValue)
        {
            query = query.Where(e => e.StreamIndex == filter.Index);
        }

        if (filter.Type.HasValue)
        {
            var typeValue = (MediaStreamTypeEntity)filter.Type.Value;
            query = query.Where(e => e.StreamType!.Value == typeValue);
        }

        return query;
    }

    private MediaStream Map(MediaStreamInfo entity)
    {
        var dto = new MediaStream();
        dto.Index = entity.StreamIndex;
        if (entity.StreamType != null)
        {
            dto.Type = (MediaStreamType)entity.StreamType;
        }

        dto.IsAVC = entity.IsAvc;
        dto.Codec = entity.Codec;
        dto.Language = entity.Language;
        dto.ChannelLayout = entity.ChannelLayout;
        dto.Profile = entity.Profile;
        dto.AspectRatio = entity.AspectRatio;
        dto.Path = RestorePath(entity.Path);
        dto.IsInterlaced = entity.IsInterlaced;
        dto.BitRate = entity.BitRate;
        dto.Channels = entity.Channels;
        dto.SampleRate = entity.SampleRate;
        dto.IsDefault = entity.IsDefault;
        dto.IsForced = entity.IsForced;
        dto.IsExternal = entity.IsExternal;
        dto.Height = entity.Height;
        dto.Width = entity.Width;
        dto.AverageFrameRate = entity.AverageFrameRate;
        dto.RealFrameRate = entity.RealFrameRate;
        dto.Level = entity.Level;
        dto.PixelFormat = entity.PixelFormat;
        dto.BitDepth = entity.BitDepth;
        dto.IsAnamorphic = entity.IsAnamorphic;
        dto.RefFrames = entity.RefFrames;
        dto.CodecTag = entity.CodecTag;
        dto.Comment = entity.Comment;
        dto.NalLengthSize = entity.NalLengthSize;
        dto.Title = entity.Title;
        dto.TimeBase = entity.TimeBase;
        dto.CodecTimeBase = entity.CodecTimeBase;
        dto.ColorPrimaries = entity.ColorPrimaries;
        dto.ColorSpace = entity.ColorSpace;
        dto.ColorTransfer = entity.ColorTransfer;
        dto.DvVersionMajor = entity.DvVersionMajor;
        dto.DvVersionMinor = entity.DvVersionMinor;
        dto.DvProfile = entity.DvProfile;
        dto.DvLevel = entity.DvLevel;
        dto.RpuPresentFlag = entity.RpuPresentFlag;
        dto.ElPresentFlag = entity.ElPresentFlag;
        dto.BlPresentFlag = entity.BlPresentFlag;
        dto.DvBlSignalCompatibilityId = entity.DvBlSignalCompatibilityId;
        dto.IsHearingImpaired = entity.IsHearingImpaired;
        dto.Rotation = entity.Rotation;

        if (dto.Type is MediaStreamType.Audio or MediaStreamType.Subtitle)
        {
            dto.LocalizedDefault = localization.GetLocalizedString("Default");
            dto.LocalizedExternal = localization.GetLocalizedString("External");

            if (dto.Type is MediaStreamType.Subtitle)
            {
                dto.LocalizedUndefined = localization.GetLocalizedString("Undefined");
                dto.LocalizedForced = localization.GetLocalizedString("Forced");
                dto.LocalizedHearingImpaired = localization.GetLocalizedString("HearingImpaired");
            }
        }

        return dto;
    }

    private MediaStreamInfo Map(MediaStream dto, Guid itemId)
    {
        var entity = new MediaStreamInfo
        {
            Item = null!,
            ItemId = itemId,
            StreamIndex = dto.Index,
            StreamType = (MediaStreamTypeEntity)dto.Type,
            IsAvc = dto.IsAVC.GetValueOrDefault(),

            Codec = dto.Codec,
            Language = dto.Language,
            ChannelLayout = dto.ChannelLayout,
            Profile = dto.Profile,
            AspectRatio = dto.AspectRatio,
            Path = GetPathToSave(dto.Path),
            IsInterlaced = dto.IsInterlaced,
            BitRate = dto.BitRate.GetValueOrDefault(0),
            Channels = dto.Channels.GetValueOrDefault(0),
            SampleRate = dto.SampleRate.GetValueOrDefault(0),
            IsDefault = dto.IsDefault,
            IsForced = dto.IsForced,
            IsExternal = dto.IsExternal,
            Height = dto.Height.GetValueOrDefault(0),
            Width = dto.Width.GetValueOrDefault(0),
            AverageFrameRate = dto.AverageFrameRate.GetValueOrDefault(0),
            RealFrameRate = dto.RealFrameRate.GetValueOrDefault(0),
            Level = (float)dto.Level.GetValueOrDefault(),
            PixelFormat = dto.PixelFormat,
            BitDepth = dto.BitDepth.GetValueOrDefault(0),
            IsAnamorphic = dto.IsAnamorphic.GetValueOrDefault(),
            RefFrames = dto.RefFrames.GetValueOrDefault(0),
            CodecTag = dto.CodecTag,
            Comment = dto.Comment,
            NalLengthSize = dto.NalLengthSize,
            Title = dto.Title,
            TimeBase = dto.TimeBase,
            CodecTimeBase = dto.CodecTimeBase,
            ColorPrimaries = dto.ColorPrimaries,
            ColorSpace = dto.ColorSpace,
            ColorTransfer = dto.ColorTransfer,
            DvVersionMajor = dto.DvVersionMajor.GetValueOrDefault(0),
            DvVersionMinor = dto.DvVersionMinor.GetValueOrDefault(0),
            DvProfile = dto.DvProfile.GetValueOrDefault(0),
            DvLevel = dto.DvLevel.GetValueOrDefault(0),
            RpuPresentFlag = dto.RpuPresentFlag.GetValueOrDefault(0),
            ElPresentFlag = dto.ElPresentFlag.GetValueOrDefault(0),
            BlPresentFlag = dto.BlPresentFlag.GetValueOrDefault(0),
            DvBlSignalCompatibilityId = dto.DvBlSignalCompatibilityId.GetValueOrDefault(0),
            IsHearingImpaired = dto.IsHearingImpaired,
            Rotation = dto.Rotation.GetValueOrDefault(0)
        };
        return entity;
    }
}
