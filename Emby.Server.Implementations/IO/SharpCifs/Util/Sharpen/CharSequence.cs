using System.Text;

namespace SharpCifs.Util.Sharpen
{
	public class CharSequence
	{
		public static implicit operator CharSequence (string str)
		{
			return new StringCharSequence (str);
		}
		
		public static implicit operator CharSequence (StringBuilder str)
		{
			return new StringCharSequence (str.ToString ());
		}
	}
	
	class StringCharSequence: CharSequence
	{
		string _str;
		
		public StringCharSequence (string str)
		{
			this._str = str;
		}
		
		public override string ToString ()
		{
			return _str;
		}
	}
}
