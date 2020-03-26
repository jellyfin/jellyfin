#pragma warning disable CS1591

using System.Diagnostics;

namespace MediaBrowser.Model.Diagnostics
{
    public interface IProcessFactory
    {
        Process Create(ProcessStartInfo options);
    }
}
