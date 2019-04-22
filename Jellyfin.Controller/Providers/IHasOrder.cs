namespace Jellyfin.Controller.Providers
{
    public interface IHasOrder
    {
        int Order { get; }
    }
}
