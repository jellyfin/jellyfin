using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp.Rtsp;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp
{
    public class ChannelScan
    {
        private readonly ILogger _logger;

        public ChannelScan(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<ChannelInfo>> Scan(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var ini = info.SourceA.Split('|')[1];
            var resource = GetType().Assembly.GetManifestResourceNames().FirstOrDefault(i => i.EndsWith(ini, StringComparison.OrdinalIgnoreCase));

            _logger.Info("Opening ini file {0}", resource);
            var list = new List<ChannelInfo>();

            using (var stream = GetType().Assembly.GetManifestResourceStream(resource))
            {
                using (var reader = new StreamReader(stream))
                {
                    var parser = new StreamIniDataParser();
                    var data = parser.ReadData(reader);

                    var count = GetInt(data, "DVB", "0", 0);

                    _logger.Info("DVB Count: {0}", count);

                    var index = 1;
                    var source = "1";

                    while (index <= count)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        using (var rtspSession = new RtspSession(info.Url, _logger))
                        {
                            float percent = count == 0 ? 0 : (float)(index) / count;
                            percent = Math.Max(percent * 100, 100);

                            //SetControlPropertyThreadSafe(pgbSearchResult, "Value", (int)percent);
                            var strArray = data["DVB"][index.ToString(CultureInfo.InvariantCulture)].Split(',');

                            string tuning;
                            if (strArray[4] == "S2")
                            {
                                tuning = string.Format("src={0}&freq={1}&pol={2}&sr={3}&fec={4}&msys=dvbs2&mtype={5}&plts=on&ro=0.35&pids=0,16,17,18,20", source, strArray[0], strArray[1].ToLower(), strArray[2].ToLower(), strArray[3], strArray[5].ToLower());
                            }
                            else
                            {
                                tuning = string.Format("src={0}&freq={1}&pol={2}&sr={3}&fec={4}&msys=dvbs&mtype={5}&pids=0,16,17,18,20", source, strArray[0], strArray[1].ToLower(), strArray[2], strArray[3], strArray[5].ToLower());
                            }

                            rtspSession.Setup(tuning, "unicast");

                            rtspSession.Play(string.Empty);

                            int signallevel;
                            int signalQuality;
                            rtspSession.Describe(out signallevel, out signalQuality);

                            await Task.Delay(500).ConfigureAwait(false);
                            index++;
                        }
                    }
                }
            }

            return list;
        }

        private int GetInt(IniData data, string s1, string s2, int defaultValue)
        {
            var value = data[s1][s2];
            int numericValue;
            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out numericValue))
            {
                return numericValue;
            }

            return defaultValue;
        }
    }

    public class SatChannel
    {
        // TODO: Add properties
    }
}
