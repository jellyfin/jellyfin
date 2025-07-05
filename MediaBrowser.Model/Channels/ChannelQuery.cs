#pragma warning disable CA1819 // Properties should not return arrays

using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Channels;

/// <summary>
/// Gets or sets the fields to return within the items, in addition to basic information.
/// </summary>
/// <value>The fields.</value>
public class ChannelQuery
{
    /// <summary>
    /// Gets or sets the fields to return within the items, in addition to basic information.
    /// </summary>
    /// <value>The fields.</value>
    public ItemFields[]? Fields { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [enable images].
    /// </summary>
    /// <value><c>true</c> if [enable images]; otherwise, <c>false</c>.</value>
    public bool? EnableImages { get; set; }

    /// <summary>
    /// Gets or sets the image type limit.
    /// </summary>
    /// <value>The image type limit.</value>
    public int? ImageTypeLimit { get; set; }

    /// <summary>
    /// Gets or sets the enabled image types.
    /// </summary>
    /// <value>The enabled image types.</value>
    public ImageType[]? EnableImageTypes { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    /// <value>The user identifier.</value>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the start index. Use for paging.
    /// </summary>
    /// <value>The start index.</value>
    public int? StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to return.
    /// </summary>
    /// <value>The limit.</value>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [supports latest items].
    /// </summary>
    /// <value><c>true</c> if [supports latest items]; otherwise, <c>false</c>.</value>
    public bool? SupportsLatestItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [supports media deletion].
    /// </summary>
    /// <value><c>true</c> if [supports media deletion]; otherwise, <c>false</c>.</value>
    public bool? SupportsMediaDeletion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is favorite.
    /// </summary>
    /// <value><c>null</c> if [is favorite] contains no value, <c>true</c> if [is favorite]; otherwise, <c>false</c>.</value>
    public bool? IsFavorite { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is a recording folder.
    /// </summary>
    /// <value><c>true</c> if [is recording folder]; otherwise, <c>false</c>.</value>
    public bool? IsRecordingsFolder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether latest channel items should be refreshed.
    /// </summary>
    /// <value><c>true</c> if [refresh latest channel items]; otherwise, <c>false</c>.</value>
    public bool RefreshLatestChannelItems { get; set; }
}
