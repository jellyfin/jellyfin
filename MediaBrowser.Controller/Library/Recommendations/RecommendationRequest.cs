using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;

namespace MediaBrowser.Controller.Library.Recommendations;

/// <summary>
/// Input record for a recommendations request.
/// </summary>
public sealed record RecommendationRequest(
    Guid UserId,
    BaseItemKind Kind,
    Guid? ParentId,
    int CategoryLimit,
    int ItemLimit,
    DtoOptions DtoOptions);
