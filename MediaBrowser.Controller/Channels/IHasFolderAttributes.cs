#pragma warning disable CA1819, CS1591

namespace MediaBrowser.Controller.Channels
{
    public interface IHasFolderAttributes
    {
        string[] Attributes { get; }
    }
}
