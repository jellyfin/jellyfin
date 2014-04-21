using System.Collections.Generic;

namespace MediaBrowser.Controller.Dlna
{
    public interface IContentDirectory
    {
        /// <summary>
        /// Gets the content directory XML.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>System.String.</returns>
        string GetContentDirectoryXml(IDictionary<string, string> headers);

        /// <summary>
        /// Processes the control request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>ControlResponse.</returns>
        ControlResponse ProcessControlRequest(ControlRequest request);
    }
}
