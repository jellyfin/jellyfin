using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Class SortNameProvider
    /// </summary>
    public class SortNameProvider : BaseMetadataProvider
    {
        public SortNameProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return true;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Last; }
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            return !string.IsNullOrEmpty(item.Name) && string.IsNullOrEmpty(item.SortName);
        }

        // Cache these since they will be used a lot
        /// <summary>
        /// The false task result
        /// </summary>
        protected static readonly Task<bool> FalseTaskResult = Task.FromResult(false);

        /// <summary>
        /// The true task result
        /// </summary>
        protected static readonly Task<bool> TrueTaskResult = Task.FromResult(true);

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            return SetSortName(item, cancellationToken) ? TrueTaskResult : FalseTaskResult;
        }

        /// <summary>
        /// Sets the name of the sort.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected bool SetSortName(BaseItem item, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(item.SortName)) return false; //let the earlier provider win

            cancellationToken.ThrowIfCancellationRequested();
            
            if (item is Episode)
            {
                //special handling for TV episodes season and episode number
                item.SortName = (item.ParentIndexNumber != null ? item.ParentIndexNumber.Value.ToString("000-") : "") 
                    + (item.IndexNumber != null ? item.IndexNumber.Value.ToString("0000 - ") : "") + item.Name;
                
            }
            else if (item is Season)
            {
                //sort seasons by season number - numerically
                item.SortName = item.IndexNumber != null ? item.IndexNumber.Value.ToString("0000") : item.Name;
            }
            else if (item is Audio)
            {
                //sort tracks by production year and index no so they will sort in order if in a multi-album list
                item.SortName = (item.ProductionYear != null ? item.ProductionYear.Value.ToString("000-") : "") 
                    + (item.IndexNumber != null ? item.IndexNumber.Value.ToString("0000 - ") : "") + item.Name;
            }
            else if (item is MusicAlbum)
            {
                //sort albums by year
                item.SortName = item.ProductionYear != null ? item.ProductionYear.Value.ToString("0000") : item.Name;
            }
            else
            {
                if (item.Name == null) return false; //some items may not have name filled in properly

                var sortable = item.Name.Trim().ToLower();
                sortable = ConfigurationManager.Configuration.SortRemoveCharacters.Aggregate(sortable, (current, search) => current.Replace(search.ToLower(), string.Empty));

                sortable = ConfigurationManager.Configuration.SortReplaceCharacters.Aggregate(sortable, (current, search) => current.Replace(search.ToLower(), " "));

                foreach (var search in ConfigurationManager.Configuration.SortRemoveWords)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var searchLower = search.ToLower();
                    // Remove from beginning if a space follows
                    if (sortable.StartsWith(searchLower + " "))
                    {
                        sortable = sortable.Remove(0, searchLower.Length + 1);
                    }
                    // Remove from middle if surrounded by spaces
                    sortable = sortable.Replace(" " + searchLower + " ", " ");

                    // Remove from end if followed by a space
                    if (sortable.EndsWith(" " + searchLower))
                    {
                        sortable = sortable.Remove(sortable.Length - (searchLower.Length + 1));
                    }
                }
                item.SortName = sortable;
            }

            return true;
        }

    }
}
