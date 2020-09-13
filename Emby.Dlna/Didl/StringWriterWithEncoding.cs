#pragma warning disable CA1305

using System;
using System.IO;
using System.Text;

namespace Emby.Dlna.Didl
{
    /// <summary>
    /// Defines the <see cref="StringWriterWithEncoding" />.
    /// </summary>
    public class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        public StringWriterWithEncoding()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/>.</param>
        public StringWriterWithEncoding(IFormatProvider formatProvider)
            : base(formatProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/>.</param>
        public StringWriterWithEncoding(StringBuilder sb)
            : base(sb)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        /// <param name="sb">The sb<see cref="StringBuilder"/>.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/>.</param>
        public StringWriterWithEncoding(StringBuilder sb, IFormatProvider formatProvider)
            : base(sb, formatProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/>.</param>
        public StringWriterWithEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/>.</param>
        /// <param name="encoding">The <see cref="Encoding"/>.</param>
        public StringWriterWithEncoding(IFormatProvider formatProvider, Encoding encoding)
            : base(formatProvider)
        {
            _encoding = encoding;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/>.</param>
        /// <param name="encoding">The <see cref="Encoding"/>.</param>
        public StringWriterWithEncoding(StringBuilder sb, Encoding encoding)
            : base(sb)
        {
            _encoding = encoding;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/>.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/>.</param>
        /// <param name="encoding">The <see cref="Encoding"/>.</param>
        public StringWriterWithEncoding(StringBuilder sb, IFormatProvider formatProvider, Encoding encoding)
            : base(sb, formatProvider)
        {
            _encoding = encoding;
        }

        /// <summary>
        /// Gets the Encoding.
        /// </summary>
        public override Encoding Encoding => _encoding ?? base.Encoding;
    }
}
