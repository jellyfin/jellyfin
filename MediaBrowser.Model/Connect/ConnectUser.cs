
namespace MediaBrowser.Model.Connect
{
    public class ConnectUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; }
    }
}
