using System;

namespace MediaBrowser.Controller.Collections
{
    public class CollectionCreationOptions
    {
        public string Name { get; set; }

        public Guid ParentId { get; set; }

        public bool IsLocked { get; set; }
    }
}
