using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Dto
{
    /// <summary>
    /// Class SessionInfoDtoBuilder
    /// </summary>
    public static class SessionInfoDtoBuilder
    {
        /// <summary>
        /// Gets the session info dto.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>SessionInfoDto.</returns>
        public static SessionInfoDto GetSessionInfoDto(SessionInfo session)
        {
            var dto = new SessionInfoDto
            {
                Client = session.Client,
                DeviceId = session.DeviceId,
                DeviceName = session.DeviceName,
                Id = session.Id.ToString("N"),
                LastActivityDate = session.LastActivityDate,
                NowPlayingPositionTicks = session.NowPlayingPositionTicks,
                SupportsRemoteControl = session.SupportsRemoteControl,
                IsPaused = session.IsPaused,
                IsMuted = session.IsMuted,
                NowViewingContext = session.NowViewingContext,
                NowViewingItemId = session.NowViewingItemId,
                NowViewingItemName = session.NowViewingItemName,
                NowViewingItemType = session.NowViewingItemType,
                ApplicationVersion = session.ApplicationVersion
            };

            if (session.NowPlayingItem != null)
            {
                dto.NowPlayingItem = DtoBuilder.GetBaseItemInfo(session.NowPlayingItem);
            }

            if (session.User != null)
            {
                dto.UserId = session.User.Id.ToString("N");
                dto.UserName = session.User.Name;
            }

            return dto;
        }
    }
}
