using MediaBrowser.Model.Connect;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Connect
{
    public class ConnectSupporterSummary
    {
        public int MaxUsers { get; set; }
        public List<ConnectUser> Users { get; set; }
        public List<UserDto> EligibleUsers { get; set; }

        public ConnectSupporterSummary()
        {
            Users = new List<ConnectUser>();
            EligibleUsers = new List<UserDto>();
        }
    }
}
