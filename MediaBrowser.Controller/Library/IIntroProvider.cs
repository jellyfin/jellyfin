using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class BaseIntroProvider
    /// </summary>
    public interface IIntroProvider
    {
        /// <summary>
        /// Gets the intros.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        IEnumerable<IntroInfo> GetIntros(BaseItem item, User user);

        /// <summary>
        /// Gets all intro files.
        /// </summary>
        /// <returns>IEnumerable{System.String}.</returns>
        IEnumerable<string> GetAllIntroFiles();
    }
}
