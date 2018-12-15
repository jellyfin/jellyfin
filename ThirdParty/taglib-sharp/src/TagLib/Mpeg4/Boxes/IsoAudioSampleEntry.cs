//
// IsoAudioSampleEntry.cs: Provides an implementation of a ISO/IEC 14496-12
// AudioSampleEntry and support for reading MPEG-4 video properties.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2006-2007 Brian Nickel
// 
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TagLib.Mpeg4 {
	/// <summary>
	///    This class extends <see cref="IsoSampleEntry" /> and implements
	///    <see cref="IAudioCodec" /> to provide an implementation of a
	///    ISO/IEC 14496-12 AudioSampleEntry and support for reading MPEG-4
	///    video properties.
	/// </summary>
	public class IsoAudioSampleEntry : IsoSampleEntry, IAudioCodec
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the channel count.
		/// </summary>
		private ushort channel_count;

		/// <summary>
		///    Contains the sample size.
		/// </summary>
		private ushort sample_size;

		/// <summary>
		///    Contains the sample rate.
		/// </summary>
		private uint   sample_rate;
		
		/// <summary>
		///    Contains the children of the box.
		/// </summary>
		private IEnumerable<Box> children;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoVisualSampleEntry" /> with a provided header and
		///    handler by reading the contents from a specified file.
		/// </summary>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    to use for the new instance.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to read the contents
		///    of the box from.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		public IsoAudioSampleEntry (BoxHeader header, TagLib.File file,
		                            IsoHandlerBox handler)
			: base (header, file, handler)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			file.Seek (base.DataPosition + 8);
			channel_count = file.ReadBlock (2).ToUShort ();
			sample_size = file.ReadBlock (2).ToUShort ();
			file.Seek (base.DataPosition + 16);
			sample_rate = file.ReadBlock (4).ToUInt ();
			children = LoadChildren (file);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the position of the data contained in the current
		///    instance, after any box specific headers.
		/// </summary>
		/// <value>
		///    A <see cref="long" /> value containing the position of
		///    the data contained in the current instance.
		/// </value>
		protected override long DataPosition {
			get {return base.DataPosition + 20;}
		}
		
		/// <summary>
		///    Gets the children of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating the
		///    children of the current instance.
		/// </value>
		public override IEnumerable<Box> Children {
			get {return children;}
		}
		
		#endregion
		
		
		
		#region IAudioCodec Properties
		
		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TimeSpan.Zero" />.
		/// </value>
		public TimeSpan Duration {
			get {return TimeSpan.Zero;}
		}
		
		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="MediaTypes.Video" />.
		/// </value>
		public MediaTypes MediaTypes {
			get {return MediaTypes.Audio;}
		}
		
		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		public string Description {
			get {
				return string.Format (
					CultureInfo.InvariantCulture,
					"MPEG-4 Audio ({0})", BoxType);
			}
		}
		
		/// <summary>
		///    Gets the bitrate of the audio represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing a bitrate of the
		///    audio represented by the current instance.
		/// </value>
		public int AudioBitrate {
			get {
				AppleElementaryStreamDescriptor esds =
					GetChildRecursively ("esds") as
						AppleElementaryStreamDescriptor;
				
				// If we don't have an stream descriptor, we
				// don't know what's what.
				if (esds == null)
					return 0;
				
				// Return from the elementary stream descriptor.
				return (int) esds.AverageBitrate;
			}
		}
		
		/// <summary>
		///    Gets the sample rate of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the sample rate of
		///    the audio represented by the current instance.
		/// </value>
		public int AudioSampleRate {
			get {return (int)(sample_rate >> 16);}
		}
		
		/// <summary>
		///    Gets the number of channels in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the number of
		///    channels in the audio represented by the current
		///    instance.
		/// </value>
		public int AudioChannels {
			get {return channel_count;}
		}
		
		/// <summary>
		///    Gets the sample size of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the sample size of
		///    the audio represented by the current instance.
		/// </value>
		public int AudioSampleSize {
			get {return sample_size;}
		}
		
		#endregion
	}
}
