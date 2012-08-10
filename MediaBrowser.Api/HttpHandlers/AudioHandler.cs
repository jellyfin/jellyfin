using System;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class AudioHandler : StaticFileHandler
    {
        private BaseItem _LibraryItem;
        /// <summary>
        /// Gets the library item that will be played, if any
        /// </summary>
        private BaseItem LibraryItem
        {
            get
            {
                if (_LibraryItem == null)
                {
                    string id = QueryString["id"];

                    if (!string.IsNullOrEmpty(id))
                    {
                        _LibraryItem = Kernel.Instance.GetItemById(Guid.Parse(id));
                    }
                }

                return _LibraryItem;
            }
        }

        public override string Path
        {
            get
            {
                if (LibraryItem != null)
                {
                    return LibraryItem.Path;
                }

                return base.Path;
            }
        }
    }
}
