namespace MediaBrowser.Controller.Providers
{
    public interface IHasOrder
    {
        int Order { get; }
    }
}