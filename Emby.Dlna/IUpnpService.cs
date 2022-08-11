#pragma warning disable CS1591

using System.Threading.Tasks;

namespace Emby.Dlna
{
    public interface IUpnpService
    {
        /// <summary>
        /// Gets the content directory XML.
        /// </summary>
        /// <returns>System.String.</returns>
        string GetServiceXml();

        /// <summary>
        /// Processes the control request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>ControlResponse.</returns>
        Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request);
    }
}
