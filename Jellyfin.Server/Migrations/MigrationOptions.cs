using System;
using System.Collections.Generic;

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
            Applied = new List<(Guid Id, string Name)>();
        }

        /// <summary>
        /// Gets the list of applied migration routine names.
        /// </summary>
        public List<(Guid Id, string Name)> Applied { get; }
    }
}
