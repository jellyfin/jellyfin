
namespace MediaBrowser.Controller.Entities
{
    public class Game : BaseItem
    {
        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        public override string MediaType
        {
            get { return Model.Entities.MediaType.Game; }
        }

        /// <summary>
        /// Gets or sets the players supported.
        /// </summary>
        /// <value>The players supported.</value>
        public int? PlayersSupported { get; set; }

        /// <summary>
        /// Gets or sets the game system.
        /// </summary>
        /// <value>The game system.</value>
        public string GameSystem { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override string MetaLocation
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override bool UseParentPathToCreateResolveArgs
        {
            get
            {
                return !IsInMixedFolder;
            }
        }
    }
}
