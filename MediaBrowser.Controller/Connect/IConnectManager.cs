
namespace MediaBrowser.Controller.Connect
{
    public interface IConnectManager
    {
        string WanIpAddress { get; }
        string WanApiAddress { get; }
    }
}
