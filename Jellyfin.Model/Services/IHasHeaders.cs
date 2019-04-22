using System.Collections.Generic;

namespace Jellyfin.Model.Services
{
    public interface IHasHeaders
    {
        IDictionary<string, string> Headers { get; }
    }
}
