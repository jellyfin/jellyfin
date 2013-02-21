using MediaBrowser.Model.Dto;
using System.ComponentModel.Composition;

namespace MediaBrowser.UI.Playback.ExternalPlayer
{
    /// <summary>
    /// Class GenericExternalPlayer
    /// </summary>
    [Export(typeof(BaseMediaPlayer))]
    public class GenericExternalPlayer : BaseExternalPlayer
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Generic Player"; }
        }

        /// <summary>
        /// Determines whether this instance can play the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if this instance can play the specified item; otherwise, <c>false</c>.</returns>
        public override bool CanPlay(BaseItemDto item)
        {
            return false;
        }
    }
}
