
namespace MediaBrowser.Model.System
{
    public interface IEnvironmentInfo
    {
        MediaBrowser.Model.System.OperatingSystem OperatingSystem { get; }
        string OperatingSystemName { get; }
        string OperatingSystemVersion { get; }
        Architecture SystemArchitecture { get; }
        string GetEnvironmentVariable(string name);
        void SetProcessEnvironmentVariable(string name, string value);
        string GetUserId();
        string StackTrace { get; }
        char PathSeparator { get; }
    }

    public enum OperatingSystem
    {
        Windows,
        Linux,
        OSX,
        BSD
    }
}
