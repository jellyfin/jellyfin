// Jellyfin.Api.Models.DiscoverSearchResult.cs
// This file is part of the Discover Feature for Jellyfin.
// It provides a custom DiscoverSearchResult Class that implements the search results for movies and series.

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Api.Models
{
    /// <summary>
    /// DTO for Discover search results combining movie and series suggestions.
    /// </summary>
    public class DiscoverSearchResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoverSearchResult"/> class.
        /// </summary>
        /// <param name="movies">Movie suggestions.</param>
        /// <param name="series">Series suggestions.</param>
        public DiscoverSearchResult(IEnumerable<RemoteSearchResult> movies, IEnumerable<RemoteSearchResult> series)
        {
            Movies = movies ?? Array.Empty<RemoteSearchResult>();
            Series = series ?? Array.Empty<RemoteSearchResult>();
        }

        /// <summary>
        /// Gets the movie search results.
        /// </summary>
        public IEnumerable<RemoteSearchResult> Movies { get; }

        /// <summary>
        /// Gets the series search results.
        /// </summary>
        public IEnumerable<RemoteSearchResult> Series { get; }
    }
}
