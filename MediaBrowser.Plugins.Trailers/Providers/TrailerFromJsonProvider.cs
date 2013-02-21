using MediaBrowser.Common.Serialization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Plugins.Trailers.Entities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Providers
{
    /// <summary>
    /// Class TrailerFromJsonProvider
    /// </summary>
    [Export(typeof(BaseMetadataProvider))]
    class TrailerFromJsonProvider : BaseMetadataProvider
    {
        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            var trailer = item as Trailer;

            return trailer != null && trailer.Parent is TrailerCollectionFolder;
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, "trailer.json"));
            return entry != null ? entry.Value.LastWriteTimeUtc : DateTime.MinValue;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            return Task.Run(() => Fetch((Trailer)item));
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool Fetch(Trailer item)
        {
            var metadataFile = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, "trailer.json"));

            if (metadataFile.HasValue)
            {
                var tempTrailer = JsonSerializer.DeserializeFromFile<Trailer>(metadataFile.Value.Path);

                ImportMetdata(tempTrailer, item);

                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }
            return false;
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
        /// Imports the metdata.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        private void ImportMetdata(Trailer source, Trailer target)
        {
            if (!string.IsNullOrWhiteSpace(source.Name))
            {
                target.Name = source.Name;
            }

            if (source.RunTimeTicks.HasValue)
            {
                target.RunTimeTicks = source.RunTimeTicks;
            }

            if (source.Genres != null)
            {
                foreach (var entry in source.Genres)
                {
                    target.AddGenre(entry);
                }
            }

            if (!string.IsNullOrWhiteSpace(source.OfficialRating))
            {
                target.OfficialRating = source.OfficialRating;
            }

            if (!string.IsNullOrWhiteSpace(source.Overview))
            {
                target.Overview = source.Overview;
            }

            if (source.People != null)
            {
                target.AddPeople(source.People);
            }

            if (source.PremiereDate.HasValue)
            {
                target.PremiereDate = source.PremiereDate;
            }

            if (source.ProductionYear.HasValue)
            {
                target.ProductionYear = source.ProductionYear;
            }

            if (source.Studios != null)
            {
                foreach (var entry in source.Studios)
                {
                    target.AddStudio(entry);
                }
            }
        }
    }
}
