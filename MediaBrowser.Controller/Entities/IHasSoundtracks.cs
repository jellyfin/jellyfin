using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasSoundtracks
    /// </summary>
    public interface IHasSoundtracks
    {
        /// <summary>
        /// Gets or sets the soundtrack ids.
        /// </summary>
        /// <value>The soundtrack ids.</value>
        List<Guid> SoundtrackIds { get; set; }
    }
}
