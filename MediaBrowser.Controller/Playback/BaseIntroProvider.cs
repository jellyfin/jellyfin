using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Playback
{
    /// <summary>
    /// Class BaseIntroProvider
    /// </summary>
    public abstract class BaseIntroProvider
    {
        /// <summary>
        /// Gets the intros.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        public abstract IEnumerable<string> GetIntros(BaseItem item, User user);
    }
}
