using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ServiceStack;
using ServiceStack.Web;

namespace MediaBrowser.Server.Implementations.HttpServer.SocketSharp
{
    public partial class WebSocketSharpRequest : IHttpRequest
    {
        static internal string GetParameter(string header, string attr)
        {
            int ap = header.IndexOf(attr);
            if (ap == -1)
                return null;

            ap += attr.Length;
            if (ap >= header.Length)
                return null;

            char ending = header[ap];
            if (ending != '"')
                ending = ' ';

            int end = header.IndexOf(ending, ap + 1);
            if (end == -1)
                return ending == '"' ? null : header.Substring(ap);

            return header.Substring(ap + 1, end - ap - 1);
        }

        async Task LoadMultiPart()
        {
            string boundary = GetParameter(ContentType, "; boundary=");
            if (boundary == null)
                return;

            using (var requestStream = GetSubStream(InputStream))
            {
                //DB: 30/01/11 - Hack to get around non-seekable stream and received HTTP request
                //Not ending with \r\n?
                var ms = new MemoryStream(32 * 1024);
                await requestStream.CopyToAsync(ms).ConfigureAwait(false);

                var input = ms;
                ms.WriteByte((byte)'\r');
                ms.WriteByte((byte)'\n');

                input.Position = 0;

                //Uncomment to debug
                //var content = new StreamReader(ms).ReadToEnd();
                //Console.WriteLine(boundary + "::" + content);
                //input.Position = 0;

                var multi_part = new HttpMultipart(input, boundary, ContentEncoding);

                HttpMultipart.Element e;
                while ((e = multi_part.ReadNextElement()) != null)
                {
                    if (e.Filename == null)
                    {
                        byte[] copy = new byte[e.Length];

                        input.Position = e.Start;
                        input.Read(copy, 0, (int)e.Length);

                        form.Add(e.Name, (e.Encoding ?? ContentEncoding).GetString(copy));
                    }
                    else
                    {
                        //
                        // We use a substream, as in 2.x we will support large uploads streamed to disk,
                        //
                        HttpPostedFile sub = new HttpPostedFile(e.Filename, e.ContentType, input, e.Start, e.Length);
                        files.AddFile(e.Name, sub);
                    }
                }
            }
        }

        public NameValueCollection Form
        {
            get
            {
                if (form == null)
                {
                    form = new WebROCollection();
                    files = new HttpFileCollection();

                    if (IsContentType("multipart/form-data", true))
                    {
                        var task = LoadMultiPart();
                        Task.WaitAll(task);
                    }
                    else if (IsContentType("application/x-www-form-urlencoded", true))
                    {
                        var task = LoadWwwForm();
                        Task.WaitAll(task);
                    }

                    form.Protect();
                }

#if NET_4_0
				if (validateRequestNewMode && !checked_form) {
					// Setting this before calling the validator prevents
					// possible endless recursion
					checked_form = true;
					ValidateNameValueCollection ("Form", query_string_nvc, RequestValidationSource.Form);
				} else
#endif
                if (validate_form && !checked_form)
                {
                    checked_form = true;
                    ValidateNameValueCollection("Form", form);
                }

                return form;
            }
        }

        public string Accept
        {
            get
            {
                return string.IsNullOrEmpty(request.Headers[HttpHeaders.Accept]) ? null : request.Headers[HttpHeaders.Accept];
            }
        }

        public string Authorization
        {
            get
            {
                return string.IsNullOrEmpty(request.Headers[HttpHeaders.Authorization]) ? null : request.Headers[HttpHeaders.Authorization];
            }
        }

        protected bool validate_cookies, validate_query_string, validate_form;
        protected bool checked_cookies, checked_query_string, checked_form;

        static void ThrowValidationException(string name, string key, string value)
        {
            string v = "\"" + value + "\"";
            if (v.Length > 20)
                v = v.Substring(0, 16) + "...\"";

            string msg = String.Format("A potentially dangerous Request.{0} value was " +
                            "detected from the client ({1}={2}).", name, key, v);

            throw new HttpRequestValidationException(msg);
        }

        static void ValidateNameValueCollection(string name, NameValueCollection coll)
        {
            if (coll == null)
                return;

            foreach (string key in coll.Keys)
            {
                string val = coll[key];
                if (val != null && val.Length > 0 && IsInvalidString(val))
                    ThrowValidationException(name, key, val);
            }
        }

