using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasSpecialFeatures
    {
        /// <summary>
        /// Gets or sets the special feature ids.
        /// </summary>
        /// <value>The special feature ids.</value>
        List<Guid> SpecialFeatureIds { get; set; }
    }
}
