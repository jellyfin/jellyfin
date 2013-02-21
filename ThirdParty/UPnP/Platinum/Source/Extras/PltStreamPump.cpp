/*****************************************************************
|
|   Platinum - Stream Pump
|
| Copyright (c) 2004-2010, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| licensing@plutinosoft.com
|  
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltStreamPump.h"
#include "NptUtils.h"

/*----------------------------------------------------------------------
|   PLT_StreamPump::PLT_StreamPump
+---------------------------------------------------------------------*/
PLT_StreamPump::PLT_StreamPump(NPT_Size size) :
    m_TotalBytesRead(0),
    m_TotalBytesWritten(0)
{
    m_RingBuffer = new NPT_RingBuffer(size);
}

/*----------------------------------------------------------------------
|   PLT_StreamPump::~PLT_StreamPump
+---------------------------------------------------------------------*/
PLT_StreamPump::~PLT_StreamPump()
{
    delete m_RingBuffer;
}
/*----------------------------------------------------------------------+
|    PLT_StreamPump::PushData
+----------------------------------------------------------------------*/
NPT_Result
PLT_StreamPump::PushData(NPT_OutputStream& output, 
                         NPT_Size&         bytes_written)
{
    NPT_Result res = NPT_ERROR_WOULD_BLOCK;
    NPT_Size   count = 0;
    NPT_Size   bytes_available = m_RingBuffer->GetContiguousAvailable();

    bytes_written = 0;

    if (bytes_available) {
        res = output.Write(m_RingBuffer->GetReadPointer(), bytes_available, &count);
        m_RingBuffer->MoveOut(count);
        bytes_written += count;

        // check if we wrapped around
        bytes_available = m_RingBuffer->GetContiguousAvailable();
        if (NPT_SUCCEEDED(res) && bytes_available) {
            res = output.Write(m_RingBuffer->GetReadPointer(), bytes_available, &count);
            m_RingBuffer->MoveOut(count);
            bytes_written += count;
        }
    }

    m_TotalBytesWritten += bytes_written;

    return res;
}

/*----------------------------------------------------------------------+
|    PLT_StreamPump::PullData
+----------------------------------------------------------------------*/
NPT_Result
PLT_StreamPump::PullData(NPT_InputStream& input, 
                         NPT_Size         max_bytes_to_read)
{
    NPT_Result res = NPT_ERROR_WOULD_BLOCK;
    NPT_Size   byte_space = m_RingBuffer->GetContiguousSpace();

    // check that there is space left
    // make sure we don't read more than our contiguous space
    NPT_Size nb_to_read = (max_bytes_to_read<byte_space)?max_bytes_to_read:byte_space;
    if (nb_to_read > 0) {
        NPT_Size count;
        res = input.Read(m_RingBuffer->GetWritePointer(), nb_to_read, &count);
        m_RingBuffer->MoveIn(count);
        max_bytes_to_read -= count;
        m_TotalBytesRead += count;

        byte_space = m_RingBuffer->GetContiguousSpace();
        nb_to_read = (max_bytes_to_read<byte_space)?max_bytes_to_read:byte_space;
        // if we filled our contiguous space, and we wrapped, check if there is more to read 
        if (NPT_SUCCEEDED(res) && (nb_to_read > 0)) {
            res = input.Read(m_RingBuffer->GetWritePointer(), nb_to_read, &count);
            m_RingBuffer->MoveIn(count);
            m_TotalBytesRead += count;
        }
    }

    return res;
}

/*----------------------------------------------------------------------
|   PLT_PipeInputStreamPump::PLT_PipeInputStreamPump
+---------------------------------------------------------------------*/
PLT_PipeInputStreamPump::PLT_PipeInputStreamPump(NPT_OutputStreamReference& output,
                                                 NPT_Size                   size) : 
    PLT_StreamPump(size),
    m_Output(output),
    m_LastRes(NPT_SUCCESS)
{
}

/*----------------------------------------------------------------------
|   PLT_PipeInputStreamPump::~PLT_PipeInputStreamPump
+---------------------------------------------------------------------*/
PLT_PipeInputStreamPump::~PLT_PipeInputStreamPump()
{
}

/*----------------------------------------------------------------------
|   PLT_PipeInputStreamPump::Receive
+---------------------------------------------------------------------*/
NPT_Result 
PLT_PipeInputStreamPump::Receive(NPT_InputStream& input, 
                                 NPT_Size         max_bytes_to_read, 
                                 NPT_Size*        bytes_read)
{
    NPT_Size count;
    NPT_Result res;

    if ((m_LastRes == NPT_SUCCESS) || (m_LastRes == NPT_ERROR_WOULD_BLOCK)) {
        // look at what we have buffered already from out input
        // and if have less than what was asked, read more
        NPT_Size available = m_RingBuffer->GetAvailable();
        if (available < max_bytes_to_read) {
            m_LastRes = PullData(input, max_bytes_to_read-available);
        }    
    } else if (!m_RingBuffer->GetAvailable()) {
        // if the buffer is now empty, return the input last error
        return m_LastRes;
    }
    
    // write as much as we can on the output stream
    res = PushData(*m_Output, count);

    if (bytes_read) *bytes_read = count;
    return res;
}


/*----------------------------------------------------------------------
|   PLT_PipeOutputStreamPump::PLT_PipeOutputStreamPump
+---------------------------------------------------------------------*/
PLT_PipeOutputStreamPump::PLT_PipeOutputStreamPump(NPT_InputStreamReference& input,
                                                   NPT_Size                  size /* 65535 */,
                                                   NPT_Size                  max_bytes_to_read /* = 0 */) : 
    PLT_StreamPump(size),
    m_Input(input),
    m_MaxBytesToRead(max_bytes_to_read),
    m_LastRes(NPT_SUCCESS)
{
}

/*----------------------------------------------------------------------
|   PLT_PipeOutputStreamPump::~PLT_PipeOutputStreamPump
+---------------------------------------------------------------------*/
PLT_PipeOutputStreamPump::~PLT_PipeOutputStreamPump()
{
}

/*----------------------------------------------------------------------
|   PLT_PipeOutputStreamPump::Transmit
+---------------------------------------------------------------------*/
NPT_Result 
PLT_PipeOutputStreamPump::Transmit(NPT_OutputStream& output)
{
    NPT_Size count;
    NPT_Result res;

    if ((m_LastRes == NPT_SUCCESS) || (m_LastRes == NPT_ERROR_WOULD_BLOCK)) {
        // fill the entire space by default
        NPT_Size max_space   = m_RingBuffer->GetSpace();
        if (max_space) {
            NPT_Size max_to_read = max_space;
            if (m_MaxBytesToRead != 0) {
                // if a total maximum amount was set, make sure we don't read more
                max_to_read = ((m_MaxBytesToRead - m_TotalBytesRead) < max_space) ? (m_MaxBytesToRead - m_TotalBytesRead) : max_space;
            }

            // any data to read
            if (max_to_read) {
                m_LastRes = PullData(*m_Input, max_to_read);   
            } else {
                m_LastRes = NPT_ERROR_EOS;
            }
        }    
    } else if (!m_RingBuffer->GetAvailable()) {
        // if the buffer is now empty, return the input last error
        return m_LastRes;
    }

    // write as much as we can on the output stream
    res = PushData(output, count);
    return res;
}

