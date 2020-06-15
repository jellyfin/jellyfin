namespace Mono.Nat.Upnp
{
    using System.Net;

    /// <summary>
    /// Defines the <see cref="IRequestMessage" />.
    /// </summary>
    internal interface IRequestMessage
    {
        /// <summary>
        /// The Encode.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns>The <see cref="HttpWebRequest"/>.</returns>
        HttpWebRequest Encode(out byte[] body);
    }
}
