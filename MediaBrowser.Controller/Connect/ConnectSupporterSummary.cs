using MediaBrowser.Model.Connect;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Connect
{
    public class ConnectSupporterSummary
    {
        public int MaxUsers { get; set; }
        public List<ConnectUser> Users { get; set; }

        public ConnectSupporterSummary()
        {
            Users = new List<ConnectUser>();
        }
    }
}
