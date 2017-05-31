using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using MediaBrowser.Model.Services;

namespace SocketHttpListener
{
    internal abstract class HttpBase
    {
        #region Private Fields

        private QueryParamCollection _headers;
        private Version _version;

        #endregion

        #region Internal Fields

        internal byte[] EntityBodyData;

        #endregion

        #region Protected Fields

        protected const string CrLf = "\r\n";

        #endregion

        #region Protected Constructors

        protected HttpBase(Version version, QueryParamCollection headers)
        {
            _version = version;
            _headers = headers;
        }

        #endregion

        #region Public Properties

        public string EntityBody
        {
            get
            {
                var data = EntityBodyData;

                return data != null && data.Length > 0
                       ? getEncoding(_headers["Content-Type"]).GetString(data, 0, data.Length)
                       : String.Empty;
            }
        }

        public QueryParamCollection Headers
        {
            get
            {
                return _headers;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return _version;
            }
        }

        #endregion

        #region Private Methods

        private static Encoding getEncoding(string contentType)
        {
            if (contentType == null || contentType.Length == 0)
                return Encoding.UTF8;

            var i = contentType.IndexOf("charset=", StringComparison.Ordinal);
            if (i == -1)
                return Encoding.UTF8;

            var charset = contentType.Substring(i + 8);
            i = charset.IndexOf(';');
            if (i != -1)
                charset = charset.Substring(0, i).TrimEnd();

            return Encoding.GetEncoding(charset.Trim('"'));
        }

        #endregion

        #region Public Methods

        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }

        #endregion
    }
}