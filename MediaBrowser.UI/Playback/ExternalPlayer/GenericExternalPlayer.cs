using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.UI.Playback.ExternalPlayer
{
    /// <summary>
    /// Class GenericExternalPlayer
    /// </summary>
    public class GenericExternalPlayer : BaseExternalPlayer
    {
        public GenericExternalPlayer(ILogger logger)
            : base(logger)
        {
        }

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
