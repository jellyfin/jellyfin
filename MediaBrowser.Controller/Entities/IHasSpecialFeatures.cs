#nullable disable

using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface for items that have special features.
    /// </summary>
    public interface IHasSpecialFeatures
    {
        /// <summary>
        /// Gets the special feature ids.
        /// </summary>
        /// <value>The special feature ids.</value>
        IReadOnlyList<Guid> SpecialFeatureIds { get; }
    }
}
