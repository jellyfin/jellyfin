using System;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.TV.Entities
{
    public class Season : Folder
    {
        /// <summary>
        /// Store these to reduce disk access in Episode Resolver
        /// </summary>
        internal string[] MetadataFiles { get; set; }

        /// <summary>
        /// Determines if the metafolder contains a given file
        /// </summary>
        internal bool ContainsMetadataFile(string file)
        {
            for (int i = 0; i < MetadataFiles.Length; i++)
            {
                if (MetadataFiles[i].Equals(file, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
