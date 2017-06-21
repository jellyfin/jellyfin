using System;
using System.IO;

namespace SharpCifs.Util.Sharpen
{
    public class RandomAccessFile
    {
        private FileStream _stream;

        public RandomAccessFile(FilePath file, string mode) : this(file.GetPath(), mode)
        {
        }

        public RandomAccessFile(string file, string mode)
        {
            if (mode.IndexOf('w') != -1)
                _stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            else
                _stream = new FileStream(file, FileMode.Open, FileAccess.Read);
        }

        public void Close()
        {
            //Stream.`Close` method deleted
            //_stream.Close ();
            _stream.Dispose();
        }

        public long GetFilePointer()
        {
            return _stream.Position;
        }

        public long Length()
        {
            return _stream.Length;
        }

        public int Read(byte[] buffer)
        {
            int r = _stream.Read(buffer, 0, buffer.Length);
            return r > 0 ? r : -1;
        }

        public int Read(byte[] buffer, int start, int size)
        {
            return _stream.Read(buffer, start, size);
        }

        public void ReadFully(byte[] buffer, int start, int size)
        {
            while (size > 0)
            {
                int num = _stream.Read(buffer, start, size);
                if (num == 0)
                {
                    throw new EofException();
                }
                size -= num;
                start += num;
            }
        }

        public void Seek(long pos)
        {
            _stream.Position = pos;
        }

        public void SetLength(long len)
        {
            _stream.SetLength(len);
        }

        public void Write(int value)
        {
            _stream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public void Write(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int start, int size)
        {
            _stream.Write(buffer, start, size);
        }
    }
}
