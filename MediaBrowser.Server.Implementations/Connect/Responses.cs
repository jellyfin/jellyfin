
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

    public class GetConnectUserResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string IsActive { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ServerUserAuthorizationResponse
    {
        
    }
}
