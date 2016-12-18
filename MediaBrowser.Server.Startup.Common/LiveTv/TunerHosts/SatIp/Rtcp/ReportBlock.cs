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
    public class ReportBlock
    {
        /// <summary>
        /// Get the length of the block.
        /// </summary>
        public int BlockLength { get { return (24); } }
        /// <summary>
        /// Get the synchronization source.
        /// </summary>
        public string SynchronizationSource { get; private set; }
        /// <summary>
        /// Get the fraction lost.
        /// </summary>
        public int FractionLost { get; private set; }
        /// <summary>
        /// Get the cumulative packets lost.
        /// </summary>
        public int CumulativePacketsLost { get; private set; }
        /// <summary>
        /// Get the highest number received.
        /// </summary>
        public int HighestNumberReceived { get; private set; }
        /// <summary>
        /// Get the inter arrival jitter.
        /// </summary>
        public int InterArrivalJitter { get; private set; }
        /// <summary>
        /// Get the timestamp of the last report.
        /// </summary>
        public int LastReportTimeStamp { get; private set; }
        /// <summary>
        /// Get the delay since the last report.
        /// </summary>
        public int DelaySinceLastReport { get; private set; }

        /// <summary>
        /// Initialize a new instance of the ReportBlock class.
        /// </summary>
        public ReportBlock() { }

        /// <summary>
        /// Unpack the data in a packet.
        /// </summary>
        /// <param name="buffer">The buffer containing the packet.</param>
        /// <param name="offset">The offset to the first byte of the packet within the buffer.</param>
        /// <returns>An ErrorSpec instance if an error occurs; null otherwise.</returns>
        public void Process(byte[] buffer, int offset)
        {
            SynchronizationSource = Utils.ConvertBytesToString(buffer, offset, 4);
            FractionLost = buffer[offset + 4];
            CumulativePacketsLost = Utils.Convert3BytesToInt(buffer, offset + 5);
            HighestNumberReceived = Utils.Convert4BytesToInt(buffer, offset + 8);
            InterArrivalJitter = Utils.Convert4BytesToInt(buffer, offset + 12);
            LastReportTimeStamp = Utils.Convert4BytesToInt(buffer, offset + 16);
            DelaySinceLastReport = Utils.Convert4BytesToInt(buffer, offset + 20);

            
        }
    }
}
