using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Provides a base entity for all of our types
    /// </summary>
    public abstract class BaseEntity
    {
        public string Name { get; set; }

        public Guid Id { get; set; }

        public string Path { get; set; }

        public Folder Parent { get; set; }

        public string PrimaryImagePath { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        public override string ToString()
        {
            return Name;
        }
        protected Dictionary<Guid, BaseProviderInfo> _providerData;
        /// <summary>
        /// Holds persistent data for providers like last refresh date.
        /// Providers can use this to determine if they need to refresh.
        /// The BaseProviderInfo class can be extended to hold anything a provider may need.
        /// 
        /// Keyed by a unique provider ID.
        /// </summary>
        public Dictionary<Guid, BaseProviderInfo> ProviderData
        {
            get
            {
                if (_providerData == null) _providerData = new Dictionary<Guid, BaseProviderInfo>();
                return _providerData;
            }
            set
            {
                _providerData = value;
            }
        }

        protected ItemResolveEventArgs _resolveArgs;
        /// <summary>
        /// We attach these to the item so that we only ever have to hit the file system once
        /// (this includes the children of the containing folder)
        /// Use ResolveArgs.FileSystemChildren to check for the existence of files instead of File.Exists
        /// </summary>
        public ItemResolveEventArgs ResolveArgs
        {
            get
            {
                if (_resolveArgs == null)
                {
                    _resolveArgs = new ItemResolveEventArgs()
                    {
                        FileInfo = FileData.GetFileData(this.Path),
                        Parent = this.Parent,
                        Cancel = false,
                        Path = this.Path
                    };
                    _resolveArgs = FileSystemHelper.FilterChildFileSystemEntries(_resolveArgs, (this.Parent != null && this.Parent.IsRoot));
                }
                return _resolveArgs;
            }
            set
            {
                _resolveArgs = value;
            }
        }

        /// <summary>
        /// Refresh metadata on us by execution our provider chain
        /// </summary>
        /// <returns>true if a provider reports we changed</returns>
        public bool RefreshMetadata()
        {
            Kernel.Instance.ExecuteMetadataProviders(this).ConfigureAwait(false);
            return true;
        }

    }
}
