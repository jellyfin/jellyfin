#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaSegments;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixManualMediaSegmentProvider : IMediaSegmentProvider
{
    public const string ProviderName = "CustomNetflix Manual Overrides";

    private readonly ICustomNetflixRepository _repository;
    private readonly CustomNetflixSchemaState _schemaState;

    public CustomNetflixManualMediaSegmentProvider(
        ICustomNetflixRepository repository,
        CustomNetflixSchemaState schemaState)
    {
        _repository = repository;
        _schemaState = schemaState;
    }

    public string Name => ProviderName;

    public bool OverridesOtherProviders => true;

    public async Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(
        MediaSegmentGenerationRequest request,
        CancellationToken cancellationToken)
        => (await _repository.GetManualMediaSegmentsAsync(request.ItemId, null, cancellationToken).ConfigureAwait(false))
            .Select(Map)
            .Where(segment => segment.Type != MediaSegmentType.Unknown && segment.EndTicks > segment.StartTicks)
            .ToArray();

    public ValueTask<bool> Supports(BaseItem item)
        => ValueTask.FromResult(
            _repository.IsEnabled
            && _schemaState.IsReady
            && item.MediaType is MediaType.Video or MediaType.Audio);

    public Task CleanupExtractedData(Guid itemId, CancellationToken cancellationToken)
        => Task.CompletedTask;

    private static MediaSegmentDto Map(CustomMediaSegmentRow segment)
        => new()
        {
            Id = segment.Id,
            ItemId = segment.ItemId,
            Type = CustomNetflixSegmentTypeMapper.ToNativeSegmentType(segment.SegmentType),
            StartTicks = TimeSpan.FromSeconds(segment.StartSeconds).Ticks,
            EndTicks = TimeSpan.FromSeconds(segment.EndSeconds).Ticks
        };
}
