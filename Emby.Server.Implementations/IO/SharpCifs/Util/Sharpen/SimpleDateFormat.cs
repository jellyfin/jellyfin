using System;
using System.Globalization;

namespace SharpCifs.Util.Sharpen
{
    public class SimpleDateFormat : DateFormat
	{
		string _format;

		CultureInfo Culture {
			get; set;
		}

		bool Lenient {
			get; set;
		}
		
		public SimpleDateFormat (): this ("g")
		{
		}

		public SimpleDateFormat (string format): this (format, CultureInfo.CurrentCulture)
		{
		}

		public SimpleDateFormat (string format, CultureInfo c)
		{
			Culture = c;
			this._format = format.Replace ("EEE", "ddd");
			this._format = this._format.Replace ("Z", "zzz");
			SetTimeZone (TimeZoneInfo.Local);
		}

		public bool IsLenient ()
		{
			return Lenient;
		}

		public void SetLenient (bool lenient)
		{
			Lenient = lenient;
		}

		public override DateTime Parse (string value)
		{
		    if (IsLenient ())
				return DateTime.Parse (value);
		    return DateTime.ParseExact (value, _format, Culture);
		}

	    public override string Format (DateTime date)
		{
			date += GetTimeZone().BaseUtcOffset;
			return date.ToString (_format);
		}
		
		public string Format (long date)
		{
			return Extensions.MillisToDateTimeOffset (date, (int)GetTimeZone ().BaseUtcOffset.TotalMinutes).DateTime.ToString (_format);
		}
	}
}
