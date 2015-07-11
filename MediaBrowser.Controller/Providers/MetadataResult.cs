using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class MetadataResult<T>
    {
        public List<PersonInfo> People { get; set; }

        public bool HasMetadata { get; set; }
        public T Item { get; set; }

        public MetadataResult()
        {
            People = new List<PersonInfo>();
        }
    }
}