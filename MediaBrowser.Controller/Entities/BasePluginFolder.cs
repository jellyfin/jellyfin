using MediaBrowser.Common.Extensions;
using System;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Plugins derive from and export this class to create a folder that will appear in the root along
    /// with all the other actual physical folders in the system.
    /// </summary>
    public abstract class BasePluginFolder : Folder, ICollectionFolder, IByReferenceItem
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public override Guid Id
        {
            get
            {
                // This doesn't get populated through the normal resolving process
                if (base.Id == Guid.Empty)
                {
                    base.Id = (Path ?? Name).GetMBId(GetType());
                }
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }        
        
        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        public override LocationType LocationType
        {
            get
            {
                return LocationType.Virtual;
            }
        }


        /// <summary>
        /// We don't resolve normally so need to fill this in
        /// </summary>
        public override string DisplayMediaType
        {
            get
            {
                return "CollectionFolder"; // Plug-in folders are collection folders
            }
            set
            {
                base.DisplayMediaType = value;
            }
        }

    }
}
