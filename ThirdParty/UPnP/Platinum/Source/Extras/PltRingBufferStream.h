/*****************************************************************
|
|   Platinum - Ring buffer stream
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

#ifndef _PLT_RING_BUFFER_STREAM_H_
#define _PLT_RING_BUFFER_STREAM_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptStreams.h"
#include "NptRingBuffer.h"
#include "NptThreads.h"

/*----------------------------------------------------------------------
|   PLT_RingBufferStream class
+---------------------------------------------------------------------*/
class PLT_RingBufferStream : public NPT_DelegatingInputStream,   
                             public NPT_DelegatingOutputStream
{
public:
    PLT_RingBufferStream(NPT_Size buffer_size = 4096, bool blocking = true);
    PLT_RingBufferStream(NPT_RingBufferReference& buffer, bool blocking = true);
    virtual ~PLT_RingBufferStream();
    
    // methods
    bool IsAborted() { return m_Aborted; }
    
    // NPT_InputStream methods
    NPT_Result Read(void*     buffer, 
                    NPT_Size  bytes_to_read, 
                    NPT_Size* bytes_read = NULL);
    NPT_Result GetSize(NPT_LargeSize& size)  {
        NPT_COMPILER_UNUSED(size);
        return NPT_ERROR_NOT_SUPPORTED;
    }
    NPT_Result GetSpace(NPT_LargeSize& space) { 
        NPT_AutoLock autoLock(m_Lock);
        space = m_RingBuffer->GetSpace();
        return NPT_SUCCESS;
    }
    NPT_Result GetAvailable(NPT_LargeSize& available) { 
        NPT_AutoLock autoLock(m_Lock);
        available = m_RingBuffer->GetAvailable();
        return NPT_SUCCESS;
    }

    // NPT_OutputStream methods
    NPT_Result Write(const void* buffer, 
                     NPT_Size    bytes_to_write, 
                     NPT_Size*   bytes_written = NULL);
    NPT_Result Flush();
    NPT_Result SetEOS();
    NPT_Result Abort();

protected:
    // NPT_DelegatingInputStream methods
    NPT_Result InputSeek(NPT_Position offset) {
        NPT_COMPILER_UNUSED(offset);
        return NPT_ERROR_NOT_SUPPORTED;
    }
    NPT_Result InputTell(NPT_Position& offset) { 
        NPT_AutoLock autoLock(m_Lock);
        offset = m_TotalBytesRead; 
        return NPT_SUCCESS;
    }

    // NPT_DelegatingOutputStream methods
    NPT_Result OutputSeek(NPT_Position offset) {
        NPT_COMPILER_UNUSED(offset);
        return NPT_ERROR_NOT_SUPPORTED;
    }
    NPT_Result OutputTell(NPT_Position& offset) {
        NPT_AutoLock autoLock(m_Lock);
        offset = m_TotalBytesWritten; 
        return NPT_SUCCESS;
    }

private:
    NPT_RingBufferReference m_RingBuffer;
    NPT_Offset              m_TotalBytesRead;
    NPT_Offset              m_TotalBytesWritten;
    NPT_Mutex               m_Lock;
    bool                    m_Blocking;
    bool                    m_Eos;
    bool                    m_Aborted;
};

typedef NPT_Reference<PLT_RingBufferStream> PLT_RingBufferStreamReference;

#endif // _PLT_RING_BUFFER_STREAM_H_ 
