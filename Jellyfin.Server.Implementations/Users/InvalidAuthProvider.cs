using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Authentication;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// An invalid authentication provider.
    /// </summary>
    public class InvalidAuthProvider : IAuthenticationProvider
    {
        private readonly DummyPasswordVerifier _dummyPasswordVerifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidAuthProvider"/> class.
        /// </summary>
        /// <param name="dummyPasswordVerifier">The dummy password verifier.</param>
        public InvalidAuthProvider(DummyPasswordVerifier dummyPasswordVerifier)
        {
            _dummyPasswordVerifier = dummyPasswordVerifier;
        }

        /// <inheritdoc />
        public string Name => "InvalidOrMissingAuthenticationProvider";

        /// <inheritdoc />
        public bool IsEnabled => false;

        /// <inheritdoc />
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            // Match the cost of a real password check so the response time does not reveal that the user exists.
            _dummyPasswordVerifier.Verify(password);
            throw new AuthenticationException("User Account cannot login with this provider. The Normal provider for this user cannot be found");
        }

        /// <inheritdoc />
        public Task ChangePassword(User user, string newPassword)
        {
            return Task.CompletedTask;
        }
    }
}
