using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Connectivity;
using MediaBrowser.Model.Dto;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides a handler to set played status for an item
    /// </summary>
    [Export(typeof(IHttpServerHandler))]
    public class PlaybackCheckInHandler : BaseSerializationHandler<Kernel, UserItemDataDto>
    {
        /// <summary>
        /// Gets the object to serialize.
        /// </summary>
        /// <returns>Task{DtoUserItemData}.</returns>
        protected override async Task<UserItemDataDto> GetObjectToSerialize()
        {
            // Get the user
            var user = await this.GetCurrentUser().ConfigureAwait(false);

            var clientType = ClientType.Other;

            if (!string.IsNullOrEmpty(QueryString["client"]))
            {
                ClientType type;

                if (Enum.TryParse(QueryString["client"], true, out type))
                {
                    clientType = type;
                }
            }

            var device = QueryString["device"];
            
            // Get the item
            var item = DtoBuilder.GetItemByClientId(QueryString["id"], user.Id);

            // Playback start check-in
            if (QueryString["type"].Equals("start", StringComparison.OrdinalIgnoreCase))
            {
                Kernel.UserDataManager.OnPlaybackStart(user, item, clientType, device);
            }
            else
            {
                long? positionTicks = null;

                if (!string.IsNullOrEmpty(QueryString["positionTicks"]))
                {
                    positionTicks = long.Parse(QueryString["positionTicks"]);
                }

                // Progress check-ins require position ticks
                if (QueryString["type"].Equals("progress", StringComparison.OrdinalIgnoreCase))
                {
                    await Kernel.UserDataManager.OnPlaybackProgress(user, item, positionTicks, clientType, device).ConfigureAwait(false);
                }
                else if (QueryString["type"].Equals("stopped", StringComparison.OrdinalIgnoreCase))
                {
                    await Kernel.UserDataManager.OnPlaybackStopped(user, item, positionTicks, clientType, device).ConfigureAwait(false);
                }
            }

            var data = item.GetUserData(user, true);

            return DtoBuilder.GetDtoUserItemData(data);
        }

        /// <summary>
        /// Gets the current user.
        /// </summary>
        /// <returns>User.</returns>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        public async Task<User> GetCurrentUser()
        {
            var handler = this;
            var id = handler.QueryString["userid"];

            var user = ApiService.GetUserById(id);

            if (user == null)
            {
                throw new UnauthorizedAccessException(string.Format("User with Id {0} does not exist", id));
            }

            var clientType = ClientType.Other;

            if (!string.IsNullOrEmpty(handler.QueryString["client"]))
            {
                ClientType type;

                if (Enum.TryParse(handler.QueryString["client"], true, out type))
                {
                    clientType = type;
                }
            }

            var device = handler.QueryString["device"];

            await Controller.Kernel.Instance.UserManager.LogUserActivity(user, clientType, device).ConfigureAwait(false);

            return user;
        }

    }
}