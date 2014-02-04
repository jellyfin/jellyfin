using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Specialized Folder class that points to a subset of the physical folders in the system.
    /// It is created from the user-specific folders within the system root
    /// </summary>
    public class CollectionFolder : Folder, ICollectionFolder
    {
        /// <summary>
        /// Gets a value indicating whether this instance is virtual folder.
        /// </summary>
        /// <value><c>true</c> if this instance is virtual folder; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsVirtualFolder
        {
            get
            {
                return true;
            }
        }

        public string CollectionType { get; set; }

        /// <summary>
        /// Allow different display preferences for each collection folder
        /// </summary>
        /// <value>The display prefs id.</value>
        [IgnoreDataMember]
        public override Guid DisplayPreferencesId
        {
            get
            {
                return Id;
            }
        }

        // Cache this since it will be used a lot
        /// <summary>
        /// The null task result
        /// </summary>
        private static readonly Task NullTaskResult = Task.FromResult<object>(null);

        /// <summary>
        /// Compare our current children (presumably just read from the repo) with the current state of the file system and adjust for any changes
        /// ***Currently does not contain logic to maintain items that are unavailable in the file system***
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <param name="forceRefreshMetadata">if set to <c>true</c> [force refresh metadata].</param>
        /// <returns>Task.</returns>
        protected override Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool? recursive = null, bool forceRefreshMetadata = false)
        {
            ResetDynamicChildren();

            return NullTaskResult;
        }

        private List<LinkedChild> _linkedChildren;
        
        /// <summary>
        /// Our children are actually just references to the ones in the physical root...
        /// </summary>
        /// <value>The linked children.</value>
        public override List<LinkedChild> LinkedChildren
        {
            get { return _linkedChildren ?? (_linkedChildren = GetLinkedChildrenInternal()); }
            set
            {
                base.LinkedChildren = value;
            }
        }
        private List<LinkedChild> GetLinkedChildrenInternal()
        {
            Dictionary<string, string> locationsDicionary;

            try
            {
                locationsDicionary = PhysicalLocations.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                return new List<LinkedChild>();
            }

            return LibraryManager.RootFolder.Children
                .OfType<Folder>()
                .Where(i => i.Path != null && locationsDicionary.ContainsKey(i.Path))
                .SelectMany(c => c.LinkedChildren)
                .ToList();
        }

        private IEnumerable<BaseItem> _actualChildren;

        /// <summary>
        /// Our children are actually just references to the ones in the physical root...
        /// </summary>
        /// <value>The actual children.</value>
        protected override IEnumerable<BaseItem> ActualChildren
        {
            get { return _actualChildren ?? (_actualChildren = GetActualChildren()); }
        }

        private IEnumerable<BaseItem> GetActualChildren()
        {
            Dictionary<string, string> locationsDicionary;

            try
            {
                locationsDicionary = PhysicalLocations.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                return new BaseItem[] { };
            }

            return
                LibraryManager.RootFolder.Children
                .OfType<Folder>()
                .Where(i => i.Path != null && locationsDicionary.ContainsKey(i.Path))
                .SelectMany(c => c.Children)
                .ToList();
        }

        public void ResetDynamicChildren()
        {
            _actualChildren = null;
            _linkedChildren = null;
        }
    }
}
