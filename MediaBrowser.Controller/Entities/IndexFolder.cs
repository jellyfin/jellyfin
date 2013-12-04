using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Entities;
using MoreLinq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class IndexFolder
    /// </summary>
    public class IndexFolder : Folder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexFolder" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="shadow">The shadow.</param>
        /// <param name="children">The children.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="groupContents">if set to <c>true</c> [group contents].</param>
        public IndexFolder(Folder parent, BaseItem shadow, IEnumerable<BaseItem> children, string indexName, bool groupContents = true)
        {
            ChildSource = children;
            ShadowItem = shadow;
            GroupContents = groupContents;
            if (shadow == null)
            {
                Name = ForcedSortName = "<Unknown>";
            }
            else
            {
                SetShadowValues();
            }
            Id = (parent.Id.ToString() + Name).GetMBId(typeof(IndexFolder));

            IndexName = indexName;
            Parent = parent;
        }

        /// <summary>
        /// Resets the parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public void ResetParent(Folder parent)
        {
            Parent = parent;
            Id = (parent.Id.ToString() + Name).GetMBId(typeof(IndexFolder));
        }

        /// <summary>
        /// Override this to true if class should be grouped under a container in indicies
        /// The container class should be defined via IndexContainer
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get
            {
                return ShadowItem != null && ShadowItem.GroupInIndex;
            }
        }

        public override LocationType LocationType
        {
            get
            {
                return LocationType.Virtual;
            }
        }

        /// <summary>
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get { return ShadowItem != null ? ShadowItem.IndexContainer : new IndexFolder(this, null, null, "<Unknown>", false); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [group contents].
        /// </summary>
        /// <value><c>true</c> if [group contents]; otherwise, <c>false</c>.</value>
        protected bool GroupContents { get; set; }
        /// <summary>
        /// Gets or sets the child source.
        /// </summary>
        /// <value>The child source.</value>
        protected IEnumerable<BaseItem> ChildSource { get; set; }
        /// <summary>
        /// Gets or sets our children.
        /// </summary>
        /// <value>Our children.</value>
        protected ConcurrentBag<BaseItem> OurChildren { get; set; }
        /// <summary>
        /// Gets the name of the index.
        /// </summary>
        /// <value>The name of the index.</value>
        public string IndexName { get; private set; }

        /// <summary>
        /// Override to return the children defined to us when we were created
        /// </summary>
        /// <value>The actual children.</value>
        protected override IEnumerable<BaseItem> LoadChildren()
        {
            var originalChildSource = ChildSource.ToList();

            var kids = originalChildSource;
            if (GroupContents)
            {
                // Recursively group up the chain
                var group = true;
                var isSubsequentLoop = false;

                while (group)
                {
                    kids = isSubsequentLoop || kids.Any(i => i.GroupInIndex)
                               ? GroupedSource(kids).ToList()
                               : originalChildSource;

                    group = kids.Any(i => i.GroupInIndex);
                    isSubsequentLoop = true;
                }
            }

            // Now - since we built the index grouping from the bottom up - we now need to properly set Parents from the top down
            SetParents(this, kids.OfType<IndexFolder>());

            return kids.DistinctBy(i => i.Id);
        }

        /// <summary>
        /// Sets the parents.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="kids">The kids.</param>
        private void SetParents(Folder parent, IEnumerable<IndexFolder> kids)
        {
            foreach (var child in kids)
            {
                child.ResetParent(parent);
                child.SetParents(child, child.Children.OfType<IndexFolder>());
            }
        }

        /// <summary>
        /// Groupeds the source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GroupedSource(IEnumerable<BaseItem> source)
        {
            return source.GroupBy(i => i.IndexContainer).Select(container => new IndexFolder(this, container.Key, container, null, false));
        }

        /// <summary>
        /// The item we are shadowing as a folder (Genre, Actor, etc.)
        /// We inherit the images and other meta from this item
        /// </summary>
        /// <value>The shadow item.</value>
        protected BaseItem ShadowItem { get; set; }

        /// <summary>
        /// Sets the shadow values.
        /// </summary>
        protected void SetShadowValues()
        {
            if (ShadowItem != null)
            {
                Name = ShadowItem.Name;
                ForcedSortName = ShadowItem.SortName;
                Genres = ShadowItem.Genres;
                Studios = ShadowItem.Studios;
                OfficialRating = ShadowItem.OfficialRatingForComparison;
                BackdropImagePaths = ShadowItem.BackdropImagePaths;
                Images = ShadowItem.Images;
                Overview = ShadowItem.Overview;
                DisplayMediaType = ShadowItem.GetType().Name;
            }
        }

        /// <summary>
        /// Overrides the base implementation to refresh metadata for local trailers
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="forceSave">if set to <c>true</c> [is new item].</param>
        /// <param name="forceRefresh">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <param name="resetResolveArgs">if set to <c>true</c> [reset resolve args].</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override Task<bool> RefreshMetadata(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true, bool resetResolveArgs = true)
        {
            // We should never get in here since these are not part of the library
            return Task.FromResult(false);
        }
    }
}
