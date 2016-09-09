using System.Collections.ObjectModel;
/*  
    Copyright (C) <2007-2016>  <Kay Diefenthal>

    SatIp.RtspSample is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SatIp.RtspSample is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SatIp.RtspSample.  If not, see <http://www.gnu.org/licenses/>.
*/
using System.Text;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp.Rtcp
{
    public class RtcpByePacket :RtcpPacket
    {
        public Collection<string> SynchronizationSources { get; private set; }
        public string ReasonForLeaving { get; private set; }
        public override void Parse(byte[] buffer, int offset)
        {
            base.Parse(buffer, offset);
            SynchronizationSources = new Collection<string>();
            int index = 4;

            while (SynchronizationSources.Count < ReportCount)
            {
                SynchronizationSources.Add(Utils.ConvertBytesToString(buffer, offset + index, 4));
                index += 4;
            }

            if (index < Length)
            {
                int reasonLength = buffer[offset + index];
                ReasonForLeaving = Utils.ConvertBytesToString(buffer, offset + index + 1, reasonLength);
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ByeBye .\n");
            sb.AppendFormat("Version : {0} .\n", Version);
            sb.AppendFormat("Padding : {0} .\n", Padding);
            sb.AppendFormat("Report Count : {0} .\n", ReportCount);
            sb.AppendFormat("PacketType: {0} .\n", Type);
            sb.AppendFormat("Length : {0} .\n", Length);
            sb.AppendFormat("SynchronizationSources : {0} .\n", SynchronizationSources);
            sb.AppendFormat("ReasonForLeaving : {0} .\n", ReasonForLeaving);            
            sb.AppendFormat(".\n");
            return sb.ToString();
        }
    }
}
