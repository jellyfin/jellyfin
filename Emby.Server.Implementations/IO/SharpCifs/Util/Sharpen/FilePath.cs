using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SharpCifs.Util.Sharpen
{
    public class FilePath
	{
		private string _path;
		private static long _tempCounter;

		public FilePath ()
		{
		}

		public FilePath (string path)
			: this ((string) null, path)
		{

		}

		public FilePath (FilePath other, string child)
			: this ((string) other, child)
		{

		}

		public FilePath (string other, string child)
		{
			if (other == null) {
				_path = child;
			} else {
				while (!string.IsNullOrEmpty(child) && (child[0] == Path.DirectorySeparatorChar || child[0] == Path.AltDirectorySeparatorChar))
					child = child.Substring (1);

				if (!string.IsNullOrEmpty(other) && other[other.Length - 1] == Path.VolumeSeparatorChar)
					other += Path.DirectorySeparatorChar;

				_path = Path.Combine (other, child);
			}
		}
		
		public static implicit operator FilePath (string name)
		{
			return new FilePath (name);
		}

		public static implicit operator string (FilePath filePath)
		{
			return filePath == null ? null : filePath._path;
		}
		
		public override bool Equals (object obj)
		{
			FilePath other = obj as FilePath;
			if (other == null)
				return false;
			return GetCanonicalPath () == other.GetCanonicalPath ();
		}
		
		public override int GetHashCode ()
		{
			return _path.GetHashCode ();
		}

		public bool CreateNewFile ()
		{
			try {
                //Stream.`Close` method deleted
                //File.Open (_path, FileMode.CreateNew).Close ();
                File.Open(_path, FileMode.CreateNew).Dispose();
                return true;
			} catch {
				return false;
			}
		}

		public static FilePath CreateTempFile ()
		{
			return new FilePath (Path.GetTempFileName ());
		}

		public static FilePath CreateTempFile (string prefix, string suffix)
		{
			return CreateTempFile (prefix, suffix, null);
		}

		public static FilePath CreateTempFile (string prefix, string suffix, FilePath directory)
		{
			string file;
			if (prefix == null) {
				throw new ArgumentNullException ("prefix");
			}
			if (prefix.Length < 3) {
				throw new ArgumentException ("prefix must have at least 3 characters");
			}
			string str = (directory == null) ? Path.GetTempPath () : directory.GetPath ();
			do {
				file = Path.Combine (str, prefix + Interlocked.Increment (ref _tempCounter) + suffix);
			} while (File.Exists (file));
			
			new FileOutputStream (file).Close ();
			return new FilePath (file);
		}


		public void DeleteOnExit ()
		{
		}


		public FilePath GetAbsoluteFile ()
		{
			return new FilePath (Path.GetFullPath (_path));
		}

		public string GetAbsolutePath ()
		{
			return Path.GetFullPath (_path);
		}

		public FilePath GetCanonicalFile ()
		{
			return new FilePath (GetCanonicalPath ());
		}

		public string GetCanonicalPath ()
		{
			string p = Path.GetFullPath (_path);
			p.TrimEnd (Path.DirectorySeparatorChar);
			return p;
		}

		public string GetName ()
		{
			return Path.GetFileName (_path);
		}

		public FilePath GetParentFile ()
		{
			return new FilePath (Path.GetDirectoryName (_path));
		}

		public string GetPath ()
		{
			return _path;
		}

		public bool IsAbsolute ()
		{
			return Path.IsPathRooted (_path);
		}

		public bool IsDirectory ()
		{
            return false; // FileHelper.Instance.IsDirectory(this);
		}

		public bool IsFile ()
		{
			return false; //FileHelper.Instance.IsFile (this);
		}

		public long LastModified ()
		{
            return 0; // FileHelper.Instance.LastModified(this);
		}

		public long Length ()
		{
            return 0; // FileHelper.Instance.Length(this);
		}

		public string[] List ()
		{
			return List (null);
		}

		public string[] List (IFilenameFilter filter)
		{
			try {
				if (IsFile ())
					return null;
				List<string> list = new List<string> ();
				foreach (string filePth in Directory.GetFileSystemEntries (_path)) {
					string fileName = Path.GetFileName (filePth);
					if ((filter == null) || filter.Accept (this, fileName)) {
						list.Add (fileName);
					}
				}
				return list.ToArray ();
			} catch {
				return null;
			}
		}

		public FilePath[] ListFiles ()
		{
			try {
				if (IsFile ())
					return null;
				List<FilePath> list = new List<FilePath> ();
				foreach (string filePath in Directory.GetFileSystemEntries (_path)) {
					list.Add (new FilePath (filePath));
				}
				return list.ToArray ();
			} catch {
				return null;
			}
		}

		static void MakeDirWritable (string dir)
		{
			//FileHelper.Instance.MakeDirWritable (dir);
		}

		static void MakeFileWritable (string file)
		{
			//FileHelper.Instance.MakeFileWritable (file);
		}

		public bool Mkdir ()
		{
			try {
				if (Directory.Exists (_path))
					return false;
				Directory.CreateDirectory (_path);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		public bool Mkdirs ()
		{
			try {
				if (Directory.Exists (_path))
					return false;
				Directory.CreateDirectory (_path);
				return true;
			} catch {
				return false;
			}
		}

		public bool RenameTo (FilePath file)
		{
			return RenameTo (file._path);
		}

		public bool RenameTo (string name)
		{
            return false; // FileHelper.Instance.RenameTo(this, name);
		}

		public bool SetLastModified (long milis)
		{
            return false; // FileHelper.Instance.SetLastModified(this, milis);
		}

		public bool SetReadOnly ()
		{
            return false; // FileHelper.Instance.SetReadOnly(this);
		}
		
		public Uri ToUri ()
		{
			return new Uri (_path);
		}
		
		// Don't change the case of this method, since ngit does reflection on it
		public bool CanExecute ()
		{
            return false; // FileHelper.Instance.CanExecute(this);
		}
		
		// Don't change the case of this method, since ngit does reflection on it
		public bool SetExecutable (bool exec)
		{
            return false; // FileHelper.Instance.SetExecutable(this, exec);
		}
		
		public string GetParent ()
		{
			string p = Path.GetDirectoryName (_path);
			if (string.IsNullOrEmpty(p) || p == _path)
				return null;
		    return p;
		}

		public override string ToString ()
		{
			return _path;
		}
		
		static internal string PathSeparator {
			get { return Path.PathSeparator.ToString (); }
		}

		static internal char PathSeparatorChar {
			get { return Path.PathSeparator; }
		}

		static internal char SeparatorChar {
			get { return Path.DirectorySeparatorChar; }
		}

		static internal string Separator {
			get { return Path.DirectorySeparatorChar.ToString (); }
		}
	}
}
