using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// This holds settings that can be personalized on a per-user, per-device basis.
    /// </summary>
    public class UserConfiguration
    {
        public int RecentItemDays { get; set; }

        public UserConfiguration()
        {
            RecentItemDays = 14;
        }
    }
}
