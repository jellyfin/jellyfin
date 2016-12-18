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
using System.Collections.ObjectModel;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp.Rtcp
{
    class SourceDescriptionBlock
    {
        /// <summary>
        /// Get the length of the block.
        /// </summary>
        public int BlockLength { get { return (blockLength + (blockLength % 4)); } }

        /// <summary>
        /// Get the synchronization source.
        /// </summary>
        public string SynchronizationSource { get; private set; }
        /// <summary>
        /// Get the list of source descriptioni items.
        /// </summary>
        public Collection<SourceDescriptionItem> Items;

        private int blockLength;

        public void Process(byte[] buffer, int offset)
        {
            SynchronizationSource = Utils.ConvertBytesToString(buffer, offset, 4);
            Items = new Collection<SourceDescriptionItem>();
            int index = 4;
            bool done = false;
            do
            {
                SourceDescriptionItem item = new SourceDescriptionItem();
                item.Process(buffer, offset + index);
                
                if (item.Type != 0)
                {
                    Items.Add(item);
                    index += item.ItemLength;                    
                    blockLength += item.ItemLength;
                }
                else
                {
                    blockLength++;
                    done = true;
                }
            }
            while (!done);            
        }
    }
}
