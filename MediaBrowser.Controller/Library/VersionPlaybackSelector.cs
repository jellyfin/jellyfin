using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Single definition of "which alternate version was most recently played" shared by the resume tile
    /// (<see cref="IUserDataManager.GetResumeUserData"/>), the media-source default ordering and Next Up.
    /// Each call site declares its own eligibility rule so the intentional differences (resumable-only vs.
    /// resumable-or-completed) are visible in one place instead of being re-implemented divergently.
    /// The SQL resume query keeps its own translation of the same rule.
    /// </summary>
    public static class VersionPlaybackSelector
    {
        /// <summary>
        /// Selects the entry whose user data has the greatest <see cref="UserItemData.LastPlayedDate"/>,
        /// considering only entries that satisfy <paramref name="isEligible"/>. On an exact tie the first
        /// encountered entry wins.
        /// </summary>
        /// <typeparam name="T">The candidate type (e.g. a version item or a media source).</typeparam>
        /// <param name="items">The candidates to choose from.</param>
        /// <param name="dataSelector">Resolves the user data for a candidate, or <c>null</c> when it has none.</param>
        /// <param name="isEligible">Whether a candidate's user data makes it a valid winner.</param>
        /// <returns>The most recently played eligible candidate, or <c>default</c> when none qualify.</returns>
        public static T? SelectMostRecentlyPlayed<T>(
            IEnumerable<T> items,
            Func<T, UserItemData?> dataSelector,
            Func<UserItemData, bool> isEligible)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(dataSelector);
            ArgumentNullException.ThrowIfNull(isEligible);

            T? winner = default;
            var winnerDate = DateTime.MinValue;
            var hasWinner = false;

            foreach (var item in items)
            {
                var data = dataSelector(item);
                if (data is null || !isEligible(data))
                {
                    continue;
                }

                var date = data.LastPlayedDate ?? DateTime.MinValue;
                if (!hasWinner || date > winnerDate)
                {
                    winner = item;
                    winnerDate = date;
                    hasWinner = true;
                }
            }

            return winner;
        }
    }
}
