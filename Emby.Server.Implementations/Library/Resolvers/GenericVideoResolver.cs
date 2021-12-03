#nullable disable

#pragma warning disable CS1591

using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;

namespace Emby.Server.Implementations.Library.Resolvers
{
    public class GenericVideoResolver<T> : BaseVideoResolver<T>
        where T : Video, new()
    {
        public GenericVideoResolver(NamingOptions namingOptions)
            : base(namingOptions)
        {
        }
    }
}
