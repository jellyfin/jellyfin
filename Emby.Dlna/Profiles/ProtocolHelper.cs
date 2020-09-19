#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    public static class ProtocolHelper
    {
        private static (string DlnaOrg, string Video, string Audio)[] _protocolInformationStrings = new[]
        {
            ("DVRMS_MPEG2", "MPV", "MPA"),
            ("DVRMS_MPEG2", string.Empty, "AC3"),
            ("VC1_APL2_FULL", "VC1", "WMA_STD"),
            ("VC1_APL2_PRO", "VC1", "WMA_PRO"),
            ("VC1_APL3_FULL", "VC1", "WMA_STD"),
            ("VC1_APL3_PRO", "VC1", "WMA_PRO"),
            ("MPEG4_P2_MP4_ASP_L5_MPEG1_L3", "MPEG4P2", "MP3"),
            ("MPEG4_P2_AVI_ASP_L5_MPEG1_L3", "MPEG4P2", "MP3"),
            ("MPEG4_P2_MP4_ASP_L5_AC3", "MPEG4P2", "AC3"),
            ("MPEG4_P2_AVI_ASP_L5_AC3", "MPEG4P2", "AC3"),
            ("AVC_AVI_MP_HD_L4_1_MPEG1_L3", "MPEG4P10", "MP3"),
            ("AVC_MP4_MP_HD_MPEG1_L3", "MPEG4P10", "MP3"),
            ("AVC_MP4_MP_HD_AC3", "MPEG4P10", "AC3"),
            ("AVC_AVI_MP_HD_L4_1_AC3", "MPEG4P10", "AC3"),
            ("WMABASE", string.Empty, ".WMA"),
            ("WMAFULL", string.Empty, "WMA"),
            ("WMAPRO", string.Empty, "WMA"),
            ("MP3", string.Empty, "MP3"),
            ("AC3", string.Empty, "AC3"),
            ("LPCM", string.Empty, "PCM"),
            ("MPEG_ES_PAL", "MPV", string.Empty),
            ("MPEG_ES_NTSC", "MPV", string.Empty),
            ("MPEG_ES_PAL_XAC3", "MPV", "AC3"),
            ("MPEG_ES_NTSC_XAC3", "MPV", "AC3"),
            ("WMVMED_BASE", "WMV", "WMA"),
            ("WMVMED_FULL", "WMV", "WMA"),
            ("WMVMED_PRO", "WMV", "WMA"),
            ("WMVHIGH_FULL", "WMV", "WMA"),
            ("WMVHIGH_PRO", "WMV", "WMA"),
            ("WMVSPLL_BASE", "WMV", "WMA"),
            ("WMVSPML_BASE", "WMV", "WMA"),
            ("WMVSPML_MP3", "WMV", "MP3"),
            ("MPEG1", "MPV", "MPA"),
            ("MPEG_PS_NTSC", "MPV", "AC3"),
            ("MPEG_PS_NTSC", string.Empty, "PCM"),
            ("MPEG_PS_NTSC", string.Empty, "MPA"),
            ("MPEG_PS_PAL", "MPV", "AC3"),
            ("MPEG_PS_PAL", "MPV", "PCM"),
            ("MPEG_PS_PAL", "MPV", "MPA"),
            ("MPEG4_P2_TS_ASP_MPEG1_L3", "MPEG4P2", "MP3"),
            ("MPEG4_P2_TS_ASP_AC3", "MPEG4P2", "AC3"),
            ("AVC_MP4_MP_SD_MPEG1_L3", "MPEG4P10", "MP3"),
            ("AVC_TS_MP_HD_MPEG1_L3", "MPEG4P10", "MP3"),
            ("AVC_MP4_MP_HD_AC3", "MPEG4P10", "AC3"),
            ("AVC_MP4_MP_SD_AC3", "MPEG4P10", "AC3"),
            ("AVC_TS_MP_HD_AC3", "MPEG4P10", "AC3")
        };

        private static string GetFileTypes(string ext)
        {
            ext = string.Empty;
            return ext;
        }

        private static (string VideoCodec, string AudioCodec, string Container) GetContainer(string ext)
        {
            foreach ((string Name, string Video, string Audio) entry in _protocolInformationStrings)
            {
                if (string.Equals(ext, entry.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return (entry.Video, entry.Audio, GetFileTypes(entry.Video));
                }
            }

            throw new InvalidOperationException();
        }

        private static bool ExtractOrganisationName(string org_pn, string dlnaOrgStr, out string result)
        {
            int index = org_pn.IndexOf(dlnaOrgStr, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                int endIndex = org_pn.IndexOf(',', index + 1);
                if (endIndex != -1)
                {
                    result = org_pn.Substring(index + dlnaOrgStr.Length, endIndex);
                }
                else
                {
                    result = org_pn.Substring(index + dlnaOrgStr.Length);
                }

                return true;
            }

            result = string.Empty;
            return false;
        }

        public static DeviceProfile BuildProfile(PlayToDeviceInfo deviceInfo)
        {
            DeviceProfile profile = new DefaultProfile
            {
                Name = deviceInfo.Name,
                Identification = new DeviceIdentification
                {
                    FriendlyName = deviceInfo.Name,
                    Manufacturer = deviceInfo.Manufacturer,
                    ModelName = deviceInfo.ModelName
                },
                Manufacturer = deviceInfo.Manufacturer,
                FriendlyName = deviceInfo.Name,
                ModelNumber = deviceInfo.ModelNumber,
                ModelName = deviceInfo.ModelName,
                ModelUrl = deviceInfo.ModelUrl,
                ModelDescription = deviceInfo.ModelDescription,
                ManufacturerUrl = deviceInfo.ManufacturerUrl,
                SerialNumber = deviceInfo.SerialNumber
            };

            profile.ProtocolInfo = deviceInfo.Capabilities;
            var supportedMediaTypes = new HashSet<string>();
            var profiles = new List<DirectPlayProfile>();
            string[] mediaInfo;

            // Clear the direct play profiles.
            profile.DirectPlayProfiles = Array.Empty<DirectPlayProfile>();

            foreach (var capability in deviceInfo
                .Capabilities
                .Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var protocolInfo = capability.Split(':');
                if (protocolInfo.Length == 4)
                {
                    // Split video/mpg.
                    mediaInfo = protocolInfo[2].Split('/');

                    string org_pn = protocolInfo[3];
                    if (!ExtractOrganisationName(org_pn, "DLNA.ORG_PN =", out string protocolName) &&
                        !ExtractOrganisationName(org_pn, "MICROSOFT.COM_PN =", out protocolName))
                    {
                        protocolName = mediaInfo[1];
                    }

                    supportedMediaTypes.Add(mediaInfo[0]);

                    try
                    {
                        (string video, string audio, string container) = GetContainer(string.IsNullOrEmpty(protocolName) ? mediaInfo[1] : protocolName);

                        if (!string.IsNullOrEmpty(container))
                        {
                            profiles.Add(new DirectPlayProfile()
                            {
                                Type = (DlnaProfileType)Enum.Parse(typeof(DlnaProfileType), mediaInfo[0], true),
                                VideoCodec = video,
                                AudioCodec = audio,
                                Container = container
                            });
                        }
                    }
                    catch
                    {
                        // ignore errors.
                    }
                }
            }

            profile.SupportedMediaTypes = supportedMediaTypes.Aggregate((i, j) => i + ',' + j);
            profile.DirectPlayProfiles = profiles.ToArray();

            return profile;
        }
    }
}
