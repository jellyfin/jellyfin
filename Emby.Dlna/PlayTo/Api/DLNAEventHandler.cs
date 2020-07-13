#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Emby.Dlna.Main;
using MediaBrowser.Model.Services;

namespace Emby.Dlna.PlayTo.Api
{
    /// <summary>
    /// DLNA subscription event endpoint.
    /// </summary>
    [Route("/Dlna/Eventing/{Id}/", "NOTIFY", Summary = "Endpoint for DLNA eventing.")]
    public class DLNAEvent : IRequiresRequestStream
    {
        /// <summary>
        /// Extract the id of the device which the DLNA device is transmitting the event to.
        /// </summary>
        [ApiMember(Name = "Id", Description = "DLNA Identifier", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "NOTIFY")]
        public string Id { get; set; }

        /// <summary>
        /// The contents of the DLNA XML LastChange event.
        /// </summary>
        public Stream RequestStream { get; set; }
    }

    /// <summary>
    /// DLNA subscription event handler.
    /// </summary>
    public class DLNAEventHandler : IService
    {
        /// <summary>
        /// Notifies PlayToManager that an event has been received for a device.
        /// </summary>
        /// <param name="request">The request information received.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task Notify(DLNAEvent request)
        {
            try
            {
                using (var reader = new StreamReader(request.RequestStream, Encoding.UTF8))
                {
                    string response = await reader.ReadToEndAsync().ConfigureAwait(false);

                    if (DlnaEntryPoint.Current?.PlayToManager != null)
                    {
                        await DlnaEntryPoint.Current.PlayToManager.NotifyDevice(new DlnaEventArgs(request.Id, response)).ConfigureAwait(false);
                    }
                }
            }
            catch
            {
                // Ignore connection forcible closed messages.
            }
        }
    }
}
