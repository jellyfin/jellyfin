using System;
using TagLib.Id3v2;

namespace TagLib.Id3v2
{
	public class EventTimeCode : ICloneable
	{
		#region Private Properties

		private EventType typeOfEvent;

		private int time;

		#endregion

		#region Public Properties

		public EventType TypeOfEvent
		{
			get { return typeOfEvent; }
			set { typeOfEvent = value; }
		}

		public int Time
		{
			get { return time; }
			set { time = value; }
		}

		#endregion

		#region Public Constructors

		public EventTimeCode(EventType typeOfEvent, 
			int time)
		{
			this.typeOfEvent = typeOfEvent;
			this.time = time;
		}

		#endregion

		#region Static Methods

		public static EventTimeCode CreateEmpty()
		{
			return new EventTimeCode(EventType.Padding, 0);
		}

		#endregion

		#region ICloneable

		public object Clone()
		{
			return new EventTimeCode(typeOfEvent, time);
		}

		#endregion
	}
}
