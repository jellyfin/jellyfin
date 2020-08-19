#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Emby.Dlna.Didl
{
    public class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding()
        {
        }

        public StringWriterWithEncoding(IFormatProvider formatProvider)
            : base(formatProvider)
        {
        }

        public StringWriterWithEncoding(StringBuilder sb)
            : base(sb, CultureInfo.InvariantCulture)
        {
        }

        public StringWriterWithEncoding(StringBuilder sb, IFormatProvider formatProvider)
            : base(sb, formatProvider)
        {
        }


        public StringWriterWithEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public StringWriterWithEncoding(IFormatProvider formatProvider, Encoding encoding)
            : base(formatProvider)
        {
            _encoding = encoding;
        }

        public StringWriterWithEncoding(StringBuilder sb, Encoding encoding)
            : base(sb, CultureInfo.InvariantCulture)
        {
            _encoding = encoding;
        }

        public StringWriterWithEncoding(StringBuilder sb, IFormatProvider formatProvider, Encoding encoding)
            : base(sb, formatProvider)
        {
            _encoding = encoding;
        }

        public override Encoding Encoding => _encoding ?? base.Encoding;
    }
}
