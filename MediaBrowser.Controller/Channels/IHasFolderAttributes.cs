#pragma warning disable CS1591

namespace MediaBrowser.Controller.Channels
{
    public interface IHasFolderAttributes
    {
        string[] Attributes { get; }
    }
}