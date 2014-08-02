using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Playlists
{
    public class Playlist : Folder
    {
        public List<string> ItemIds { get; set; }

        public Playlist()
        {
            ItemIds = new List<string>();
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return base.GetChildren(user, includeLinkedChildren);
        }
    }
}
