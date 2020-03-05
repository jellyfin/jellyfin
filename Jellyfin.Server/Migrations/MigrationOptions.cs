namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// Configuration part that holds all migrations that were applied.
    /// </summary>
    public class MigrationOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationOptions"/> class.
        /// </summary>
        public MigrationOptions()
        {
            Applied = System.Array.Empty<string>();
        }

#pragma warning disable CA1819 // Properties should not return arrays
        /// <summary>
        /// Gets or sets he list of applied migration routine names.
        /// </summary>
        public string[] Applied { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
