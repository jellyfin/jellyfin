#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Model.Services
{
    public interface IHasHeaders
    {
        IDictionary<string, string> Headers { get; }
    }
}
