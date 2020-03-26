#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Emby.Dlna.Common;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Server
{
    public class DescriptionXmlBuilder
    {
        private readonly DeviceProfile _profile;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly string _serverUdn;
        private readonly string _serverAddress;
        private readonly string _serverName;
        private readonly string _serverId;

        public DescriptionXmlBuilder(DeviceProfile profile, string serverUdn, string serverAddress, string serverName, string serverId)
        {
            if (string.IsNullOrEmpty(serverUdn))
            {
                throw new ArgumentNullException(nameof(serverUdn));
            }

            if (string.IsNullOrEmpty(serverAddress))
            {
                throw new ArgumentNullException(nameof(serverAddress));
            }

            _profile = profile;
            _serverUdn = serverUdn;
            _serverAddress = serverAddress;
            _serverName = serverName;
            _serverId = serverId;
        }

        private static bool EnableAbsoluteUrls => false;

        public string GetXml()
        {
            var builder = new StringBuilder();

            builder.Append("<?xml version=\"1.0\"?>");

            builder.Append("<root");

            var attributes = _profile.XmlRootAttributes.ToList();

            attributes.Insert(0, new XmlAttribute
            {
                Name = "xmlns:dlna",
                Value = "urn:schemas-dlna-org:device-1-0"
            });
            attributes.Insert(0, new XmlAttribute
            {
                Name = "xmlns",
                Value = "urn:schemas-upnp-org:device-1-0"
            });

            foreach (var att in attributes)
            {
                builder.AppendFormat(" {0}=\"{1}\"", att.Name, att.Value);
            }

            builder.Append(">");

            builder.Append("<specVersion>");
            builder.Append("<major>1</major>");
            builder.Append("<minor>0</minor>");
            builder.Append("</specVersion>");

            if (!EnableAbsoluteUrls)
            {
                builder.Append("<URLBase>" + Escape(_serverAddress) + "</URLBase>");
            }

            AppendDeviceInfo(builder);

            builder.Append("</root>");

            return builder.ToString();
        }

        private void AppendDeviceInfo(StringBuilder builder)
        {
            builder.Append("<device>");
            AppendDeviceProperties(builder);

            AppendIconList(builder);

            builder.Append("<presentationURL>" + Escape(_serverAddress) + "/web/index.html</presentationURL>");

            AppendServiceList(builder);
            builder.Append("</device>");
        }

        private static readonly char[] s_escapeChars = new char[]
        {
            '<',
            '>',
            '"',
            '\'',
            '&'
        };

        private static readonly string[] s_escapeStringPairs = new[]
        {
            "<",
            "&lt;",
            ">",
            "&gt;",
            "\"",
            "&quot;",
            "'",
            "&apos;",
            "&",
            "&amp;"
        };

        private static string GetEscapeSequence(char c)
        {
            int num = s_escapeStringPairs.Length;
            for (int i = 0; i < num; i += 2)
            {
                string text = s_escapeStringPairs[i];
                string result = s_escapeStringPairs[i + 1];
                if (text[0] == c)
                {
                    return result;
                }
            }
            return c.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Replaces invalid XML characters in a string with their valid XML equivalent.</summary>
        /// <returns>The input string with invalid characters replaced.</returns>
        /// <param name="str">The string within which to escape invalid characters. </param>
        public static string Escape(string str)
        {
            if (str == null)
            {
                return null;
            }

            StringBuilder stringBuilder = null;
            int length = str.Length;
            int num = 0;
            while (true)
            {
                int num2 = str.IndexOfAny(s_escapeChars, num);
                if (num2 == -1)
                {
                    break;
                }
                if (stringBuilder == null)
                {
                    stringBuilder = new StringBuilder();
                }
                stringBuilder.Append(str, num, num2 - num);
                stringBuilder.Append(GetEscapeSequence(str[num2]));
                num = num2 + 1;
            }
            if (stringBuilder == null)
            {
                return str;
            }
            stringBuilder.Append(str, num, length - num);
            return stringBuilder.ToString();
        }

        private void AppendDeviceProperties(StringBuilder builder)
        {
            builder.Append("<dlna:X_DLNACAP/>");

            builder.Append("<dlna:X_DLNADOC xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">DMS-1.50</dlna:X_DLNADOC>");
            builder.Append("<dlna:X_DLNADOC xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">M-DMS-1.50</dlna:X_DLNADOC>");

            builder.Append("<deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>");

            builder.Append("<friendlyName>" + Escape(GetFriendlyName()) + "</friendlyName>");
            builder.Append("<manufacturer>" + Escape(_profile.Manufacturer ?? string.Empty) + "</manufacturer>");
            builder.Append("<manufacturerURL>" + Escape(_profile.ManufacturerUrl ?? string.Empty) + "</manufacturerURL>");

            builder.Append("<modelDescription>" + Escape(_profile.ModelDescription ?? string.Empty) + "</modelDescription>");
            builder.Append("<modelName>" + Escape(_profile.ModelName ?? string.Empty) + "</modelName>");

            builder.Append("<modelNumber>" + Escape(_profile.ModelNumber ?? string.Empty) + "</modelNumber>");
            builder.Append("<modelURL>" + Escape(_profile.ModelUrl ?? string.Empty) + "</modelURL>");

            if (string.IsNullOrEmpty(_profile.SerialNumber))
            {
                builder.Append("<serialNumber>" + Escape(_serverId) + "</serialNumber>");
            }
            else
            {
                builder.Append("<serialNumber>" + Escape(_profile.SerialNumber) + "</serialNumber>");
            }

            builder.Append("<UPC/>");

            builder.Append("<UDN>uuid:" + Escape(_serverUdn) + "</UDN>");

            if (!string.IsNullOrEmpty(_profile.SonyAggregationFlags))
            {
                builder.Append("<av:aggregationFlags xmlns:av=\"urn:schemas-sony-com:av\">" + Escape(_profile.SonyAggregationFlags) + "</av:aggregationFlags>");
            }
        }

        private string GetFriendlyName()
        {
            if (string.IsNullOrEmpty(_profile.FriendlyName))
            {
                return "Jellyfin - " + _serverName;
            }

            var characterList = new List<char>();

            foreach (var c in _serverName)
            {
                if (char.IsLetterOrDigit(c) || c == '-')
                {
                    characterList.Add(c);
                }
            }

            var characters = characterList.ToArray();

            var serverName = new string(characters);

            var name = _profile.FriendlyName?.Replace("${HostName}", serverName, StringComparison.OrdinalIgnoreCase);

            return name ?? string.Empty;
        }

        private void AppendIconList(StringBuilder builder)
        {
            builder.Append("<iconList>");

            foreach (var icon in GetIcons())
            {
                builder.Append("<icon>");

                builder.Append("<mimetype>" + Escape(icon.MimeType ?? string.Empty) + "</mimetype>");
                builder.Append("<width>" + Escape(icon.Width.ToString(_usCulture)) + "</width>");
                builder.Append("<height>" + Escape(icon.Height.ToString(_usCulture)) + "</height>");
                builder.Append("<depth>" + Escape(icon.Depth ?? string.Empty) + "</depth>");
                builder.Append("<url>" + BuildUrl(icon.Url) + "</url>");

                builder.Append("</icon>");
            }

            builder.Append("</iconList>");
        }

        private void AppendServiceList(StringBuilder builder)
        {
            builder.Append("<serviceList>");

            foreach (var service in GetServices())
            {
                builder.Append("<service>");

                builder.Append("<serviceType>" + Escape(service.ServiceType ?? string.Empty) + "</serviceType>");
                builder.Append("<serviceId>" + Escape(service.ServiceId ?? string.Empty) + "</serviceId>");
                builder.Append("<SCPDURL>" + BuildUrl(service.ScpdUrl) + "</SCPDURL>");
                builder.Append("<controlURL>" + BuildUrl(service.ControlUrl) + "</controlURL>");
                builder.Append("<eventSubURL>" + BuildUrl(service.EventSubUrl) + "</eventSubURL>");

                builder.Append("</service>");
            }

            builder.Append("</serviceList>");
        }

        private string BuildUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            url = url.TrimStart('/');

            url = "/dlna/" + _serverUdn + "/" + url;

            if (EnableAbsoluteUrls)
            {
                url = _serverAddress.TrimEnd('/') + url;
            }

            return Escape(url);
        }

        private IEnumerable<DeviceIcon> GetIcons()
            => new[]
            {
                new DeviceIcon
                {
                    MimeType = "image/png",
                    Depth = "24",
                    Width = 240,
                    Height = 240,
                    Url = "icons/logo240.png"
                },

                new DeviceIcon
                {
                    MimeType = "image/jpeg",
                    Depth = "24",
                    Width = 240,
                    Height = 240,
                    Url = "icons/logo240.jpg"
                },

                new DeviceIcon
                {
                    MimeType = "image/png",
                    Depth = "24",
                    Width = 120,
                    Height = 120,
                    Url = "icons/logo120.png"
                },

                new DeviceIcon
                {
                    MimeType = "image/jpeg",
                    Depth = "24",
                    Width = 120,
                    Height = 120,
                    Url = "icons/logo120.jpg"
                },

                new DeviceIcon
                {
                    MimeType = "image/png",
                    Depth = "24",
                    Width = 48,
                    Height = 48,
                    Url = "icons/logo48.png"
                },

                new DeviceIcon
                {
                    MimeType = "image/jpeg",
                    Depth = "24",
                    Width = 48,
                    Height = 48,
                    Url = "icons/logo48.jpg"
                }
            };

        private IEnumerable<DeviceService> GetServices()
        {
            var list = new List<DeviceService>();

            list.Add(new DeviceService
            {
                ServiceType = "urn:schemas-upnp-org:service:ContentDirectory:1",
                ServiceId = "urn:upnp-org:serviceId:ContentDirectory",
                ScpdUrl = "contentdirectory/contentdirectory.xml",
                ControlUrl = "contentdirectory/control",
                EventSubUrl = "contentdirectory/events"
            });

            list.Add(new DeviceService
            {
                ServiceType = "urn:schemas-upnp-org:service:ConnectionManager:1",
                ServiceId = "urn:upnp-org:serviceId:ConnectionManager",
                ScpdUrl = "connectionmanager/connectionmanager.xml",
                ControlUrl = "connectionmanager/control",
                EventSubUrl = "connectionmanager/events"
            });

            if (_profile.EnableMSMediaReceiverRegistrar)
            {
                list.Add(new DeviceService
                {
                    ServiceType = "urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1",
                    ServiceId = "urn:microsoft.com:serviceId:X_MS_MediaReceiverRegistrar",
                    ScpdUrl = "mediareceiverregistrar/mediareceiverregistrar.xml",
                    ControlUrl = "mediareceiverregistrar/control",
                    EventSubUrl = "mediareceiverregistrar/events"
                });
            }

            return list;
        }

        public override string ToString()
        {
            return GetXml();
        }
    }
}
