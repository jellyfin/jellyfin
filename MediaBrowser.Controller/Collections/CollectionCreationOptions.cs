using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Collections
{
    public class CollectionCreationOptions : IHasProviderIds
    {
        public string Name { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsLocked { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        public string[] ItemIdList { get; set; }

        public Guid[] UserIds { get; set; }

        public CollectionCreationOptions()
        {
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ItemIdList = Array.Empty<string>();
            UserIds = Array.Empty<Guid>();
        }
    }
}
