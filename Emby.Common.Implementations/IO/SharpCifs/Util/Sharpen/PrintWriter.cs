using System;
using System.IO;
using System.Text;

namespace SharpCifs.Util.Sharpen
{
    public class PrintWriter : TextWriter
    {
        TextWriter _writer;
        private FileStream _stream;

        public PrintWriter(FilePath path)
        {
            //Stream(string path) constructor deleted
            _stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            _writer = new StreamWriter(_stream);
        }

        public PrintWriter(TextWriter other)
        {
            _writer = other;
        }

        public override Encoding Encoding
        {
            get { return _writer.Encoding; }
        }

        public void Close() // remove `override`
        {
            //Stream.`Close` method deleted
            //_writer.Close ();
            _writer.Dispose();
            _stream.Dispose();
        }

        public override void Flush()
        {
            _writer.Flush();
        }

        public override IFormatProvider FormatProvider
        {
            get
            {
                return _writer.FormatProvider;
            }
        }

        public override string NewLine
        {
            get
            {
                return _writer.NewLine;
            }
            set
            {
                _writer.NewLine = value;
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _writer.Write(buffer, index, count);
        }

        public override void Write(char[] buffer)
        {
            _writer.Write(buffer);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            _writer.Write(format, arg0, arg1, arg2);
        }

        public override void Write(string format, object arg0, object arg1)
        {
            _writer.Write(format, arg0, arg1);
        }

        public override void Write(string format, object arg0)
        {
            _writer.Write(format, arg0);
        }

        public override void Write(string format, params object[] arg)
        {
            _writer.Write(format, arg);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            _writer.WriteLine(buffer, index, count);
        }

        public override void WriteLine(char[] buffer)
        {
            _writer.WriteLine(buffer);
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            _writer.WriteLine(format, arg0, arg1, arg2);
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            _writer.WriteLine(format, arg0, arg1);
        }

        public override void WriteLine(string format, object arg0)
        {
            _writer.WriteLine(format, arg0);
        }

        public override void WriteLine(string format, params object[] arg)
        {
            _writer.WriteLine(format, arg);
        }

        public override void WriteLine(ulong value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(uint value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(string value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(float value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(object value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(long value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(int value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(decimal value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(char value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine(bool value)
        {
            _writer.WriteLine(value);
        }

        public override void WriteLine()
        {
            _writer.WriteLine();
        }

        public override void Write(bool value)
        {
            _writer.Write(value);
        }

        public override void Write(char value)
        {
            _writer.Write(value);
        }

        public override void Write(decimal value)
        {
            _writer.Write(value);
        }

        public override void Write(double value)
        {
            _writer.Write(value);
        }

        public override void Write(int value)
        {
            _writer.Write(value);
        }

        public override void Write(long value)
        {
            _writer.Write(value);
        }

        public override void Write(object value)
        {
            _writer.Write(value);
        }

        public override void Write(float value)
        {
            _writer.Write(value);
        }

        public override void Write(string value)
        {
            _writer.Write(value);
        }

        public override void Write(uint value)
        {
            _writer.Write(value);
        }

        public override void Write(ulong value)
        {
            _writer.Write(value);
        }
    }
}
