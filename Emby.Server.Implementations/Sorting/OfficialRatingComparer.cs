using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;

namespace Emby.Server.Implementations.Sorting;

/// <summary>
/// Class providing comparison for official ratings.
/// </summary>
public class OfficialRatingComparer : IBaseItemComparer
{
    private readonly ILocalizationManager _localizationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfficialRatingComparer"/> class.
    /// </summary>
    /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    public OfficialRatingComparer(ILocalizationManager localizationManager)
    {
        _localizationManager = localizationManager;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public ItemSortBy Type => ItemSortBy.OfficialRating;

    /// <summary>
    /// Compares the specified x.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <returns>System.Int32.</returns>
    public int Compare(BaseItem? x, BaseItem? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        var zeroRating = new ParentalRatingScore(0, 0);

        var ratingX = string.IsNullOrEmpty(x.OfficialRating) ? zeroRating : _localizationManager.GetRatingScore(x.OfficialRating) ?? zeroRating;
        var ratingY = string.IsNullOrEmpty(y.OfficialRating) ? zeroRating : _localizationManager.GetRatingScore(y.OfficialRating) ?? zeroRating;
        var scoreCompare = ratingX.Score.CompareTo(ratingY.Score);
        if (scoreCompare is 0)
        {
            return (ratingX.SubScore ?? 0).CompareTo(ratingY.SubScore ?? 0);
        }

        return scoreCompare;
    }
}
