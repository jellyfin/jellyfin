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

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        Guid Id { get; }
    }
}
