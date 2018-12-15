using System.Collections.Generic;

namespace TagLib.Id3v2
{
	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Event Time Codes (ETCO) Frames.
	/// </summary>
	/// <remarks>
	///    Event time codes Frames should contain a list of events occurring 
	///    throughout the track such as the start of the main part and the end of it.
	///    To see all available event types see <see cref="EventType"/>.
	/// </remarks>
	/// <example>
	///    <para>Reading the Event Time Codes from a tag.</para>
	///    <code lang="C#">
	/// using TagLib;
	/// using TagLib.Id3v2;
	/// 
	/// public static class LookupUtil
	/// {
	/// 	public static ByteVector GetTrackEvents(string filename)
	/// 	{
	/// 		File file = File.Create (filename, ReadStyle.None);
	/// 		Id3v2.Tag tag = file.GetTag (TagTypes.Id3v2, false) as Id3v2.Tag;
	/// 		if (tag == null)
	/// 			return new ByteVector ();
	/// 		
	/// 		EventTimeCodesFrame frame = EventTimeCodesFrame.Get (tag, false);
	/// 		if (frame == null)
	/// 			return new ByteVector ();
	///
	/// 		return frame.Data;
	/// 	}
	/// }
	///    </code>
	///    <code lang="C++">
	/// #using &lt;System.dll>
	/// #using &lt;taglib-sharp.dll>
	///
	/// using System;
	/// using TagLib;
	/// using TagLib::Id3v2;
	/// 
	/// public ref class LookupUtil abstract sealed
	/// {
	/// public:
	/// 	static ByteVector^ GetTrackEvents (String^ filename)
	/// 	{
	/// 		File^ file = File::Create (filename, ReadStyle::None);
	/// 		Id3v2::Tag^ tag = dynamic_cast&lt;Id3v2::Tag^> (file.GetTag (TagTypes::Id3v2, false));
	/// 		if (tag == null)
	/// 			return gcnew ByteVector;
	/// 		
	/// 		EventTimeCodesFrame^ frame = EventTimeCodesFrame::Get (tag, false);
	/// 		if (frame == null)
	/// 			return gcnew ByteVector;
	///
	/// 		return frame->Data;
	/// 	}
	/// }
	///    </code>
	///    <code lang="VB">
	/// Imports TagLib
	/// Imports TagLib.Id3v2
	/// 
	/// Public Shared Class LookupUtil
	/// 	Public Shared Sub GetTrackEvents (filename As String) As TagLib.ByteVector
	/// 		Dim file As File = File.Create (filename, ReadStyle.None)
	/// 		Dim tag As Id3v2.Tag = file.GetTag (TagTypes.Id3v2, False)
	/// 		If tag Is Nothing Return New ByteVector ()
	/// 		
	/// 		Dim frame As EventTimeCodesFrame = EventTimeCodesFrame.Get (tag, False)
	/// 		If frame Is Nothing Return New ByteVector ()
	///
	/// 		Return frame.Data
	/// 	End Sub
	/// End Class
	///    </code>
	///    <code lang="Boo">
	/// import TagLib
	/// import TagLib.Id3v2
	/// 
	/// public static class LookupUtil:
	/// 	static def GetTrackEvents (filename as string) as TagLib.ByteVector:
	/// 		file as File = File.Create (filename, ReadStyle.None)
	/// 		tag as Id3v2.Tag = file.GetTag (TagTypes.Id3v2, false)
	/// 		if tag == null:
	/// 			return ByteVector ()
	/// 		
	/// 		frame as EventTimeCodesFrame = EventTimeCodesFrame.Get (tag, false)
	/// 		if frame == null:
	/// 			return ByteVector ()
	///
	/// 		return frame.Data
	///    </code>
	/// </example>
	public class EventTimeCodesFrame : Frame
	{
		#region Private Properties

		private TimestampFormat timestampFormat;

		private List<EventTimeCode> events;

