using MediaBrowser.Model.Entities;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MediaBrowser.TV.Entities
{
    public class Season : Folder
    {
        /// <summary>
        /// Store these to reduce disk access in Episode Resolver
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> MetadataFiles { get; set; }
    }
}
