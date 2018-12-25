using System;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasSpecialFeatures
    {
        /// <summary>
        /// Gets or sets the special feature ids.
        /// </summary>
        /// <value>The special feature ids.</value>
        Guid[] SpecialFeatureIds { get; set; }
    }
}
