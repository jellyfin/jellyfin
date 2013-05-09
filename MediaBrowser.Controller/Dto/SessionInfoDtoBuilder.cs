using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Net;
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
                Id = session.Id,
                LastActivityDate = session.LastActivityDate,
                NowPlayingPositionTicks = session.NowPlayingPositionTicks
            };

            if (session.NowPlayingItem != null)
            {
                dto.NowPlayingItem = DtoBuilder.GetBaseItemInfo(session.NowPlayingItem);
            }

            if (session.UserId.HasValue)
            {
                dto.UserId = session.UserId.Value.ToString("N");
            }

            dto.SupportsRemoteControl = session.WebSocket != null &&
                                        session.WebSocket.State == WebSocketState.Open;

            return dto;
        }
    }
}
