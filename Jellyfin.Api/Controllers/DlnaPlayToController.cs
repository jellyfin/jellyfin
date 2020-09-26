using System.IO;
using System.Text;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo;
using Emby.Dlna.PlayTo.EventArgs;
using Jellyfin.Api.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Dlna PlayTo Controller.
    /// </summary>
    [Route("Dlna")]
    public class DlnaPlayToController : BaseJellyfinApiController
    {
        private readonly IPlayToManager _playToManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaPlayToController"/> class.
        /// </summary>
        /// <param name="playToManager">Instance of the <seealso cref="IPlayToManager"/> instance.</param>
        public DlnaPlayToController(IPlayToManager playToManager)
        {
            _playToManager = playToManager;
        }

        /// <summary>
        /// Processes device subscription events.
        /// Has to be a url, as the XML content from devices can be corrupt.
        /// </summary>
        /// <param name="id">Id of the device.</param>
        /// <returns>Event subscription response.</returns>
        [HttpNotify]
        [Route("Eventing/{id}")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ignore in openapi docs]
        public async Task<ActionResult> ProcessDeviceNotification([FromRoute] string id)
        {
            try
            {
                if (_playToManager.IsPlayToEnabled)
                {
                    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                    var response = await reader.ReadToEndAsync().ConfigureAwait(false);
                    await _playToManager.NotifyDevice(new DlnaEventArgs(id, response)).ConfigureAwait(false);
                }
            }
            catch
            {
                // Ignore connection forcible closed messages.
            }

            return Ok();
        }
    }
}
