#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Resolvers
{
    public abstract class Scanner : IScanner
    {
        public bool Enabled { get; set; } = false;

        public bool Default { get; set; } = false;

        public ResolverPriority Priority => ResolverPriority.Plugin;

        protected BaseItem ApplyMetadata(BaseItem t)
        {
            protected abstract BaseItem ApplyMetadata(BaseItem t);
        }

        public ICollection<BaseItem> ApplyMetadata(ICollection<BaseItem> ts)
        {
            foreach (var t in ts)
            {
                ApplyMetadata(t);
            }

            return ts;
        }
    }
}
