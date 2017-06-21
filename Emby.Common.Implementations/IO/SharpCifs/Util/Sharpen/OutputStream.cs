using System;
using System.IO;

namespace SharpCifs.Util.Sharpen
{
    public class OutputStream : IDisposable
    {
        protected Stream Wrapped;

        public static implicit operator OutputStream(Stream s)
        {
            return Wrap(s);
        }

        public static implicit operator Stream(OutputStream s)
        {
            return s.GetWrappedStream();
        }

        public virtual void Close()
        {
            if (Wrapped != null)
            {
                //Stream.`Close` method deleted
                //Wrapped.Close ();
                Wrapped.Dispose();
            }
        }

        public void Dispose()
        {
            Close();
        }

        public virtual void Flush()
        {
            if (Wrapped != null)
            {
                Wrapped.Flush();
            }
        }

        internal Stream GetWrappedStream()
        {
            // Always create a wrapper stream (not directly Wrapped) since the subclass
            // may be overriding methods that need to be called when used through the Stream class
            return new WrappedSystemStream(this);
        }

        static internal OutputStream Wrap(Stream s)
        {
            OutputStream stream = new OutputStream();
            stream.Wrapped = s;
            return stream;
        }

        public virtual void Write(int b)
        {
            if (Wrapped is WrappedSystemStream)
                ((WrappedSystemStream)Wrapped).OutputStream.Write(b);
            else
            {
                if (Wrapped == null)
                    throw new NotImplementedException();
                Wrapped.WriteByte((byte)b);
            }
        }

        public virtual void Write(byte[] b)
        {
            Write(b, 0, b.Length);
        }

        public virtual void Write(byte[] b, int offset, int len)
        {
            if (Wrapped is WrappedSystemStream)
                ((WrappedSystemStream)Wrapped).OutputStream.Write(b, offset, len);
            else
            {
                if (Wrapped != null)
                {
                    Wrapped.Write(b, offset, len);
                }
                else
                {
                    for (int i = 0; i < len; i++)
                    {
                        Write(b[i + offset]);
                    }
                }
            }
        }
    }
}
