
namespace MediaBrowser.Server.Implementations.Connect
{
    public class ServerRegistrationResponse
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string AccessKey { get; set; }
    }

    public class UpdateServerRegistrationResponse
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
    }
}
