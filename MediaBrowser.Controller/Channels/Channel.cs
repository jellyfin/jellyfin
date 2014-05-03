using MediaBrowser.Controller.Entities;
using System;
using System.Linq;

namespace MediaBrowser.Controller.Channels
{
    public class Channel : BaseItem
    {
        public string OriginalChannelName { get; set; }

        public override bool IsVisible(User user)
        {
            if (user.Configuration.BlockedChannels.Contains(Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
            
            return base.IsVisible(user);
        }
    }
}
