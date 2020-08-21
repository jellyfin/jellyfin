#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Emby.Dlna.Rssdp
{
    /// <summary>
    /// Correctly implements the <see cref="IDisposable"/> interface and pattern for an object containing only managed resources, and adds a few common niceities not on the interface such as an <see cref="IsDisposed"/> property.
    /// </summary>
    public abstract class SsdpInfrastructure : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether sets or returns a boolean indicating whether or not this instance has been disposed.
        /// </summary>
        /// <seealso cref="Dispose()"/>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Builds an SSDP message.
        /// </summary>
        /// <param name="header">SSDP Header string.</param>
        /// <param name="values">SSDP paramaters.</param>
        /// <returns>Formatted string.</returns>
        public static string BuildMessage(string header, Dictionary<string, string> values)
        {
            var builder = new StringBuilder();

            builder.AppendFormat(CultureInfo.InvariantCulture, "{0}\r\n", header);
            if (values != null)
            {
                foreach (var pair in values)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}\r\n", pair.Key, pair.Value);
                }
            }

            builder.Append("\r\n");

            return builder.ToString();
        }

        /// <summary>
        /// Disposes this object instance and all internally managed resources.
        /// </summary>
        /// <remarks>
        /// <para>Sets the <see cref="IsDisposed"/> property to true. Does not explicitly throw an exception if called multiple times, but makes no promises about behaviour of derived classes.</para>
        /// </remarks>
        /// <seealso cref="IsDisposed"/>
        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "We do exactly as asked, but CA doesn't seem to like us also setting the IsDisposed property. Too bad, it's a good idea and shouldn't cause an exception or anything likely to interfer with the dispose process.")]
        public void Dispose()
        {
            IsDisposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a Header from the collection.
        /// </summary>
        /// <param name="headerName">Name to look for.</param>
        /// <param name="headers">Collection to search.</param>
        /// <returns>Value of the property.</returns>
        protected static Uri? GetFirstHeaderUriValue(string headerName, HttpHeaders headers)
        {
            if (headers == null)
            {
                return null;
            }

            string value = string.Empty;
            if (headers.TryGetValues(headerName, out IEnumerable<string> values) && values != null)
            {
                value = values.FirstOrDefault();
            }

            if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri retVal))
            {
                return retVal;
            }

            return null;
        }

        /// <summary>
        /// Returns a Header from the collection.
        /// </summary>
        /// <param name="headerName">Name to look for.</param>
        /// <param name="headers">Collection to search.</param>
        /// <returns>Value of the property.</returns>
        protected static string GetFirstHeaderValue(string headerName, HttpHeaders headers)
        {
            if (headers == null)
            {
                return string.Empty;
            }

            string retVal = string.Empty;
            if (headers.TryGetValues(headerName, out IEnumerable<string> values) && values != null)
            {
                retVal = values.FirstOrDefault();
            }

            return retVal;
        }

        /// <summary>
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Throws and <see cref="ObjectDisposedException"/> if the <see cref="IsDisposed"/> property is true.
        /// </summary>
        /// <seealso cref="IsDisposed"/>
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="IsDisposed"/> property is true.</exception>
        /// <seealso cref="Dispose()"/>
        protected virtual void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
