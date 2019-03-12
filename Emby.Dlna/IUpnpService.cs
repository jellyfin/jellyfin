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
        ControlResponse ProcessControlRequest(ControlRequest request);
    }
}
