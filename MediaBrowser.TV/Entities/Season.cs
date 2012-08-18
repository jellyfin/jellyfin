using System.Collections.Generic;
using System.Runtime.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.TV.Entities
{
    public class Season : Folder
    {
        /// <summary>
        /// Store these to reduce disk access in Episode Resolver
        /// </summary>
        internal IEnumerable<string> MetadataFiles { get; set; }
    }
}
