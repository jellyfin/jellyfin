using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Api.Model
{
    public class BaseItemInfo
    {
        public BaseItem Item { get; set; }

        public UserItemData UserItemData { get; set; }

        public IEnumerable<BaseItem> Children { get; set; }
    }
}
