using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace SharpCifs.Util.Sharpen
{
    public abstract class MessageDigest
	{
	    public void Digest (byte[] buffer, int o, int len)
		{
			byte[] d = Digest ();
			d.CopyTo (buffer, o);
		}

		public byte[] Digest (byte[] buffer)
		{
			Update (buffer);
			return Digest ();
		}

		public abstract byte[] Digest ();
		public abstract int GetDigestLength ();
		public static MessageDigest GetInstance (string algorithm)
		{
			switch (algorithm.ToLower ()) {
			case "sha-1":
                //System.Security.CryptographySHA1Managed not found
                //return new MessageDigest<SHA1Managed> ();
                return new MessageDigest<System.Security.Cryptography.SHA1>();
            case "md5":
                return new MessageDigest<Md5Managed> ();
            }
			throw new NotSupportedException (string.Format ("The requested algorithm \"{0}\" is not supported.", algorithm));
		}

		public abstract void Reset ();
		public abstract void Update (byte[] b);
		public abstract void Update (byte b);
		public abstract void Update (byte[] b, int offset, int len);
	}


	public class MessageDigest<TAlgorithm> : MessageDigest where TAlgorithm : HashAlgorithm //, new() //use static `Create` method
	{
		private TAlgorithm _hash;
        //private CryptoStream _stream; //don't work .NET Core
        private MemoryStream _stream;


        public MessageDigest ()
		{
			Init ();
		}

		public override byte[] Digest ()
		{
            //CryptoStream -> MemoryStream, needless method
            //_stream.FlushFinalBlock ();

            //HashAlgorithm.`Hash` property deleted
            //byte[] hash = _hash.Hash;
            byte[] hash = _hash.ComputeHash(_stream.ToArray());

            Reset ();
			return hash;
		}

		public void Dispose ()
		{
			if (_stream != null) {
				_stream.Dispose ();
			}
			_stream = null;
        }

        public override int GetDigestLength ()
		{
			return (_hash.HashSize / 8);
		}

		private void Init ()
		{
            //use static `Create` method
            //_hash = Activator.CreateInstance<TAlgorithm> ();
            var createMethod = typeof(TAlgorithm).GetRuntimeMethod("Create", new Type[0]);
            _hash = (TAlgorithm)createMethod.Invoke(null, new object[] {});

            //HashAlgorithm cannot cast `ICryptoTransform` on .NET Core, gave up using CryptoStream. 
            //_stream = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write);
            //_stream = new CryptoStream(_tmpStream, (ICryptoTransform)_hash, CryptoStreamMode.Write);
            _stream = new MemoryStream();
        }

        public override void Reset ()
		{
			Dispose ();
			Init ();
		}

		public override void Update (byte[] input)
		{
			_stream.Write (input, 0, input.Length);
		}

		public override void Update (byte input)
		{
			_stream.WriteByte (input);
		}

		public override void Update (byte[] input, int index, int count)
		{
			if (count < 0)
				Console.WriteLine ("Argh!");
			_stream.Write (input, index, count);
		}
	}
}
