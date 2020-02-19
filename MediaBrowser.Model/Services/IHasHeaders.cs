#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Collections.Generic;

namespace MediaBrowser.Model.Services
{
    public interface IHasHeaders
    {
        IDictionary<string, string> Headers { get; }
    }
}
