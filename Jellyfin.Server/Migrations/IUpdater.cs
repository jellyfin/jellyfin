using System;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations
{
        /// <summary>
        /// Interface that descibes a migration routine.
        /// </summary>
        internal interface IUpdater
        {
            /// <summary>
            /// Gets maximum version this Updater applies to.
            /// If current version is greater or equal to it, skip the updater.
            /// </summary>
            public abstract Version Maximum { get; }

            /// <summary>
            /// Execute the migration from version "from".
            /// </summary>
            /// <param name="host">Host that hosts current version.</param>
            /// <param name="logger">Host logger.</param>
            /// <param name="from">Version to migrate from.</param>
            /// <returns>Whether configuration was changed.</returns>
            public abstract bool Perform(CoreAppHost host, ILogger logger, Version from);
        }
}
