using System;
using System.Text.RegularExpressions;

namespace SharpCifs.Util.Sharpen
{
    internal class Matcher
	{
		private int _current;
		private MatchCollection _matches;
		private Regex _regex;
		private string _str;

		internal Matcher (Regex regex, string str)
		{
			this._regex = regex;
			this._str = str;
		}

		public int End ()
		{
			if ((_matches == null) || (_current >= _matches.Count)) {
				throw new InvalidOperationException ();
			}
			return (_matches[_current].Index + _matches[_current].Length);
		}

		public bool Find ()
		{
			if (_matches == null) {
				_matches = _regex.Matches (_str);
				_current = 0;
			}
			return (_current < _matches.Count);
		}

		public bool Find (int index)
		{
			_matches = _regex.Matches (_str, index);
			_current = 0;
			return (_matches.Count > 0);
		}

		public string Group (int n)
		{
			if ((_matches == null) || (_current >= _matches.Count)) {
				throw new InvalidOperationException ();
			}
			Group grp = _matches[_current].Groups[n];
			return grp.Success ? grp.Value : null;
		}

		public bool Matches ()
		{
			_matches = null;
			return Find ();
		}

		public string ReplaceFirst (string txt)
		{
			return _regex.Replace (_str, txt, 1);
		}

		public Matcher Reset (CharSequence str)
		{
			return Reset (str.ToString ());
		}

		public Matcher Reset (string str)
		{
			_matches = null;
			this._str = str;
			return this;
		}

		public int Start ()
		{
			if ((_matches == null) || (_current >= _matches.Count)) {
				throw new InvalidOperationException ();
			}
			return _matches[_current].Index;
		}
	}
}