		#endregion

		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="EventTimeCodesFrame" /> with empty
		///    identifier data.
		/// </summary>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see cref="Get" /> for more
		///    integrated frame creation.
		/// </remarks>
		public EventTimeCodesFrame() : base (FrameType.ETCO, 4)
		{
			Flags = FrameFlags.FileAlterPreservation;
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="EventTimeCodesFrame" /> by reading its raw data
		///    in a specified ID3v2 version.
		/// </summary>
		/// <param name="timestampFormat">
		///    A <see cref="TimestampFormat" /> Specifies the time unit to use in this frame.
		/// </param>
		public EventTimeCodesFrame(TimestampFormat timestampFormat) : base(FrameType.ETCO, 4)
		{
			this.timestampFormat = timestampFormat;
			Flags = FrameFlags.FileAlterPreservation;
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="EventTimeCodesFrame" /> by reading its raw data
		///    in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public EventTimeCodesFrame(ByteVector data, 
			byte version) : base (data, version)
		{
			SetData(data, 0, version, true);
		}


		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="EventTimeCodesFrame" /> by reading its raw data
		///    in a specified ID3v2 version.
		/// </summary>
		/// <param name="frameHeader">
		///    A <see cref="FrameHeader" /> containing the header of the frame
		/// </param>
		public EventTimeCodesFrame(FrameHeader frameHeader) : base (frameHeader)
		{

		}


		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="EventTimeCodesFrame" /> by reading its raw data
		///    in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> indicating at what offset in
		///    <paramref name="data" /> the frame actually begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> containing the header of the
		///    frame found at <paramref name="offset" /> in the data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public EventTimeCodesFrame(ByteVector data,
								int offset,
								FrameHeader header,
								byte version) : base (header)
		{
			SetData(data, offset, version, false);
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets or sets the timestamp format for this frame instance.
		/// </summary>
		/// <value>
		/// A <see cref="TimestampFormat"/> that will be used in this frame instance.
		/// </value>
		public TimestampFormat TimestampFormat
		{
			get { return timestampFormat; }
			set { timestampFormat = value; }
		}

		/// <summary>
		/// Gets or sets the events this frame contains.
		/// Each <see cref="EventTimeCode"/> represents a single event at a certain point in time.
		/// </summary>
		/// <value>
		/// A <see cref="List{EventTimeCode}"/> that are stored in this frame instance.
		/// </value>
		public List<EventTimeCode> Events
		{
			get { return events; }
			set { events = value; }
		}

		#endregion

		#region Public Static Methods

		/// <summary>
		///    Gets a play count frame from a specified tag, optionally
		///    creating it if it does not exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="EventTimeCodesFrame" /> object containing the
		///    matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static EventTimeCodesFrame Get(Tag tag, bool create)
		{
			EventTimeCodesFrame etco;
			foreach (Frame frame in tag)
			{
				etco = frame as EventTimeCodesFrame;

				if (etco != null)
					return etco;
			}

			if (!create)
				return null;

			etco = new EventTimeCodesFrame();
			tag.AddFrame(etco);
			return etco;
		}

		#endregion

		#region Protected Methods

		/// <summary>
		///    Populates the values in the current instance by parsing
		///    its field data in a specified version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the
		///    extracted field data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is encoded in.
		/// </param>
		protected override void ParseFields(ByteVector data, byte version)
		{
			events = new List<EventTimeCode>();
			timestampFormat = (TimestampFormat)data.Data[0];

			var incomingEventsData = data.Mid(1);
			for (var i = 0; i < incomingEventsData.Count - 1; i++)
			{
				var eventType = (EventType)incomingEventsData.Data[i];
				i++;

				var timestampData = new ByteVector(incomingEventsData.Data[i], 
					incomingEventsData.Data[i+1],
					incomingEventsData.Data[i+2],
					incomingEventsData.Data[i+3]);

				i += 3;

				var timestamp = timestampData.ToInt();

				events.Add(new EventTimeCode(eventType, timestamp));
			}
		}

		/// <summary>
		///    Renders the values in the current instance into field
		///    data for a specified version.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is to be encoded in.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered field data.
		/// </returns>
		protected override ByteVector RenderFields(byte version)
		{
			var data = new List<byte>();
			data.Add((byte)timestampFormat);

			foreach (var @event in events)
			{
				data.Add((byte)@event.TypeOfEvent);

				var timeData = ByteVector.FromInt(@event.Time);
				data.AddRange(timeData.Data);
			}

			return new ByteVector(data.ToArray());
		}

		#endregion

		#region ICloneable

		/// <summary>
		///    Creates a deep copy of the current instance.
		/// </summary>
		/// <returns>
		///    A new <see cref="Frame" /> object identical to the
		///    current instance.
		/// </returns>
		public override Frame Clone()
		{
			var frame = new EventTimeCodesFrame(header);
			frame.timestampFormat = timestampFormat;
			frame.events = events.ConvertAll(item => (EventTimeCode)item.Clone());
			return frame;
		}

		#endregion
	}
}
