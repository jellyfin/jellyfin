using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.SocketSharp
{
    public partial class WebSocketSharpRequest : IHttpRequest
    {
        internal static string GetParameter(ReadOnlySpan<char> header, string attr)
        {
            int ap = header.IndexOf(attr.AsSpan(), StringComparison.Ordinal);
            if (ap == -1)
            {
                return null;
            }

            ap += attr.Length;
            if (ap >= header.Length)
            {
                return null;
            }

            char ending = header[ap];
            if (ending != '"')
            {
                ending = ' ';
            }

            var slice = header.Slice(ap + 1);
            int end = slice.IndexOf(ending);
            if (end == -1)
            {
                return ending == '"' ? null : header.Slice(ap).ToString();
            }

            return slice.Slice(0, end - ap - 1).ToString();
        }

        private async Task LoadMultiPart(WebROCollection form)
        {
            string boundary = GetParameter(ContentType.AsSpan(), "; boundary=");
            if (boundary == null)
            {
                return;
            }

            using (var requestStream = InputStream)
            {
                // DB: 30/01/11 - Hack to get around non-seekable stream and received HTTP request
                // Not ending with \r\n?
                var ms = new MemoryStream(32 * 1024);
                await requestStream.CopyToAsync(ms).ConfigureAwait(false);

                var input = ms;
                ms.WriteByte((byte)'\r');
                ms.WriteByte((byte)'\n');

                input.Position = 0;

                // Uncomment to debug
                // var content = new StreamReader(ms).ReadToEnd();
                // Console.WriteLine(boundary + "::" + content);
                // input.Position = 0;

                var multi_part = new HttpMultipart(input, boundary, ContentEncoding);

                HttpMultipart.Element e;
                while ((e = multi_part.ReadNextElement()) != null)
                {
                    if (e.Filename == null)
                    {
                        byte[] copy = new byte[e.Length];

                        input.Position = e.Start;
                        await input.ReadAsync(copy, 0, (int)e.Length).ConfigureAwait(false);

                        form.Add(e.Name, (e.Encoding ?? ContentEncoding).GetString(copy, 0, copy.Length));
                    }
                    else
                    {
                        // We use a substream, as in 2.x we will support large uploads streamed to disk,
                        files[e.Name] = new HttpPostedFile(e.Filename, e.ContentType, input, e.Start, e.Length);
                    }
                }
            }
        }

        public async Task<QueryParamCollection> GetFormData()
        {
            var form = new WebROCollection();
            files = new Dictionary<string, HttpPostedFile>();

            if (IsContentType("multipart/form-data"))
            {
                await LoadMultiPart(form).ConfigureAwait(false);
            }
            else if (IsContentType("application/x-www-form-urlencoded"))
            {
                await LoadWwwForm(form).ConfigureAwait(false);
            }

            if (validate_form && !checked_form)
            {
                checked_form = true;
                ValidateNameValueCollection("Form", form);
            }

            return form;
        }

        public string Accept => StringValues.IsNullOrEmpty(request.Headers[HeaderNames.Accept]) ? null : request.Headers[HeaderNames.Accept].ToString();

        public string Authorization => StringValues.IsNullOrEmpty(request.Headers[HeaderNames.Authorization]) ? null : request.Headers[HeaderNames.Authorization].ToString();

        protected bool validate_form { get; set; }
        protected bool checked_form { get; set; }

        private static void ThrowValidationException(string name, string key, string value)
        {
            string v = "\"" + value + "\"";
            if (v.Length > 20)
            {
                v = v.Substring(0, 16) + "...\"";
            }

            string msg = string.Format(
                CultureInfo.InvariantCulture,
                "A potentially dangerous Request.{0} value was detected from the client ({1}={2}).",
                name,
                key,
                v);

            throw new Exception(msg);
        }

        private static void ValidateNameValueCollection(string name, QueryParamCollection coll)
        {
            if (coll == null)
            {
                return;
            }

            foreach (var pair in coll)
            {
                var key = pair.Name;
                var val = pair.Value;
                if (val != null && val.Length > 0 && IsInvalidString(val))
                {
                    ThrowValidationException(name, key, val);
                }
            }
        }

        internal static bool IsInvalidString(string val)
            => IsInvalidString(val, out var validationFailureIndex);

        internal static bool IsInvalidString(string val, out int validationFailureIndex)
        {
            validationFailureIndex = 0;

            int len = val.Length;
            if (len < 2)
            {
                return false;
            }

            char current = val[0];
            for (int idx = 1; idx < len; idx++)
            {
                char next = val[idx];

                // See http://secunia.com/advisories/14325
                if (current == '<' || current == '\xff1c')
                {
                    if (next == '!' || next < ' '
                        || (next >= 'a' && next <= 'z')
                        || (next >= 'A' && next <= 'Z'))
                    {
                        validationFailureIndex = idx - 1;
                        return true;
                    }
                }
                else if (current == '&' && next == '#')
                {
                    validationFailureIndex = idx - 1;
                    return true;
                }

                current = next;
            }

            return false;
        }

        private bool IsContentType(string ct)
        {
            if (ContentType == null)
            {
                return false;
            }

            return ContentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase);
        }

        private async Task LoadWwwForm(WebROCollection form)
        {
            using (var input = InputStream)
            {
                using (var ms = new MemoryStream())
                {
                    await input.CopyToAsync(ms).ConfigureAwait(false);
                    ms.Position = 0;

                    using (var s = new StreamReader(ms, ContentEncoding))
                    {
                        var key = new StringBuilder();
                        var value = new StringBuilder();
                        int c;

                        while ((c = s.Read()) != -1)
                        {
                            if (c == '=')
                            {
                                value.Length = 0;
                                while ((c = s.Read()) != -1)
                                {
                                    if (c == '&')
                                    {
                                        AddRawKeyValue(form, key, value);
                                        break;
                                    }
                                    else
                                    {
                                        value.Append((char)c);
                                    }
                                }

                                if (c == -1)
                                {
                                    AddRawKeyValue(form, key, value);
                                    return;
                                }
                            }
                            else if (c == '&')
                            {
                                AddRawKeyValue(form, key, value);
                            }
                            else
                            {
                                key.Append((char)c);
                            }
                        }

                        if (c == -1)
                        {
                            AddRawKeyValue(form, key, value);
                        }
                    }
                }
            }
        }

        private static void AddRawKeyValue(WebROCollection form, StringBuilder key, StringBuilder value)
        {
            form.Add(WebUtility.UrlDecode(key.ToString()), WebUtility.UrlDecode(value.ToString()));

            key.Length = 0;
            value.Length = 0;
        }

        private Dictionary<string, HttpPostedFile> files;

        private class WebROCollection : QueryParamCollection
        {
            public override string ToString()
            {
                var result = new StringBuilder();
                foreach (var pair in this)
                {
                    if (result.Length > 0)
                    {
                        result.Append('&');
                    }

                    var key = pair.Name;
                    if (key != null && key.Length > 0)
                    {
                        result.Append(key);
                        result.Append('=');
                    }

                    result.Append(pair.Value);
                }

                return result.ToString();
            }
        }
        private class HttpMultipart
        {

            public class Element
            {
                public string ContentType { get; set; }

                public string Name { get; set; }

                public string Filename { get; set; }

                public Encoding Encoding { get; set; }

                public long Start { get; set; }

                public long Length { get; set; }

                public override string ToString()
                {
                    return "ContentType " + ContentType + ", Name " + Name + ", Filename " + Filename + ", Start " +
                        Start.ToString(CultureInfo.CurrentCulture) + ", Length " + Length.ToString(CultureInfo.CurrentCulture);
                }
            }

            private const byte LF = (byte)'\n';

            private const byte CR = (byte)'\r';

            private Stream data;

            private string boundary;

            private byte[] boundaryBytes;

            private byte[] buffer;

            private bool atEof;

            private Encoding encoding;

            private StringBuilder sb;

            // See RFC 2046
            // In the case of multipart entities, in which one or more different
            // sets of data are combined in a single body, a "multipart" media type
            // field must appear in the entity's header.  The body must then contain
            // one or more body parts, each preceded by a boundary delimiter line,
            // and the last one followed by a closing boundary delimiter line.
            // After its boundary delimiter line, each body part then consists of a
            // header area, a blank line, and a body area.  Thus a body part is
            // similar to an RFC 822 message in syntax, but different in meaning.

            public HttpMultipart(Stream data, string b, Encoding encoding)
            {
                this.data = data;
                boundary = b;
                boundaryBytes = encoding.GetBytes(b);
                buffer = new byte[boundaryBytes.Length + 2]; // CRLF or '--'
                this.encoding = encoding;
                sb = new StringBuilder();
            }

            public Element ReadNextElement()
            {
                if (atEof || ReadBoundary())
                {
                    return null;
                }

                var elem = new Element();
                ReadOnlySpan<char> header;
                while ((header = ReadLine().AsSpan()).Length != 0)
                {
                    if (header.StartsWith("Content-Disposition:".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        elem.Name = GetContentDispositionAttribute(header, "name");
                        elem.Filename = StripPath(GetContentDispositionAttributeWithEncoding(header, "filename"));
                    }
                    else if (header.StartsWith("Content-Type:".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        elem.ContentType = header.Slice("Content-Type:".Length).Trim().ToString();
                        elem.Encoding = GetEncoding(elem.ContentType);
                    }
                }

                long start = data.Position;
                elem.Start = start;
                long pos = MoveToNextBoundary();
                if (pos == -1)
                {
                    return null;
                }

                elem.Length = pos - start;
                return elem;
            }

            private string ReadLine()
            {
                // CRLF or LF are ok as line endings.
                bool got_cr = false;
                int b = 0;
                sb.Length = 0;
                while (true)
                {
                    b = data.ReadByte();
                    if (b == -1)
                    {
                        return null;
                    }

                    if (b == LF)
                    {
                        break;
                    }

                    got_cr = b == CR;
                    sb.Append((char)b);
                }

                if (got_cr)
                {
                    sb.Length--;
                }

                return sb.ToString();
            }

            private static string GetContentDispositionAttribute(ReadOnlySpan<char> l, string name)
            {
                int idx = l.IndexOf((name + "=\"").AsSpan(), StringComparison.Ordinal);
                if (idx < 0)
                {
                    return null;
                }

                int begin = idx + name.Length + "=\"".Length;
                int end = l.Slice(begin).IndexOf('"');
                if (end < 0)
                {
                    return null;
                }

                if (begin == end)
                {
                    return string.Empty;
                }

                return l.Slice(begin, end - begin).ToString();
            }

            private string GetContentDispositionAttributeWithEncoding(ReadOnlySpan<char> l, string name)
            {
                int idx = l.IndexOf((name + "=\"").AsSpan(), StringComparison.Ordinal);
                if (idx < 0)
                {
                    return null;
                }

                int begin = idx + name.Length + "=\"".Length;
                int end = l.Slice(begin).IndexOf('"');
                if (end < 0)
                {
                    return null;
                }

                if (begin == end)
                {
                    return string.Empty;
                }

                ReadOnlySpan<char> temp = l.Slice(begin, end - begin);
                byte[] source = new byte[temp.Length];
                for (int i = temp.Length - 1; i >= 0; i--)
                {
                    source[i] = (byte)temp[i];
                }

                return encoding.GetString(source, 0, source.Length);
            }

            private bool ReadBoundary()
            {
                try
                {
                    string line;
                    do
                    {
                        line = ReadLine();
                    }
                    while (line.Length == 0);

                    if (line[0] != '-' || line[1] != '-')
                    {
                        return false;
                    }

                    if (!line.EndsWith(boundary, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                catch
                {

                }

                return false;
            }

            private static bool CompareBytes(byte[] orig, byte[] other)
            {
                for (int i = orig.Length - 1; i >= 0; i--)
                {
                    if (orig[i] != other[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            private long MoveToNextBoundary()
            {
                long retval = 0;
                bool got_cr = false;

                int state = 0;
                int c = data.ReadByte();
                while (true)
                {
                    if (c == -1)
                    {
                        return -1;
                    }

                    if (state == 0 && c == LF)
                    {
                        retval = data.Position - 1;
                        if (got_cr)
                        {
                            retval--;
                        }

                        state = 1;
                        c = data.ReadByte();
                    }
                    else if (state == 0)
                    {
                        got_cr = c == CR;
                        c = data.ReadByte();
                    }
                    else if (state == 1 && c == '-')
                    {
                        c = data.ReadByte();
                        if (c == -1)
                        {
                            return -1;
                        }

                        if (c != '-')
                        {
                            state = 0;
                            got_cr = false;
                            continue; // no ReadByte() here
                        }

                        int nread = data.Read(buffer, 0, buffer.Length);
                        int bl = buffer.Length;
                        if (nread != bl)
                        {
                            return -1;
                        }

                        if (!CompareBytes(boundaryBytes, buffer))
                        {
                            state = 0;
                            data.Position = retval + 2;
                            if (got_cr)
                            {
                                data.Position++;
                                got_cr = false;
                            }

                            c = data.ReadByte();
                            continue;
                        }

                        if (buffer[bl - 2] == '-' && buffer[bl - 1] == '-')
                        {
                            atEof = true;
                        }
                        else if (buffer[bl - 2] != CR || buffer[bl - 1] != LF)
                        {
                            state = 0;
                            data.Position = retval + 2;
                            if (got_cr)
                            {
                                data.Position++;
                                got_cr = false;
                            }

                            c = data.ReadByte();
                            continue;
                        }

                        data.Position = retval + 2;
                        if (got_cr)
                        {
                            data.Position++;
                        }

                        break;
                    }
                    else
                    {
                        // state == 1
                        state = 0; // no ReadByte() here
                    }
                }

                return retval;
            }

            private static string StripPath(string path)
            {
                if (path == null || path.Length == 0)
                {
                    return path;
                }

                if (path.IndexOf(":\\", StringComparison.Ordinal) != 1
                    && !path.StartsWith("\\\\", StringComparison.Ordinal))
                {
                    return path;
                }

                return path.Substring(path.LastIndexOf('\\') + 1);
            }
        }
    }
}
