#pragma warning disable CS1591

namespace MediaBrowser.Controller.Providers
{
    public interface IHasOrder
    {
        int Order { get; }
    }
}
