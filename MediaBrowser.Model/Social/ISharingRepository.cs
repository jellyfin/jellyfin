using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Social
{
    public interface ISharingRepository
    {
        Task CreateShare(SocialShareInfo info);
        Task DeleteShare(string id);
        SocialShareInfo GetShareInfo(string id);
    }
}
