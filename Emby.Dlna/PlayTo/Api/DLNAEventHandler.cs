using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Emby.Dlna.Main;
using MediaBrowser.Model.Services;

namespace Emby.Dlna.PlayTo.Api
{
    [Route("/Dlna/Eventing/{Id}/", "NOTIFY", Summary = "Endpoint for DLNA eventing.")]
    public class DLNAEvent : IRequiresRequestStream
    {
        [ApiMember(Name = "Id", Description = "DLNA Identifier", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "NOTIFY")]
        public string Id { get; set; }

        public Stream RequestStream { get; set; }
    }

    public class DLNAEventHandler : IService
    {
        public async Task Notify(DLNAEvent request)
        {
            try
            {
                using (var reader = new StreamReader(request.RequestStream, Encoding.UTF8))
                {
                    string response = await reader.ReadToEndAsync().ConfigureAwait(false);

                    if (DlnaEntryPoint.Current?.PlayToManager != null)
                    {
                        await DlnaEntryPoint.Current.PlayToManager.FireEvent(new DlnaEventArgs(request.Id, response)).ConfigureAwait(false);
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
