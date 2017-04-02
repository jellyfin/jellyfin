using System;
using System.Globalization;

namespace SharpCifs.Util.Sharpen
{
	public abstract class DateFormat
	{
		public const int Default = 2;
		
		public static DateFormat GetDateTimeInstance (int dateStyle, int timeStyle)
		{
			return GetDateTimeInstance (dateStyle, timeStyle, CultureInfo.CurrentCulture);
		}
		
		public static DateFormat GetDateTimeInstance (int dateStyle, int timeStyle, CultureInfo aLocale)
		{
			return new SimpleDateFormat (aLocale.DateTimeFormat.FullDateTimePattern, aLocale);
		}
		
		TimeZoneInfo _timeZone;

	    public abstract DateTime Parse (string value);
		
		public TimeZoneInfo GetTimeZone ()
		{
			return _timeZone;
		}
		
		public void SetTimeZone (TimeZoneInfo timeZone)
		{
			this._timeZone = timeZone;
		}
	
		public abstract string Format (DateTime time);
	}
}

