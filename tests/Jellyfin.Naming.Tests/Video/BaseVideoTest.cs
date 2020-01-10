using Emby.Naming.Common;
using Emby.Naming.Video;

namespace Jellyfin.Naming.Tests.Video
{
    public abstract class BaseVideoTest
    {
        protected VideoResolver GetParser()
        {
            var options = new NamingOptions();

            return new VideoResolver(options);
        }
    }
}
