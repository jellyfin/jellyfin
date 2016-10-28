using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Services
{
    public interface IHttpResponse : IResponse
    {
        //ICookies Cookies { get; }

        /// <summary>
        /// Adds a new Set-Cookie instruction to Response
        /// </summary>
        /// <param name="cookie"></param>
        void SetCookie(Cookie cookie);

        /// <summary>
        /// Removes all pending Set-Cookie instructions 
        /// </summary>
        void ClearCookies();
    }
}
