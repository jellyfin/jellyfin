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
    class RtcpAppPacket : RtcpPacket
    {
        /// <summary>
        /// Get the synchronization source.
        /// </summary>
        public int SynchronizationSource { get; private set; }
        /// <summary>
        /// Get the name.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Get the identity.
        /// </summary>
        public int Identity { get; private set; }
        /// <summary>
        /// Get the variable data portion.
        /// </summary>
        public string Data { get; private set; }

        public override void Parse(byte[] buffer, int offset)
        {
            base.Parse(buffer, offset);
            SynchronizationSource = Utils.Convert4BytesToInt(buffer, offset + 4);
            Name = Utils.ConvertBytesToString(buffer, offset + 8, 4);
            Identity = Utils.Convert2BytesToInt(buffer, offset + 12);

            int dataLength = Utils.Convert2BytesToInt(buffer, offset + 14);
            if (dataLength != 0)
                Data = Utils.ConvertBytesToString(buffer, offset + 16, dataLength);
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Application Specific.\n");
            sb.AppendFormat("Version : {0} .\n", Version);
            sb.AppendFormat("Padding : {0} .\n", Padding);
            sb.AppendFormat("Report Count : {0} .\n", ReportCount);
            sb.AppendFormat("PacketType: {0} .\n", Type);
            sb.AppendFormat("Length : {0} .\n", Length);
            sb.AppendFormat("SynchronizationSource : {0} .\n", SynchronizationSource);
            sb.AppendFormat("Name : {0} .\n", Name);
            sb.AppendFormat("Identity : {0} .\n", Identity);
            sb.AppendFormat("Data : {0} .\n", Data);            
            sb.AppendFormat(".\n");
            return sb.ToString();
        }
    }
}
