using System.Threading.Tasks;
using MediaBrowser.Controller.Playlists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Jellyfin.Api.Auth.PlaylistShareAccessPolicy
{
    /// <summary>
    /// Playlist share access handler. Allows anonymous users with valid share token.
    /// </summary>
    public class PlaylistShareAccessHandler : AuthorizationHandler<PlaylistShareAccessRequirement>
    {
        private readonly IPlaylistManager _playlistManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistShareAccessHandler"/> class.
        /// </summary>
        /// <param name="playlistManager">Instance of the <see cref="IPlaylistManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public PlaylistShareAccessHandler(
            IPlaylistManager playlistManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _playlistManager = playlistManager;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PlaylistShareAccessRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var routeData = httpContext.GetRouteData();
            var shareToken = routeData?.Values["shareToken"]?.ToString();

            if (string.IsNullOrWhiteSpace(shareToken))
            {
                // Try query parameter as fallback
                shareToken = httpContext.Request.Query["shareToken"].ToString();
            }

            if (string.IsNullOrWhiteSpace(shareToken))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var playlist = _playlistManager.GetPlaylistByShareToken(shareToken);
            if (playlist is not null)
            {
                httpContext.Items["SharedPlaylist"] = playlist;
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
