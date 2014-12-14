using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Collections
{
    public class CollectionCreationOptions : IHasProviderIds
    {
        public string Name { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsLocked { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        public List<Guid> ItemIdList { get; set; }
        public List<Guid> UserIds { get; set; }

        public CollectionCreationOptions()
        {
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ItemIdList = new List<Guid>();
            UserIds = new List<Guid>();
        }
    }
}
