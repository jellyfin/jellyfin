using System;
using System.IO;

namespace SharpCifs.Util.Sharpen
{
    internal class WrappedSystemStream : Stream
	{
		private InputStream _ist;
		private OutputStream _ost;
		int _position;
		int _markedPosition;

		public WrappedSystemStream (InputStream ist)
		{
			this._ist = ist;
		}

		public WrappedSystemStream (OutputStream ost)
		{
			this._ost = ost;
		}
		
		public InputStream InputStream {
			get { return _ist; }
		}

		public OutputStream OutputStream {
			get { return _ost; }
		}

        public void Close()    //remove `override`
        {
			if (_ist != null) {
                //Stream.`Close` method deleted
				//_ist.Close ();
                _ist.Dispose();
			}
			if (_ost != null) {
                //Stream.`Close` method deleted
				//_ost.Close ();
                _ost.Dispose();
			}
		}

		public override void Flush ()
		{
			_ost.Flush ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			int res = _ist.Read (buffer, offset, count);
			if (res != -1) {
				_position += res;
				return res;
			}
		    return 0;
		}

		public override int ReadByte ()
		{
			int res = _ist.Read ();
			if (res != -1)
				_position++;
			return res;
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			if (origin == SeekOrigin.Begin)
				Position = offset;
			else if (origin == SeekOrigin.Current)
				Position = Position + offset;
			else if (origin == SeekOrigin.End)
				Position = Length + offset;
			return Position;
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			_ost.Write (buffer, offset, count);
			_position += count;
		}

		public override void WriteByte (byte value)
		{
			_ost.Write (value);
			_position++;
		}

		public override bool CanRead {
			get { return (_ist != null); }
		}

		public override bool CanSeek {
			get { return true; }
		}

		public override bool CanWrite {
			get { return (_ost != null); }
		}

		public override long Length {
			get { return _ist.Length; }
		}
		
		internal void OnMark (int nb)
		{
			_markedPosition = _position;
			_ist.Mark (nb);
		}
		
		public override long Position {
			get
			{
			    if (_ist != null && _ist.CanSeek ())
					return _ist.Position;
			    return _position;
			}
		    set
		    {
		        if (value == _position)
					return;
		        if (value == _markedPosition)
		            _ist.Reset ();
		        else if (_ist != null && _ist.CanSeek ()) {
		            _ist.Position = value;
		        }
		        else
		            throw new NotSupportedException ();
		    }
		}
	}
}
