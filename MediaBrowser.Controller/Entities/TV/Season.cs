using System;

namespace MediaBrowser.Controller.Entities.TV
{
    public class Season : Folder
    {
        /// <summary>
        /// Store these to reduce disk access in Episode Resolver
        /// </summary>
        public string[] MetadataFiles
        {
            get
            {
                return ResolveArgs.MetadataFiles ?? new string[] { };
            }
        }

        /// <summary>
        /// Determines if the metafolder contains a given file
        /// </summary>
        public bool ContainsMetadataFile(string file)
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
