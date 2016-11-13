using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Model.System
{
    public interface IEnvironmentInfo
    {
        MediaBrowser.Model.System.OperatingSystem OperatingSystem { get; }
        string OperatingSystemName { get; }
        string OperatingSystemVersion { get; }
        Architecture SystemArchitecture { get; }
        string GetEnvironmentVariable(string name);
        string GetUserId();
    }

    public enum OperatingSystem
    {
        Windows,
        Linux,
        OSX
    }
}
