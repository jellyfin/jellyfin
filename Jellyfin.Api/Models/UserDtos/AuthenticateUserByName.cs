namespace Jellyfin.Api.Models.UserDtos
{
    public class AuthenticateUserByName
    {
        public string Username { get; set; }
        public string Pw { get; set; }
        public string Password { get; set; }
    }
}
