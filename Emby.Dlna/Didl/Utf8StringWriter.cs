using System.IO;
using System.Text;

namespace Emby.Dlna.Didl
{
    /// <summary>
    /// Defines the <see cref="Utf8StringWriter" />.
    /// </summary>
    public class Utf8StringWriter : StringWriter
    {
        /// <summary>
        /// Gets the Encoding type of UTF8.
        /// </summary>
        public override Encoding Encoding => Encoding.UTF8;
    }
}
