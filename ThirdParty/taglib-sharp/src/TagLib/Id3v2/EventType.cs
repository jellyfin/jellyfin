namespace TagLib.Id3v2
{
	/// <summary>
	///    Specifies the event type used by a <see
	///    cref="EventTimeCode" /> and <see cref="EventTimeCodesFrame"/>.
	/// </summary>
	public enum EventType
	{
		/// <summary>
		/// The padding - no meaning
		/// </summary>
		Padding = 0x00,

		/// <summary>
		/// The end of initial silence
		/// </summary>
		EndOfInitialSilence = 0x01,

		/// <summary>
		/// The intro start
		/// </summary>
		IntroStart = 0x02,

		/// <summary>
		/// The main part start
		/// </summary>
		MainPartStart = 0x03,

		/// <summary>
		/// The outro start
		/// </summary>
		OutroStart = 0x04,

		/// <summary>
		/// The outro end
		/// </summary>
		OutroEnd = 0x05,

		/// <summary>
		/// The verse start
		/// </summary>
		VerseStart = 0x06,

		/// <summary>
		/// The refrain start
		/// </summary>
		RefrainStart = 0x07,

		/// <summary>
		/// The interlude start
		/// </summary>
		InterludeStart = 0x08,

		/// <summary>
		/// The theme start
		/// </summary>
		ThemeStart = 0x09,

		/// <summary>
		/// The variation start
		/// </summary>
		VariationStart = 0x0A,

		/// <summary>
		/// The key change
		/// </summary>
		KeyChange = 0x0B,

		/// <summary>
		/// The time change
		/// </summary>
		TimeChange = 0x0C,

		/// <summary>
		/// momentary unwanted noise (Snap, Crackle & Pop)
		/// </summary>
		MomentaryUnwantedNoise = 0x0D,

		/// <summary>
		/// The sustained noise
		/// </summary>
		SustainedNoise = 0x0E,

		/// <summary>
		/// The sustained noise end
		/// </summary>
		SustainedNoiseEnd = 0x0F,

		/// <summary>
		/// The intro end
		/// </summary>
		IntroEnd = 0x10,

		/// <summary>
		/// The main part end
		/// </summary>
		MainPartEnd = 0x11,

		/// <summary>
		/// The verse end
		/// </summary>
		VerseEnd = 0x12,

		/// <summary>
		/// The refrain end
		/// </summary>
		RefrainEnd = 0x13,

		/// <summary>
		/// The theme end
		/// </summary>
		ThemeEnd = 0x14,

		/// <summary>
		/// Profanity starts
		/// </summary>
		Profanity = 0x15,

		/// <summary>
		/// The profanity end
		/// </summary>
		ProfanityEnd = 0x16,

		/// <summary>
		/// The audio end
		/// </summary>
		AudioEnd = 0xFD,

		/// <summary>
		/// The audio file end
		/// </summary>
		AudioFileEnd = 0xFE,
	}
}
