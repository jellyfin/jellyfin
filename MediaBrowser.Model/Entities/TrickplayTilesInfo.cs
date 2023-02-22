namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class TrickplayTilesInfo.
    /// </summary>
    public class TrickplayTilesInfo
    {
        /// <summary>
        /// Gets or sets width of an individual tile.
        /// </summary>
        /// <value>The width.</value>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets height of an individual tile.
        /// </summary>
        /// <value>The height.</value>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets amount of tiles per row.
        /// </summary>
        /// <value>The tile grid's width.</value>
        public int TileWidth { get; set; }

        /// <summary>
        /// Gets or sets amount of tiles per column.
        /// </summary>
        /// <value>The tile grid's height.</value>
        public int TileHeight { get; set; }

        /// <summary>
        /// Gets or sets total amount of non-black tiles.
        /// </summary>
        /// <value>The tile count.</value>
        public int TileCount { get; set; }

        /// <summary>
        /// Gets or sets interval in milliseconds between each trickplay tile.
        /// </summary>
        /// <value>The interval.</value>
        public int Interval { get; set; }

        /// <summary>
        /// Gets or sets peak bandwith usage in bits per second.
        /// </summary>
        /// <value>The bandwidth.</value>
        public int Bandwidth { get; set; }
    }
}