        internal static bool IsInvalidString(string val)
        {
            int validationFailureIndex;

            return IsInvalidString(val, out validationFailureIndex);
        }

        internal static bool IsInvalidString(string val, out int validationFailureIndex)
        {
            validationFailureIndex = 0;

            int len = val.Length;
            if (len < 2)
                return false;

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

        public void ValidateInput()
        {
            validate_cookies = true;
            validate_query_string = true;
            validate_form = true;
        }

        bool IsContentType(string ct, bool starts_with)
        {
            if (ct == null || ContentType == null) return false;

            if (starts_with)
                return StrUtils.StartsWith(ContentType, ct, true);

            return String.Compare(ContentType, ct, true, Helpers.InvariantCulture) == 0;
        }

        async Task LoadWwwForm()
        {
            using (Stream input = GetSubStream(InputStream))
            {
                using (var ms = new MemoryStream())
                {
                    await input.CopyToAsync(ms).ConfigureAwait(false);
                    ms.Position = 0;

                    using (StreamReader s = new StreamReader(ms, ContentEncoding))
                    {
                        StringBuilder key = new StringBuilder();
                        StringBuilder value = new StringBuilder();
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
                                        AddRawKeyValue(key, value);
                                        break;
                                    }
                                    else
                                        value.Append((char)c);
                                }
                                if (c == -1)
                                {
                                    AddRawKeyValue(key, value);
                                    return;
                                }
                            }
                            else if (c == '&')
                                AddRawKeyValue(key, value);
                            else
                                key.Append((char)c);
                        }
                        if (c == -1)
                            AddRawKeyValue(key, value);
                    }
                }
            }
        }

        void AddRawKeyValue(StringBuilder key, StringBuilder value)
        {
            string decodedKey = HttpUtility.UrlDecode(key.ToString(), ContentEncoding);
            form.Add(decodedKey,
                  HttpUtility.UrlDecode(value.ToString(), ContentEncoding));

            key.Length = 0;
            value.Length = 0;
        }

        WebROCollection form;

        HttpFileCollection files;

        public sealed class HttpFileCollection : NameObjectCollectionBase
        {
            internal HttpFileCollection()
            {
            }

            internal void AddFile(string name, HttpPostedFile file)
            {
                BaseAdd(name, file);
            }

            public void CopyTo(Array dest, int index)
            {
                /* XXX this is kind of gross and inefficient
                 * since it makes a copy of the superclass's
                 * list */
                object[] values = BaseGetAllValues();
                values.CopyTo(dest, index);
            }

            public string GetKey(int index)
            {
                return BaseGetKey(index);
            }

            public HttpPostedFile Get(int index)
            {
                return (HttpPostedFile)BaseGet(index);
            }

            public HttpPostedFile Get(string key)
            {
                return (HttpPostedFile)BaseGet(key);
            }

            public HttpPostedFile this[string key]
            {
                get
                {
                    return Get(key);
                }
            }

            public HttpPostedFile this[int index]
            {
                get
                {
                    return Get(index);
                }
            }

            public string[] AllKeys
            {
                get
                {
                    return BaseGetAllKeys();
                }
            }
        }
        class WebROCollection : NameValueCollection
        {
            bool got_id;
            int id;

            public bool GotID
            {
                get { return got_id; }
            }

            public int ID
            {
                get { return id; }
                set
                {
                    got_id = true;
                    id = value;
                }
            }
            public void Protect()
            {
                IsReadOnly = true;
            }

            public void Unprotect()
            {
                IsReadOnly = false;
            }

            public override string ToString()
            {
                StringBuilder result = new StringBuilder();
                foreach (string key in AllKeys)
                {
                    if (result.Length > 0)
                        result.Append('&');

                    if (key != null && key.Length > 0)
                    {
                        result.Append(key);
                        result.Append('=');
                    }
                    result.Append(Get(key));
                }

                return result.ToString();
            }
        }

        public sealed class HttpPostedFile
        {
            string name;
            string content_type;
            Stream stream;

            class ReadSubStream : Stream
            {
                Stream s;
                long offset;
                long end;
                long position;

                public ReadSubStream(Stream s, long offset, long length)
                {
                    this.s = s;
                    this.offset = offset;
                    this.end = offset + length;
                    position = offset;
                }

                public override void Flush()
                {
                }

                public override int Read(byte[] buffer, int dest_offset, int count)
                {
                    if (buffer == null)
                        throw new ArgumentNullException("buffer");

                    if (dest_offset < 0)
                        throw new ArgumentOutOfRangeException("dest_offset", "< 0");

                    if (count < 0)
                        throw new ArgumentOutOfRangeException("count", "< 0");

                    int len = buffer.Length;
                    if (dest_offset > len)
                        throw new ArgumentException("destination offset is beyond array size");
                    // reordered to avoid possible integer overflow
                    if (dest_offset > len - count)
                        throw new ArgumentException("Reading would overrun buffer");

                    if (count > end - position)
                        count = (int)(end - position);

                    if (count <= 0)
                        return 0;

                    s.Position = position;
                    int result = s.Read(buffer, dest_offset, count);
                    if (result > 0)
                        position += result;
                    else
                        position = end;

                    return result;
                }

                public override int ReadByte()
                {
                    if (position >= end)
                        return -1;

                    s.Position = position;
                    int result = s.ReadByte();
                    if (result < 0)
                        position = end;
                    else
                        position++;

                    return result;
                }

                public override long Seek(long d, SeekOrigin origin)
                {
                    long real;
                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            real = offset + d;
                            break;
                        case SeekOrigin.End:
                            real = end + d;
                            break;
                        case SeekOrigin.Current:
                            real = position + d;
                            break;
                        default:
                            throw new ArgumentException();
                    }

                    long virt = real - offset;
                    if (virt < 0 || virt > Length)
                        throw new ArgumentException();

                    position = s.Seek(real, SeekOrigin.Begin);
                    return position;
                }

                public override void SetLength(long value)
                {
                    throw new NotSupportedException();
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    throw new NotSupportedException();
                }

                public override bool CanRead
                {
                    get { return true; }
                }
                public override bool CanSeek
                {
                    get { return true; }
                }
                public override bool CanWrite
                {
                    get { return false; }
                }

                public override long Length
                {
                    get { return end - offset; }
                }

                public override long Position
                {
                    get
                    {
                        return position - offset;
                    }
                    set
                    {
                        if (value > Length)
                            throw new ArgumentOutOfRangeException();

                        position = Seek(value, SeekOrigin.Begin);
                    }
                }
            }

            internal HttpPostedFile(string name, string content_type, Stream base_stream, long offset, long length)
            {
                this.name = name;
                this.content_type = content_type;
                this.stream = new ReadSubStream(base_stream, offset, length);
            }

            public string ContentType
            {
                get
                {
                    return content_type;
                }
            }

            public int ContentLength
            {
                get
                {
                    return (int)stream.Length;
                }
            }

            public string FileName
            {
                get
                {
                    return name;
                }
            }

            public Stream InputStream
            {
                get
                {
                    return stream;
                }
            }
        }

        class Helpers
        {
            public static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        }

        internal sealed class StrUtils
        {
            StrUtils() { }

            public static bool StartsWith(string str1, string str2)
            {
                return StartsWith(str1, str2, false);
            }

            public static bool StartsWith(string str1, string str2, bool ignore_case)
            {
                int l2 = str2.Length;
                if (l2 == 0)
                    return true;

                int l1 = str1.Length;
                if (l2 > l1)
                    return false;

                return 0 == String.Compare(str1, 0, str2, 0, l2, ignore_case, Helpers.InvariantCulture);
            }

            public static bool EndsWith(string str1, string str2)
            {
                return EndsWith(str1, str2, false);
            }

            public static bool EndsWith(string str1, string str2, bool ignore_case)
            {
                int l2 = str2.Length;
                if (l2 == 0)
                    return true;

                int l1 = str1.Length;
                if (l2 > l1)
                    return false;

                return 0 == String.Compare(str1, l1 - l2, str2, 0, l2, ignore_case, Helpers.InvariantCulture);
            }
        }

        class HttpMultipart
        {

            public class Element
            {
                public string ContentType;
                public string Name;
                public string Filename;
                public Encoding Encoding;
                public long Start;
                public long Length;

                public override string ToString()
                {
                    return "ContentType " + ContentType + ", Name " + Name + ", Filename " + Filename + ", Start " +
                        Start.ToString() + ", Length " + Length.ToString();
                }
            }

            Stream data;
            string boundary;
            byte[] boundary_bytes;
            byte[] buffer;
            bool at_eof;
            Encoding encoding;
            StringBuilder sb;

            const byte HYPHEN = (byte)'-', LF = (byte)'\n', CR = (byte)'\r';

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
                //DB: 30/01/11: cannot set or read the Position in HttpListener in Win.NET
                //var ms = new MemoryStream(32 * 1024);
                //data.CopyTo(ms);
                //this.data = ms;

                boundary = b;
                boundary_bytes = encoding.GetBytes(b);
                buffer = new byte[boundary_bytes.Length + 2]; // CRLF or '--'
                this.encoding = encoding;
                sb = new StringBuilder();
            }

            string ReadLine()
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
                    sb.Length--;

                return sb.ToString();

            }

            static string GetContentDispositionAttribute(string l, string name)
            {
                int idx = l.IndexOf(name + "=\"");
                if (idx < 0)
                    return null;
                int begin = idx + name.Length + "=\"".Length;
                int end = l.IndexOf('"', begin);
                if (end < 0)
                    return null;
                if (begin == end)
                    return "";
                return l.Substring(begin, end - begin);
            }

            string GetContentDispositionAttributeWithEncoding(string l, string name)
            {
                int idx = l.IndexOf(name + "=\"");
                if (idx < 0)
                    return null;
                int begin = idx + name.Length + "=\"".Length;
                int end = l.IndexOf('"', begin);
                if (end < 0)
                    return null;
                if (begin == end)
                    return "";

                string temp = l.Substring(begin, end - begin);
                byte[] source = new byte[temp.Length];
                for (int i = temp.Length - 1; i >= 0; i--)
                    source[i] = (byte)temp[i];

                return encoding.GetString(source);
            }

            bool ReadBoundary()
            {
                try
                {
                    string line = ReadLine();
                    while (line == "")
                        line = ReadLine();
                    if (line[0] != '-' || line[1] != '-')
                        return false;

                    if (!StrUtils.EndsWith(line, boundary, false))
                        return true;
                }
                catch
                {
                }

                return false;
            }

            string ReadHeaders()
            {
                string s = ReadLine();
                if (s == "")
                    return null;

                return s;
            }

            bool CompareBytes(byte[] orig, byte[] other)
            {
                for (int i = orig.Length - 1; i >= 0; i--)
                    if (orig[i] != other[i])
                        return false;

                return true;
            }

            long MoveToNextBoundary()
            {
                long retval = 0;
                bool got_cr = false;

                int state = 0;
                int c = data.ReadByte();
                while (true)
                {
                    if (c == -1)
                        return -1;

                    if (state == 0 && c == LF)
                    {
                        retval = data.Position - 1;
                        if (got_cr)
                            retval--;
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
                            return -1;

                        if (c != '-')
                        {
                            state = 0;
                            got_cr = false;
                            continue; // no ReadByte() here
                        }

                        int nread = data.Read(buffer, 0, buffer.Length);
                        int bl = buffer.Length;
                        if (nread != bl)
                            return -1;

                        if (!CompareBytes(boundary_bytes, buffer))
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
                            at_eof = true;
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
                            data.Position++;
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

            public Element ReadNextElement()
            {
                if (at_eof || ReadBoundary())
                    return null;

                Element elem = new Element();
                string header;
                while ((header = ReadHeaders()) != null)
                {
                    if (StrUtils.StartsWith(header, "Content-Disposition:", true))
                    {
                        elem.Name = GetContentDispositionAttribute(header, "name");
                        elem.Filename = StripPath(GetContentDispositionAttributeWithEncoding(header, "filename"));
                    }
                    else if (StrUtils.StartsWith(header, "Content-Type:", true))
                    {
                        elem.ContentType = header.Substring("Content-Type:".Length).Trim();
                        elem.Encoding = GetEncoding(elem.ContentType);
                    }
                }

                long start = 0;
                start = data.Position;
                elem.Start = start;
                long pos = MoveToNextBoundary();
                if (pos == -1)
                    return null;

                elem.Length = pos - start;
                return elem;
            }

            static string StripPath(string path)
            {
                if (path == null || path.Length == 0)
                    return path;

                if (path.IndexOf(":\\") != 1 && !path.StartsWith("\\\\"))
                    return path;
                return path.Substring(path.LastIndexOf('\\') + 1);
            }
        }

    }
}
