#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Collections
{
    public sealed class CollectionCreationOptions : IHasProviderIds
    {
        private Dictionary<string, string> _providerIds;

        public CollectionCreationOptions()
        {
            _providerIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ItemIdList = Array.Empty<string>();
            UserIds = Array.Empty<Guid>();
        }

        public string Name { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsLocked { get; set; }

        // private implementation of ProviderIds property from interface to allow setter access
        Dictionary<string, string> IHasProviderIds.ProviderIds
        {
            get => _providerIds;
            set => _providerIds = value;
        }

        public Dictionary<string, string> ProviderIds { get => _providerIds; }

        public IReadOnlyList<string> ItemIdList { get; set; }

        public IReadOnlyList<Guid> UserIds { get; set; }
    }
}
