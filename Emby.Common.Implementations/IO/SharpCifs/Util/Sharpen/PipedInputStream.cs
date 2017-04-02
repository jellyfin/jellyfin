using System;
using System.Threading;

namespace SharpCifs.Util.Sharpen
{
	internal class PipedInputStream : InputStream
	{
		private byte[] _oneBuffer;
		public const int PipeSize = 1024;
		
		protected byte[] Buffer;
		private bool _closed;
		private ManualResetEvent _dataEvent;
		private int _end;
		private int _start;
		private object _thisLock;
		private bool _allowGrow = false;
		
		public int In {
			get { return _start; }
			set { _start = value; }
		}
		
		public int Out {
			get { return _end; }
			set { _end = value; }
		}

		public PipedInputStream ()
		{
			_thisLock = new object ();
			_dataEvent = new ManualResetEvent (false);
			Buffer = new byte[PipeSize + 1];
		}

		public PipedInputStream (PipedOutputStream os): this ()
		{
			os.Attach (this);
		}

		public override void Close ()
		{
			lock (_thisLock) {
				_closed = true;
				_dataEvent.Set ();
			}
		}

		public override int Available ()
		{
			lock (_thisLock) {
				if (_start <= _end) {
					return (_end - _start);
				}
				return ((Buffer.Length - _start) + _end);
			}
		}

		public override int Read ()
		{
			if (_oneBuffer == null)
				_oneBuffer = new byte[1];
			if (Read (_oneBuffer, 0, 1) == -1)
				return -1;
			return _oneBuffer[0];
		}

		public override int Read (byte[] b, int offset, int len)
		{
			int length = 0;
			do {
				_dataEvent.WaitOne ();
				lock (_thisLock) {
					if (_closed && Available () == 0) {
						return -1;
					}
					if (_start < _end) {
						length = Math.Min (len, _end - _start);
						Array.Copy (Buffer, _start, b, offset, length);
						_start += length;
					} else if (_start > _end) {
						length = Math.Min (len, Buffer.Length - _start);
						Array.Copy (Buffer, _start, b, offset, length);
						len -= length;
						_start = (_start + length) % Buffer.Length;
						if (len > 0) {
							int i = Math.Min (len, _end);
							Array.Copy (Buffer, 0, b, offset + length, i);
							_start += i;
							length += i;
						}
					}
					if (_start == _end && !_closed) {
						_dataEvent.Reset ();
					}
					Monitor.PulseAll (_thisLock);
				}
			} while (length == 0);
			return length;
		}
		
		private int Allocate (int len)
		{
			int alen;
			while ((alen = TryAllocate (len)) == 0) {
				// Wait until somebody reads data
				try {
					Monitor.Wait (_thisLock);
				} catch {
					_closed = true;
					_dataEvent.Set ();
					throw;
				}
			}
			return alen;
		}
		
		int TryAllocate (int len)
		{
			int free;
			if (_start <= _end) {
				free = (Buffer.Length - _end) + _start;
			} else {
				free = _start - _end;
			}
			if (free <= len) {
				if (!_allowGrow)
					return free > 0 ? free - 1 : 0;
				int sizeInc = (len - free) + 1;
				byte[] destinationArray = new byte[Buffer.Length + sizeInc];
				if (_start <= _end) {
					Array.Copy (Buffer, _start, destinationArray, _start, _end - _start);
				} else {
					Array.Copy (Buffer, 0, destinationArray, 0, _end);
					Array.Copy (Buffer, _start, destinationArray, _start + sizeInc, Buffer.Length - _start);
					_start += sizeInc;
				}
				Buffer = destinationArray;
			}
			return len;
		}
		
		internal void Write (int b)
		{
			lock (_thisLock) {
				Allocate (1);
				Buffer[_end] = (byte)b;
				_end = (_end + 1) % Buffer.Length;
				_dataEvent.Set ();
			}
		}
		
		internal void Write (byte[] b, int offset, int len)
		{
			do {
				lock (_thisLock) {
					int alen = Allocate (len);
					int length = Math.Min (Buffer.Length - _end, alen);
					Array.Copy (b, offset, Buffer, _end, length);
					_end = (_end + length) % Buffer.Length;
					if (length < alen) {
						Array.Copy (b, offset + length, Buffer, 0, alen - length);
						_end += alen - length;
					}
					_dataEvent.Set ();
					len -= alen;
					offset += alen;
				}
			} while (len > 0);
		}
	}
}
