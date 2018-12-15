//
// GPSEntryTag.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009-2010 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
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

namespace TagLib.IFD.Tags
{
	/// <summary>
	///    Entry tags occuring in the GPS IFD
	///    The complete overview can be obtained at:
	///    http://www.awaresystems.be/imaging/tiff.html
	/// </summary>
	public enum GPSEntryTag : ushort
	{

		/// <summary>
		///     Indicates the version of GPSInfoIFD. (Hex: 0x0000)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsversionid.html
		/// </summary>
		GPSVersionID                                       = 0,

		/// <summary>
		///     Indicates whether the latitude is north or south latitude. (Hex: 0x0001)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpslatituderef.html
		/// </summary>
		GPSLatitudeRef                                     = 1,

		/// <summary>
		///     Indicates the latitude. (Hex: 0x0002)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpslatitude.html
		/// </summary>
		GPSLatitude                                        = 2,

		/// <summary>
		///     Indicates whether the longitude is east or west longitude. (Hex: 0x0003)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpslongituderef.html
		/// </summary>
		GPSLongitudeRef                                    = 3,

		/// <summary>
		///     Indicates the longitude. (Hex: 0x0004)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpslongitude.html
		/// </summary>
		GPSLongitude                                       = 4,

		/// <summary>
		///     Indicates the altitude used as the reference altitude. (Hex: 0x0005)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsaltituderef.html
		/// </summary>
		GPSAltitudeRef                                     = 5,

		/// <summary>
		///     Indicates the altitude based on the reference in GPSAltitudeRef. (Hex: 0x0006)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsaltitude.html
		/// </summary>
		GPSAltitude                                        = 6,

		/// <summary>
		///     Indicates the time as UTC (Coordinated Universal Time). (Hex: 0x0007)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpstimestamp.html
		/// </summary>
		GPSTimeStamp                                       = 7,

		/// <summary>
		///     Indicates the GPS satellites used for measurements. (Hex: 0x0008)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpssatellites.html
		/// </summary>
		GPSSatellites                                      = 8,

		/// <summary>
		///     Indicates the status of the GPS receiver when the image is recorded. (Hex: 0x0009)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsstatus.html
		/// </summary>
		GPSStatus                                          = 9,

		/// <summary>
		///     Indicates the GPS measurement mode. (Hex: 0x000A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsmeasuremode.html
		/// </summary>
		GPSMeasureMode                                     = 10,

		/// <summary>
		///     Indicates the GPS DOP (data degree of precision). (Hex: 0x000B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdop.html
		/// </summary>
		GPSDOP                                             = 11,

		/// <summary>
		///     Indicates the unit used to express the GPS receiver speed of movement. (Hex: 0x000C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsspeedref.html
		/// </summary>
		GPSSpeedRef                                        = 12,

		/// <summary>
		///     Indicates the speed of GPS receiver movement. (Hex: 0x000D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsspeed.html
		/// </summary>
		GPSSpeed                                           = 13,

		/// <summary>
		///     Indicates the reference for giving the direction of GPS receiver movement. (Hex: 0x000E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpstrackref.html
		/// </summary>
		GPSTrackRef                                        = 14,

		/// <summary>
		///     Indicates the direction of GPS receiver movement. (Hex: 0x000F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpstrack.html
		/// </summary>
		GPSTrack                                           = 15,

		/// <summary>
		///     Indicates the reference for giving the direction of the image when it is captured. (Hex: 0x0010)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsimgdirectionref.html
		/// </summary>
		GPSImgDirectionRef                                 = 16,

		/// <summary>
		///     Indicates the direction of the image when it was captured. (Hex: 0x0011)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsimgdirection.html
		/// </summary>
		GPSImgDirection                                    = 17,

		/// <summary>
		///     Indicates the geodetic survey data used by the GPS receiver. (Hex: 0x0012)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsmapdatum.html
		/// </summary>
		GPSMapDatum                                        = 18,

		/// <summary>
		///     Indicates whether the latitude of the destination point is north or south latitude. (Hex: 0x0013)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdestlatituderef.html
		/// </summary>
		GPSDestLatitudeRef                                 = 19,

		/// <summary>
		///     Indicates the latitude of the destination point. (Hex: 0x0014)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdestlatitude.html
		/// </summary>
		GPSDestLatitude                                    = 20,

		/// <summary>
		///     Indicates whether the longitude of the destination point is east or west longitude. (Hex: 0x0015)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdestlongituderef.html
		/// </summary>
		GPSDestLongitudeRef                                = 21,

		/// <summary>
		///     Indicates the longitude of the destination point. (Hex: 0x0016)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdestlongitude.html
		/// </summary>
		GPSDestLongitude                                   = 22,

		/// <summary>
		///     Indicates the reference used for giving the bearing to the destination point. (Hex: 0x0017)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdestbearingref.html
		/// </summary>
		GPSDestBearingRef                                  = 23,

		/// <summary>
		///     Indicates the bearing to the destination point. (Hex: 0x0018)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdestbearing.html
		/// </summary>
		GPSDestBearing                                     = 24,

		/// <summary>
		///     Indicates the unit used to express the distance to the destination point. (Hex: 0x0019)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdestdistanceref.html
		/// </summary>
		GPSDestDistanceRef                                 = 25,

		/// <summary>
		///     Indicates the distance to the destination point. (Hex: 0x001A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdestdistance.html
		/// </summary>
		GPSDestDistance                                    = 26,

		/// <summary>
		///     A character string recording the name of the method used for location finding. (Hex: 0x001B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsprocessingmethod.html
		/// </summary>
		GPSProcessingMethod                                = 27,

		/// <summary>
		///     A character string recording the name of the GPS area. (Hex: 0x001C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsareainformation.html
		/// </summary>
		GPSAreaInformation                                 = 28,

		/// <summary>
		///     A character string recording date and time information relative to UTC (Coordinated Universal Time). (Hex: 0x001D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdatestamp.html
		/// </summary>
		GPSDateStamp                                       = 29,

		/// <summary>
		///     Indicates whether differential correction is applied to the GPS receiver. (Hex: 0x001E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/gps/gpsdifferential.html
		/// </summary>
		GPSDifferential                                    = 30,
	}
}
