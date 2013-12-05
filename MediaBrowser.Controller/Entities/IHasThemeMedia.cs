using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasThemeMedia
    /// </summary>
    public interface IHasThemeMedia
    {
        /// <summary>
        /// Gets or sets the theme song ids.
        /// </summary>
        /// <value>The theme song ids.</value>
        List<Guid> ThemeSongIds { get; set; }

        /// <summary>
        /// Gets or sets the theme video ids.
        /// </summary>
        /// <value>The theme video ids.</value>
        List<Guid> ThemeVideoIds { get; set; }
    }
}
