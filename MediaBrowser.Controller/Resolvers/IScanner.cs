#pragma warning disable CS1591

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.Resolvers
{
    public interface IScanner
    {
        bool Enabled { get; set; }

        bool Default { get; set; }

        public ICollection<BaseItem> ApplyMetadata(ICollection<BaseItem> ts);
    }
}
