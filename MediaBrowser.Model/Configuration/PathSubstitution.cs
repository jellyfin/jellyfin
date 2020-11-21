#nullable enable
#pragma warning disable CS1591
#pragma warning disable CA1819

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Defines the <see cref="PathSubstitution" />.
    /// </summary>
    public class PathSubstitution
    {
        /// <summary>
        /// Gets or sets the value to substitute.
        /// </summary>
        public string From { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value to substitution with.
        /// </summary>
        public string To { get; set; } = string.Empty;
    }
}
