using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Movies
{
    /// <summary>
    /// Class MovieProviderFromXml
    /// </summary>
    [Export(typeof(BaseMetadataProvider))]
    public class MovieProviderFromXml : BaseMetadataProvider
    {
        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Movie || item is BoxSet;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, "movie.xml"));
            return entry != null ? entry.Value.LastWriteTimeUtc : DateTime.MinValue;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            return Task.Run(() => Fetch(item, cancellationToken));
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool Fetch(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var metadataFile = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, "movie.xml"));

            if (metadataFile.HasValue)
            {
                var path = metadataFile.Value.Path;
                var boxset = item as BoxSet;
                if (boxset != null)
                {
                    new BaseItemXmlParser<BoxSet>(Logger).Fetch(boxset, path, cancellationToken);
                }
                else
                {
                    new BaseItemXmlParser<Movie>(Logger).Fetch((Movie)item, path, cancellationToken);
                }
                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }

            return false;
        }
    }
}
