using ProtoBuf;
using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Used to hold information about a user's list of configured virtual folders
    /// </summary>
    [ProtoContract]
    public class VirtualFolderInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the locations.
        /// </summary>
        /// <value>The locations.</value>
        [ProtoMember(2)]
        public List<string> Locations { get; set; }
    }
}
