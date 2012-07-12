using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Users
{
    public class User : BaseItem
    {
        public string Password { get; set; }
        public string MaxParentalRating { get; set; }
        public bool HideBlockedContent { get; set; }

        private Dictionary<Guid, UserItemData> _ItemData = new Dictionary<Guid, UserItemData>();
        public Dictionary<Guid, UserItemData> ItemData { get { return _ItemData; } set { _ItemData = value; } }
    }
}
