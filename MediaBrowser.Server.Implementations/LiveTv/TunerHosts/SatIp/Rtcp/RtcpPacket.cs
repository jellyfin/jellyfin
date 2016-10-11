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

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp.Rtcp
{
    public abstract class RtcpPacket
    {
        public int Version { get; private set; }
        public bool Padding { get; private set; }
        public int ReportCount { get; private set; }
        public int Type { get; private set; }
        public int Length { get; private set; }        

        public virtual void Parse(byte[] buffer, int offset)
        {
            Version = buffer[offset] >> 6;
            Padding = (buffer[offset] & 0x20) != 0;
            ReportCount = buffer[offset] & 0x1f;
            Type = buffer[offset + 1];
            Length = (Utils.Convert2BytesToInt(buffer, offset + 2) * 4) + 4;             
        }
    }
}
