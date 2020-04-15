using System;

namespace MediaBrowser.Model.QuickConnect
{
    /// <summary>
    /// Stores the non-sensitive results of an incoming quick connect request.
    /// </summary>
    public class QuickConnectResultDto
    {
        /// <summary>
        /// Gets a value indicating whether this request is authorized.
        /// </summary>
        public bool Authenticated { get; private set; }

        /// <summary>
        /// Gets the user facing code used so the user can quickly differentiate this request from others.
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Gets the public value used to uniquely identify this request. Can only be used to authorize the request.
        /// </summary>
        public string Lookup { get; private set; }

        /// <summary>
        /// Gets the device friendly name.
        /// </summary>
        public string FriendlyName { get; private set; }

        /// <summary>
        /// Gets the DateTime that this request was created.
        /// </summary>
        public DateTime DateAdded { get; private set; }

        /// <summary>
        /// Cast an internal quick connect result to a DTO by removing all sensitive properties.
        /// </summary>
        /// <param name="result">QuickConnectResult object to cast</param>
        public static implicit operator QuickConnectResultDto(QuickConnectResult result)
        {
            QuickConnectResultDto resultDto = new QuickConnectResultDto
            {
                Authenticated = result.Authenticated,
                Code = result.Code,
                FriendlyName = result.FriendlyName,
                DateAdded = result.DateAdded,
                Lookup = result.Lookup
            };

            return resultDto;
        }
    }
}
